using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class PauseMenuController : MonoBehaviour
{
    const float DefaultSliderValue = 0.5f;
    const float MinMouseSensitivity = 100f;
    const float MaxMouseSensitivity = 700f;

    GameObject overlayRoot;
    Slider volumeSlider;
    Slider sensitivitySlider;
    Text controlsText;
    Font uiFont;

    PlayerMovement[] playerMovements;
    MouseLook[] mouseLooks;
    RemoteCameraDisruptor remoteCameraDisruptor;
    BottleBlueCheckpointShield bottleBlueShield;
    RemotePathfinderHelper remotePathfinderHelper;

    bool isPaused;

    void Awake()
    {
        CreateEventSystemIfNeeded();
        CacheGameplayComponents();
        uiFont = LoadFont();
        BuildUi();
        ApplyInitialSliderValues();
        SetPauseState(false);
    }

    void OnDestroy()
    {
        Time.timeScale = 1f;
    }

    void Update()
    {
        if (Object.FindFirstObjectByType<StartMenuController>() != null)
            return;

        if (Input.GetKeyDown(KeyCode.Escape))
            SetPauseState(!isPaused);

        if (isPaused)
            UpdateControlHints();
    }

    void CacheGameplayComponents()
    {
        playerMovements = Object.FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None);
        mouseLooks = Object.FindObjectsByType<MouseLook>(FindObjectsSortMode.None);
        remoteCameraDisruptor = Object.FindFirstObjectByType<RemoteCameraDisruptor>();
        bottleBlueShield = Object.FindFirstObjectByType<BottleBlueCheckpointShield>();
        remotePathfinderHelper = Object.FindFirstObjectByType<RemotePathfinderHelper>();
    }

    void SetPauseState(bool pauseActive)
    {
        CacheGameplayComponents();
        isPaused = pauseActive;

        if (overlayRoot != null)
            overlayRoot.SetActive(pauseActive);

        Time.timeScale = pauseActive ? 0f : 1f;

        foreach (PlayerMovement movement in playerMovements)
        {
            if (movement != null)
                movement.enabled = !pauseActive;
        }

        foreach (MouseLook mouseLook in mouseLooks)
        {
            if (mouseLook != null)
                mouseLook.enabled = !pauseActive;
        }

        Cursor.lockState = pauseActive ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = pauseActive;

        if (pauseActive)
            UpdateControlHints();
    }

    void BuildUi()
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 950;

        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        gameObject.AddComponent<GraphicRaycaster>();

        Image overlay = CreateImage("PauseOverlay", transform, new Color(0f, 0f, 0f, 0.7f));
        StretchToFullScreen(overlay.rectTransform);
        overlayRoot = overlay.gameObject;

        Text header = CreateText("Header", overlayRoot.transform, "Пауза", 64, TextAnchor.MiddleCenter);
        SetRect(header.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -80f), new Vector2(600f, 80f));

        CreateLabeledSlider(overlayRoot.transform, "Громкость", new Vector2(0f, 200f), out volumeSlider, ApplyVolume);
        CreateLabeledSlider(overlayRoot.transform, "Чувствительность мыши", new Vector2(0f, 60f), out sensitivitySlider, ApplyMouseSensitivity);

        controlsText = CreateText("ControlsText", overlayRoot.transform, string.Empty, 38, TextAnchor.UpperCenter);
        controlsText.horizontalOverflow = HorizontalWrapMode.Wrap;
        controlsText.verticalOverflow = VerticalWrapMode.Overflow;
        SetRect(controlsText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 250f), new Vector2(1400f, 420f));
    }

    void ApplyInitialSliderValues()
    {
        float volumeValue = Mathf.Clamp01(AudioListener.volume);
        volumeSlider.SetValueWithoutNotify(volumeValue);
        ApplyVolume(volumeValue);

        float sensitivityValue = DefaultSliderValue;
        if (mouseLooks != null)
        {
            for (int i = 0; i < mouseLooks.Length; i++)
            {
                if (mouseLooks[i] == null)
                    continue;

                sensitivityValue = SensitivityToSliderValue(mouseLooks[i].mouseSensitivity);
                break;
            }
        }

        sensitivitySlider.SetValueWithoutNotify(sensitivityValue);
        ApplyMouseSensitivity(sensitivityValue);
    }

    void UpdateControlHints()
    {
        if (controlsText == null)
            return;

        StringBuilder builder = new StringBuilder();
        builder.AppendLine("Взаимодействие с предметами - E");

        if (remoteCameraDisruptor != null && remoteCameraDisruptor.IsPickedUp)
            builder.AppendLine("R - отключить ближайшую камеру");

        if (bottleBlueShield != null && bottleBlueShield.IsPickedUp)
            builder.AppendLine("Q - стать невидимым для камер на 10 секунд");

        if (remotePathfinderHelper != null && remotePathfinderHelper.IsPickedUp)
        {
            builder.AppendLine("F - показать количество клеток здания до выхода");
            builder.AppendLine("G - показать ближайший рычаг");
        }

        controlsText.text = builder.ToString().TrimEnd();
    }

    void ApplyVolume(float value)
    {
        AudioListener.volume = Mathf.Clamp01(value);
    }

    void ApplyMouseSensitivity(float sliderValue)
    {
        float sensitivity = Mathf.Lerp(MinMouseSensitivity, MaxMouseSensitivity, Mathf.Clamp01(sliderValue));
        if (mouseLooks == null)
            return;

        for (int i = 0; i < mouseLooks.Length; i++)
        {
            if (mouseLooks[i] != null)
                mouseLooks[i].mouseSensitivity = sensitivity;
        }
    }

    float SensitivityToSliderValue(float sensitivity)
    {
        if (Mathf.Approximately(MaxMouseSensitivity, MinMouseSensitivity))
            return DefaultSliderValue;

        return Mathf.Clamp01((sensitivity - MinMouseSensitivity) / (MaxMouseSensitivity - MinMouseSensitivity));
    }

    Font LoadFont()
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null)
            font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        return font;
    }

    void CreateEventSystemIfNeeded()
    {
        if (EventSystem.current != null)
            return;

        GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        Object.DontDestroyOnLoad(eventSystem);
    }

    void CreateLabeledSlider(Transform parent, string label, Vector2 anchoredPosition, out Slider slider, UnityEngine.Events.UnityAction<float> onValueChanged)
    {
        GameObject container = new GameObject(label + "Container", typeof(RectTransform));
        container.transform.SetParent(parent, false);
        RectTransform containerRect = container.GetComponent<RectTransform>();
        SetRect(containerRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), anchoredPosition, new Vector2(760f, 140f));

        Text labelText = CreateText(label + "Label", container.transform, label, 38, TextAnchor.MiddleLeft);
        SetRect(labelText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -30f), new Vector2(0f, 64f));
        labelText.horizontalOverflow = HorizontalWrapMode.Wrap;
        labelText.verticalOverflow = VerticalWrapMode.Overflow;

        slider = CreateSlider(label + "Slider", container.transform);
        RectTransform sliderRect = slider.GetComponent<RectTransform>();
        sliderRect.anchorMin = new Vector2(0f, 0f);
        sliderRect.anchorMax = new Vector2(1f, 0f);
        sliderRect.pivot = new Vector2(0.5f, 0f);
        sliderRect.anchoredPosition = new Vector2(0f, 0f);
        sliderRect.sizeDelta = new Vector2(0f, 48f);
        slider.onValueChanged.AddListener(onValueChanged);
        slider.SetValueWithoutNotify(DefaultSliderValue);
    }

    Image CreateImage(string name, Transform parent, Color color)
    {
        GameObject imageObject = new GameObject(name, typeof(RectTransform), typeof(Image));
        imageObject.transform.SetParent(parent, false);
        Image image = imageObject.GetComponent<Image>();
        image.color = color;
        return image;
    }

    Text CreateText(string name, Transform parent, string content, int fontSize, TextAnchor alignment)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(parent, false);
        Text text = textObject.GetComponent<Text>();
        text.text = content;
        text.font = uiFont;
        text.fontSize = fontSize;
        text.fontStyle = FontStyle.Bold;
        text.alignment = alignment;
        text.color = Color.white;
        text.supportRichText = false;
        return text;
    }

    Slider CreateSlider(string name, Transform parent)
    {
        GameObject sliderObject = new GameObject(name, typeof(RectTransform), typeof(Slider));
        sliderObject.transform.SetParent(parent, false);
        Slider slider = sliderObject.GetComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.direction = Slider.Direction.LeftToRight;

        Image background = CreateImage("Background", sliderObject.transform, new Color(1f, 1f, 1f, 0.25f));
        RectTransform backgroundRect = background.rectTransform;
        backgroundRect.anchorMin = new Vector2(0f, 0.5f);
        backgroundRect.anchorMax = new Vector2(1f, 0.5f);
        backgroundRect.pivot = new Vector2(0.5f, 0.5f);
        backgroundRect.anchoredPosition = Vector2.zero;
        backgroundRect.sizeDelta = new Vector2(0f, 10f);

        Image fillArea = CreateImage("Fill", sliderObject.transform, Color.white);
        RectTransform fillRect = fillArea.rectTransform;
        fillRect.anchorMin = new Vector2(0f, 0.5f);
        fillRect.anchorMax = new Vector2(1f, 0.5f);
        fillRect.pivot = new Vector2(0.5f, 0.5f);
        fillRect.anchoredPosition = Vector2.zero;
        fillRect.sizeDelta = new Vector2(-24f, 10f);

        Image handle = CreateImage("Handle", sliderObject.transform, Color.white);
        RectTransform handleRect = handle.rectTransform;
        handleRect.anchorMin = new Vector2(0f, 0.5f);
        handleRect.anchorMax = new Vector2(0f, 0.5f);
        handleRect.pivot = new Vector2(0.5f, 0.5f);
        handleRect.sizeDelta = new Vector2(20f, 44f);

        slider.fillRect = fillRect;
        slider.handleRect = handleRect;
        slider.targetGraphic = handle;
        return slider;
    }

    void StretchToFullScreen(RectTransform rectTransform)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }

    void SetRect(RectTransform rectTransform, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = sizeDelta;
    }
}

public static class PauseMenuBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void CreatePauseMenuController()
    {
        if (Object.FindFirstObjectByType<PauseMenuController>() != null)
            return;

        GameObject controllerObject = new GameObject("PauseMenuController");
        controllerObject.AddComponent<PauseMenuController>();
    }
}