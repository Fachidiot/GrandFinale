using UnityEngine;

public class PlayerInputs : MonoBehaviour
{
    [SerializeField] private OptionKeyData keyData;

    private float horizontalInput = 0f;
    private float verticalInput = 0f;

    // Movement
    public float GetAxisHorizontal()
    {
        return horizontalInput;
    }
    public float GetAxisVertical()
    {
        return verticalInput;
    }

    private float CalculateAxis(float current, float raw)
    {
        float finalRaw = (raw != 0) ? raw : 0;

        return Mathf.MoveTowards(current, finalRaw, 50 * Time.deltaTime);
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
        return Input.GetKey(keyData.m_KeyJump);
    }
    public bool GetSprint()
    {
        return Input.GetKey(keyData.m_KeySprint);
    }
    public bool GetCrouch()
    {
        return Input.GetKey(keyData.m_KeyCrouch);
    }

    // Attack
    public bool GetAttack()
    {
        return Input.GetKey(keyData.m_Attack);
    }

    public bool GetAimed()
    {
        return Input.GetKey(keyData.m_Aimed);
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
    public bool GetEscape()
    {
        return Input.GetKeyDown(keyData.m_KeyEscape);
    }

    void Start()
    {
        if (OptionDataManager.Instance)
            keyData = OptionDataManager.Instance.OptionData.m_keyData;
    }

    void Update()
    {
        float horizontalRaw = Input.GetKey(keyData.m_KeyMoveLeft) ? -1 : Input.GetKey(keyData.m_KeyMoveRight) ? 1 : 0;
        float verticalRaw = Input.GetKey(keyData.m_KeyMoveDown) ? -1 : Input.GetKey(keyData.m_KeyMoveUp) ? 1 : 0;

        horizontalInput = CalculateAxis(horizontalInput, horizontalRaw);
        verticalInput = CalculateAxis(verticalInput, verticalRaw);

        if (null == OptionDataManager.Instance)
            return;

        if (keyData != OptionDataManager.Instance.OptionData.m_keyData)
            keyData = OptionDataManager.Instance.OptionData.m_keyData;
    }
}
