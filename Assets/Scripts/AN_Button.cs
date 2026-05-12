using UnityEngine;

public class AN_Button : MonoBehaviour
{
    [Header("Связь с менеджером")]
    public FloorManager floorManager;

    [Header("Настройки")]
    public bool isLever = true;
    public AN_DoorScript DoorObject;
    public bool isOpened = false;
    public float interactionDistance = 3f;

    private Animator anim;
    private bool alreadyPressed = false;

    void Start()
    {
        anim = GetComponent<Animator>();
    }

    void Update()
    {
        // Ищем игрока поблизости (используем твой скрипт PlayerMovement как маркер)
        PlayerMovement player = FindObjectOfType<PlayerMovement>();
        if (player == null) return;

        float dist = Vector3.Distance(transform.position, player.transform.position);

        if (Input.GetKeyDown(KeyCode.E) && dist <= interactionDistance)
        {
            OnPress();
        }
    }

    void OnPress()
    {
        if (floorManager != null)
        {
            if (!alreadyPressed)
            {
                alreadyPressed = true;
                floorManager.RegisterLeverPress();
                PlayAnimation();
            }
        }
        else if (DoorObject != null)
        {
            DoorObject.Action();
            PlayAnimation();
        }
    }

    void PlayAnimation()
    {
        isOpened = !isOpened;
        if (anim != null)
        {
            if (isLever) anim.SetBool("LeverUp", isOpened);
            else anim.SetTrigger("ButtonPress");
        }
    }
}