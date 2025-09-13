using UnityEngine;
public class CameraController : MonoBehaviour
{
    public Transform controlledCamera;
    public float maxViewAngle = 75;
    public float sensitivity = 50;

    private void Start()
    {
        controlledCamera.localRotation = Quaternion.identity;
    }

    void Update()
    {
        MouseLocker();
        if (Cursor.lockState != CursorLockMode.Locked) return;
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
}
