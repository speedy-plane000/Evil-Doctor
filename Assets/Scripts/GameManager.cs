using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Центральный менеджер игры. Отслеживает состояние, текущий этаж, победу.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public int CurrentFloor { get; private set; } = 3;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        // Устанавливаем начальный чекпоинт в точке появления игрока
        var spawn = GameObject.FindWithTag("Respawn");
        if (spawn != null)
            CheckpointManager.Instance?.RegisterSpawn(spawn.transform.position,
                                                      spawn.transform.rotation);
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

    /// <summary>Перезапустить сцену.</summary>
    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
