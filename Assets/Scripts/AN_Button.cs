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

    [Header("Квест")]
    public bool countsAsLever = false;
    public enum LeverFloor { Floor2, Floor1 }
    public LeverFloor leverFloor;

    public bool WasAlreadyPressed => alreadyPressed;

    private Animator anim;
    private bool alreadyPressed = false;
    private PlayerMovement cachedPlayer;

    void Start()
    {
        anim = GetComponent<Animator>();
        cachedPlayer = FindObjectOfType<PlayerMovement>();
    }

    void Update()
    {
        if (cachedPlayer == null) return;
        float dist = Vector3.Distance(transform.position, cachedPlayer.transform.position);
        if (Input.GetKeyDown(KeyCode.E) && dist <= interactionDistance)
            OnPress();
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

                if (countsAsLever)
                {
                    if (leverFloor == LeverFloor.Floor2) QuestData.RegisterFloor2Lever();
                    else QuestData.RegisterFloor1Lever();
                }
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