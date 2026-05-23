using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Центральный менеджер игры. Отслеживает состояние, текущий этаж, победу.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public int CurrentFloor { get; private set; } = 3;
    
    [Header("Настройки окончания игры")]
    [SerializeField] private float restartDelayAfterDeath = 3f;
    [SerializeField] private float restartDelayAfterSuccess = 5f;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }


    /// <summary>Вызывается при смене этажа (через триггеры переходов).</summary>
    public void SetFloor(int floor)
    {
        CurrentFloor = floor;
        Debug.Log($"[GameManager] Игрок перешёл на этаж {floor}");
    }

    /// <summary>Вызывается при выходе игрока на улицу — победа.</summary>
    public void TriggerWin()
    {
        Debug.Log("[GameManager] Игрок сбежал! Победа!");
        // TODO: показать экран победы
    }
    
    /// <summary>Вызывается при успешном побеге (антидот введен вовремя).</summary>
    public void TriggerEscapeSuccess()
    {
        Debug.Log("[GameManager] Игрок успешно сбежал с антидотом!");
        
        // Через некоторое время перезагружаем сцену или загружаем следующую
        Invoke(nameof(LoadNextSceneOrRestart), restartDelayAfterSuccess);
    }
    
    /// <summary>Вызывается при смерти игрока (без антидота или не успел ввести).</summary>
    public void TriggerDeath()
    {
        Debug.Log("[GameManager] Игрок умер!");
        
        // Через некоторое время перезагружаем сцену
        Invoke(nameof(RestartGame), restartDelayAfterDeath);
    }

    /// <summary>Перезапустить сцену.</summary>
    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
    
    /// <summary>Загрузить следующую сцену или перезапустить текущую.</summary>
    private void LoadNextSceneOrRestart()
    {
        int nextSceneIndex = SceneManager.GetActiveScene().buildIndex + 1;
        if (nextSceneIndex < SceneManager.sceneCountInBuildSettings)
        {
            SceneManager.LoadScene(nextSceneIndex);
        }
        else
        {
            RestartGame(); // Если следующей сцены нет, перезапускаем текущую
        }
    }
}
