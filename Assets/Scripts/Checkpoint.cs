using UnityEngine;

/// <summary>
/// Невидимый триггер-диск на полу. При входе игрока сохраняет этот чекпоинт.
/// Разместите его в начале каждого этажа.
/// </summary>
public class Checkpoint : MonoBehaviour
{
    [SerializeField] int floorNumber = 1;

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        // Сохраняем позицию на уровне пола (немного выше центра триггера)
        Vector3 respawnPos = new Vector3(
            transform.position.x,
            transform.position.y + 0.1f,
            transform.position.z);

        CheckpointManager.Instance?.SetCheckpoint(respawnPos, Quaternion.identity);
        GameManager.Instance?.SetFloor(floorNumber);
    }
}
