using UnityEngine;
using UnityEngine.InputSystem;

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
    float _pitch;          // вертикальный угол камеры

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
        Vector2 mouseDelta = Mouse.current.delta.ReadValue();
        float mx = mouseDelta.x * mouseSensitivity;
        float my = mouseDelta.y * mouseSensitivity;

        _pitch -= my;
        _pitch  = Mathf.Clamp(_pitch, -80f, 80f);

        cameraHolder.localEulerAngles = new Vector3(_pitch, 0f, 0f);
        transform.Rotate(Vector3.up, mx);
    }

    void Move()
    {
        if (_cc.isGrounded && _yVelocity < 0f)
            _yVelocity = -2f;

        var kb = Keyboard.current;
        float h = (kb.dKey.isPressed ? 1f : 0f) - (kb.aKey.isPressed ? 1f : 0f);
        float v = (kb.wKey.isPressed ? 1f : 0f) - (kb.sKey.isPressed ? 1f : 0f);

        Vector3 move = transform.right * h + transform.forward * v;
        move = Vector3.ClampMagnitude(move, 1f);

        _yVelocity += gravity * Time.deltaTime;

        Vector3 velocity = move * moveSpeed + Vector3.up * _yVelocity;
        _cc.Move(velocity * Time.deltaTime);
    }

    /// <summary>
    /// Мгновенная телепортация без физических артефактов CharacterController.
    /// Вызывается CheckpointManager.
    /// </summary>
    public void Teleport(Vector3 position)
    {
        _cc.enabled = false;
        transform.position = position;
        _cc.enabled = true;
        _yVelocity = 0f;
    }
}
