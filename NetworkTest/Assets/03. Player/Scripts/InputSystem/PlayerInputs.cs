using System;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerInputs : MonoBehaviour
{
    [SerializeField] private OptionKeyData keyData;
    [SerializeField] private float interactionDistance = 4f;
    [SerializeField] private LayerMask interactLayerMask;

    private float horizontalInput = 0f;
    private float verticalInput = 0f;
    private float bending = 0f;
    private bool isPaused = false;
    private Camera mainCamera;
    private Interactable lastInteractable;
    private bool isClimbing = false; // Add this variable
    private IAction isAction;

    // Movement
    public float GetAxisHorizontal()
    {
        return horizontalInput;
    }
    public float GetAxisVertical()
    {
        return verticalInput;
    }

    public float GetBending()
    {
        return bending;
    }

    public void SetClimbingState(bool climbing)
    {
        isClimbing = climbing;
        if (isClimbing)
        {
            // Reset horizontal and bending inputs immediately when climbing starts
            horizontalInput = 0f;
            bending = 0f;
        }
    }

    public void SetActionState(IAction action)
    {
        isAction = action;
        if (isAction.Value)
        {
            horizontalInput = 0f;
            bending = 0f;
        }
    }
    public IAction GetActionState()
    {
        return isAction;
    }

    private float CalculateAxis(float current, float raw)
    {
        float finalRaw = (raw != 0) ? raw : 0;

        return Mathf.MoveTowards(current, finalRaw, 10 * Time.deltaTime);
    }

    public bool GetMoveLeft()
    {
        return Input.GetKey(keyData.m_KeyMoveLeft);
    }
    public bool GetMoveRight()
    {
        return Input.GetKey(keyData.m_KeyMoveRight);
    }
    public bool GetMoveUp()
    {
        return Input.GetKey(keyData.m_KeyMoveUp);
    }
    public bool GetMoveDown()
    {
        return Input.GetKey(keyData.m_KeyMoveDown);
    }
    public bool GetJump()
    {
        return Input.GetKeyDown(keyData.m_KeyJump);
    }
    public bool GetSprint()
    {
        return Input.GetKey(keyData.m_KeySprint);
    }
    public bool GetCrouch()
    {
        return Input.GetKeyDown(keyData.m_KeyCrouch);
    }

    // Attack
    public bool GetAttack()
    {
        return Input.GetKey(KeyCode.Mouse0);
    }
    public bool GetAimed()
    {
        return Input.GetKey(KeyCode.Mouse1);
    }
    public bool GetReload()
    {
        return Input.GetKeyDown(keyData.m_KeyReload);
    }

    // Accessable
    public bool GetSlot0()
    {
        return Input.GetKeyDown(keyData.m_KeyUnArmed);
    }
    public bool GetSlot1()
    {
        return Input.GetKeyDown(keyData.m_KeySlot1);
    }
    public bool GetSlot2()
    {
        return Input.GetKeyDown(keyData.m_KeySlot2);
    }
    public bool GetSlot3()
    {
        return Input.GetKeyDown(keyData.m_KeySlot3);
    }
    public bool GetSlot4()
    {
        return Input.GetKeyDown(keyData.m_KeySlot4);
    }

    // Interact
    public bool GetInteract()
    {
        return Input.GetKeyDown(keyData.m_KeyInteract);
    }
    public bool GetInventory()
    {
        return Input.GetKeyDown(keyData.m_KeyInventory);
    }
    public bool GetFullInventory()
    {
        return Input.GetKeyDown(keyData.m_KeyInventory);
    }

    // UI
    public bool GetEscape()
    {
        return Input.GetKeyDown(KeyCode.Escape);
    }
    public bool GetChatOpen()
    {
        return Input.GetKeyDown(KeyCode.Slash);
    }

    void Start()
    {
        if (OptionDataManager.Instance)
            keyData = OptionDataManager.Instance.OptionData.m_keyData;
        GameManager.OnPauseStateChanged += OnPause; // Subscribe to pause event
        mainCamera = Camera.main;

        SetCursorState(false);
    }

    void OnPause(bool pause)
    {
        isPaused = pause;
    }

    void Update()
    {
        if (isPaused)
        {
            horizontalInput = 0f;
            verticalInput = 0f;
            bending = 0f;
            return;
        }

        if (null != PlayerManager.Instance.LocalPlayer && PlayerManager.Instance.LocalPlayer.gameObject)
        {
            HandleInteraction();
            CheckForInteractableUI();
        }

        // Axis Raw
        float horizontalRaw = 0f;
        float bendingRaw = 0f;

        float verticalRaw = Input.GetKey(keyData.m_KeyMoveDown) ? -1 : Input.GetKey(keyData.m_KeyMoveUp) ? 1 : 0;

        if (!isClimbing)
        {
            horizontalRaw = Input.GetKey(keyData.m_KeyMoveLeft) ? -1 : Input.GetKey(keyData.m_KeyMoveRight) ? 1 : 0;
            bendingRaw = Input.GetKey(keyData.m_BendingRight) ? -1 : Input.GetKey(keyData.m_BendingLeft) ? 1 : 0;
        }
        if (null != isAction && isAction.Value)
        {
            verticalRaw = 0;
            horizontalRaw = 0;
        }

        horizontalInput = CalculateAxis(horizontalInput, horizontalRaw);
        verticalInput = CalculateAxis(verticalInput, verticalRaw);
        bending = CalculateAxis(bending, bendingRaw);

        if (null == OptionDataManager.Instance)
            return;

        if (keyData != OptionDataManager.Instance.OptionData.m_keyData)
            keyData = OptionDataManager.Instance.OptionData.m_keyData;

    }

    private void HandleInteraction()
    {
        if (GetInteract())
        {
            TryInteract();
        }
    }

    private void TryInteract()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        int layerMask = 1 << LayerMask.NameToLayer(GameManager.Instance.GameSettings.interactableLayer);
        if (Physics.Raycast(ray, out RaycastHit hit, interactionDistance, layerMask))
        {
            if (hit.collider.TryGetComponent<Interactable>(out var interactable))
            {
                interactable.Interact(PlayerManager.Instance.LocalPlayer.gameObject);
            }
        }
    }

    private void CheckForInteractableUI()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            return;
        }

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        int layerMask = 1 << LayerMask.NameToLayer(GameManager.Instance.GameSettings.interactableLayer);

        if (Physics.Raycast(ray, out RaycastHit hit, interactionDistance, layerMask))
        {
            if (hit.collider.TryGetComponent<Interactable>(out var interactable))
            {
                if (interactable != lastInteractable)
                {
                    UIEvents.InteractableFocusChanged(interactable.interactionText);
                    lastInteractable = interactable;
                }
                return;
            }
        }

        // If we hit nothing or something not interactable
        if (lastInteractable != null)
        {
            UIEvents.InteractableFocusChanged("");
            lastInteractable = null;
        }
    }

    // 마우스 커서를 잠그고 숨깁니다. (게임 플레이 모드)
    public void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked; // 화면 중앙 고정
        Cursor.visible = false;                   // 커서 숨김
    }

    // 마우스 커서 잠금을 풀고 보이게 합니다. (UI/메뉴 모드)
    public void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;   // 자유롭게 이동 가능
        Cursor.visible = true;                    // 커서 보임
    }

    // 상태에 따라 커서를 제어하는 통합 함수
    public void SetCursorState(bool isLocked)
    {
        if (isLocked)
        {
            LockCursor();
        }
        else
        {
            UnlockCursor();
        }
    }
}

[Serializable]
public class OptionKeyData
{
    [Header("Movement")]
    public KeyCode m_KeyMoveLeft;
    public KeyCode m_KeyMoveRight;
    public KeyCode m_KeyMoveUp;
    public KeyCode m_KeyMoveDown;
    public KeyCode m_KeyJump;
    public KeyCode m_KeySprint;
    public KeyCode m_KeyCrouch;
    public KeyCode m_BendingRight;
    public KeyCode m_BendingLeft;

    [Header("Accessable")]
    public KeyCode m_KeyUnArmed;
    public KeyCode m_KeySlot1;
    public KeyCode m_KeySlot2;
    public KeyCode m_KeySlot3;
    public KeyCode m_KeySlot4;

    [Header("Interaction")]
    public KeyCode m_KeyInteract;
    public KeyCode m_KeyInventory;
    public KeyCode m_KeyReload;
}