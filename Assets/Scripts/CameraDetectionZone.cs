using UnityEngine;

/// <summary>
/// Красная зона обнаружения камеры на полу.
/// При наступании игрока — немедленный телепорт к последнему чекпоинту.
///
/// Добавьте компонент к плоскому объекту (Cylinder / Quad / Box),
/// назначьте красный полупрозрачный материал и включите isTrigger на коллайдере.
/// </summary>
public class CameraDetectionZone : MonoBehaviour
{
    [Tooltip("Активирована ли зона камерой (можно отключить рычагом)")]
    [SerializeField] bool isActive = true;

    /// <summary>Позволяет рычагу отключить зону.</summary>
    public void SetActive(bool value) => isActive = value;

    void OnTriggerEnter(Collider other)
    {
        if (!isActive) return;
        if (!other.CompareTag("Player")) return;

        CheckpointManager.Instance?.RespawnPlayer();
    }
}
