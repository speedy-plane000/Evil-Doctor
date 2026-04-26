using UnityEngine;

/// <summary>
/// FPS-контроллер игрока: движение WASD + мышь для поворота камеры.
/// Требует CharacterController на том же GameObject.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Движение")]
    [SerializeField] float moveSpeed = 4f;
    [SerializeField] float gravity   = -20f;

    [Header("Камера")]
    [SerializeField] Transform cameraHolder;
    [SerializeField] float mouseSensitivity = 2f;

    CharacterController _cc;
    float _yVelocity;
    float _pitch;

    void Start()
    {
        _cc = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;
    }

    void Update()
    {
        Look();
        Move();
    }

    void Look()
    {
        float mx = Input.GetAxis("Mouse X") * mouseSensitivity;
        float my = Input.GetAxis("Mouse Y") * mouseSensitivity;

        _pitch -= my;
        _pitch  = Mathf.Clamp(_pitch, -80f, 80f);

        cameraHolder.localEulerAngles = new Vector3(_pitch, 0f, 0f);
        transform.Rotate(Vector3.up, mx);
    }

    void Move()
    {
        if (_cc.isGrounded && _yVelocity < 0f)
            _yVelocity = -2f;

        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        Vector3 move = transform.right * h + transform.forward * v;
        move = Vector3.ClampMagnitude(move, 1f);

        _yVelocity += gravity * Time.deltaTime;

        Vector3 velocity = move * moveSpeed + Vector3.up * _yVelocity;
        _cc.Move(velocity * Time.deltaTime);
    }
}
