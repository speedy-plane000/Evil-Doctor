using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class KeypadTaskLogic : MonoBehaviour
{
    [Header("Связь с 3D миром")]
    public TaskStation myStation;

    [Header("UI Элементы (Текст)")]
    public TextMeshProUGUI targetCodeText;
    public TextMeshProUGUI inputCodeText;
    public TextMeshProUGUI timerText;

    [Header("UI Элементы (Визуальный таймер)")]
    [Tooltip("Опционально: слайдер, который будет уменьшаться")]
    public Slider timerSlider;

    [Header("UI Кнопки")]
    [Tooltip("Если не задано, будет найдена первая сетка кнопок внутри панели")]
    public Transform buttonsRoot;

    [Header("Настройки сложности")]
    public float timeLimit = 5f;

    static readonly int[] KeypadDigits = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 0 };

    string requiredCode;
    string currentInput = "";
    float timeRemaining;
    bool isGameActive = false;

    void OnEnable()
    {
        SetupKeypadButtons();
        GenerateNewCode();
    }

    void Update()
    {
        if (!isGameActive)
            return;

        timeRemaining -= Time.deltaTime;

        if (timerText != null)
        {
            timerText.text = "TIME: " + Mathf.Max(0, timeRemaining).ToString("F2") + "s";
            timerText.color = timeRemaining <= 2f ? Color.red : Color.white;
        }

        if (timerSlider != null)
        {
            timerSlider.value = timeRemaining;
        }

        if (timeRemaining <= 0f)
        {
            TimeOut();
        }
    }

    void SetupKeypadButtons()
    {
        Transform root = ResolveButtonsRoot();
        if (root == null)
            return;

        GridLayoutGroup grid = root.GetComponent<GridLayoutGroup>();
        if (grid != null)
        {
            grid.constraint = GridLayoutGroup.Constraint.Flexible;
            grid.cellSize = new Vector2(120f, 180f);
            grid.spacing = new Vector2(20f, 20f);
        }

        List<Button> buttons = new List<Button>();
        for (int i = 0; i < root.childCount; i++)
        {
            Button button = root.GetChild(i).GetComponent<Button>();
            if (button != null)
                buttons.Add(button);
        }

        for (int i = 0; i < buttons.Count; i++)
        {
            Button button = buttons[i];
            bool isDigitButton = i < KeypadDigits.Length;
            button.gameObject.SetActive(isDigitButton);
            if (!isDigitButton)
                continue;

            int digit = KeypadDigits[i];
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => ButtonPress(digit));

            Image image = button.targetGraphic as Image;
            if (image == null)
                image = button.GetComponent<Image>();
            if (image != null)
                image.color = Color.white;

            TextMeshProUGUI[] labels = button.GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (TextMeshProUGUI label in labels)
            {
                label.text = digit.ToString();
            }
        }
    }

    Transform ResolveButtonsRoot()
    {
        if (buttonsRoot != null)
            return buttonsRoot;

        GridLayoutGroup grid = GetComponentInChildren<GridLayoutGroup>(true);
        if (grid != null)
            return grid.transform;

        return transform;
    }

    void GenerateNewCode()
    {
        CancelInvoke();
        currentInput = "";

        if (inputCodeText != null)
        {
            inputCodeText.text = "";
            inputCodeText.color = Color.white;
        }

        timeRemaining = timeLimit;
        if (timerSlider != null)
        {
            timerSlider.maxValue = timeLimit;
            timerSlider.value = timeLimit;
        }

        requiredCode = Random.Range(1000, 10000).ToString();

        if (targetCodeText != null)
        {
            targetCodeText.text = "CODE: " + requiredCode;
            targetCodeText.color = Color.yellow;
        }

        isGameActive = true;
    }

    public void ButtonPress(int number)
    {
        if (!isGameActive || currentInput.Length >= 4)
            return;

        currentInput += number.ToString();

        if (inputCodeText != null)
            inputCodeText.text = currentInput;

        if (currentInput.Length == 4)
        {
            CheckCode();
        }
    }

    void CheckCode()
    {
        isGameActive = false;

        if (currentInput == requiredCode)
        {
            if (inputCodeText != null)
            {
                inputCodeText.text = "ACCEPTED";
                inputCodeText.color = Color.green;
            }

            Invoke(nameof(FinishGame), 0.8f);
        }
        else
        {
            if (inputCodeText != null)
            {
                inputCodeText.text = "ERROR";
                inputCodeText.color = Color.red;
            }

            Invoke(nameof(ResetInput), 0.6f);
        }
    }

    void TimeOut()
    {
        isGameActive = false;

        if (inputCodeText != null)
        {
            inputCodeText.text = "TIME OUT";
            inputCodeText.color = Color.red;
        }

        Invoke(nameof(CloseWindowWithFail), 1f);
    }

    void ResetInput()
    {
        if (timeRemaining > 0f)
        {
            currentInput = "";
            if (inputCodeText != null)
            {
                inputCodeText.text = "";
                inputCodeText.color = Color.white;
            }

            isGameActive = true;
        }
        else
        {
            TimeOut();
        }
    }

    void CloseWindowWithFail()
    {
        if (myStation == null)
        {
            PlayerMovement player = FindObjectOfType<PlayerMovement>();
            if (player != null)
                player.enabled = true;

            MouseLook mouseLook = FindObjectOfType<MouseLook>();
            if (mouseLook != null)
                mouseLook.enabled = true;

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            return;
        }

        myStation.FailTask();
    }

    void FinishGame()
    {
        if (myStation != null)
        {
            myStation.CompleteTask();
        }
    }
}