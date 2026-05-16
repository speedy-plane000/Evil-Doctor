using UnityEngine;
using UnityEngine.UI;
using TMPro;

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

    [Header("Настройки сложности")]
    public float timeLimit = 5f; 

    private string requiredCode;
    private string currentInput = "";
    private float timeRemaining;
    private bool isGameActive = false;

    void OnEnable()
    {
        GenerateNewCode();
    }

    void Update()
    {
        if (!isGameActive) return;

        
        timeRemaining -= Time.deltaTime;

        
        if (timerText != null)
        {
            timerText.text = "TIME: " + Mathf.Max(0, timeRemaining).ToString("F2") + "s";

            
            if (timeRemaining <= 2f) timerText.color = Color.red;
            else timerText.color = Color.white;
        }

        if (timerSlider != null)
        {
            timerSlider.value = timeRemaining;
        }

        
        if (timeRemaining <= 0)
        {
            TimeOut();
        }
    }

    void GenerateNewCode()
    {
        currentInput = "";
        inputCodeText.text = "";
        inputCodeText.color = Color.white;

        
        timeRemaining = timeLimit;
        if (timerSlider != null)
        {
            timerSlider.maxValue = timeLimit;
            timerSlider.value = timeLimit;
        }

        
        requiredCode = Random.Range(1000, 10000).ToString();
        targetCodeText.text = "CODE: " + requiredCode;
        targetCodeText.color = Color.yellow;

        
        isGameActive = true;
    }

    public void ButtonPress(int number)
    {
        if (!isGameActive || currentInput.Length >= 4) return;

        currentInput += number.ToString();
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
            inputCodeText.text = "ACCEPTED";
            inputCodeText.color = Color.green;
            Invoke("FinishGame", 0.8f);
        }
        else
        {
            inputCodeText.text = "ERROR";
            inputCodeText.color = Color.red;
            Invoke("ResetInput", 0.6f);
        }
    }

    void TimeOut()
    {
        isGameActive = false;
        inputCodeText.text = "TIME OUT";
        inputCodeText.color = Color.red;

        
        Invoke("CloseWindowWithFail", 1f);
    }

    void ResetInput()
    {
        
        if (timeRemaining > 0)
        {
            currentInput = "";
            inputCodeText.text = "";
            inputCodeText.color = Color.white;
            isGameActive = true;
        }
        else
        {
            TimeOut();
        }
    }

    void CloseWindowWithFail()
    {
        if (myStation != null)
        {
            
            myStation.taskUIPanel.SetActive(false);

            PlayerMovement player = FindObjectOfType<PlayerMovement>();
            if (player != null) player.enabled = true;

            MouseLook mouseLook = FindObjectOfType<MouseLook>();
            if (mouseLook != null) mouseLook.enabled = true;

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    void FinishGame()
    {
        if (myStation != null)
        {
            myStation.CompleteTask();
        }
    }
}