using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 모든 다리(IKStepper)의 움직임 순서를 관리하고 지휘하는 '뇌' 역할을 하는 스크립트입니다.
/// 각 다리가 스스로 움직이는 대신, 이 매니저가 어떤 다리가 언제 스텝을 떼어야 할지 결정하고 명령을 내립니다.
/// </summary>
[DefaultExecutionOrder(+1)] // 모든 IKChain이 IK 계산을 마친 후에 이 스텝 로직이 호출되도록 합니다.
public class IKStepManager : MonoBehaviour
{
    public bool printDebugLogs;

    public Spider spider;

    public enum StepMode { AlternatingTetrapodGait, QueueWait, QueueNoWait }
    /*
     * 스텝 모드에 대한 참고 사항:
     * * AlternatingTetrapodGait (교대 4족 보행): 실제 거미의 걸음걸이에서 영감을 받은 모드입니다.
     * 다리들을 A와 B, 두 그룹으로 나눕니다.
     * 타이머가 "stepTime" 간격으로 이 그룹들을 교대합니다.
     * 각 그룹은 정해진 간격 내에서 스텝이 허용되는 특정 프레임만 가집니다.
     * 이를 통해, 같은 그룹의 다리들은 필요할 때 항상 동시에 움직이고, 다른 그룹이 움직이는 동안에는 절대 움직이지 않습니다.
     * 'dynamic step time'이 선택되면, 각 다리의 동적 스텝 시간의 평균값이 사용됩니다.
     * 이 모드는 각 다리에 지정된 비동기(asynchronicity) 설정을 사용하지 않습니다. 그룹에 의해 이미 비동기성이 보장되기 때문입니다.
     * * QueueWait (큐 대기): 스텝을 떼고 싶어하는 다리들을 큐(queue)에 저장하고, 큐의 순서대로 스텝을 수행합니다.
     * 이 모드는 항상 큐의 다음 다리를 우선시하며, 해당 다리가 스텝을 뗄 수 있을 때까지 기다립니다.
     * 하지만 이 대기 시간이 너무 길어지면 다른 다리들의 스텝을 방해할 수 있습니다.
     * 위 모드와 달리, 이 모드는 각 다리에 정의된 비동기 설정을 사용하여 다리가 스텝을 뗄 수 있는지 결정합니다.
     * * QueueNoWait (큐 비대기): 위의 'QueueWait' 모드와 유사하지만, 큐의 다음 다리를 기다리지 않는다는 점이 다릅니다.
     * 다리들은 여전히 큐 순서대로 순회하지만, 만약 어떤 다리가 스텝을 뗄 수 없는 상태라면, 순회를 계속하여 다음 다리들이 스텝을 뗄 수 있는지 확인하고 수행합니다.
     * 더 구체적으로, 이것은 일반적인 큐가 아니라 '스텝이 필요한 다리 목록'에 가깝습니다.
     * 목록을 순서대로 순회하며 k번째 다리가 스텝을 뗄 수 있으면, 스텝을 실행하고 목록에서 k번째 요소를 제거합니다.
     */

    [Header("스텝 모드")]
    public StepMode stepMode;

    // 큐 모드에서는 이 리스트의 순서가 stepCheck 우선순위가 됩니다.
    [Header("큐 모드용 다리 목록")]
    public List<IKStepper> ikSteppers;
    private List<IKStepper> stepQueue;
    private Dictionary<int, bool> waitingForStep;

    [Header("보행 패턴(Gait) 모드용 다리 목록")]
    public List<IKStepper> gaitGroupA;
    public List<IKStepper> gaitGroupB;
    private List<IKStepper> currentGaitGroup;
    private float nextSwitchTime;

    [Header("스텝 시간")]
    public bool dynamicStepTime = true; // 동적 스텝 시간 사용 여부
    public float stepTimePerVelocity; // 속도에 따른 스텝 시간 계수
    [Range(0, 1.0f)]
    public float maxStepTime; // 최대 스텝 시간

    public enum GaitStepForcing { NoForcing, ForceIfOneLegSteps, ForceAlways }
    [Header("디버그")]
    public GaitStepForcing gaitStepForcing;

    /// <summary>
    /// 컴포넌트와 변수들을 초기화합니다.
    /// </summary>
    private void Awake()
    {

        /* 큐 모드 초기화 */

        stepQueue = new List<IKStepper>();

        // 비활성화된 IKStepper들을 목록에서 제거합니다.
        int k = 0;
        foreach (var ikStepper in ikSteppers.ToArray())
        {
            if (!ikStepper.allowedTargetManipulationAccess()) ikSteppers.RemoveAt(k);
            else k++;
        }

        // 스텝 대기 상태를 저장할 해시맵을 false로 초기화합니다.
        waitingForStep = new Dictionary<int, bool>();
        foreach (var ikStepper in ikSteppers)
        {
            waitingForStep.Add(ikStepper.GetInstanceID(), false);
        }

        /* 교대 4족 보행 모드 초기화 */

        // 그룹들에서 비활성화된 IKStepper들을 제거합니다.
        k = 0;
        foreach (var ikStepper in gaitGroupA.ToArray())
        {
            if (!ikStepper.allowedTargetManipulationAccess()) gaitGroupA.RemoveAt(k);
            else k++;
        }
        k = 0;
        foreach (var ikStepper in gaitGroupB.ToArray())
        {
            if (!ikStepper.allowedTargetManipulationAccess()) gaitGroupB.RemoveAt(k);
            else k++;
        }

        // A 그룹부터 시작하고, 다음 전환 시간은 최대 스텝 시간으로 설정합니다.
        currentGaitGroup = gaitGroupA;
        nextSwitchTime = maxStepTime;
    }

    /// <summary>
    /// 매 프레임 후반에 호출되어, 선택된 스텝 모드에 맞는 로직을 실행합니다.
    /// </summary>
    private void LateUpdate()
    {
        if (stepMode == StepMode.AlternatingTetrapodGait) AlternatingTetrapodGait();
        else QueueStepMode();
    }

    /// <summary>
    /// 'QueueWait' 또는 'QueueNoWait' 모드의 로직을 처리합니다.
    /// 스텝이 필요한 다리를 큐에 넣고, 큐를 순회하며 스텝을 실행시킵니다.
    /// </summary>
    private void QueueStepMode()
    {

        /* 아직 스텝 대기열에 없는 모든 다리에 대해 스텝이 필요한지 확인합니다.
         * 스텝이 필요하다면, 큐에 추가합니다.
         */
        foreach (var ikStepper in ikSteppers)
        {

            // 다리가 이미 스텝 대기 중인지 확인합니다.
            if (waitingForStep[ikStepper.GetInstanceID()] == true) continue;

            // 스텝이 필요한지 확인하고, 필요하다면 큐에 추가합니다.
            if (ikStepper.stepCheck())
            {
                stepQueue.Add(ikStepper);
                waitingForStep[ikStepper.GetInstanceID()] = true;
                if (printDebugLogs) Debug.Log(ikStepper.name + " 가 큐 " + stepQueue.Count + "번째 위치에 추가됨");
            }
        }

        if (printDebugLogs) printQueue();

        /* 스텝 큐를 순서대로 순회하며 다리들이 스텝을 뗄 자격이 있는지 확인합니다.
         * 스텝을 뗄 수 있다면, 스텝을 실행시킵니다.
         * 그렇지 않다면 두 가지 경우가 있습니다:
         * - 'QueueWait' 모드라면, 순회를 중단합니다.
         * - 'QueueNoWait' 모드라면, 순회를 계속합니다.
         */
        int k = 0;
        foreach (var ikStepper in stepQueue.ToArray())
        {
            if (ikStepper.allowedToStep())
            {
                ikStepper.getIKChain().unpauseSolving();
                ikStepper.step(calculateStepTime(ikStepper));
                // 스텝을 뗀 다리를 목록에서 제거합니다.
                waitingForStep[ikStepper.GetInstanceID()] = false;
                stepQueue.RemoveAt(k);
                if (printDebugLogs) Debug.Log(ikStepper.name + " 가 스텝을 허용받아 큐에서 제거됨.");
            }
            else
            {
                if (printDebugLogs) Debug.Log(ikStepper.name + " 는 스텝을 허용받지 못함.");

                // 'QueueWait' 모드가 선택되었다면 여기서 순회를 멈춥니다.
                if (stepMode == StepMode.QueueWait)
                {
                    if (printDebugLogs) Debug.Log("대기 모드이므로 이번 프레임의 스텝 처리를 종료합니다.");
                    break;
                }
                k++; // 현재 요소를 목록에서 제거하지 않았으므로, 인덱스 k를 1 증가시킵니다.
            }
        }

        /* 큐에 여전히 남아있는 (스텝을 허용받지 못한) 모든 다리들을 순회합니다.
         * 이 다리들은 기다리는 동안 IK 계산을 일시 중지시킵니다.
         */
        foreach (var ikStepper in stepQueue)
        {
            ikStepper.getIKChain().pauseSolving();
        }
    }

    /// <summary>
    /// 'AlternatingTetrapodGait' (교대 4족 보행) 모드의 로직을 처리합니다.
    /// 정해진 시간마다 다리 그룹(A/B)을 교대하며 스텝을 실행시킵니다.
    /// </summary>
    private void AlternatingTetrapodGait()
    {

        // 다음 그룹 전환 시간이 아직 안 됐으면 아무것도 하지 않습니다.
        if (Time.time < nextSwitchTime) return;

        /* 전환 시간이 되었으므로, 그룹을 바꾸고 새로운 전환 시간을 설정합니다.
         * 동적 스텝 시간의 경우, 모든 다리가 동시에 스텝을 완료해야 다음 그룹으로 전환할 수 있으므로,
         * 각 다리별 스텝 시간을 사용하는 것은 의미가 없습니다.
         * 따라서, 현재 그룹의 평균 스텝 시간을 계산하여 모든 다리에 동일하게 사용합니다.
         * TODO: 자연스러움을 위해 각 다리의 스텝 시간에 랜덤 오프셋을 추가하고, 그 중 최대값을 다음 전환 시간으로 사용하는 것을 고려해볼 수 있습니다.
         */
        currentGaitGroup = (currentGaitGroup == gaitGroupA) ? gaitGroupB : gaitGroupA;
        float stepTime = calculateAverageStepTime(currentGaitGroup);
        nextSwitchTime = Time.time + stepTime;

        if (printDebugLogs)
        {
            string text = ((currentGaitGroup == gaitGroupA) ? "그룹: A" : "그룹: B") + " 스텝 시간: " + stepTime;
            Debug.Log(text);
        }

        /* 이제 현재 보행 그룹의 스텝을 수행합니다.
         * 그룹 내의 다리는 스텝이 필요할 때만 스텝을 뗍니다.
         * 하지만 디버그 목적으로, 강제 모드가 선택된 경우 다른 다리들도 강제로 스텝을 떼게 할 수 있습니다.
         */
        if (gaitStepForcing == GaitStepForcing.ForceAlways)
        {
            foreach (var ikStepper in currentGaitGroup) ikStepper.step(stepTime);
        }
        else if (gaitStepForcing == GaitStepForcing.ForceIfOneLegSteps)
        {
            bool b = false;
            foreach (var ikStepper in currentGaitGroup)
            {
                b = b || ikStepper.stepCheck();
                if (b == true) break;
            }
            if (b == true) foreach (var ikStepper in currentGaitGroup) ikStepper.step(stepTime);
        }
        else
        {
            foreach (var ikStepper in currentGaitGroup)
            {
                if (ikStepper.stepCheck()) ikStepper.step(stepTime);
            }
        }
    }

    /// <summary>
    /// 특정 다리의 동적 스텝 시간을 계산합니다.
    /// 다리 끝(End Effector)의 속도에 반비례하여 스텝 시간을 조절합니다.
    /// </summary>
    private float calculateStepTime(IKStepper ikStepper)
    {
        if (dynamicStepTime)
        {
            float k = stepTimePerVelocity * spider.getScale(); // 속도가 1일 때, 이 값이 스텝 시간이 됩니다.
            float velocityMagnitude = ikStepper.getIKChain().getEndeffectorVelocityPerSecond().magnitude;
            return (velocityMagnitude == 0) ? maxStepTime : Mathf.Clamp(k / velocityMagnitude, 0, maxStepTime);
        }
        else return maxStepTime;
    }

    /// <summary>
    /// 지정된 다리 그룹의 평균 동적 스텝 시간을 계산합니다.
    /// </summary>
    private float calculateAverageStepTime(List<IKStepper> ikSteppers)
    {
        if (dynamicStepTime)
        {
            float stepTime = 0;
            foreach (var ikStepper in ikSteppers)
            {
                stepTime += calculateStepTime(ikStepper);
            }
            return stepTime / ikSteppers.Count;
        }
        else return maxStepTime;
    }

    /// <summary>
    /// 디버깅을 위해 현재 스텝 큐에 있는 다리 목록을 콘솔에 출력합니다.
    /// </summary>
    private void printQueue()
    {
        if (stepQueue == null) return;
        string queueText = "[";
        if (stepQueue.Count != 0)
        {
            foreach (var ikStepper in stepQueue)
            {
                queueText += ikStepper.name + ", ";
            }
            queueText = queueText.Substring(0, queueText.Length - 2);
        }
        queueText += "]";
        Debug.Log("큐: " + queueText);
    }
}