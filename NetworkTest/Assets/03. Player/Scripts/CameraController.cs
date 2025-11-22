using UnityEngine;
public class CameraController : MonoBehaviour
{
    public Transform controlledCamera;
    public float maxViewAngle = 75;
    public float sensitivity = 50;

    private bool isPause = false;
    private float inputDisabledTimer = 0f;

    private void Start()
    {
        controlledCamera.localRotation = Quaternion.identity;
        GameManager.OnPauseStateChanged += OnPause;
    }

    void Update()
    {
        if (isPause)
            return;

        if (inputDisabledTimer > 0)
        {
            inputDisabledTimer -= Time.deltaTime;
            return;
        }

        MouseLocker();
        if (Cursor.lockState != CursorLockMode.Locked)
            return;
        SetCameraRotation(Input.GetAxis("Mouse Y") * -sensitivity, Input.GetAxis("Mouse X") * sensitivity);
    }

    public void SetCameraRotation(float vertical, float horizontal)
    {
        var previewVertical = controlledCamera.eulerAngles.x;

        var horizontalView = controlledCamera.up * horizontal;
        var verticalView = controlledCamera.right * vertical;

        controlledCamera.Rotate((horizontalView + verticalView) * Time.deltaTime, Space.World);

        controlledCamera.rotation = Quaternion.Euler(controlledCamera.eulerAngles.x, controlledCamera.eulerAngles.y, 0);

        if (Vector3.Angle(Vector3.up, controlledCamera.forward) > 90 + maxViewAngle |
            Vector3.Angle(Vector3.up, controlledCamera.forward) < 90 - maxViewAngle)
        {
            controlledCamera.rotation = Quaternion.Euler(previewVertical, controlledCamera.rotation.eulerAngles.y, controlledCamera.rotation.z);
        }
    }

    public void RotateCamera180()
    {
        // 1. 현재 카메라의 월드 오일러 각도를 가져옵니다.
        Vector3 currentEulerAngles = controlledCamera.eulerAngles;

        // 2. Y축 회전(Yaw)에 180도를 더합니다.
        float newYaw = currentEulerAngles.y + 180f;

        // 3. 새로운 오일러 각을 만듭니다. X(Pitch)와 Z(Roll)는 그대로 유지합니다.
        Vector3 newEulerAngles = new Vector3(currentEulerAngles.x, newYaw, currentEulerAngles.z);

        // 4. 새로운 회전값을 즉시 적용합니다.
        controlledCamera.rotation = Quaternion.Euler(newEulerAngles);

        // 5. 짧은 시간 동안 마우스 입력을 비활성화하여 회전이 즉시 풀리는 것을 방지합니다.
        inputDisabledTimer = 0.1f;
    }

    void MouseLocker()
    {
        // mouse lock
        if (Input.GetMouseButtonDown(0))
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        // mouse unlock
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    void OnPause(bool pause)
    {
        if (pause)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        isPause = pause;
    }
}
