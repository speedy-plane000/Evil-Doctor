using UnityEngine;

/// <summary>
/// Триггер для запуска процесса окончания игры
/// Разместите на лестнице в конце или на зеленом кубе (основа мира)
/// </summary>
public class EndGameTrigger : MonoBehaviour
{
    [Header("Настройки")]
    [SerializeField] private bool isOneTimeTrigger = true;
    [SerializeField] private float triggerDelay = 0.5f; // задержка перед запуском
    
    [Header("Визуальные подсказки")]
    [SerializeField] private GameObject visualHint; // визуальная подсказка (например, светящаяся область)
    [SerializeField] private string triggerMessage = "Выход найден!";
    [SerializeField] private float messageDuration = 2f;
    
    private bool _hasTriggered = false;
    private EndGameManager _endGameManager;
    
    void Start()
    {
        _endGameManager = FindObjectOfType<EndGameManager>();
        
        // Активируем визуальную подсказку если есть
        if (visualHint != null)
        {
            visualHint.SetActive(true);
        }
    }
    
    void OnTriggerEnter(Collider other)
    {
        Debug.Log("EndGameTrigger: вошёл " + other.name + " tag=" + other.tag);

        if (_hasTriggered && isOneTimeTrigger) return;
        if (!other.CompareTag("Player")) return;
        if (_endGameManager == null) return;
        
        // Проверяем, не запущен ли уже процесс окончания
        if (_endGameManager.IsEndGameInProgress()) return;
        
        _hasTriggered = true;
        
        // Показываем сообщение
        if (!string.IsNullOrEmpty(triggerMessage))
        {
            _endGameManager.ShowMessage(triggerMessage, messageDuration, Color.yellow);
        }
        
        // Запускаем процесс окончания с задержкой
        Invoke(nameof(StartEndGameProcess), triggerDelay);
    }
    
    void StartEndGameProcess()
    {
        if (_endGameManager != null)
        {
            _endGameManager.StartEndGameSequence();
        }
    }
    
    /// <summary>
    /// Сбросить триггер (для тестирования)
    /// </summary>
    public void ResetTrigger()
    {
        _hasTriggered = false;
    }
    
    void OnDrawGizmos()
    {
        // Визуализация триггера в редакторе
        Gizmos.color = Color.yellow;
        var collider = GetComponent<Collider>();
        if (collider != null)
        {
            Gizmos.DrawWireCube(transform.position + collider.bounds.center - transform.position, 
                               collider.bounds.size);
        }
    }
}