using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class StartMenuController : MonoBehaviour
{
    const float FadeDurationSeconds = 3f;
    const float DefaultSliderValue = 0.5f;
    const float MinMouseSensitivity = 100f;
    const float MaxMouseSensitivity = 700f;

    CanvasGroup blackOverlayGroup;
    GameObject mainPanel;
    GameObject settingsPanel;
    Slider volumeSlider;
    Slider sensitivitySlider;
    Font uiFont;
    PlayerMovement[] playerMovements;
    MouseLook[] mouseLooks;
    bool hasStarted;

    void Awake()
    {
        if (FindObjectsByType<StartMenuController>(FindObjectsSortMode.None).Length > 1)
        {
            Destroy(gameObject);
            return;
        }

        CreateEventSystemIfNeeded();
        CacheGameplayComponents();
        uiFont = LoadFont();
        BuildUi();
        SetMenuState(true);
        ApplyVolume(DefaultSliderValue);
        ApplyMouseSensitivity(DefaultSliderValue);
    }

    void OnDestroy()
    {
        Time.timeScale = 1f;
    }

    void CacheGameplayComponents()
    {
        playerMovements = FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None);
        mouseLooks = FindObjectsByType<MouseLook>(FindObjectsSortMode.None);
    }

    void SetMenuState(bool menuActive)
    {
        Time.timeScale = menuActive ? 0f : 1f;

        foreach (PlayerMovement movement in playerMovements)
        {
            if (movement != null)
                movement.enabled = !menuActive;
        }

        foreach (MouseLook mouseLook in mouseLooks)
        {
            if (mouseLook != null)
                mouseLook.enabled = !menuActive;
        }

        Cursor.lockState = menuActive ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = menuActive;
    }

    void BuildUi()
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;

        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        gameObject.AddComponent<GraphicRaycaster>();

        Image blackOverlay = CreateImage("BlackOverlay", transform, Color.black);
        StretchToFullScreen(blackOverlay.rectTransform);
        blackOverlayGroup = blackOverlay.gameObject.AddComponent<CanvasGroup>();

        mainPanel = CreatePanel("MainPanel", transform);
        settingsPanel = CreatePanel("SettingsPanel", transform);

        BuildMainPanel(mainPanel.transform);
        BuildSettingsPanel(settingsPanel.transform);

        settingsPanel.SetActive(false);
    }

    void BuildMainPanel(Transform parent)
    {
        Text title = CreateText("Title", parent, "Evil Doctor", 60, TextAnchor.MiddleCenter);
        SetRect(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 180f), new Vector2(700f, 100f));

        Button startButton = CreateButton("StartButton", parent, "Начать");
        SetRect(startButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 25f), new Vector2(340f, 70f));
        startButton.onClick.AddListener(StartGame);

        Button settingsButton = CreateButton("SettingsButton", parent, "Настройки");
        SetRect(settingsButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -65f), new Vector2(340f, 70f));
        settingsButton.onClick.AddListener(OpenSettings);
    }

    void BuildSettingsPanel(Transform parent)
    {
        Image window = CreateImage("Window", parent, Color.black);
        SetRect(window.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900f, 540f));
        AddWhiteOutline(window.gameObject);

        Text header = CreateText("Header", window.transform, "Настройки", 48, TextAnchor.MiddleCenter);
        SetRect(header.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -70f), new Vector2(500f, 70f));

        CreateLabeledSlider(window.transform, "Громкость", new Vector2(0f, 70f), out volumeSlider, ApplyVolume);
        CreateLabeledSlider(window.transform, "Чувствительность мыши", new Vector2(0f, -70f), out sensitivitySlider, ApplyMouseSensitivity);

        Button backButton = CreateButton("BackButton", window.transform, "Назад");
        RectTransform backRect = backButton.GetComponent<RectTransform>();
        backRect.anchorMin = new Vector2(1f, 0f);
        backRect.anchorMax = new Vector2(1f, 0f);
        backRect.pivot = new Vector2(1f, 0f);
        backRect.anchoredPosition = new Vector2(-30f, 30f);
        backRect.sizeDelta = new Vector2(220f, 60f);
        backButton.onClick.AddListener(CloseSettings);
    }

    void CreateLabeledSlider(Transform parent, string label, Vector2 anchoredPosition, out Slider slider, UnityEngine.Events.UnityAction<float> onValueChanged)
    {
        GameObject container = new GameObject(label + "Container", typeof(RectTransform));
        container.transform.SetParent(parent, false);
        RectTransform containerRect = container.GetComponent<RectTransform>();
        SetRect(containerRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), anchoredPosition, new Vector2(720f, 120f));

        Text labelText = CreateText(label + "Label", container.transform, label, 30, TextAnchor.MiddleLeft);
        SetRect(labelText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -20f), new Vector2(0f, 40f));
        labelText.horizontalOverflow = HorizontalWrapMode.Wrap;

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

    void StartGame()
    {
        if (hasStarted)
            return;

        hasStarted = true;
        mainPanel.SetActive(false);
        settingsPanel.SetActive(false);
        StartCoroutine(FadeIntoGame());
    }

    IEnumerator FadeIntoGame()
    {
        float elapsed = 0f;
        while (elapsed < FadeDurationSeconds)
        {
            elapsed += Time.unscaledDeltaTime;
            blackOverlayGroup.alpha = 1f - Mathf.Clamp01(elapsed / FadeDurationSeconds);
            yield return null;
        }

        blackOverlayGroup.alpha = 0f;
        SetMenuState(false);
        Destroy(gameObject);
    }

    void OpenSettings()
    {
        mainPanel.SetActive(false);
        settingsPanel.SetActive(true);
    }

    void CloseSettings()
    {
        settingsPanel.SetActive(false);
        mainPanel.SetActive(true);
    }

    void ApplyVolume(float value)
    {
        AudioListener.volume = value;
    }

    void ApplyMouseSensitivity(float value)
    {
        float sensitivity = Mathf.Lerp(MinMouseSensitivity, MaxMouseSensitivity, value);
        foreach (MouseLook mouseLook in mouseLooks)
        {
            if (mouseLook != null)
                mouseLook.mouseSensitivity = sensitivity;
        }
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
        DontDestroyOnLoad(eventSystem);
    }

    GameObject CreatePanel(string name, Transform parent)
    {
        GameObject panel = new GameObject(name, typeof(RectTransform));
        panel.transform.SetParent(parent, false);
        RectTransform rect = panel.GetComponent<RectTransform>();
        StretchToFullScreen(rect);
        return panel;
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
        text.alignment = alignment;
        text.color = Color.white;
        text.supportRichText = false;
        return text;
    }

    Button CreateButton(string name, Transform parent, string label)
    {
        Image buttonImage = CreateImage(name, parent, Color.black);
        Button button = buttonImage.gameObject.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = Color.black;
        colors.highlightedColor = new Color(0.15f, 0.15f, 0.15f, 1f);
        colors.pressedColor = new Color(0.3f, 0.3f, 0.3f, 1f);
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = new Color(0.1f, 0.1f, 0.1f, 0.7f);
        button.colors = colors;
        AddWhiteOutline(button.gameObject);

        Text text = CreateText("Label", button.transform, label, 28, TextAnchor.MiddleCenter);
        StretchToFullScreen(text.rectTransform);
        return button;
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

    void AddWhiteOutline(GameObject target)
    {
        Outline outline = target.AddComponent<Outline>();
        outline.effectColor = Color.white;
        outline.effectDistance = new Vector2(2f, -2f);
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

public static class StartMenuBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void CreateStartMenu()
    {
        if (Object.FindFirstObjectByType<StartMenuController>() != null)
            return;

        GameObject menuObject = new GameObject("StartMenuController");
        menuObject.AddComponent<StartMenuController>();
    }
}