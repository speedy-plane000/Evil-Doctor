using UnityEngine;
using UnityEngine.UI;

public enum TaskUiLogic
{
    Auto,
    Keypad,
    TerminalColors,
    WireTask,
    Maze
}

public class TaskStation : MonoBehaviour
{
    const string ColorTaskNamePattern = "color";
    static int activeTaskUiCount;
    public static bool IsAnyTaskUiActive => activeTaskUiCount > 0;

    [Header("Связь с дверью")]
    public AN_DoorScript targetDoor;

    [Header("Настройки Светяшки (Лампочки)")]
    [Tooltip("Перетащи сюда объект Light, который висит над дверью")]
    public Light doorLight;


    public Color[] possibleColors = { Color.red, Color.blue, Color.green, Color.yellow, Color.magenta };

    [HideInInspector]
    public Color targetColor; 

    [Header("UI Задания")]
    public GameObject taskUIPanel;
    [Tooltip("Auto: по имени объекта (если содержит 'color' — цветовая задача). Лучше явно выбрать режим для новых станций.")]
    public TaskUiLogic uiLogic = TaskUiLogic.Auto;
    public float interactionDistance = 3f;

    private bool isTaskActive = false;
    private bool isTaskCompleted = false;

    void Start()
    {
        if (possibleColors != null && possibleColors.Length > 0)
        {
            targetColor = possibleColors[UnityEngine.Random.Range(0, possibleColors.Length)];
        }

        if (doorLight != null)
        {
            doorLight.color = targetColor;
        }
    }

    void OnDisable()
    {
        if (isTaskActive)
        {
            isTaskActive = false;
            activeTaskUiCount = Mathf.Max(0, activeTaskUiCount - 1);
        }
    }

    void Update()
    {
        if (isTaskActive)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
                CloseTaskWindow();
            return;
        }

        if (isTaskCompleted) return;

        PlayerMovement player = FindObjectOfType<PlayerMovement>();
        if (player == null) return;

        float dist = Vector3.Distance(transform.position, player.transform.position);

        if (Input.GetKeyDown(KeyCode.E) && dist <= interactionDistance)
        {
            StartTask(player);
        }
    }

    void StartTask(PlayerMovement player)
    {
        isTaskActive = true;
        activeTaskUiCount++;
        PrepareTaskUi();
        taskUIPanel.SetActive(true);

        player.enabled = false;
        MouseLook mouseLook = FindObjectOfType<MouseLook>();
        if (mouseLook != null) mouseLook.enabled = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void PrepareTaskUi()
    {
    if (taskUIPanel == null)
        return;

    KeypadTaskLogic keypadLogic = taskUIPanel.GetComponentInChildren<KeypadTaskLogic>(true);
    TerminalColorsTaskLogic colorsLogic = taskUIPanel.GetComponentInChildren<TerminalColorsTaskLogic>(true);
    WireTaskLogic wireLogic = taskUIPanel.GetComponentInChildren<WireTaskLogic>(true);
    MazeTaskLogic mazeLogic = taskUIPanel.GetComponentInChildren<MazeTaskLogic>(true);
        


    if (keypadLogic != null) keypadLogic.enabled = false;
    if (colorsLogic != null) colorsLogic.enabled = false;
    if (wireLogic != null) wireLogic.enabled = false;
    if (mazeLogic != null) mazeLogic.enabled = false;


        switch (uiLogic)
    {
            case TaskUiLogic.Maze:
                if (mazeLogic != null)
                {
                    mazeLogic.myStation = this;
                    mazeLogic.enabled = true;
                }
                break;

            case TaskUiLogic.WireTask:
            if (wireLogic != null)
            {
                wireLogic.myStation = this;
                wireLogic.enabled = true;
            }
            break;
            
        case TaskUiLogic.Keypad:
            if (keypadLogic != null)
            {
                keypadLogic.myStation = this;
                keypadLogic.enabled = true;
            }
            break;
            
        case TaskUiLogic.TerminalColors:
            if (colorsLogic == null)
                colorsLogic = EnsureTerminalColorsLogic(keypadLogic);

            if (colorsLogic != null)
            {
                CopySharedTaskUi(keypadLogic, colorsLogic);
                colorsLogic.myStation = this;
                colorsLogic.enabled = true;
            }
            break;
        
        case TaskUiLogic.Auto:
        default:
            
            bool useColorsTask = UsesColorTask();
            if (useColorsTask)
            {
                if (colorsLogic == null)
                    colorsLogic = EnsureTerminalColorsLogic(keypadLogic);

                if (colorsLogic != null)
                {
                    CopySharedTaskUi(keypadLogic, colorsLogic);
                    colorsLogic.myStation = this;
                    colorsLogic.enabled = true;
                }
                if (keypadLogic != null) keypadLogic.enabled = false;
            }
            else
            {
                if (keypadLogic != null)
                {
                    keypadLogic.myStation = this;
                    keypadLogic.enabled = true;
                }
                if (colorsLogic != null) colorsLogic.enabled = false;
            }
            break;
        }
    }

    TerminalColorsTaskLogic EnsureTerminalColorsLogic(KeypadTaskLogic keypadLogic)
    {
        if (keypadLogic == null)
            return null;

        TerminalColorsTaskLogic existingLogic = keypadLogic.GetComponent<TerminalColorsTaskLogic>();
        if (existingLogic != null)
            return existingLogic;

        return keypadLogic.gameObject.AddComponent<TerminalColorsTaskLogic>();
    }

    bool UsesColorTask()
    {
        if (uiLogic == TaskUiLogic.TerminalColors)
            return true;

        if (uiLogic == TaskUiLogic.Keypad)
            return false;

        string lowerName = gameObject.name.ToLowerInvariant();
        return lowerName.Contains(ColorTaskNamePattern);
    }

    void CopySharedTaskUi(KeypadTaskLogic source, TerminalColorsTaskLogic target)
    {
        if (target == null || source == null)
            return;

        target.targetCodeText = source.targetCodeText;
        target.inputCodeText = source.inputCodeText;
        target.timerText = source.timerText;
        target.timerSlider = source.timerSlider;
        target.timeLimit = source.timeLimit;
        target.buttonsRoot = source.buttonsRoot;

        if (target.buttonsRoot == null)
        {
            GridLayoutGroup grid = source.GetComponentInChildren<GridLayoutGroup>(true);
            if (grid != null)
                target.buttonsRoot = grid.transform;
        }
    }

    public void CompleteTask()
    {
        isTaskCompleted = true;

        if (doorLight != null)
            doorLight.color = Color.green; 

        CloseTaskWindow();

        if (targetDoor != null) targetDoor.Action();
    }

    public void FailTask()
    {
        Debug.Log("Задание провалено! Окно закрывается.");
        CloseTaskWindow();
    }

    private void CloseTaskWindow()
    {
        if (isTaskActive)
        {
            isTaskActive = false;
            activeTaskUiCount = Mathf.Max(0, activeTaskUiCount - 1);
        }
        taskUIPanel.SetActive(false);

        PlayerMovement player = FindObjectOfType<PlayerMovement>();
        if (player != null) player.enabled = true;

        MouseLook mouseLook = FindObjectOfType<MouseLook>();
        if (mouseLook != null) mouseLook.enabled = true;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}