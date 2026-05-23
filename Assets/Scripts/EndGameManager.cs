using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

/// <summary>
/// </summary>
public class EndGameManager : MonoBehaviour
{
    [Header("Настройки таймера")]
    [SerializeField] private float fadeDuration = 5f; // время затемнения экрана
    [SerializeField] private float antidoteInputTime = 5f; // время для ввода антидота
    
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
    private bool _isFading = false;
    private float _currentFadeTime = 0f;
    private float _currentAntidoteTime = 0f;
    private Coroutine _endGameCoroutine;
    
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
                AntidoteInjectedSuccessfully();
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

        _isEndGameInProgress = true;
        _endGameCoroutine = StartCoroutine(EndGameSequence());
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
            ShowMessage(noAntidoteMessage, float.MaxValue, deathMessageColor);
            yield return StartCoroutine(FadeScreen());
            // Здесь можно добавить перезагрузку сцены или переход в меню
            Debug.Log("[EndGameManager] Игрок умер без антидота");
            yield break;
        }
        
        // Если антидот собран - даем время на ввод
        ShowMessage(antidotePromptMessage, float.MaxValue, warningMessageColor);
        
        // Запускаем таймер для ввода антидота
        _currentAntidoteTime = antidoteInputTime;
        
        if (timerText != null)
        {
            timerText.gameObject.SetActive(true);
        }
        
        // Таймер обратного отсчета
        while (_currentAntidoteTime > 0 && _isEndGameInProgress)
        {
            _currentAntidoteTime -= Time.deltaTime;
            
            if (timerText != null)
            {
                timerText.text = $"Время: {Mathf.Ceil(_currentAntidoteTime)}с";
                timerText.color = Color.Lerp(Color.red, Color.yellow, _currentAntidoteTime / antidoteInputTime);
            }
            
            // Начинаем постепенное затемнение в последние 2 секунды
            if (_currentAntidoteTime <= 2f && !_isFading)
            {
                StartCoroutine(FadeScreen());
            }
            
            yield return null;
        }
        
        // Если время вышло и антидот не введен
        if (_currentAntidoteTime <= 0 && _isEndGameInProgress)
        {
            ShowMessage(timeoutMessage, float.MaxValue, deathMessageColor);
            yield return new WaitForSeconds(2f);
            Debug.Log("[EndGameManager] Игрок не успел ввести антидот");
            // Здесь можно добавить перезагрузку сцены или переход в меню
        }
    }
    
    /// <summary>
    /// Успешный ввод антидота
    /// </summary>
    private void AntidoteInjectedSuccessfully()
    {
        if (!_isEndGameInProgress) return;
        
        StopAllCoroutines();
        _isEndGameInProgress = false;
        
        // Останавливаем затемнение
        _isFading = false;
        
        // Показываем сообщение об успехе
        ShowMessage(successMessage, 5f, successMessageColor);
        
        // Убираем таймер
        if (timerText != null)
        {
            timerText.gameObject.SetActive(false);
        }

        _isFading = false; 
        StartCoroutine(FadeScreen());

        Debug.Log("[EndGameManager] Игрок успешно сбежал!");
        
    }
    
    /// <summary>
    /// Затемнение экрана
    /// </summary>
    IEnumerator FadeScreen()
    {
        if (_isFading || fadeOverlay == null) yield break;
        
        _isFading = true;
        _currentFadeTime = 0f;
        
        while (_currentFadeTime < fadeDuration && _isFading)
        {
            _currentFadeTime += Time.deltaTime;
            float alpha = Mathf.Clamp01(_currentFadeTime / fadeDuration);
            fadeOverlay.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, alpha);
            yield return null;
        }
        
        if (_isFading)
        {
            fadeOverlay.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 1f);
        }
    }
    
    /// <summary>
    /// Убрать затемнение экрана
    /// </summary>
    IEnumerator FadeOutScreen()
    {
        float startAlpha = fadeOverlay.color.a;
        float time = 0f;
        
        while (time < 1f)
        {
            time += Time.deltaTime;
            float alpha = Mathf.Lerp(startAlpha, 0f, time);
            fadeOverlay.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, alpha);
            yield return null;
        }
        
        fadeOverlay.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 0f);
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
        _isFading = false;
        _currentFadeTime = 0f;
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