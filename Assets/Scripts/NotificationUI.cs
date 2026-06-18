using UnityEngine;
using TMPro;
using System.Collections;

public class NotificationUI : MonoBehaviour
{
    public static NotificationUI Instance;
    public TextMeshProUGUI notificationText;

    void Awake()
    {
        Instance = this;
    }

    public void Show(string message, float duration = 3f, Color color = default)
    {
        if (color == default) color = Color.yellow;
        StopAllCoroutines();
        StartCoroutine(ShowCoroutine(message, duration, color));
    }

    IEnumerator ShowCoroutine(string message, float duration, Color color)
    {
        notificationText.text = message;
        notificationText.color = color;
        notificationText.gameObject.SetActive(true);
        yield return new WaitForSeconds(duration);
        notificationText.gameObject.SetActive(false);
    }
}