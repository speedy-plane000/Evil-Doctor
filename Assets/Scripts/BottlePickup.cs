using UnityEngine;

/// <summary>
/// Скрипт для подбора бутылки с антидотом
/// </summary>
public class BottlePickup : MonoBehaviour
{
    [Header("Настройки")]
    [SerializeField] private GameObject pickupEffect; // опциональный эффект при подборе
    [SerializeField] private AudioClip pickupSound;   // опциональный звук при подборе
    
    [Header("Сообщение")]
    [SerializeField] private string pickupMessage = "Вы подобрали антидот!";
    [SerializeField] private float messageDuration = 3f;
    
    private bool _isPickedUp = false;
    
    void OnTriggerEnter(Collider other)
    {
        if (_isPickedUp) return;
        if (!other.CompareTag("Player")) return;
        
        // Сообщаем EndGameManager, что антидот подобран
        var endGameManager = FindObjectOfType<EndGameManager>();
        if (endGameManager != null)
        {
            endGameManager.SetAntidoteCollected(true);
        }
        
        // Воспроизводим эффекты
        if (pickupEffect != null)
        {
            Instantiate(pickupEffect, transform.position, Quaternion.identity);
        }
        
        if (pickupSound != null)
        {
            AudioSource.PlayClipAtPoint(pickupSound, transform.position);
        }
        
        // Показываем сообщение
        if (endGameManager != null && !string.IsNullOrEmpty(pickupMessage))
        {
            endGameManager.ShowMessage(pickupMessage, messageDuration, Color.green);
        }
        
        // Деактивируем объект
        _isPickedUp = true;
        gameObject.SetActive(false);
        
        Debug.Log("[BottlePickup] Антидот подобран игроком");
    }
    
    /// <summary>
    /// </summary>
    public void ResetBottle()
    {
        _isPickedUp = false;
        gameObject.SetActive(true);
    }
}