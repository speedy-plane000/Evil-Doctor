using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TerminalColorsTaskLogic : MonoBehaviour
{
    [Header("Связь с 3D миром")]
    public TaskStation myStation;

    [Header("UI Элементы (Текст)")]
    public TextMeshProUGUI targetCodeText;
    public TextMeshProUGUI inputCodeText;
    public TextMeshProUGUI timerText;

    [Header("UI Элементы (Визуальный таймер)")]
    public Slider timerSlider;

    [Header("UI Кнопки")]
    [Tooltip("Если не задано, будет найдена первая сетка кнопок внутри панели")]
    public Transform buttonsRoot;

    [Header("Настройки сложности")]
    public float timeLimit = 5f;

    static readonly Color[] RequiredSequenceColors =
    {
        Color.green,
        new Color(0.62f, 0.25f, 1f, 1f),
        new Color(1f, 0.55f, 0f, 1f),
        Color.blue
    };

    static readonly string[] RequiredSequenceNames =
    {
        "ЗЕЛЁНЫЙ",
        "ФИОЛЕТОВЫЙ",
        "ОРАНЖЕВЫЙ",
        "СИНИЙ"
    };

    const string SequencePromptPrefix = "Нажми: ";
    const string SequenceSeparator = " → ";

    float timeRemaining;
    bool isGameActive;
    int currentStep;

    void OnEnable()
    {
        SetupColorButtons();
        StartRound();
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

    void SetupColorButtons()
    {
        Transform root = ResolveButtonsRoot();
        if (root == null)
            return;

        GridLayoutGroup grid = root.GetComponent<GridLayoutGroup>();
        if (grid != null)
        {
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 2;
            grid.cellSize = new Vector2(170f, 170f);
            grid.spacing = new Vector2(24f, 24f);
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
            bool shouldBeVisible = i < RequiredSequenceColors.Length;
            button.gameObject.SetActive(shouldBeVisible);
            if (!shouldBeVisible)
                continue;

            int colorIndex = i;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => ButtonPress(colorIndex));

            RectTransform rectTransform = button.transform as RectTransform;
            if (rectTransform != null)
                rectTransform.sizeDelta = new Vector2(170f, 170f);

            Image image = button.targetGraphic as Image;
            if (image == null)
                image = button.GetComponent<Image>();
            if (image != null)
                image.color = RequiredSequenceColors[colorIndex];

            TextMeshProUGUI[] labels = button.GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (TextMeshProUGUI label in labels)
            {
                label.text = string.Empty;
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

    void StartRound()
    {
        CancelInvoke();
        currentStep = 0;
        isGameActive = true;

        if (inputCodeText != null)
        {
            inputCodeText.text = GetStepText(1);
            inputCodeText.color = Color.white;
        }

        timeRemaining = timeLimit;
        if (timerSlider != null)
        {
            timerSlider.maxValue = timeLimit;
            timerSlider.value = timeLimit;
        }

        if (targetCodeText != null)
        {
            targetCodeText.text = SequencePromptPrefix + string.Join(SequenceSeparator, RequiredSequenceNames);
            targetCodeText.color = Color.yellow;
        }
    }

    public void ButtonPress(int colorIndex)
    {
        if (!isGameActive)
            return;

        if (colorIndex == currentStep)
        {
            currentStep++;

            if (currentStep >= RequiredSequenceColors.Length)
            {
                isGameActive = false;
                if (inputCodeText != null)
                {
                    inputCodeText.text = "ДОСТУП РАЗРЕШЁН";
                    inputCodeText.color = Color.green;
                }

                Invoke(nameof(FinishGame), 0.8f);
                return;
            }

            if (inputCodeText != null)
            {
                inputCodeText.text = GetStepText(currentStep + 1);
                inputCodeText.color = Color.white;
            }

            return;
        }

        isGameActive = false;
        if (inputCodeText != null)
        {
            inputCodeText.text = $"ОШИБКА! НУЖЕН: {RequiredSequenceNames[currentStep]}";
            inputCodeText.color = Color.red;
        }

        Invoke(nameof(ResetSequence), 0.8f);
    }

    void ResetSequence()
    {
        if (timeRemaining > 0f)
        {
            currentStep = 0;
            isGameActive = true;

            if (inputCodeText != null)
            {
                inputCodeText.text = GetStepText(1);
                inputCodeText.color = Color.white;
            }
        }
        else
        {
            TimeOut();
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

    void CloseWindowWithFail()
    {
        if (myStation != null)
            myStation.FailTask();
    }

    void FinishGame()
    {
        if (myStation != null)
            myStation.CompleteTask();
    }

    string GetStepText(int stepNumber)
    {
        return $"Шаг {stepNumber}/{RequiredSequenceColors.Length}";
    }
}