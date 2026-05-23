using UnityEngine;
using UnityEngine.UI;
using TMPro;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Вспомогательный скрипт для настройки UI окончания игры
/// Добавьте этот скрипт на объект с EndGameManager для автоматической настройки UI
/// </summary>
[RequireComponent(typeof(EndGameManager))]
public class EndGameUISetup : MonoBehaviour
{
    [Header("UI Префабы")]
    [SerializeField] private GameObject canvasPrefab;
    [SerializeField] private GameObject fadeOverlayPrefab;
    [SerializeField] private GameObject messageTextPrefab;
    [SerializeField] private GameObject timerTextPrefab;
    
    private void Start()
    {
        SetupUI();
    }
    
    private void SetupUI()
    {
        var endGameManager = GetComponent<EndGameManager>();
        
        // Создаем Canvas если его нет
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            if (canvasPrefab != null)
            {
                var canvasObj = Instantiate(canvasPrefab);
                canvas = canvasObj.GetComponent<Canvas>();
            }
            else
            {
                var canvasObj = new GameObject("EndGameCanvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();
            }
        }
        
        // Создаем родительский объект для UI окончания игры
        GameObject endGameUI = new GameObject("EndGameUI");
        endGameUI.transform.SetParent(canvas.transform);
        endGameUI.transform.localPosition = Vector3.zero;
        endGameUI.transform.localScale = Vector3.one;
        
        // Создаем затемняющую панель
        Image fadeOverlay;
        if (fadeOverlayPrefab != null)
        {
            var overlayObj = Instantiate(fadeOverlayPrefab, endGameUI.transform);
            fadeOverlay = overlayObj.GetComponent<Image>();
        }
        else
        {
            var overlayObj = new GameObject("FadeOverlay");
            overlayObj.transform.SetParent(endGameUI.transform);
            
            fadeOverlay = overlayObj.AddComponent<Image>();
            fadeOverlay.color = new Color(0, 0, 0, 0);
            fadeOverlay.rectTransform.anchorMin = Vector2.zero;
            fadeOverlay.rectTransform.anchorMax = Vector2.one;
            fadeOverlay.rectTransform.offsetMin = Vector2.zero;
            fadeOverlay.rectTransform.offsetMax = Vector2.zero;
        }
        
        // Создаем текст сообщения
        TextMeshProUGUI messageText;
        if (messageTextPrefab != null)
        {
            var textObj = Instantiate(messageTextPrefab, endGameUI.transform);
            messageText = textObj.GetComponent<TextMeshProUGUI>();
        }
        else
        {
            var textObj = new GameObject("MessageText");
            textObj.transform.SetParent(endGameUI.transform);
            
            messageText = textObj.AddComponent<TextMeshProUGUI>();
            messageText.text = "";
            messageText.alignment = TextAlignmentOptions.Center;
            messageText.fontSize = 24;
            messageText.enableAutoSizing = true;
            messageText.fontSizeMin = 18;
            messageText.fontSizeMax = 36;
            
            var rectTransform = messageText.rectTransform;
            rectTransform.anchorMin = new Vector2(0.1f, 0.4f);
            rectTransform.anchorMax = new Vector2(0.9f, 0.6f);
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }
        
        // Создаем текст таймера
        TextMeshProUGUI timerText;
        if (timerTextPrefab != null)
        {
            var timerObj = Instantiate(timerTextPrefab, endGameUI.transform);
            timerText = timerObj.GetComponent<TextMeshProUGUI>();
        }
        else
        {
            var timerObj = new GameObject("TimerText");
            timerObj.transform.SetParent(endGameUI.transform);
            
            timerText = timerObj.AddComponent<TextMeshProUGUI>();
            timerText.text = "";
            timerText.alignment = TextAlignmentOptions.Center;
            timerText.fontSize = 20;
            timerText.color = Color.yellow;
            
            var rectTransform = timerText.rectTransform;
            rectTransform.anchorMin = new Vector2(0.3f, 0.7f);
            rectTransform.anchorMax = new Vector2(0.7f, 0.8f);
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }
        
 
   
        
        // Деактивируем UI
        endGameUI.SetActive(false);
        
        Debug.Log("[EndGameUISetup] UI настроен автоматически");
    }
    
#if UNITY_EDITOR
    [UnityEditor.MenuItem("Evil Doctor/Setup End Game UI")]
    private static void SetupEndGameUIFromMenu()
    {
        var manager = FindObjectOfType<EndGameManager>();
        if (manager == null)
        {
            var managerObj = new GameObject("EndGameManager");
            manager = managerObj.AddComponent<EndGameManager>();
            managerObj.AddComponent<EndGameUISetup>();
        }
        else
        {
            var setup = manager.GetComponent<EndGameUISetup>();
            if (setup == null)
            {
                setup = manager.gameObject.AddComponent<EndGameUISetup>();
            }
            setup.SetupUI();
        }
    }
#endif
}