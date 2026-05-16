using UnityEngine;

public class TaskStation : MonoBehaviour
{
    [Header("Связь с дверью")]
    public AN_DoorScript targetDoor;

    [Header("Настройки Светяшки (Лампочки)")]
    [Tooltip("Перетащи сюда объект Light, который висит над дверью")]
    public Light doorLight;

    // Массив возможных цветов (Красный, Синий, Зеленый, Желтый, Пурпурный)
    public Color[] possibleColors = { Color.red, Color.blue, Color.green, Color.yellow, Color.magenta };

    [HideInInspector]
    public Color targetColor; // Тот самый цвет, который ищет скрипт проводов!

    [Header("UI Задания")]
    public GameObject taskUIPanel;
    public float interactionDistance = 3f;

    private bool isTaskActive = false;
    private bool isTaskCompleted = false;

    void Start()
    {
        // При старте игры выбираем случайный цвет из массива и красим лампочку
        if (possibleColors != null && possibleColors.Length > 0)
        {
            targetColor = possibleColors[UnityEngine.Random.Range(0, possibleColors.Length)];
        }

        if (doorLight != null)
        {
            doorLight.color = targetColor;
        }
    }

    void Update()
    {
        if (isTaskActive || isTaskCompleted) return;

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
        taskUIPanel.SetActive(true);

        player.enabled = false;
        MouseLook mouseLook = FindObjectOfType<MouseLook>();
        if (mouseLook != null) mouseLook.enabled = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    // Вызывается при правильном решении любой мини-игры
    public void CompleteTask()
    {
        isTaskCompleted = true;

        if (doorLight != null)
            doorLight.color = Color.green; // Меняем цвет лампочки на зеленый (успех)

        CloseTaskWindow();

        if (targetDoor != null) targetDoor.Action();
    }

    // Вызывается при ошибке (например, в проводах или тайм-ауте пин-кода)
    public void FailTask()
    {
        Debug.Log("Задание провалено! Окно закрывается.");
        CloseTaskWindow();
    }

    // Общая функция закрытия интерфейса и разморозки игрока
    private void CloseTaskWindow()
    {
        isTaskActive = false;
        taskUIPanel.SetActive(false);

        PlayerMovement player = FindObjectOfType<PlayerMovement>();
        if (player != null) player.enabled = true;

        MouseLook mouseLook = FindObjectOfType<MouseLook>();
        if (mouseLook != null) mouseLook.enabled = true;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}