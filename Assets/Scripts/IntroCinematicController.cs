using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Self-contained promotional intro for Lab Experience Escape.
/// The complete presentation is created at runtime so the IntroCinematic scene
/// has no dependencies on objects from the menu or gameplay scenes.
/// </summary>
public class IntroCinematicController : MonoBehaviour
{
    [Header("Scene transition")]
    [SerializeField] string mainMenuScene = "MainMenu";
    [SerializeField, Min(0f)] float skipDelay = 0.5f;

    [Header("Audio")]
    [SerializeField] AudioClip introMusic;
    [SerializeField, Range(0f, 1f)] float musicVolume = 0.55f;

    [Header("Ảnh nền camera an ninh (để trống = dùng nền màu như cũ)")]
    [SerializeField] Sprite logoBackdrop;
    [SerializeField] Sprite incidentPhoto;
    [SerializeField] Sprite survivorPhoto;
    [SerializeField] Sprite titleBackdrop;

    [Header("Logo Helix Corp (để trống = dùng ô vuông + chữ H như cũ)")]
    [SerializeField] Sprite emblemLogo;

    [Header("Hiệu ứng đánh máy")]
    [SerializeField] float typeCharsPerSecond = 45f;

    readonly Color teal = new(0.13f, 0.95f, 0.82f, 1f);
    readonly Color paleTeal = new(0.72f, 1f, 0.95f, 1f);
    readonly Color warningRed = new(1f, 0.17f, 0.2f, 1f);

    CanvasGroup logoGroup;
    CanvasGroup incidentGroup;
    CanvasGroup survivorGroup;
    CanvasGroup titleGroup;
    CanvasGroup[] panels;
    Text logoMark;
    Text logoGhostRed;
    Text logoGhostBlue;
    Text incidentBody;
    Text survivorBody;
    Text navigationText;
    Text panelCounter;
    string incidentFullText;
    string survivorFullText;
    Coroutine typewriterRoutine;
    bool isTyping;
    Image flashOverlay;
    RectTransform[] scanlines;
    Vector2 logoHome;

    float startedAt;
    float nextGlitchAt;
    int currentPanel;
    bool isTransitioning;
    bool isLeaving;

    readonly List<Image> recIndicators = new List<Image>();

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
        UpdateNavigation();
    }

    void Update()
    {
        AnimateScanlines();
        AnimateLogoGlitch();
        AnimateRecIndicators();

        if (isLeaving || Time.unscaledTime - startedAt < skipDelay)
            return;

        if (Input.GetKeyDown(KeyCode.Escape))
            GoToMainMenu();

        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            // Bấm Enter lúc chữ đang gõ dở → hiện hết ngay (không nhảy panel);
            // bấm lần nữa lúc đã gõ xong mới thật sự qua panel kế — đúng kiểu visual novel.
            if (isTyping)
                CompleteTypewriter();
            else
                AdvancePanel();
        }
    }

    void EnsureCamera()
    {
        if (Camera.main != null)
            return;

        var cameraObject = new GameObject("Cinematic Camera");
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

        var canvasObject = new GameObject("Cinematic Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
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

        var atmosphere = CreatePanel("Atmosphere", root, new Color(0.01f, 0.055f, 0.07f, 1f), Vector2.zero, Vector2.one);
        atmosphere.transform.SetAsLastSibling();

        CreatePanel("Top Letterbox", root, Color.black, new Vector2(0f, 0.91f), Vector2.one);
        CreatePanel("Bottom Letterbox", root, Color.black, Vector2.zero, new Vector2(1f, 0.09f));
        CreatePanel("Top Accent", root, new Color(teal.r, teal.g, teal.b, 0.35f), new Vector2(0f, 0.905f), new Vector2(1f, 0.908f));
        CreatePanel("Bottom Accent", root, new Color(teal.r, teal.g, teal.b, 0.2f), new Vector2(0f, 0.092f), new Vector2(1f, 0.095f));

        scanlines = new RectTransform[18];
        for (int i = 0; i < scanlines.Length; i++)
        {
            float y = 0.1f + i * 0.047f;
            Image line = CreatePanel("Scanline " + (i + 1), root, new Color(0.25f, 1f, 0.9f, i % 4 == 0 ? 0.035f : 0.012f), new Vector2(0f, y), new Vector2(1f, y));
            line.rectTransform.sizeDelta = new Vector2(0f, i % 4 == 0 ? 2f : 1f);
            scanlines[i] = line.rectTransform;
        }

        CreateText("System Label", root, font, "HELIX SECURE NETWORK  //  FACILITY-07", 19, FontStyle.Bold,
            new Color(teal.r, teal.g, teal.b, 0.58f), TextAnchor.MiddleLeft,
            new Vector2(0.035f, 0.93f), new Vector2(0.65f, 0.985f));

        Text status = CreateText("Status", root, font, "CONNECTION UNSTABLE", 17, FontStyle.Bold,
            new Color(warningRed.r, warningRed.g, warningRed.b, 0.78f), TextAnchor.MiddleRight,
            new Vector2(0.65f, 0.93f), new Vector2(0.965f, 0.985f));
        status.horizontalOverflow = HorizontalWrapMode.Overflow;

        panelCounter = CreateText("Panel Counter", root, font, "01 / 04", 17, FontStyle.Bold,
            new Color(teal.r, teal.g, teal.b, 0.65f), TextAnchor.MiddleLeft,
            new Vector2(0.035f, 0.015f), new Vector2(0.3f, 0.075f));

        navigationText = CreateText("Navigation", root, font, "ENTER  -  NEXT PANEL     ESC  -  SKIP", 17, FontStyle.Normal,
            new Color(0.72f, 0.82f, 0.82f, 0.7f), TextAnchor.MiddleRight,
            new Vector2(0.55f, 0.015f), new Vector2(0.965f, 0.075f));

        logoGroup = CreateGroup("Helix Corporation", root);
        CreateBackdrop(logoGroup.transform as RectTransform, logoBackdrop, 0.55f);
        Image emblem = CreatePanel("Emblem", logoGroup.transform as RectTransform, new Color(teal.r, teal.g, teal.b, 0.92f),
            new Vector2(0.446f, 0.57f), new Vector2(0.554f, 0.76f), emblemLogo);
        var emblemOutline = emblem.gameObject.AddComponent<Outline>();
        emblemOutline.effectColor = new Color(0.6f, 1f, 0.95f, 0.65f);
        emblemOutline.effectDistance = new Vector2(3f, -3f);

        if (emblemLogo != null)
            emblem.preserveAspect = true; // ảnh logo thật — giữ đúng tỉ lệ gốc, không kéo méo
        else
            CreateText("Emblem H", emblem.rectTransform, font, "H", 112, FontStyle.Bold, new Color(0.015f, 0.08f, 0.09f, 1f),
                TextAnchor.MiddleCenter, Vector2.zero, Vector2.one);

        logoGhostRed = CreateText("Logo Red Ghost", logoGroup.transform as RectTransform, font, "HELIX CORP", 66, FontStyle.Bold,
            new Color(1f, 0.08f, 0.16f, 0.28f), TextAnchor.MiddleCenter,
            new Vector2(0.15f, 0.35f), new Vector2(0.85f, 0.55f));
        logoGhostBlue = CreateText("Logo Blue Ghost", logoGroup.transform as RectTransform, font, "HELIX CORP", 66, FontStyle.Bold,
            new Color(0.05f, 0.5f, 1f, 0.3f), TextAnchor.MiddleCenter,
            new Vector2(0.15f, 0.35f), new Vector2(0.85f, 0.55f));
        logoMark = CreateText("Logo", logoGroup.transform as RectTransform, font, "HELIX CORP", 66, FontStyle.Bold,
            paleTeal, TextAnchor.MiddleCenter,
            new Vector2(0.15f, 0.35f), new Vector2(0.85f, 0.55f));
        logoHome = logoMark.rectTransform.anchoredPosition;
        CreateText("Motto", logoGroup.transform as RectTransform, font, "ADVANCING HUMAN POTENTIAL", 18, FontStyle.Normal,
            new Color(teal.r, teal.g, teal.b, 0.82f), TextAnchor.UpperCenter,
            new Vector2(0.2f, 0.28f), new Vector2(0.8f, 0.39f));

        incidentGroup = CreateGroup("Incident Report", root);
        CreateBackdrop(incidentGroup.transform as RectTransform, incidentPhoto, 0.45f);
        CreateRecIndicator(incidentGroup.transform as RectTransform, font);
        CreateText("Incident Heading", incidentGroup.transform as RectTransform, font, "CRITICAL EVENT  //  05.17.2026", 28, FontStyle.Bold,
            warningRed, TextAnchor.MiddleLeft, new Vector2(0.14f, 0.68f), new Vector2(0.86f, 0.78f));
        CreateText("Incident Location", incidentGroup.transform as RectTransform, font, "FACILITY-07  /  PROJECT ELYSIUM", 48, FontStyle.Bold,
            paleTeal, TextAnchor.MiddleLeft, new Vector2(0.14f, 0.54f), new Vector2(0.86f, 0.69f));
        CreatePanel("Incident Rule", incidentGroup.transform as RectTransform, new Color(teal.r, teal.g, teal.b, 0.8f),
            new Vector2(0.14f, 0.525f), new Vector2(0.86f, 0.529f));
        incidentBody = CreateText("Incident Body", incidentGroup.transform as RectTransform, font, string.Empty, 25, FontStyle.Normal,
            new Color(0.78f, 0.9f, 0.9f, 1f), TextAnchor.UpperLeft,
            new Vector2(0.14f, 0.2f), new Vector2(0.86f, 0.5f));
        incidentBody.lineSpacing = 1.25f;
        incidentBody.resizeTextForBestFit = false; // cỡ chữ cố định — tránh nhảy size khi gõ dần từng ký tự
        incidentFullText =
            "> NEURAL LINK SYNCHRONIZATION FAILED.\n" +
            "> NEXUS FLUID CONTAINMENT BREACH DETECTED.\n" +
            "> ECHO ENTITIES ACTIVE. FACILITY LOCKDOWN ENGAGED.";

        survivorGroup = CreateGroup("Survivor Record", root);
        CreateBackdrop(survivorGroup.transform as RectTransform, survivorPhoto, 0.45f);
        CreateRecIndicator(survivorGroup.transform as RectTransform, font);
        CreateText("Record Heading", survivorGroup.transform as RectTransform, font, "RECOVERED LIFE-SIGN  //  CONTAINMENT WING", 24, FontStyle.Bold,
            teal, TextAnchor.MiddleLeft, new Vector2(0.14f, 0.68f), new Vector2(0.86f, 0.78f));
        CreateText("Survivor Name", survivorGroup.transform as RectTransform, font, "DR. ALEX RIVERA", 58, FontStyle.Bold,
            paleTeal, TextAnchor.MiddleLeft, new Vector2(0.14f, 0.5f), new Vector2(0.86f, 0.69f));
        CreatePanel("Survivor Rule", survivorGroup.transform as RectTransform, new Color(warningRed.r, warningRed.g, warningRed.b, 0.75f),
            new Vector2(0.14f, 0.485f), new Vector2(0.86f, 0.489f));
        survivorFullText =
            "The experiment created living memory constructs called Echo Entities.\n" +
            "They now roam the sealed halls of Facility-07.\n\n" +
            "Alex is the sole survivor. Escape before the Nexus remembers you.";
        survivorBody = CreateText("Survivor Body", survivorGroup.transform as RectTransform, font, string.Empty,
            26, FontStyle.Normal, new Color(0.78f, 0.9f, 0.9f, 1f), TextAnchor.UpperLeft,
            new Vector2(0.14f, 0.19f), new Vector2(0.86f, 0.46f));
        survivorBody.lineSpacing = 1.25f;
        survivorBody.resizeTextForBestFit = false; // giữ cố định, cùng lý do như incidentBody

        titleGroup = CreateGroup("Title Reveal", root);
        CreateBackdrop(titleGroup.transform as RectTransform, titleBackdrop, 0.5f);
        CreateText("Specimen", titleGroup.transform as RectTransform, font, "SUBJECT: DR. ALEX RIVERA  //  STATUS: SOLE SURVIVOR", 19, FontStyle.Bold,
            new Color(teal.r, teal.g, teal.b, 0.7f), TextAnchor.LowerCenter,
            new Vector2(0.1f, 0.67f), new Vector2(0.9f, 0.77f));
        CreateText("Lab", titleGroup.transform as RectTransform, font, "LAB", 42, FontStyle.Bold,
            warningRed, TextAnchor.LowerCenter, new Vector2(0.1f, 0.53f), new Vector2(0.9f, 0.68f));
        Text title = CreateText("Title", titleGroup.transform as RectTransform, font, "EXPERIENCE ESCAPE", 76, FontStyle.Bold,
            paleTeal, TextAnchor.MiddleCenter, new Vector2(0.08f, 0.34f), new Vector2(0.92f, 0.57f));
        var titleOutline = title.gameObject.AddComponent<Outline>();
        titleOutline.effectColor = new Color(teal.r, teal.g, teal.b, 0.38f);
        titleOutline.effectDistance = new Vector2(3f, -3f);
        CreateText("Tagline", titleGroup.transform as RectTransform, font, "EVERY MEMORY LEAVES AN ECHO", 21, FontStyle.Normal,
            new Color(teal.r, teal.g, teal.b, 0.82f), TextAnchor.UpperCenter,
            new Vector2(0.15f, 0.25f), new Vector2(0.85f, 0.38f));

        flashOverlay = CreatePanel("Glitch Flash", root, new Color(teal.r, teal.g, teal.b, 0f), Vector2.zero, Vector2.one);
        flashOverlay.raycastTarget = false;

        logoGroup.alpha = 0f;
        incidentGroup.alpha = 0f;
        survivorGroup.alpha = 0f;
        titleGroup.alpha = 0f;

        panels = new[] { logoGroup, incidentGroup, survivorGroup, titleGroup };
    }

    void AdvancePanel()
    {
        if (isLeaving || isTransitioning)
            return;

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
        flashOverlay.color = new Color(teal.r, teal.g, teal.b, 0.08f);
        yield return Fade(panels[currentPanel], 1f, 0f, 0.18f);

        currentPanel = nextPanel;
        UpdateNavigation();
        StartTypewriterForCurrentPanel();
        yield return Fade(panels[currentPanel], 0f, 1f, 0.28f);
        flashOverlay.color = new Color(teal.r, teal.g, teal.b, 0f);
        isTransitioning = false;
    }

    void UpdateNavigation()
    {
        panelCounter.text = $"{currentPanel + 1:00} / {panels.Length:00}";
        navigationText.text = currentPanel == panels.Length - 1
            ? "ENTER  -  OPEN MAIN MENU     ESC  -  SKIP"
            : "ENTER  -  NEXT PANEL     ESC  -  SKIP";
    }

    // Panel Incident (index 1) và Survivor (index 2) có đoạn log/hồ sơ dài → gõ dần từng ký tự.
    // Panel Logo/Title không có đoạn văn bản dài nên bỏ qua (typewriterRoutine = null).
    void StartTypewriterForCurrentPanel()
    {
        if (typewriterRoutine != null)
            StopCoroutine(typewriterRoutine);

        if (currentPanel == 1)
            typewriterRoutine = StartCoroutine(TypeText(incidentBody, incidentFullText));
        else if (currentPanel == 2)
            typewriterRoutine = StartCoroutine(TypeText(survivorBody, survivorFullText));
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
            float delay = baseDelay;
            if (c == '\n') delay += 0.25f;      // ngắt dòng — dừng lâu hơn 1 nhịp
            else if (c == '.') delay += 0.12f;  // hết câu — dừng thêm chút

            yield return new WaitForSecondsRealtime(delay);
        }

        isTyping = false;
        typewriterRoutine = null;
    }

    // Bấm Enter giữa lúc đang gõ → hiện hết chữ ngay lập tức, không nhảy panel (kiểu visual novel).
    void CompleteTypewriter()
    {
        if (typewriterRoutine != null)
            StopCoroutine(typewriterRoutine);

        if (currentPanel == 1) incidentBody.text = incidentFullText;
        else if (currentPanel == 2) survivorBody.text = survivorFullText;

        isTyping = false;
        typewriterRoutine = null;
    }

    void PlayMusic()
    {
        if (introMusic == null)
            return;

        var source = gameObject.AddComponent<AudioSource>();
        source.clip = introMusic;
        source.volume = musicVolume;
        source.loop = true;
        source.playOnAwake = false;
        source.Play();
    }

    void AnimateScanlines()
    {
        if (scanlines == null)
            return;

        float shift = Mathf.Repeat(Time.unscaledTime * 16f, 52f);
        for (int i = 0; i < scanlines.Length; i++)
            scanlines[i].anchoredPosition = new Vector2(0f, -shift);
    }

    void AnimateLogoGlitch()
    {
        if (logoGroup == null || logoGroup.alpha <= 0.01f || Time.unscaledTime < nextGlitchAt)
            return;

        nextGlitchAt = Time.unscaledTime + Random.Range(0.045f, 0.14f);
        logoMark.rectTransform.anchoredPosition = logoHome + Random.insideUnitCircle * 2.5f;
        logoGhostRed.rectTransform.anchoredPosition = logoHome + new Vector2(Random.Range(-7f, -2f), Random.Range(-2f, 2f));
        logoGhostBlue.rectTransform.anchoredPosition = logoHome + new Vector2(Random.Range(2f, 7f), Random.Range(-2f, 2f));
    }

    // Nhấp nháy đều các chấm "REC" (2 lần/giây) — chỉ những panel có ảnh camera mới có chấm trong danh sách.
    void AnimateRecIndicators()
    {
        if (recIndicators.Count == 0) return;

        bool on = Mathf.FloorToInt(Time.unscaledTime * 2f) % 2 == 0;
        float alpha = on ? 1f : 0.15f;

        foreach (var dot in recIndicators)
        {
            if (dot == null) continue;
            dot.color = new Color(warningRed.r, warningRed.g, warningRed.b, alpha);
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
        if (isLeaving)
            return;

        isLeaving = true;
        StopAllCoroutines();

        if (Application.CanStreamedLevelBeLoaded(mainMenuScene))
            SceneManager.LoadScene(mainMenuScene);
        else
            Debug.LogError($"Intro cinematic could not load scene '{mainMenuScene}'. Add it to Build Settings.");
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

    // Ảnh nền "camera an ninh" cho 1 panel — phủ kín + lớp tối (scrim) để chữ đè lên vẫn đọc được.
    static void CreateBackdrop(RectTransform parent, Sprite sprite, float scrimAlpha)
    {
        if (sprite == null) return;

        CreatePanel("Backdrop Photo", parent, Color.black, Vector2.zero, Vector2.one, sprite);
        CreatePanel("Backdrop Scrim", parent, new Color(0f, 0f, 0f, scrimAlpha), Vector2.zero, Vector2.one);
    }

    // Chấm đỏ + chữ "REC" nhấp nháy góc trên-trái, đúng kiểu clip camera an ninh phục hồi được.
    void CreateRecIndicator(RectTransform parent, Font uiFont)
    {
        Image dot = CreatePanel("Rec Dot", parent, warningRed, new Vector2(0.045f, 0.9f), new Vector2(0.062f, 0.928f));
        recIndicators.Add(dot);

        CreateText("Rec Label", parent, uiFont, "REC", 17, FontStyle.Bold, warningRed, TextAnchor.MiddleLeft,
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
