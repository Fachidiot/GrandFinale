using UnityEngine;
using Cinemachine;
using System.Collections;
using DG.Tweening;

public class DungeonEntryAction : MonoBehaviour, INpcInteractable
{
    [Header("Interaction Settings")]
    [Tooltip("플레이어에게 보여질 상호작용 안내 문구입니다.")]
    [SerializeField] private string interactPrompt = "던전 입장 [F(상호작용키)]";
    [Tooltip("이동할 목적지(던전 내부 시작점)의 Transform입니다.")]
    [SerializeField] private Transform destinationPoint;

    [Header("Camera & Sequence")]
    [SerializeField] private CinemachineVirtualCamera entryCamera;
    [SerializeField] private int activePriority = 20;
    [SerializeField] private float sequenceDuration = 2.0f;

    [Header("Door Animation")]
    [SerializeField] private Transform leftDoor;
    [SerializeField] private Transform rightDoor;
    [SerializeField] private Vector3 leftDoorOpenRot = new Vector3(0, -90, 0);
    [SerializeField] private Vector3 rightDoorOpenRot = new Vector3(0, 90, 0);

    private PlayerInputs _detectedPlayerInputs;
    private int _originalCameraPriority;
    private bool _isSequenceActive = false;

    // 이벤트
    private void OnTriggerEnter(Collider other)
    {
        // 플레이어 감지 및 상호작용 UI 표시 (멀티플레이어 환경에서는 로컬 플레이어만 감지)
        if (_detectedPlayerInputs != null || _isSequenceActive) return;

        PlayerInputs inputs = other.GetComponent<PlayerInputs>();

        if (inputs != null && inputs.enabled /* && inputs.isLocalPlayer */)
        {
            _detectedPlayerInputs = inputs;
            UIEvents.FireInteractState(interactPrompt);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // 플레이어 감지 해제 및 상호작용 UI 숨김
        if (_detectedPlayerInputs != null && other.gameObject == _detectedPlayerInputs.gameObject)
        {
            if (!_isSequenceActive)
            {
                UIEvents.FireInteractState(null);
            }
            _detectedPlayerInputs = null;
        }
    }

    private void Update()
    {
        // 상호작용 입력 감지
        if (_detectedPlayerInputs != null && !_isSequenceActive && _detectedPlayerInputs.GetInteract())
        {
            OnInteract(_detectedPlayerInputs.gameObject);
        }
    }

    // 인터페이스 구현
    public string GetPrompt()
    {
        return interactPrompt;
    }

    public void OnInteract(GameObject interactor)
    {
        // 상호작용 시작 (네트워크 메시지 전송 필요)
        if (destinationPoint == null)
        {
            Debug.LogError("[DungeonEntryAction] 목적지(Destination Point)가 설정되지 않았습니다!");
            return;
        }

        UIEvents.FireInteractState(null);
        PlayerInputs inputsToDisable = _detectedPlayerInputs;
        _detectedPlayerInputs = null;

        StartCoroutine(ProcessEntryRoutine(interactor, inputsToDisable));
    }


    private IEnumerator ProcessEntryRoutine(GameObject player, PlayerInputs playerInputs)
    {
        // 던전 입장 연출 및 플레이어 이동
        _isSequenceActive = true;
        Debug.Log("[DungeonEntryAction] 던전 입장 시퀀스 시작");

        CharacterController cc = player.GetComponent<CharacterController>();

        // 0. 입력 비활성화 (로컬 플레이어만)
        if (playerInputs) playerInputs.enabled = false;

        // 1. 카메라 전환 (로컬 연출)
        if (entryCamera != null)
        {
            _originalCameraPriority = entryCamera.Priority;
            entryCamera.Priority = activePriority;
        }

        // 2. 문 열기 애니메이션 (네트워크 동기화 필요)
        if (leftDoor) leftDoor.DOLocalRotate(leftDoorOpenRot, 1.5f).SetEase(Ease.InOutQuad);
        if (rightDoor) rightDoor.DOLocalRotate(rightDoorOpenRot, 1.5f).SetEase(Ease.InOutQuad);

        // 3. 연출 대기
        yield return new WaitForSeconds(sequenceDuration);

        Debug.Log("[DungeonEntryAction] 플레이어 이동 처리");

        // 4. 플레이어 이동 (로컬 플레이어만, 네트워크 동기화 필요)
        if (player != null && destinationPoint != null)
        {
            if (cc) cc.enabled = false;
            player.transform.SetPositionAndRotation(destinationPoint.position, destinationPoint.rotation);
            // 이동 후 즉시 네트워크 동기화
            yield return null;
            if (cc) cc.enabled = true;
        }

        // 5. 마무리 (카메라 복구, 문 닫기, 입력 활성화)
        if (entryCamera != null)
        {
            yield return new WaitForSeconds(0.5f);
            entryCamera.Priority = _originalCameraPriority;
        }

        CloseDoors();

        if (playerInputs) playerInputs.enabled = true;

        _isSequenceActive = false;
        Debug.Log("[DungeonEntryAction] 던전 입장 시퀀스 종료");
    }

    private void CloseDoors()
    {
        // 문 닫기 애니메이션 (네트워크 동기화 필요)
        if (leftDoor) leftDoor.DOLocalRotate(Vector3.zero, 1.0f).SetEase(Ease.InOutQuad);
        if (rightDoor) rightDoor.DOLocalRotate(Vector3.zero, 1.0f).SetEase(Ease.InOutQuad);
    }
}