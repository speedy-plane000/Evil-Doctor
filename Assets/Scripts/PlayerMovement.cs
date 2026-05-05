using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Crouch")]
    public float crouchHeight = 1f; 
    public float standHeight = 2f;  
    public float crouchSpeed = 2.5f; 
    private bool isCrouching;

    [Header("Movement")]
    public float walkSpeed = 5f;
    public float jumpHeight = 1.5f;
    public float gravity = -9.81f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundDistance = 0.4f;
    public LayerMask groundMask;

    [Header("Footstep Sounds")]
    public AudioClip concreteSound;
    public AudioClip grassSound;

    [Header("Head Bobbing")]
    public float bobFrequency = 5f;  
    public float bobAmount = 0.05f;   
    private float defaultYPos;        
    private float timer = 0;

    private CharacterController controller;
    private Vector3 velocity;
    private bool isGrounded;

    public AudioSource footstepAudio;
    public float stepDelay = 0.5f;

    private float stepTimer;
    public Transform mainCamera;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        defaultYPos = mainCamera.transform.localPosition.y;
    }

    void Update()
    {
        CheckGround();
        HandleCrouch();
        Move();
        Jump();
        ApplyGravity();
        HandleFootsteps();
        HandleHeadBob();
    }

    void CheckGround()
    {
        isGrounded = Physics.CheckSphere(
            groundCheck.position,
            groundDistance,
            groundMask
        );

        if (isGrounded && velocity.y < 0)
            velocity.y = -2f;
    }

    void Move()
    {
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");
        Vector3 move = transform.right * x + transform.forward * z;
        controller.Move(move * walkSpeed * Time.deltaTime);
    }

    void Jump()
    {
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
    }

    void ApplyGravity()
    {
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    void HandleFootsteps()
    {
        if (!isGrounded || isCrouching)
        {
            stepTimer = 0;
            return;
        }

        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        if (x == 0 && z == 0)
        {
            stepTimer = 0;
            return;
        }

        stepTimer -= Time.deltaTime;

        if (stepTimer <= 0)
        {
            PlaySurfaceSpecificSound();
            stepTimer = stepDelay;
        }
    }

    void PlaySurfaceSpecificSound()
    {
        RaycastHit hit;
        
        if (Physics.Raycast(transform.position, Vector3.down, out hit, 1.5f))
        {
            
            switch (hit.collider.tag)
            {
                case "Concrete":
                    footstepAudio.PlayOneShot(concreteSound);
                    break;
                case "grass":
                    footstepAudio.PlayOneShot(grassSound);
                    break;
                default:
                    footstepAudio.PlayOneShot(concreteSound);
                    break;
            }
        }
    }

    void HandleCrouch()
    {
        
        if (Input.GetKeyDown(KeyCode.LeftControl))
        {
            isCrouching = true;
            controller.height = crouchHeight;
            walkSpeed = crouchSpeed; 
        }

        
        if (Input.GetKeyUp(KeyCode.LeftControl))
        {
            isCrouching = false;
            controller.height = standHeight;
            walkSpeed = 5f; 
        }
    }

    void HandleHeadBob()
    {
        
        if (!isGrounded || (Input.GetAxis("Horizontal") == 0 && Input.GetAxis("Vertical") == 0))
        {
            timer = 0;
            Vector3 newPos = mainCamera.transform.localPosition;
            newPos.y = Mathf.Lerp(newPos.y, defaultYPos, Time.deltaTime * 5f);
            mainCamera.transform.localPosition = newPos;
            return;
        }

        
        timer += Time.deltaTime * (isCrouching ? bobFrequency * 0.5f : bobFrequency);

        Vector3 pos = mainCamera.transform.localPosition;
        
        pos.y = defaultYPos + Mathf.Sin(timer) * (isCrouching ? bobAmount * 0.5f : bobAmount);
        mainCamera.transform.localPosition = pos;
    }



}