using UnityEngine;

/// <summary>
/// Триггер у выхода с улицы. При входе игрока объявляет победу.
/// </summary>
public class ExitTrigger : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
            GameManager.Instance?.TriggerWin();
    }
}
