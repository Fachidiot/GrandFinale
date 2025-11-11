/* * 이 파일은 github.com/PhilS94의 Unity-Procedural-IK-Wall-Walking-Spider 프로젝트의 일부입니다.
 * Copyright (C) 2020 Philipp Schofield - All Rights Reserved
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Raycasting;

/// <summary>
/// 각 다리의 '한 걸음'을 관리하는 스크립트입니다.
/// IKChain과 연동하여, 다리가 언제(when) 그리고 어디로(where) 발을 뻗어야 할지 결정하고 실행합니다.
/// 실제 스텝 실행은 IKStepManager가 이 스크립트의 함수들을 호출하여 관리합니다.
/// </summary>
[RequireComponent(typeof(IKChain))]
public class IKStepper : MonoBehaviour
{
       [Tooltip("이 다리가 속한 거미의 메인 Spider 스크립트")]
        public Spider spider;

        [Header("디버그")]
        [Tooltip("씬(Scene) 뷰에 디버그 정보 표시 여부")]
        public bool showDebug;
        [Tooltip("콘솔에 디버그 로그 출력 여부")]
        public bool printDebugLogs;
        [Tooltip("스텝을 뗄 때마다 에디터를 일시정지할지 여부")]
        public bool pauseOnStep = false;
        [Tooltip("디버그 기즈모 아이콘 크기")]
        [Range(1, 10.0f)]
        public float debugIconScale;

        [Header("스텝 레이어")]
        [Tooltip("발을 디딜 수 있는 표면의 레이어")]
        public LayerMask stepLayer;

        [Header("다리 동기화")]
        [Tooltip("이 다리가 움직일 때 함께 움직이면 안 되는 다른 다리들")]
        public IKStepper[] asyncChain;

        [Header("스텝 타이밍")]
        [Tooltip("한 번 스텝을 뗀 후 다음 스텝까지의 최소 대기 시간")]
        [Range(0.0f, 5.0f)]
        public float stepCooldown = 0.0f;
        private float timeSinceLastStep;
        [Tooltip("거미가 멈춘 후 몇 초 뒤에 다리 움직임을 완전히 멈출지")]
        [Range(0.0f, 2.0f)]
        public float stopSteppingAfterSecondsStill;

        [Header("스텝 전환 (궤적)")]
        [Tooltip("발을 들어 올리는 높이")]
        [Range(0.0f, 10.0f)]
        public float stepHeight;
        [Tooltip("발을 들어 올리는 움직임의 속도와 높이를 제어하는 커브")]
        public AnimationCurve stepAnimation;

        [Header("기본 위치")]
        [Tooltip("몸체로부터 다리가 얼마나 멀리 떨어져 있는지 (-1 ~ 1)")]
        [Range(-1.0f, 1.0f)]
        public float defaultOffsetLength;
        [Tooltip("몸체로부터 다리가 얼마나 높은 곳에 있는지 (-1 ~ 1)")]
        [Range(-1.0f, 1.0f)]
        public float defaultOffsetHeight;
        [Tooltip("몸체로부터 다리가 얼마나 앞/뒤에 있는지 (보폭, -1 ~ 1)")]
        [Range(-1.0f, 1.0f)]
        public float defaultOffsetStride;

        [Header("기본 위치 오버슛 배율")]
        [Tooltip("다음 발 위치를 계산할 때 기본 위치보다 얼마나 더 멀리 뻗을지 (1.0 이상)")]
        [Range(1.0f, 2.0f)]
        public float defaultOvershootMultiplier = 1.5f;

        [Header("최후의 IK 목표 위치")]
        [Tooltip("발 디딜 곳을 못 찾았을 때 사용할 최후의 목표 높이 (0 ~ 1)")]
        [Range(0f, 1.0f)]
        public float lastResortHeight;

        [Header("레이 캐스팅")]
        [Tooltip("RayCast(선)를 쓸지 SphereCast(구체)를 쓸지")]
        public CastMode castMode;
        [Tooltip("SphereCast를 사용할 경우의 구체 반지름")]
        public float radius;

        [Header("전방 레이")]
        [Tooltip("몸통 기준 레이 시작 높이 (0 ~ 1)")]
        [Range(0f, 1f)]
        public float rayFrontalHeight;
        [Tooltip("레이 길이 비율 (0 ~ 1, 다리 길이 기준)")]
        [Range(0f, 1f)]
        public float rayFrontalLength;
        [Tooltip("레이 시작 위치 오프셋 (0 ~ 1)")]
        [Range(0f, 1f)]
        public float rayFrontalOriginOffset;

        [Header("바깥쪽 레이")]
        [Tooltip("레이가 향하는 몸통 위쪽 기준점")]
        public Vector3 rayTopFocalPoint;
        [Tooltip("레이 시작 위치 오프셋 (0 ~ 1)")]
        [Range(0f, 1f)]
        public float rayOutwardsOriginOffset;
        [Tooltip("레이 끝 위치 오프셋 (0 ~ 1)")]
        [Range(0f, 1f)]
        public float rayOutwardsEndOffset;

        [Header("하단 레이")]
        [Tooltip("발 위치 기준 레이 시작 높이")]
        [Range(0f, 6f)]
        public float downRayHeight;
        [Tooltip("발 위치 기준 아래로 탐색할 최대 깊이")]
        [Range(0f, 6f)]
        public float downRayDepth;

        [Header("안쪽 레이")]
        [Tooltip("레이가 향하는 몸통 아래쪽 기준점")]
        public Vector3 rayBottomFocalPoint;
        [Tooltip("레이 끝 위치 오프셋 (0 ~ 1)")]
        [Range(0f, 1f)]
        public float rayInwardsEndOffset;


        private IKChain ikChain;

        private bool isStepping = false;
        private float timeStandingStill;

        private float minDistance;

        private Dictionary<string, Cast> casts;
        RaycastHit hitInfo;

        private JointHinge rootJoint;
        private float chainLength;
        private Vector3 defaultPositionLocal;
        private Vector3 lastResortPositionLocal;
        private Vector3 frontalStartPositionLocal;
        private Vector3 prediction;

        // 디버그용 변수들
        private Vector3 lastEndEffectorPos;
        private Vector3 projPrediction;
        private Vector3 overshootPrediction;
        private Vector3 minOrient;
        private Vector3 maxOrient;
        private string lastHitRay;

        /// <summary>
        /// 컴포넌트와 변수들을 초기화합니다.
        /// </summary>
        private void Awake()
    {
        ikChain = GetComponent<IKChain>();

        rootJoint = ikChain.getRootJoint();
        timeSinceLastStep = 2 * stepCooldown; // 시작하자마자 바로 스텝을 뗄 수 있도록 초기화
        timeStandingStill = 0f;

        // 체인 길이를 미리 계산해두고 나중에 계속 사용합니다.
        chainLength = ikChain.calculateChainLength();

        // 루트 관절과 발 끝 사이의 최소 거리를 설정합니다. 이 거리보다 가까워지면 강제로 스텝을 떼게 됩니다.
        minDistance = 0.2f * chainLength;

        // 기본 위치를 설정합니다.
        defaultPositionLocal = calculateDefault();
        lastResortPositionLocal = defaultPositionLocal + new Vector3(0, lastResortHeight * 4f * spider.getNonScaledColliderRadius(), 0);
        frontalStartPositionLocal = new Vector3(0, rayFrontalHeight * 4f * spider.getNonScaledColliderRadius(), 0);

        // 예측 위치를 초기화합니다.
        prediction = getDefault();

        // 디버그 변수들이 0점에서 그려지지 않도록 초기화합니다.
        lastEndEffectorPos = prediction;
        projPrediction = prediction;
        overshootPrediction = prediction;

        // RayCast 또는 SphereCast를 초기화합니다.
        casts = new Dictionary<string, Cast>();
        updateCasts();

        // IKChain의 시작 목표 지점을 설정합니다.
        ikChain.setTarget(getDefaultTarget());
    }

    /// <summary>
    /// Stride, Length, Height 파라미터를 사용해 다리의 '이상적인 기본 위치'를 계산합니다.
    /// 이 위치는 새로운 스텝 지점을 계산할 때 중요한 기준점이 됩니다.
    /// </summary>
    private Vector3 calculateDefault()
    {
        float diameter = chainLength - minDistance;
        Vector3 rootRotAxis = rootJoint.getRotationAxis();

        // transform.up과 rootJoint.getRotationAxis() 사용에 주의해야 합니다.
        // 현재 제 경우에는 오른쪽 다리가 반전된 것을 제외하면 동일하지만, 일반적으로는 다를 수 있습니다.
        Vector3 normal = spider.transform.up;

        Vector3 toEnd = ikChain.getEndEffector().position - rootJoint.getRotationPoint();
        toEnd = Vector3.ProjectOnPlane(toEnd, normal).normalized;

        Vector3 pivot = spider.getColliderBottomPoint() + Vector3.ProjectOnPlane(rootJoint.getRotationPoint() - spider.transform.position, normal);

        Vector3 midOrient = Quaternion.AngleAxis(0.5f * (rootJoint.maxAngle + rootJoint.minAngle), rootRotAxis) * toEnd;

        // 관절 가동 범위(DOF Arc) 디버그를 위해 변수들을 설정합니다.
        minOrient = spider.transform.InverseTransformDirection(Quaternion.AngleAxis(rootJoint.minAngle, rootRotAxis) * toEnd);
        maxOrient = spider.transform.InverseTransformDirection(Quaternion.AngleAxis(rootJoint.maxAngle, rootRotAxis) * toEnd);

        // 이제 stride, length, height 파라미터를 사용해 기본 위치를 설정합니다.
        Vector3 defOrientation = Quaternion.AngleAxis(defaultOffsetStride * 0.5f * rootJoint.getAngleRange(), rootRotAxis) * midOrient;
        Vector3 def = pivot;
        def += (minDistance + 0.5f * (1f + defaultOffsetLength) * diameter) * defOrientation;
        def += defaultOffsetHeight * 2f * spider.getColliderRadius() * rootRotAxis;
        return spider.transform.InverseTransformPoint(def);
    }

    /// <summary>
    /// 다음 발 디딜 곳을 찾기 위한 RayCast/SphereCast들을 정의하고 Dictionary에 저장합니다.
    /// Dictionary에 추가되는 순서가 곧 레이를 쏘는 우선순위가 되므로, 신중하게 순서를 정해야 합니다.
    /// </summary>
    private void updateCasts()
    {

        Vector3 defaultPos = getDefault();
        Vector3 normal = spider.transform.up;

        // 전방 레이 파라미터
        Vector3 frontal = getFrontalStartPosition();
        Vector3 frontalPredictionEnd = frontal + Vector3.ProjectOnPlane(prediction - frontal, spider.transform.up).normalized * rayFrontalLength * chainLength;
        Vector3 frontalDefaultEnd = frontal + Vector3.ProjectOnPlane(defaultPos - frontal, spider.transform.up).normalized * rayFrontalLength * chainLength;
        Vector3 frontalPredictionOrigin = Vector3.Lerp(frontal, frontalPredictionEnd, rayFrontalOriginOffset);
        Vector3 frontalDefaultOrigin = Vector3.Lerp(frontal, frontalDefaultEnd, rayFrontalOriginOffset);

        // 바깥쪽 레이 파라미터
        Vector3 top = getTopFocalPoint();
        Vector3 topPredictionEnd = top + 2 * (prediction - top);
        Vector3 topDefaultEnd = top + 2 * (defaultPos - top);

        Vector3 outwardsPredictionOrigin = Vector3.Lerp(top, prediction, rayOutwardsOriginOffset);
        Vector3 outwardsPredictionEnd = Vector3.Lerp(prediction, topPredictionEnd, rayOutwardsEndOffset);
        Vector3 outwardsDefaultOrigin = Vector3.Lerp(top, defaultPos, rayOutwardsOriginOffset);
        Vector3 outwardsDefaultEnd = Vector3.Lerp(prediction, topDefaultEnd, rayOutwardsEndOffset);

        // 하단 레이 파라미터
        float height = downRayHeight * spider.getColliderRadius();
        float depth = downRayDepth * spider.getColliderRadius();
        Vector3 downwardsPredictionOrigin = prediction + normal * height;
        Vector3 downwardsPredictionEnd = prediction - normal * depth;
        Vector3 downwardsDefaultOrigin = defaultPos + normal * height;
        Vector3 downwardsDefaultEnd = defaultPos - normal * depth;

        // 안쪽 레이 파라미터
        Vector3 bottom = getBottomFocalPoint();
        Vector3 bottomBorder = spider.transform.position - 1.5f * spider.getColliderRadius() * normal;
        Vector3 bottomMid = spider.transform.position - 4f * spider.getColliderRadius() * normal;

        float inwardsPredictionLength = rayInwardsEndOffset * Vector3.Distance(bottom, prediction);
        float inwardsDefaultLength = rayInwardsEndOffset * Vector3.Distance(bottom, defaultPos);

        Vector3 inwardsPredictionEndClose = bottomBorder;
        Vector3 inwardsPredictionEndMid = bottomMid;
        Vector3 inwardsPredictionEndFar = Vector3.Lerp(prediction, bottom, inwardsPredictionLength / Vector3.Distance(prediction, bottom));

        Vector3 inwardsDefaultEndClose = bottomBorder;
        Vector3 inwardsDefaultEndMid = bottomMid;
        Vector3 inwardsDefaultEndFar = Vector3.Lerp(defaultPos, bottom, inwardsDefaultLength / Vector3.Distance(prediction, bottom));

        // 스케일이 적용된 반지름
        float r = spider.getScale() * radius;

        // 'Prediction Out' 레이는 평평한 표면이나 구멍에서는 목표 지점을 찾을 수 없습니다.
        // 왜냐하면 예측 지점에서 멈추는데, 이 예측 지점은 콜라이더가 끝나는 기본 높이에 있기 때문입니다.
        casts.Clear();
        casts = new Dictionary<string, Cast> {
            { "Prediction Frontal", getCast(frontalPredictionOrigin, frontalPredictionEnd, r) },
            { "Prediction Out", getCast(outwardsPredictionOrigin,outwardsPredictionEnd, r) },
            { "Prediction Down", getCast(downwardsPredictionOrigin,downwardsPredictionEnd, r) },
            { "Prediction In Far", getCast(prediction,inwardsPredictionEndFar,r) },
            { "Prediction In Mid", getCast(prediction, inwardsPredictionEndMid, r) },
            { "Prediction In Close", getCast(prediction, inwardsPredictionEndClose, r) },

            { "Default Frontal", getCast(frontalDefaultOrigin, frontalDefaultEnd, r) },
            { "Default Out", getCast(outwardsDefaultOrigin,outwardsDefaultEnd, r) },
            { "Default Down", getCast(downwardsDefaultOrigin,downwardsDefaultEnd, r) },
            { "Default In Far", getCast(defaultPos,inwardsDefaultEndFar, r) },
            { "Default In Mid", getCast(defaultPos, inwardsDefaultEndMid, r) },
            { "Default In Close", getCast(defaultPos, inwardsDefaultEndClose, r) },
        };
    }

    /// <summary>
    /// 선택된 castMode에 따라 RayCast 또는 SphereCast 객체를 생성하여 반환합니다.
    /// </summary>
    private Cast getCast(Vector3 start, Vector3 end, float radius, Transform parentStart = null, Transform parentEnd = null)
    {
        if (castMode == CastMode.RayCast) return new RayCast(start, end, parentStart, parentEnd);
        else return new SphereCast(start, end, radius, parentStart, parentEnd);
    }

    /// <summary>
    /// 매 프레임 시간을 갱신하고, 디버그 정보를 그립니다.
    /// </summary>
    private void Update()
    {
        timeSinceLastStep += Time.deltaTime;
        if (!spider.getIsMoving()) timeStandingStill += Time.deltaTime;
        else timeStandingStill = 0f;

#if UNITY_EDITOR
        if (showDebug && UnityEditor.Selection.Contains(transform.gameObject)) drawDebug();
#endif
    }

    /// <summary>
    /// 이 다리가 지금 스텝을 떼어야 하는지 여부를 확인합니다.
    /// 주로 IK 솔버의 오차(error) 값이 지정된 허용치(tolerance)보다 큰지를 기준으로 판단합니다.
    /// </summary>
    public bool stepCheck()
    {
        // 만약 현재 스텝을 진행 중이라면, 다른 행동을 할 필요가 없습니다.
        if (isStepping) return false;

        // 일정 시간 이상 멈춰 있었다면 더 이상 스텝을 허용하지 않습니다. (제자리에서 계속 발을 구르는 현상 방지)
        if (timeStandingStill > stopSteppingAfterSecondsStill)
        {
            return false;
        }

        // 현재 목표 지점이 공중에 있다면 스텝을 뗍니다.
        if (!ikChain.getTarget().grounded) return true;

        // IK 솔버의 오차가 허용치보다 커지면 스텝을 뗍니다. 이것이 스텝 여부를 결정하는 주된 방식입니다.
        else if (ikChain.getError() > ikChain.getTolerance()) return true;

        // 대안으로, 루트 관절에 너무 가까워져도 스텝을 뗍니다.
        else if (Vector3.Distance(rootJoint.getRotationPoint(), ikChain.getTarget().position) < minDistance) return true;

        return false;
    }

    /// <summary>
    /// 이 다리가 스텝을 떼는 것이 '허용되는지' 여부를 확인합니다.
    /// (스텝 쿨타임, 다른 다리와의 동기화 문제 등을 체크)
    /// </summary>
    public bool allowedToStep()
    {
        if (isStepping) return false;

        if (!ikChain.getTarget().grounded) return true;

        if (timeSinceLastStep < stepCooldown) return false;

        foreach (var chain in asyncChain)
        {
            if (chain.getIsStepping())
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// 스텝을 실행하는 코루틴을 호출하는 외부용 함수입니다.
    /// </summary>
    public void step(float stepTime)
    {
        StopAllCoroutines(); // 이미 진행중인 코루틴을 그냥 멈춰도 괜찮은지 확인해야 합니다.
        StartCoroutine(Step(stepTime));
    }

    /// <summary>
    /// 실제 스텝 동작을 수행하는 코루틴입니다.
    /// 정해진 시간(stepTime) 동안, 발을 들어올려 새로운 목표 지점까지 호(arc)를 그리며 이동시킵니다.
    /// </summary>
    private IEnumerator Step(float stepTime)
    {
        if (pauseOnStep) Debug.Break();

        if (printDebugLogs) Debug.Log(gameObject.name + " 가 스텝을 시작합니다.");

        // 목표 위치를 계산합니다.
        Vector3 desiredPosition = calculateDesiredPosition();
        overshootPrediction = desiredPosition; // 디버그를 위해 이 값을 저장합니다.

        // 발 끝의 현재 속도를 얻어와 목표 위치를 보정합니다. (거미가 스텝 중에 움직일 것을 대비)
        // 이 새로운 값을 예측 위치로 설정합니다.
        Vector3 endEffectorVelocity = ikChain.getEndeffectorVelocityPerSecond();
        prediction = desiredPosition + endEffectorVelocity * stepTime;

        // 마지막으로, 계산된 예측 위치를 바탕으로 레이캐스팅을 통해 실제 표면 위의 목표 지점을 찾습니다.
        TargetInfo newTarget = findTargetOnSurface();

        // 이전 목표 지점이나 새로운 목표 지점 둘 중 하나라도 땅에 닿아있을 때만 스텝을 실행합니다.
        // 그렇지 않으면 공중에 뜬 다리가 아치를 그리며 움직이는 것을 방지할 수 있습니다.
        if (ikChain.getTarget().grounded || newTarget.grounded)
        {
            isStepping = true;
            TargetInfo lastTarget = ikChain.getTarget();
            TargetInfo lerpTarget;
            float time = Time.deltaTime;

            while (time < stepTime)
            {
                lerpTarget.position = Vector3.Lerp(lastTarget.position, newTarget.position, time / stepTime) + stepHeight * 0.01f * spider.getScale() * stepAnimation.Evaluate(time / stepTime) * spider.transform.up;
                lerpTarget.normal = Vector3.Lerp(lastTarget.normal, newTarget.normal, time / stepTime);
                lerpTarget.grounded = false;

                time += Time.deltaTime;
                ikChain.setTarget(lerpTarget);
                yield return null;
            }
            isStepping = false;
            timeSinceLastStep = 0.0f;
        }

        ikChain.setTarget(newTarget);
        if (printDebugLogs) Debug.Log(gameObject.name + " 가 스텝을 완료했습니다.");
    }

    /// <summary>
    /// 다리가 다음 스텝에 뻗고자 하는 '이상적인' 위치를 계산합니다.
    /// 현재 발 끝 위치에서 기본 위치까지 선을 긋고, 그 선을 overshootMultiplier 만큼 연장하여 계산합니다.
    /// </summary>
    private Vector3 calculateDesiredPosition()
    {
        Vector3 endeffectorPosition = ikChain.getEndEffector().position;
        Vector3 defaultPosition = getDefault();
        Vector3 normal = spider.transform.up;

        // 옵션 1: 거미의 움직임을 예측 과정에 포함 (prediction += SpiderMoveVector * stepTime)
        //      문제점: 스텝 중에 거미가 멈추면 과하게 예측하게 됨.
        //              스텝 중에 거미가 방향을 바꾸면 범위를 벗어날 수 있음.
        //      해결책: stepTime을 짧게 유지하여 큰 변화가 없도록 함.

        // 옵션 2: 스텝 코루틴 안에서 예측 위치를 동적으로 업데이트.
        //      문제점: 스텝이 끝난 뒤에야 발이 표면에 닿는지 알 수 있음.
        //              즉, 발이 공중이나 장애물에 닿을 수 있으며, 그 이후의 처리를 고민해야 함.
        //              (마지막 프레임에 위치를 업데이트하거나, 다른 스텝 코루틴을 시작하거나?)

        // 현재는 옵션 1을 선택합니다.

        // 발 끝 위치를 기본 위치의 높이와 맞춥니다.
        Vector3 start = Vector3.ProjectOnPlane(endeffectorPosition, normal);
        start = spider.transform.InverseTransformPoint(start);
        start.y = defaultPositionLocal.y;
        start = spider.transform.TransformPoint(start);

        // 디버그용 값
        projPrediction = start;
        lastEndEffectorPos = endeffectorPosition;

        // 속도 예측으로 오버슛(더 멀리 뻗기)을 적용합니다.
        return start + (defaultPosition - start) * defaultOvershootMultiplier;
    }

    /// <summary>
    /// 유효한 표면 위에서 새로운 목표 지점을 찾으려 시도합니다.
    /// 'prediction' 파라미터를 사용해 주변 지형을 스캔하는 레이캐스트들을 구성합니다.
    /// 목표를 찾으면 해당 정보를 반환하고, 못 찾으면 최후의 목표 지점을 반환합니다.
    /// </summary>
    private TargetInfo findTargetOnSurface()
    {

        LayerMask layer = stepLayer;

        // 만약 도달 가능한 거리에 콜라이더가 없다면, 표면을 찾을 필요 없이 기본값을 반환합니다.
        // 이는 거미가 공중에 떠 있을 때의 연산 비용을 줄여줍니다.
        if (Physics.OverlapSphere(rootJoint.getRotationPoint(), chainLength, layer, QueryTriggerInteraction.Ignore) == null)
        {
            return getLastResortTarget();
        }

        // 새로운 예측 지점에 맞춰 캐스트들을 업데이트합니다. 더 똑똑하게 할 수 있을까?
        updateCasts();

        // 이제 캐스트들을 사용해 광선을 쏘아 실제 표면 위의 지점을 찾습니다.
        foreach (var cast in casts)
        {

            // 만약 거미가 레이의 시작점을 볼 수 없다면 쏠 필요가 없습니다. (장애물에 가려진 경우)
            if (new RayCast(spider.transform.position, cast.Value.getOrigin()).castRay(out hitInfo, layer)) continue;

            if (cast.Value.castRay(out hitInfo, layer))
            {

                // 전방 레이의 경우, 너무 가파른 경사는 허용하지 않습니다. (+-65도)
                if (cast.Key == "Frontal" && Vector3.Angle(cast.Value.getDirection(), hitInfo.normal) < 180f - 65f) continue;

                if (printDebugLogs) Debug.Log("'" + cast.Key + "' 캐스트에서 목표 지점을 찾음");
                lastHitRay = cast.Key;
                return new TargetInfo(hitInfo.point, hitInfo.normal);

            }
        }

        // 레이가 목표 지점을 찾지 못했으므로 기본 위치를 반환합니다.
        if (printDebugLogs) Debug.Log("어떤 레이도 목표 지점을 찾지 못했습니다. 기본 위치를 반환합니다.");
        return getLastResortTarget();
    }

    // --- Getters (정보 반환 함수들) ---
    public IKChain getIKChain()
    {
        return ikChain;
    }

    private bool getIsStepping()
    {
        return isStepping;
    }

    public bool allowedTargetManipulationAccess()
    {
        return ikChain.isTargetExternallyHandled();
    }

    private Vector3 getDefault()
    {
        return spider.transform.TransformPoint(defaultPositionLocal);
    }
    public TargetInfo getDefaultTarget()
    {
        return new TargetInfo(getDefault(), spider.transform.up);
    }
    private Vector3 getLastResort()
    {
        return spider.transform.TransformPoint(lastResortPositionLocal);
    }
    private TargetInfo getLastResortTarget()
    {
        return new TargetInfo(getLastResort(), spider.transform.up, false);
    }
    private Vector3 getTopFocalPoint()
    {
        return spider.transform.TransformPoint(rayTopFocalPoint);
    }
    private Vector3 getBottomFocalPoint()
    {
        return spider.transform.TransformPoint(rayBottomFocalPoint);
    }
    private Vector3 getFrontalStartPosition()
    {
        return spider.transform.TransformPoint(frontalStartPositionLocal);
    }

    /// <summary>
    /// 기즈모(Gizmos)를 사용해 디버그 정보를 시각적으로 그립니다.
    /// </summary>
    private void drawDebug(bool points = true, bool steppingProcess = true, bool rayCasts = true, bool DOFArc = true)
    {

        float scale = spider.getScale() * 0.0001f * debugIconScale;
        if (points)
        {
            // 기본 위치
            DebugShapes.DrawPoint(getDefault(), Color.magenta, scale);

            // 최후의 목표 위치
            DebugShapes.DrawPoint(getLastResortTarget().position, Color.cyan, scale);

            // 위/아래 레이 시작점
            DebugShapes.DrawPoint(getTopFocalPoint(), Color.green, scale);
            DebugShapes.DrawPoint(getBottomFocalPoint(), Color.green, scale);

            // 현재 목표 지점
            if (isStepping) DebugShapes.DrawPoint(ikChain.getTarget().position, Color.cyan, scale, 0.2f);
            else DebugShapes.DrawPoint(ikChain.getTarget().position, Color.cyan, scale);
        }

        if (steppingProcess)
        {
            // 예측 과정을 그립니다.
            DebugShapes.DrawPoint(lastEndEffectorPos, Color.white, scale);
            DebugShapes.DrawPoint(projPrediction, Color.grey, scale);
            DebugShapes.DrawPoint(overshootPrediction, Color.green, scale);
            DebugShapes.DrawPoint(prediction, Color.yellow, scale);
            Debug.DrawLine(lastEndEffectorPos, projPrediction, Color.white);
            Debug.DrawLine(projPrediction, overshootPrediction, Color.grey);
            Debug.DrawLine(overshootPrediction, prediction, Color.green);
        }

        if (rayCasts)
        {
            // 레이캐스트들을 그립니다.
            Color col = Color.black;
            foreach (var cast in casts)
            {
                if (cast.Key.Contains("Default")) col = Color.magenta;
                else if (cast.Key.Contains("Prediction")) col = Color.yellow;

                if (cast.Key != lastHitRay) col = Color.Lerp(col, Color.white, 0.5f);
                cast.Value.draw(col);
            }
        }

        if (DOFArc)
        {
            // 관절 가동 범위를 그립니다.
            Vector3 v = spider.transform.TransformDirection(minOrient);
            Vector3 w = spider.transform.TransformDirection(maxOrient);
            Vector3 p = spider.transform.InverseTransformPoint(rootJoint.getRotationPoint());
            p.y = defaultPositionLocal.y;
            p = spider.transform.TransformPoint(p);
            DebugShapes.DrawCircleSection(p, v, w, rootJoint.getRotationAxis(), minDistance, chainLength, Color.red);
        }
    }


#if UNITY_EDITOR
    /// <summary>
    /// 에디터에서 선택했을 때 기즈모를 그립니다. (실행 중이 아닐 때도 표시)
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        if (UnityEditor.EditorApplication.isPlaying) return;
        if (!UnityEditor.Selection.Contains(transform.gameObject)) return;
        if (!showDebug) return;

        // 포인터를 설정하기 위해 Awake()를 실행합니다.
        Awake();

        drawDebug(true, false, true, true);
    }
#endif
}