using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Happy Ending — Player đã uống bình cure (hết nhiễm độc) và thoát ra trước khi hết giờ.
// Dựng UI hoàn toàn bằng code lúc runtime, cùng phong cách "camera an ninh HELIX CORP"
// như IntroCinematicController.cs (scanline, REC nhấp nháy, đánh máy từng ký tự, ENTER/ESC).
public class HappyEndingController : MonoBehaviour
{
    [Header("Scene transition")]
    [SerializeField] string mainMenuScene = "MainMenu";
    [SerializeField, Min(0f)] float skipDelay = 0.5f;

    [Header("Audio")]
    [SerializeField] AudioClip endingMusic;
    [SerializeField, Range(0f, 1f)] float musicVolume = 0.55f;
    [SerializeField] AudioClip typewriterBeep;
    [SerializeField, Range(0f, 1f)] float typewriterBeepVolume = 0.5f;
    [SerializeField] AudioClip panelTransitionSound;
    [SerializeField, Range(0f, 1f)] float panelTransitionVolume = 1f;

    [Header("Ảnh nền camera an ninh (để trống = dùng nền màu)")]
    [SerializeField] Sprite statusBackdrop;
    [SerializeField] Sprite epilogueBackdrop;
    [SerializeField] Sprite titleBackdrop;

    [Header("Hiệu ứng đánh máy")]
    [SerializeField] float typeCharsPerSecond = 45f;

    readonly Color mint = new(0.25f, 0.9f, 0.6f, 1f);
    readonly Color paleMint = new(0.78f, 1f, 0.9f, 1f);
    readonly Color calmBlue = new(0.3f, 0.65f, 0.95f, 1f);

    CanvasGroup statusGroup;
    CanvasGroup epilogueGroup;
    CanvasGroup titleGroup;
    CanvasGroup[] panels;
    Text statusBody;
    Text epilogueBody;
    Text navigationText;
    Text panelCounter;
    string statusFullText;
    string epilogueFullText;
    Coroutine typewriterRoutine;
    bool isTyping;
    Image flashOverlay;
    RectTransform[] scanlines;

    float startedAt;
    int currentPanel;
    bool isTransitioning;
    bool isLeaving;

    readonly System.Collections.Generic.List<Image> recIndicators = new();

    void Awake()
    {
        Time.timeScale = 1f;
        startedAt = Time.unscaledTime;
        EnsureCamera();
        BuildPresentation();
        PlayMusic();
    }

    void Start()
    {
        currentPanel = 0;
        panels[currentPanel].alpha = 1f;
        StartTypewriterForCurrentPanel();
        UpdateNavigation();
    }

    void Update()
    {
        AnimateScanlines();
        AnimateRecIndicators();

        if (isLeaving || Time.unscaledTime - startedAt < skipDelay)
            return;

        if (Input.GetKeyDown(KeyCode.Escape))
            GoToMainMenu();

        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            if (isTyping)
                CompleteTypewriter();
            else
                AdvancePanel();
        }
    }

    void EnsureCamera()
    {
        if (Camera.main != null) return;

        var cameraObject = new GameObject("Ending Camera");
        cameraObject.tag = "MainCamera";
        var camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Color.black;
        camera.orthographic = true;
        cameraObject.transform.position = new Vector3(0f, 0f, -10f);
    }

    void BuildPresentation()
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        var canvasObject = new GameObject("Ending Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;

        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        RectTransform root = canvasObject.GetComponent<RectTransform>();
        CreatePanel("Void", root, Color.black, Vector2.zero, Vector2.one);

        var atmosphere = CreatePanel("Atmosphere", root, new Color(0.02f, 0.06f, 0.05f, 1f), Vector2.zero, Vector2.one);
        atmosphere.transform.SetAsLastSibling();

        CreatePanel("Top Letterbox", root, Color.black, new Vector2(0f, 0.91f), Vector2.one);
        CreatePanel("Bottom Letterbox", root, Color.black, Vector2.zero, new Vector2(1f, 0.09f));
        CreatePanel("Top Accent", root, new Color(mint.r, mint.g, mint.b, 0.35f), new Vector2(0f, 0.905f), new Vector2(1f, 0.908f));
        CreatePanel("Bottom Accent", root, new Color(mint.r, mint.g, mint.b, 0.2f), new Vector2(0f, 0.092f), new Vector2(1f, 0.095f));

        scanlines = new RectTransform[18];
        for (int i = 0; i < scanlines.Length; i++)
        {
            float y = 0.1f + i * 0.047f;
            Image line = CreatePanel("Scanline " + (i + 1), root, new Color(0.4f, 1f, 0.75f, i % 4 == 0 ? 0.03f : 0.01f), new Vector2(0f, y), new Vector2(1f, y));
            line.rectTransform.sizeDelta = new Vector2(0f, i % 4 == 0 ? 2f : 1f);
            scanlines[i] = line.rectTransform;
        }

        CreateText("System Label", root, font, "HELIX SECURE NETWORK  //  RELEASE LOG", 19, FontStyle.Bold,
            new Color(mint.r, mint.g, mint.b, 0.58f), TextAnchor.MiddleLeft,
            new Vector2(0.035f, 0.93f), new Vector2(0.65f, 0.985f));

        CreateText("Status", root, font, "CONNECTION STABLE", 17, FontStyle.Bold,
            new Color(calmBlue.r, calmBlue.g, calmBlue.b, 0.78f), TextAnchor.MiddleRight,
            new Vector2(0.65f, 0.93f), new Vector2(0.965f, 0.985f));

        panelCounter = CreateText("Panel Counter", root, font, "01 / 03", 17, FontStyle.Bold,
            new Color(mint.r, mint.g, mint.b, 0.65f), TextAnchor.MiddleLeft,
            new Vector2(0.035f, 0.015f), new Vector2(0.3f, 0.075f));

        navigationText = CreateText("Navigation", root, font, "ENTER  -  NEXT PANEL     ESC  -  SKIP", 17, FontStyle.Normal,
            new Color(0.72f, 0.86f, 0.82f, 0.7f), TextAnchor.MiddleRight,
            new Vector2(0.55f, 0.015f), new Vector2(0.965f, 0.075f));

        // Panel 0 — Status: containment lifted
        statusGroup = CreateGroup("Status Report", root);
        CreateBackdrop(statusGroup.transform as RectTransform, statusBackdrop, 0.45f);
        CreateRecIndicator(statusGroup.transform as RectTransform, font);
        CreateText("Status Heading", statusGroup.transform as RectTransform, font, "CONTAINMENT LIFTED  //  SUBJECT CLEARED", 26, FontStyle.Bold,
            mint, TextAnchor.MiddleLeft, new Vector2(0.14f, 0.68f), new Vector2(0.86f, 0.78f));
        CreateText("Status Location", statusGroup.transform as RectTransform, font, "FACILITY-07  /  RELEASE AUTHORIZED", 44, FontStyle.Bold,
            paleMint, TextAnchor.MiddleLeft, new Vector2(0.14f, 0.54f), new Vector2(0.86f, 0.69f));
        CreatePanel("Status Rule", statusGroup.transform as RectTransform, new Color(mint.r, mint.g, mint.b, 0.8f),
            new Vector2(0.14f, 0.525f), new Vector2(0.86f, 0.529f));
        statusBody = CreateText("Status Body", statusGroup.transform as RectTransform, font, string.Empty, 25, FontStyle.Normal,
            new Color(0.82f, 0.92f, 0.88f, 1f), TextAnchor.UpperLeft,
            new Vector2(0.14f, 0.2f), new Vector2(0.86f, 0.5f));
        statusBody.lineSpacing = 1.25f;
        statusBody.resizeTextForBestFit = false;
        statusFullText =
            "> NEXUS FLUID INFECTION NEUTRALIZED.\n" +
            "> QUARANTINE PROTOCOL LIFTED.\n" +
            "> SUBJECT AUTHORIZED FOR RELEASE.";

        // Panel 1 — Epilogue
        epilogueGroup = CreateGroup("Epilogue", root);
        CreateBackdrop(epilogueGroup.transform as RectTransform, epilogueBackdrop, 0.45f);
        CreateText("Epilogue Heading", epilogueGroup.transform as RectTransform, font, "SIX MONTHS LATER", 24, FontStyle.Bold,
            calmBlue, TextAnchor.MiddleLeft, new Vector2(0.14f, 0.68f), new Vector2(0.86f, 0.78f));
        CreateText("Epilogue Name", epilogueGroup.transform as RectTransform, font, "DR. ALEX RIVERA", 56, FontStyle.Bold,
            paleMint, TextAnchor.MiddleLeft, new Vector2(0.14f, 0.5f), new Vector2(0.86f, 0.69f));
        CreatePanel("Epilogue Rule", epilogueGroup.transform as RectTransform, new Color(calmBlue.r, calmBlue.g, calmBlue.b, 0.75f),
            new Vector2(0.14f, 0.485f), new Vector2(0.86f, 0.489f));
        epilogueFullText =
            "Alex walked out of Facility-07 into the sunlight for the first time in weeks.\n\n" +
            "No more Echoes. No more sirens.\n" +
            "Just an ordinary life, waiting to be lived again.";
        epilogueBody = CreateText("Epilogue Body", epilogueGroup.transform as RectTransform, font, string.Empty,
            26, FontStyle.Normal, new Color(0.82f, 0.92f, 0.88f, 1f), TextAnchor.UpperLeft,
            new Vector2(0.14f, 0.19f), new Vector2(0.86f, 0.46f));
        epilogueBody.lineSpacing = 1.25f;
        epilogueBody.resizeTextForBestFit = false;

        // Panel 2 — Title
        titleGroup = CreateGroup("Title Reveal", root);
        CreateBackdrop(titleGroup.transform as RectTransform, titleBackdrop, 0.5f);
        CreateText("Specimen", titleGroup.transform as RectTransform, font, "SUBJECT: DR. ALEX RIVERA  //  STATUS: CURED", 19, FontStyle.Bold,
            new Color(mint.r, mint.g, mint.b, 0.7f), TextAnchor.LowerCenter,
            new Vector2(0.1f, 0.67f), new Vector2(0.9f, 0.77f));
        Text title = CreateText("Title", titleGroup.transform as RectTransform, font, "ESCAPE COMPLETE", 74, FontStyle.Bold,
            paleMint, TextAnchor.MiddleCenter, new Vector2(0.08f, 0.34f), new Vector2(0.92f, 0.57f));
        var titleOutline = title.gameObject.AddComponent<Outline>();
        titleOutline.effectColor = new Color(mint.r, mint.g, mint.b, 0.4f);
        titleOutline.effectDistance = new Vector2(3f, -3f);
        CreateText("Tagline", titleGroup.transform as RectTransform, font, "THE ECHO FADES. ALEX REMAINS.", 21, FontStyle.Normal,
            new Color(mint.r, mint.g, mint.b, 0.82f), TextAnchor.UpperCenter,
            new Vector2(0.15f, 0.25f), new Vector2(0.85f, 0.38f));

        flashOverlay = CreatePanel("Glitch Flash", root, new Color(mint.r, mint.g, mint.b, 0f), Vector2.zero, Vector2.one);
        flashOverlay.raycastTarget = false;

        statusGroup.alpha = 0f;
        epilogueGroup.alpha = 0f;
        titleGroup.alpha = 0f;

        panels = new[] { statusGroup, epilogueGroup, titleGroup };
    }

    void AdvancePanel()
    {
        if (isLeaving || isTransitioning) return;

        if (currentPanel >= panels.Length - 1)
        {
            GoToMainMenu();
            return;
        }

        StartCoroutine(SwitchPanel(currentPanel + 1));
    }

    IEnumerator SwitchPanel(int nextPanel)
    {
        isTransitioning = true;
        flashOverlay.color = new Color(mint.r, mint.g, mint.b, 0.08f);
        AudioOneShot.Play(panelTransitionSound, transform.position, panelTransitionVolume);
        yield return Fade(panels[currentPanel], 1f, 0f, 0.18f);

        currentPanel = nextPanel;
        UpdateNavigation();
        StartTypewriterForCurrentPanel();
        yield return Fade(panels[currentPanel], 0f, 1f, 0.28f);
        flashOverlay.color = new Color(mint.r, mint.g, mint.b, 0f);
        isTransitioning = false;
    }

    void UpdateNavigation()
    {
        panelCounter.text = $"{currentPanel + 1:00} / {panels.Length:00}";
        navigationText.text = currentPanel == panels.Length - 1
            ? "ENTER  -  RETURN TO MENU     ESC  -  SKIP"
            : "ENTER  -  NEXT PANEL     ESC  -  SKIP";
    }

    void StartTypewriterForCurrentPanel()
    {
        if (typewriterRoutine != null) StopCoroutine(typewriterRoutine);

        if (currentPanel == 0)
            typewriterRoutine = StartCoroutine(TypeText(statusBody, statusFullText));
        else if (currentPanel == 1)
            typewriterRoutine = StartCoroutine(TypeText(epilogueBody, epilogueFullText));
        else
            typewriterRoutine = null;
    }

    IEnumerator TypeText(Text textComponent, string fullText)
    {
        isTyping = true;
        textComponent.text = string.Empty;

        float baseDelay = 1f / Mathf.Max(1f, typeCharsPerSecond);

        for (int i = 0; i < fullText.Length; i++)
        {
            textComponent.text = fullText.Substring(0, i + 1);

            char c = fullText[i];
            if (!char.IsWhiteSpace(c))
                AudioOneShot.Play(typewriterBeep, transform.position, typewriterBeepVolume);

            float delay = baseDelay;
            if (c == '\n') delay += 0.25f;
            else if (c == '.') delay += 0.12f;

            yield return new WaitForSecondsRealtime(delay);
        }

        isTyping = false;
        typewriterRoutine = null;
    }

    void CompleteTypewriter()
    {
        if (typewriterRoutine != null) StopCoroutine(typewriterRoutine);

        if (currentPanel == 0) statusBody.text = statusFullText;
        else if (currentPanel == 1) epilogueBody.text = epilogueFullText;

        isTyping = false;
        typewriterRoutine = null;
    }

    void PlayMusic()
    {
        if (endingMusic == null) return;

        var source = gameObject.AddComponent<AudioSource>();
        source.clip = endingMusic;
        source.volume = musicVolume;
        source.loop = true;
        source.playOnAwake = false;
        source.Play();
    }

    void AnimateScanlines()
    {
        if (scanlines == null) return;

        float shift = Mathf.Repeat(Time.unscaledTime * 12f, 52f);
        for (int i = 0; i < scanlines.Length; i++)
            scanlines[i].anchoredPosition = new Vector2(0f, -shift);
    }

    void AnimateRecIndicators()
    {
        if (recIndicators.Count == 0) return;

        bool on = Mathf.FloorToInt(Time.unscaledTime * 2f) % 2 == 0;
        float alpha = on ? 1f : 0.15f;

        foreach (var dot in recIndicators)
        {
            if (dot == null) continue;
            dot.color = new Color(mint.r, mint.g, mint.b, alpha);
        }
    }

    IEnumerator Fade(CanvasGroup group, float from, float to, float duration)
    {
        float elapsed = 0f;
        group.alpha = from;
        while (elapsed < duration && !isLeaving)
        {
            elapsed += Time.unscaledDeltaTime;
            group.alpha = Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, elapsed / duration));
            yield return null;
        }
        group.alpha = to;
    }

    void GoToMainMenu()
    {
        if (isLeaving) return;

        isLeaving = true;
        StopAllCoroutines();

        if (Application.CanStreamedLevelBeLoaded(mainMenuScene))
            SceneManager.LoadScene(mainMenuScene);
        else
            Debug.LogError($"HappyEnding could not load scene '{mainMenuScene}'. Add it to Build Settings.");
    }

    static CanvasGroup CreateGroup(string name, RectTransform parent)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasGroup));
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        Stretch(rect, Vector2.zero, Vector2.one);
        return go.GetComponent<CanvasGroup>();
    }

    static Image CreatePanel(string name, RectTransform parent, Color color, Vector2 anchorMin, Vector2 anchorMax,
        Sprite sprite = null)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        Stretch(rect, anchorMin, anchorMax);

        Image image = go.GetComponent<Image>();
        if (sprite != null)
        {
            image.sprite = sprite;
            image.color = Color.white;
        }
        else
        {
            image.color = color;
        }
        image.raycastTarget = false;
        return image;
    }

    static void CreateBackdrop(RectTransform parent, Sprite sprite, float scrimAlpha)
    {
        if (sprite == null) return;

        CreatePanel("Backdrop Photo", parent, Color.black, Vector2.zero, Vector2.one, sprite);
        CreatePanel("Backdrop Scrim", parent, new Color(0f, 0f, 0f, scrimAlpha), Vector2.zero, Vector2.one);
    }

    void CreateRecIndicator(RectTransform parent, Font uiFont)
    {
        Image dot = CreatePanel("Rec Dot", parent, mint, new Vector2(0.045f, 0.9f), new Vector2(0.062f, 0.928f));
        recIndicators.Add(dot);

        CreateText("Rec Label", parent, uiFont, "REC", 17, FontStyle.Bold, mint, TextAnchor.MiddleLeft,
            new Vector2(0.07f, 0.895f), new Vector2(0.16f, 0.93f));
    }

    static Text CreateText(string name, RectTransform parent, Font font, string value, int size, FontStyle style,
        Color color, TextAnchor alignment, Vector2 anchorMin, Vector2 anchorMax)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        Stretch(rect, anchorMin, anchorMax);

        Text text = go.GetComponent<Text>();
        text.font = font;
        text.text = value;
        text.fontSize = size;
        text.fontStyle = style;
        text.color = color;
        text.alignment = alignment;
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = Mathf.Max(12, size / 2);
        text.resizeTextMaxSize = size;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        text.raycastTarget = false;
        return text;
    }

    static void Stretch(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
    }
}
