using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>
/// </summary>
public class EndGameManager : MonoBehaviour
{
    [Header("Настройки таймера")]
    [SerializeField] private float fadeDuration = 6f; // время затемнения экрана
    [SerializeField] private float antidoteInputTime = 6f; // время для ввода антидота
    [SerializeField] private float returnToMenuDelayAfterBlack = 4f; // время черного экрана перед главным меню
    
    [Header("UI элементы")]
    [SerializeField] private Image fadeOverlay; // затемняющая панель
    [SerializeField] private TextMeshProUGUI messageText; // текст сообщения
    [SerializeField] private TextMeshProUGUI timerText; // текст таймера
    [SerializeField] private GameObject endGameUI; // родительский объект UI
    
    [Header("Цвета")]
    [SerializeField] private Color fadeColor = Color.black;
    [SerializeField] private Color deathMessageColor = Color.red;
    [SerializeField] private Color successMessageColor = Color.green;
    [SerializeField] private Color warningMessageColor = Color.yellow;
    
    [Header("Сообщения")]
    [SerializeField] private string noAntidoteMessage = "Вы вышли на солнце и умерли, так как у вас нет антидота, чтобы обезвредить препарат, введённый вам доктором";
    [SerializeField] private string antidotePromptMessage = "Нажмите ПРОБЕЛ, чтобы ввести антидот";
    [SerializeField] private string timeoutMessage = "Вы вышли на солнце и умерли, так как не успели ввести антидот, чтобы обезвредить препарат, введённый вам доктором";
    [SerializeField] private string successMessage = "Вы успешно сбежали от злого доктора!";
    
    [Header("Прочее")]
    [SerializeField] private GameObject questHUD;

    // Состояние
    private bool _antidoteCollected = false;
    private bool _isEndGameInProgress = false;
    private bool _antidoteInjectedSuccessfully = false;
    private float _currentAntidoteTime = 0f;
    
    // Синглтон
    public static EndGameManager Instance { get; private set; }
    
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        // Инициализация UI
        InitializeUI();
    }
    
    void InitializeUI()
    {
        if (endGameUI != null)
        {
            endGameUI.SetActive(false);
        }
        
        if (fadeOverlay != null)
        {
            fadeOverlay.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 0f);
        }
        
        if (messageText != null)
        {
            messageText.text = "";
            messageText.gameObject.SetActive(false);
        }
        
        if (timerText != null)
        {
            timerText.text = "";
            timerText.gameObject.SetActive(false);
        }
    }
    
    void Update()
    {
        // Если идет процесс ввода антидота, проверяем нажатие пробела
        if (_isEndGameInProgress && _antidoteCollected && _currentAntidoteTime > 0)
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                OnAntidoteInjectedSuccessfully();
            }
        }
    }
    
    /// <summary>
    /// Установить статус сбора антидота
    /// </summary>
    public void SetAntidoteCollected(bool collected)
    {
        _antidoteCollected = collected;
        Debug.Log($"[EndGameManager] Антидот собран: {collected}");
    }
    
    /// <summary>
    /// Начать последовательность окончания игры
    /// </summary>
    public void StartEndGameSequence()
    {
        if (_isEndGameInProgress) return;

        if (questHUD != null)
            questHUD.SetActive(false);

        
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            var rb = player.GetComponent<Rigidbody>();
            if (rb != null) rb.linearVelocity = Vector3.zero;

            var movement = player.GetComponent<PlayerMovement>();
            if (movement != null) movement.enabled = false;
        }

        _antidoteInjectedSuccessfully = false;
        _isEndGameInProgress = true;
        StartCoroutine(EndGameSequence());
    }
    
    IEnumerator EndGameSequence()
    {
        // Активируем UI
        if (endGameUI != null)
        {
            endGameUI.SetActive(true);
        }
        
        // Если антидот не собран - сразу смерть
        if (!_antidoteCollected)
        {
            yield return StartCoroutine(FadeToBlackWithMessageAndReturn(noAntidoteMessage, deathMessageColor));
            Debug.Log("[EndGameManager] Игрок умер без антидота");
            yield break;
        }
        
        // Если антидот собран - даем время на ввод
        ShowMessage(antidotePromptMessage, float.MaxValue, warningMessageColor);
        UpdateFadeAlpha(0f);
        
        // Запускаем таймер для ввода антидота
        _currentAntidoteTime = antidoteInputTime;
        
        if (timerText != null)
        {
            timerText.gameObject.SetActive(true);
        }
        
        // Таймер обратного отсчета
        float elapsed = 0f;
        float safeFadeDuration = Mathf.Max(0.01f, fadeDuration);

        while (_currentAntidoteTime > 0f && _isEndGameInProgress)
        {
            _currentAntidoteTime -= Time.deltaTime;
            elapsed += Time.deltaTime;
            UpdateFadeAlpha(elapsed / safeFadeDuration);
            
            if (timerText != null)
            {
                float safeInputTime = Mathf.Max(0.01f, antidoteInputTime);
                timerText.text = $"Время: {Mathf.Ceil(Mathf.Max(0f, _currentAntidoteTime))}с";
                timerText.color = Color.Lerp(Color.red, Color.yellow, Mathf.Clamp01(_currentAntidoteTime / safeInputTime));
            }

            if (_antidoteInjectedSuccessfully)
                break;

            yield return null;
        }

        if (_antidoteInjectedSuccessfully)
        {
            _isEndGameInProgress = false;
            if (timerText != null)
                timerText.gameObject.SetActive(false);

            ShowMessage(successMessage, 5f, successMessageColor);
            yield return StartCoroutine(FadeOutScreen(0.75f));
            Debug.Log("[EndGameManager] Игрок успешно сбежал!");
            yield break;
        }
        
        // Если время вышло и антидот не введен
        if (_currentAntidoteTime <= 0 && _isEndGameInProgress)
        {
            ShowMessage(timeoutMessage, float.MaxValue, deathMessageColor);
            if (timerText != null)
                timerText.gameObject.SetActive(false);

            UpdateFadeAlpha(1f);
            yield return new WaitForSecondsRealtime(returnToMenuDelayAfterBlack);
            ReturnToMainMenu();
            Debug.Log("[EndGameManager] Игрок не успел ввести антидот");
        }
    }
    
    /// <summary>
    /// Успешный ввод антидота
    /// </summary>
    private void OnAntidoteInjectedSuccessfully()
    {
        if (!_isEndGameInProgress) return;
        _antidoteInjectedSuccessfully = true;
    }
    
    /// <summary>
    /// Затемнение экрана с сообщением и возвратом в главное меню
    /// </summary>
    IEnumerator FadeToBlackWithMessageAndReturn(string message, Color messageColor)
    {
        ShowMessage(message, float.MaxValue, messageColor);
        if (timerText != null)
            timerText.gameObject.SetActive(false);

        float safeFadeDuration = Mathf.Max(0.01f, fadeDuration);
        float elapsed = 0f;
        while (elapsed < safeFadeDuration)
        {
            elapsed += Time.deltaTime;
            UpdateFadeAlpha(elapsed / safeFadeDuration);
            yield return null;
        }

        yield return new WaitForSecondsRealtime(returnToMenuDelayAfterBlack);
        ReturnToMainMenu();
    }
    
    /// <summary>
    /// Убрать затемнение экрана
    /// </summary>
    IEnumerator FadeOutScreen(float duration)
    {
        if (fadeOverlay == null)
            yield break;

        float safeDuration = Mathf.Max(0.01f, duration);
        float startAlpha = fadeOverlay.color.a;
        float time = 0f;
        
        while (time < safeDuration)
        {
            time += Time.deltaTime;
            float alpha = Mathf.Lerp(startAlpha, 0f, Mathf.Clamp01(time / safeDuration));
            UpdateFadeAlpha(alpha, true);
            yield return null;
        }
        
        UpdateFadeAlpha(0f);
    }
    
    /// <summary>
    /// Показать сообщение на экране
    /// </summary>
    public void ShowMessage(string message, float duration, Color color)
    {
        if (messageText == null) return;
        
        messageText.text = message;
        messageText.color = color;
        messageText.gameObject.SetActive(true);
        
        if (duration < float.MaxValue)
        {
            StartCoroutine(HideMessageAfterDelay(duration));
        }
    }
    
    IEnumerator HideMessageAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (messageText != null)
        {
            messageText.gameObject.SetActive(false);
        }
    }

    void UpdateFadeAlpha(float value, bool directAlpha = false)
    {
        if (fadeOverlay == null)
            return;

        float alpha = directAlpha ? Mathf.Clamp01(value) : Mathf.Clamp01(value);
        fadeOverlay.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, alpha);
    }

    void ReturnToMainMenu()
    {
        _isEndGameInProgress = false;
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
    
    /// <summary>
    /// Проверить, идет ли процесс окончания игры
    /// </summary>
    public bool IsEndGameInProgress()
    {
        return _isEndGameInProgress;
    }
    
    /// <summary>
    /// Сбросить состояние менеджера (для тестирования)
    /// </summary>
    public void ResetManager()
    {
        StopAllCoroutines();
        _isEndGameInProgress = false;
        _antidoteInjectedSuccessfully = false;
        _currentAntidoteTime = 0f;
        InitializeUI();
    }
    
    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}