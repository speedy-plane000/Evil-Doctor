using UnityEngine;

public class MouseLook : MonoBehaviour
{
    public float mouseSensitivity = 100f;
    [Range(1f, 30f)]
    public float smoothing = 10f;
    public Transform playerBody;
    public float eyeHeight = 1.6f;

    private float xRotation = 0f;
    private float currentXRotation = 0f;
    private float currentYRotation = 0f;
    private float targetYRotation = 0f;

    void Start()
    {
        currentYRotation = playerBody.eulerAngles.y;
        targetYRotation = currentYRotation;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        targetYRotation += mouseX;

        float t = 1f - Mathf.Exp(-smoothing * Time.deltaTime);
        currentXRotation = Mathf.Lerp(currentXRotation, xRotation, t);
        currentYRotation = Mathf.Lerp(currentYRotation, targetYRotation, t);

        transform.localRotation = Quaternion.Euler(currentXRotation, 0f, 0f);
        playerBody.rotation = Quaternion.Euler(0f, currentYRotation, 0f);
    }
}