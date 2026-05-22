using UnityEngine;
using UnityEngine.UI;

public static class CrosshairSettings
{
    const string CrosshairEnabledKey = "CrosshairEnabled";

    public static bool IsCrosshairEnabled
    {
        get => PlayerPrefs.GetInt(CrosshairEnabledKey, 1) == 1;
        set => PlayerPrefs.SetInt(CrosshairEnabledKey, value ? 1 : 0);
    }

    public static string GetToggleButtonLabel()
    {
        return IsCrosshairEnabled ? "Прицел: ВКЛ" : "Прицел: ВЫКЛ";
    }
}

[DisallowMultipleComponent]
public class CrosshairController : MonoBehaviour
{
    Image crosshairDot;

    void Awake()
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 900;

        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        gameObject.AddComponent<GraphicRaycaster>().enabled = false;

        GameObject dotObject = new GameObject("CrosshairDot", typeof(RectTransform), typeof(Image));
        dotObject.transform.SetParent(transform, false);
        crosshairDot = dotObject.GetComponent<Image>();
        crosshairDot.color = Color.white;
        crosshairDot.raycastTarget = false;

        RectTransform dotRect = dotObject.GetComponent<RectTransform>();
        dotRect.anchorMin = new Vector2(0.5f, 0.5f);
        dotRect.anchorMax = new Vector2(0.5f, 0.5f);
        dotRect.pivot = new Vector2(0.5f, 0.5f);
        dotRect.anchoredPosition = Vector2.zero;
        dotRect.sizeDelta = new Vector2(6f, 6f);
    }

    void Update()
    {
        if (crosshairDot == null)
            return;

        bool shouldShow = CrosshairSettings.IsCrosshairEnabled
                          && Cursor.lockState == CursorLockMode.Locked
                          && !Cursor.visible;

        crosshairDot.enabled = shouldShow;
    }
}

public static class CrosshairBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void CreateCrosshairController()
    {
        if (Object.FindFirstObjectByType<CrosshairController>() != null)
            return;

        GameObject controllerObject = new GameObject("CrosshairController");
        controllerObject.AddComponent<CrosshairController>();
    }
}