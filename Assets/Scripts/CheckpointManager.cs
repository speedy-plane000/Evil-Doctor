using UnityEngine;

/// <summary>
/// Хранит текущий чекпоинт и выполняет телепортацию игрока при провале.
/// </summary>
public class CheckpointManager : MonoBehaviour
{
    public static CheckpointManager Instance { get; private set; }

    Vector3    _respawnPos;
    Quaternion _respawnRot;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    /// <summary>Задаёт начальную точку появления (вызывается GameManager.Start).</summary>
    public void RegisterSpawn(Vector3 position, Quaternion rotation)
    {
        _respawnPos = position;
        _respawnRot = rotation;
    }

    /// <summary>Обновляет активный чекпоинт.</summary>
    public void SetCheckpoint(Vector3 position, Quaternion rotation)
    {
        _respawnPos = position;
        _respawnRot = rotation;
        Debug.Log($"[CheckpointManager] Чекпоинт сохранён: {position}");
    }

    /// <summary>Телепортирует игрока на последний чекпоинт.</summary>
    public void RespawnPlayer()
    {
        var player = GameObject.FindWithTag("Player");
        if (player == null) return;

        var pc = player.GetComponent<PlayerController>();
        if (pc != null)
        {
            pc.Teleport(_respawnPos);
            player.transform.rotation = _respawnRot;
        }
        else
        {
            var cc = player.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
            player.transform.SetPositionAndRotation(_respawnPos, _respawnRot);
            if (cc != null) cc.enabled = true;
        }
    }
}
