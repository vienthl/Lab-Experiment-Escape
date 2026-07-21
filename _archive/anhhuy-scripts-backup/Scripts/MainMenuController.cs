using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Toàn bộ Main Menu dựng bằng code lúc runtime (theo đúng cách IntroCinematicController.cs
// đã làm) — không cần kéo-thả Canvas/Button gì trong Editor. Chỉ cần 1 GameObject rỗng
// gắn script này trong scene MainMenu.
public class MainMenuController : MonoBehaviour
{
    [Header("Scene")]
    [SerializeField] string playSceneName = "Level1";

    [Header("Âm thanh")]
    [SerializeField] AudioClip menuMusic;
    [SerializeField, Range(0f, 1f)] float musicVolume = 0.5f;
    [SerializeField] AudioClip clickSound;

    [Header("Hình ảnh (để trống = dùng màu phẳng mặc định)")]
    [SerializeField] Sprite backgroundImage;
    [SerializeField] Sprite titleLogo;
    [SerializeField] Sprite buttonSprite;
    [SerializeField] Sprite buttonHighlightSprite;
    [SerializeField] Sprite panelFrameSprite;

    const string MasterVolumeKey = "MasterVolume";
    const string MusicVolumeKey = "MusicVolume";
    const string SFXVolumeKey = "SFXVolume";
    const string FullscreenKey = "Fullscreen";
    const string ResolutionIndexKey = "ResolutionIndex";

    readonly Color bgColor = new Color(0.04f, 0.05f, 0.07f, 1f);
    readonly Color panelColor = new Color(0.08f, 0.1f, 0.12f, 0.96f);
    readonly Color buttonColor = new Color(0.13f, 0.17f, 0.2f, 1f);
    readonly Color accentColor = new Color(0.2f, 0.9f, 0.75f, 1f);
    readonly Color textColor = Color.white;

    Font font;
    AudioSource musicSource;
    AudioSource sfxSource;

    CanvasGroup mainGroup;
    CanvasGroup settingsGroup;
    Text playButtonLabel;

    Slider masterSlider, musicSlider, sfxSlider;
    Toggle fullscreenToggle;
    Text resolutionLabel;

    readonly List<Resolution> resolutions = new List<Resolution>();
    int resolutionIndex;

    void Awake()
    {
        EnsureEventSystem();
        EnsureAudioListener();
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.loop = true;
        musicSource.playOnAwake = false;

        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;

        BuildUI();
        BuildResolutionList();
        LoadSavedSettings();
        PlayMusic();
        ApplyPlayButtonContext();
    }

    // ── Dựng UI ──────────────────────────────────────────────────────────

    void BuildUI()
    {
        var canvasGo = new GameObject("Main Menu Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        RectTransform root = canvasGo.GetComponent<RectTransform>();

        CreatePanel("Background", root, bgColor, Vector2.zero, Vector2.one, backgroundImage);

        if (titleLogo != null)
        {
            // preserveAspect: ảnh logo có tỉ lệ ngang/dọc bất kỳ (không nhất thiết vuông) —
            // để true thì Unity tự co theo đúng tỉ lệ gốc, không kéo méo ảnh.
            var logoImage = CreatePanel("Title Logo", root, Color.white,
                new Vector2(0.22f, 0.72f), new Vector2(0.78f, 0.92f), titleLogo);
            logoImage.preserveAspect = true;

            // Vẫn hiện tên game bằng chữ bên dưới logo — logo chỉ là hình biểu tượng, không có chữ.
            CreateText("Title Text", root, font, "LAB EXPERIMENT ESCAPE", 32, FontStyle.Bold, accentColor,
                TextAnchor.MiddleCenter, new Vector2(0.1f, 0.65f), new Vector2(0.9f, 0.71f));
        }
        else
        {
            CreateText("Title", root, font, "LAB EXPERIMENT ESCAPE", 64, FontStyle.Bold, accentColor,
                TextAnchor.MiddleCenter, new Vector2(0.1f, 0.72f), new Vector2(0.9f, 0.86f));
        }

        BuildMainButtons(root);
        BuildSettingsPanel(root);

        settingsGroup.alpha = 0f;
        settingsGroup.interactable = false;
        settingsGroup.blocksRaycasts = false;
    }

    void BuildMainButtons(RectTransform root)
    {
        mainGroup = CreateGroup("Main Buttons", root);

        Button playButton = CreateButton("Play Button", mainGroup.transform as RectTransform, "PLAY",
            new Vector2(0.38f, 0.5f), new Vector2(0.62f, 0.58f), PlayGame);
        playButtonLabel = playButton.GetComponentInChildren<Text>();

        CreateButton("Settings Button", mainGroup.transform as RectTransform, "SETTINGS",
            new Vector2(0.38f, 0.4f), new Vector2(0.62f, 0.48f), OpenSettings);

        CreateButton("Quit Button", mainGroup.transform as RectTransform, "QUIT",
            new Vector2(0.38f, 0.3f), new Vector2(0.62f, 0.38f), QuitGame);
    }

    void BuildSettingsPanel(RectTransform root)
    {
        settingsGroup = CreateGroup("Settings Panel", root);

        CreatePanel("Panel Background", settingsGroup.transform as RectTransform, panelColor,
            new Vector2(0.25f, 0.12f), new Vector2(0.75f, 0.88f), panelFrameSprite);

        // Hạ thấp hơn nữa so với mép trên panel — khung panel_frame có viền/rivet dày phía trên,
        // để title quá sát (0.86) sẽ bị dính viền như đã thấy.
        CreateText("Settings Title", settingsGroup.transform as RectTransform, font, "SETTINGS", 40, FontStyle.Bold,
            accentColor, TextAnchor.MiddleCenter, new Vector2(0.25f, 0.72f), new Vector2(0.75f, 0.80f));

        masterSlider = BuildSliderRow(settingsGroup.transform as RectTransform, "Master Volume", 0.64f, ApplyMasterVolume);
        musicSlider  = BuildSliderRow(settingsGroup.transform as RectTransform, "Music Volume", 0.54f, ApplyMusicVolume);
        sfxSlider    = BuildSliderRow(settingsGroup.transform as RectTransform, "SFX Volume", 0.44f, ApplySFXVolume);

        BuildFullscreenRow(settingsGroup.transform as RectTransform, 0.34f);
        BuildResolutionRow(settingsGroup.transform as RectTransform, 0.24f);

        CreateButton("Back Button", settingsGroup.transform as RectTransform, "BACK",
            new Vector2(0.38f, 0.15f), new Vector2(0.62f, 0.21f), CloseSettings);
    }

    // Panel chiếm 0.25-0.75 màn hình — cột nội dung chừa lề ~0.08 mỗi bên (0.33-0.67) để
    // tránh viền dày của khung panel_frame (lần chỉnh trước 0.05 vẫn chưa đủ).
    Slider BuildSliderRow(RectTransform parent, string label, float y, UnityEngine.Events.UnityAction<float> onChange)
    {
        CreateText(label + " Label", parent, font, label, 22, FontStyle.Normal, textColor,
            TextAnchor.MiddleLeft, new Vector2(0.33f, y), new Vector2(0.48f, y + 0.06f));

        return CreateSlider(label + " Slider", parent, new Vector2(0.52f, y), new Vector2(0.67f, y + 0.06f), 1f, onChange);
    }

    void BuildFullscreenRow(RectTransform parent, float y)
    {
        CreateText("Fullscreen Label", parent, font, "Fullscreen", 22, FontStyle.Normal, textColor,
            TextAnchor.MiddleLeft, new Vector2(0.33f, y), new Vector2(0.48f, y + 0.06f));

        fullscreenToggle = CreateToggle("Fullscreen Toggle", parent, new Vector2(0.52f, y), new Vector2(0.56f, y + 0.06f), ApplyFullscreen);
    }

    void BuildResolutionRow(RectTransform parent, float y)
    {
        CreateText("Resolution Label", parent, font, "Resolution", 22, FontStyle.Normal, textColor,
            TextAnchor.MiddleLeft, new Vector2(0.33f, y), new Vector2(0.48f, y + 0.06f));

        CreateButton("Resolution Prev", parent, "<", new Vector2(0.52f, y), new Vector2(0.56f, y + 0.06f), () => ChangeResolution(-1));

        resolutionLabel = CreateText("Resolution Value", parent, font, "", 20, FontStyle.Normal, textColor,
            TextAnchor.MiddleCenter, new Vector2(0.565f, y), new Vector2(0.625f, y + 0.06f));

        CreateButton("Resolution Next", parent, ">", new Vector2(0.63f, y), new Vector2(0.67f, y + 0.06f), () => ChangeResolution(1));
    }

    // ── Hành động nút bấm ─────────────────────────────────────────────────

    void PlayGame()
    {
        // Đang Pause (Level1 vẫn còn load ở dưới, MainMenu chỉ đè lên trên) → nút PLAY giờ là RESUME,
        // không load lại Level1 mà chỉ gỡ MainMenu ra + chạy tiếp game đang đóng băng.
        if (GameManager.Instance != null && GameManager.Instance.CurrentContext == GameManager.GameContext.Paused)
        {
            GameManager.Instance.ResumeGame();
            return;
        }

        // Vừa chết (context Dead) → nút đang là RESTART: tiêu thụ trạng thái Dead rồi load Level1
        // y hệt Play bình thường (load mới hoàn toàn = chơi lại từ đầu level).
        GameManager.Instance?.ClearDeathContext();
        SceneManager.LoadScene(playSceneName);
    }

    // Đổi label nút PLAY theo ngữ cảnh: vừa mở từ Title (Fresh) → "PLAY", đang Pause → "RESUME",
    // vừa chết bấm phím xác nhận (Dead) → "RESTART".
    void ApplyPlayButtonContext()
    {
        if (playButtonLabel == null || GameManager.Instance == null) return;

        playButtonLabel.text = GameManager.Instance.CurrentContext switch
        {
            GameManager.GameContext.Paused => "RESUME",
            GameManager.GameContext.Dead => "RESTART",
            _ => "PLAY",
        };
    }

    void OpenSettings()
    {
        mainGroup.alpha = 0f;
        mainGroup.interactable = false;
        mainGroup.blocksRaycasts = false;

        settingsGroup.alpha = 1f;
        settingsGroup.interactable = true;
        settingsGroup.blocksRaycasts = true;
    }

    void CloseSettings()
    {
        SaveSettings();

        settingsGroup.alpha = 0f;
        settingsGroup.interactable = false;
        settingsGroup.blocksRaycasts = false;

        mainGroup.alpha = 1f;
        mainGroup.interactable = true;
        mainGroup.blocksRaycasts = true;
    }

    void QuitGame()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    void PlayClick()
    {
        if (clickSound != null)
            sfxSource.PlayOneShot(clickSound);
    }

    void PlayMusic()
    {
        if (menuMusic == null) return;
        musicSource.clip = menuMusic;
        // Master volume đã được AudioListener.volume nhân toàn cục (xem ApplyMasterVolume) — không cộng dồn ở đây nữa.
        musicSource.volume = musicVolume * PlayerPrefs.GetFloat(MusicVolumeKey, 1f);
        musicSource.Play();
    }

    // ── Settings: âm lượng / fullscreen / resolution ────────────────────

    void LoadSavedSettings()
    {
        float master = PlayerPrefs.GetFloat(MasterVolumeKey, 1f);
        float music  = PlayerPrefs.GetFloat(MusicVolumeKey, 1f);
        float sfx    = PlayerPrefs.GetFloat(SFXVolumeKey, 1f);
        bool isFullscreen = PlayerPrefs.GetInt(FullscreenKey, 1) == 1;

        masterSlider.SetValueWithoutNotify(master);
        musicSlider.SetValueWithoutNotify(music);
        sfxSlider.SetValueWithoutNotify(sfx);
        fullscreenToggle.SetIsOnWithoutNotify(isFullscreen);

        ApplyMasterVolume(master);
        ApplyMusicVolume(music);
        ApplySFXVolume(sfx);

        resolutionIndex = Mathf.Clamp(PlayerPrefs.GetInt(ResolutionIndexKey, GetCurrentResolutionIndex()), 0, resolutions.Count - 1);
        UpdateResolutionLabel();
        ApplyResolution(resolutionIndex);
    }

    void SaveSettings()
    {
        PlayerPrefs.SetFloat(MasterVolumeKey, masterSlider.value);
        PlayerPrefs.SetFloat(MusicVolumeKey, musicSlider.value);
        PlayerPrefs.SetFloat(SFXVolumeKey, sfxSlider.value);
        PlayerPrefs.SetInt(FullscreenKey, fullscreenToggle.isOn ? 1 : 0);
        PlayerPrefs.SetInt(ResolutionIndexKey, resolutionIndex);
        PlayerPrefs.Save();
    }

    void ApplyMasterVolume(float value)
    {
        AudioListener.volume = Mathf.Clamp01(value);
    }

    void ApplyMusicVolume(float value)
    {
        if (musicSource != null)
            musicSource.volume = Mathf.Clamp01(value) * musicVolume;
    }

    void ApplySFXVolume(float value)
    {
        if (sfxSource != null)
            sfxSource.volume = Mathf.Clamp01(value);
    }

    void ApplyFullscreen(bool isFullscreen)
    {
        Screen.fullScreenMode = isFullscreen ? FullScreenMode.ExclusiveFullScreen : FullScreenMode.Windowed;
    }

    void BuildResolutionList()
    {
        resolutions.Clear();
        resolutions.AddRange(Screen.resolutions);
    }

    void ChangeResolution(int delta)
    {
        if (resolutions.Count == 0) return;

        resolutionIndex = (resolutionIndex + delta + resolutions.Count) % resolutions.Count;
        UpdateResolutionLabel();
        ApplyResolution(resolutionIndex);
    }

    void UpdateResolutionLabel()
    {
        if (resolutionLabel == null || resolutions.Count == 0) return;
        Resolution r = resolutions[resolutionIndex];
        resolutionLabel.text = $"{r.width} x {r.height}";
    }

    void ApplyResolution(int index)
    {
        if (index < 0 || index >= resolutions.Count) return;

        Resolution r = resolutions[index];
        bool isFullscreen = fullscreenToggle != null && fullscreenToggle.isOn;
        Screen.SetResolution(r.width, r.height,
            isFullscreen ? FullScreenMode.ExclusiveFullScreen : FullScreenMode.Windowed,
            r.refreshRateRatio);
    }

    int GetCurrentResolutionIndex()
    {
        for (int i = 0; i < resolutions.Count; i++)
        {
            if (resolutions[i].width == Screen.currentResolution.width &&
                resolutions[i].height == Screen.currentResolution.height)
                return i;
        }
        return 0;
    }

    // ── EventSystem (bắt buộc để Button nhận click) ─────────────────────

    static void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null) return;

        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }

    // Nhạc menu/click sound cần 1 AudioListener trong scene mới phát ra tiếng —
    // nếu scene không còn Camera nào (Canvas ScreenSpaceOverlay không bắt buộc phải có Camera),
    // gắn luôn AudioListener lên chính object này cho chắc.
    void EnsureAudioListener()
    {
        if (FindFirstObjectByType<AudioListener>() != null) return;

        gameObject.AddComponent<AudioListener>();
    }

    // ── Hàm dựng UI dùng chung ───────────────────────────────────────────

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
            image.type = Image.Type.Simple;
        }
        else
        {
            image.color = color;
        }

        return image;
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
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        return text;
    }

    Button CreateButton(string name, RectTransform parent, string label, Vector2 anchorMin, Vector2 anchorMax,
        UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        Stretch(rect, anchorMin, anchorMax);

        Image image = go.GetComponent<Image>();
        Button button = go.GetComponent<Button>();
        button.targetGraphic = image;

        if (buttonSprite != null)
        {
            image.sprite = buttonSprite;
            image.color = Color.white;
            image.type = Image.Type.Sliced;

            button.transition = Selectable.Transition.SpriteSwap;
            SpriteState state = button.spriteState;
            state.highlightedSprite = buttonHighlightSprite != null ? buttonHighlightSprite : buttonSprite;
            state.pressedSprite = buttonHighlightSprite != null ? buttonHighlightSprite : buttonSprite;
            button.spriteState = state;
        }
        else
        {
            image.color = buttonColor;
            ColorBlock colors = button.colors;
            colors.normalColor = buttonColor;
            colors.highlightedColor = accentColor;
            colors.pressedColor = accentColor * 0.75f;
            colors.selectedColor = buttonColor;
            button.colors = colors;
        }

        CreateText(name + " Label", rect, font, label, 24, FontStyle.Bold, textColor,
            TextAnchor.MiddleCenter, Vector2.zero, Vector2.one);

        button.onClick.AddListener(() => { PlayClick(); onClick?.Invoke(); });
        return button;
    }

    Slider CreateSlider(string name, RectTransform parent, Vector2 anchorMin, Vector2 anchorMax, float value,
        UnityEngine.Events.UnityAction<float> onChange)
    {
        var go = new GameObject(name, typeof(RectTransform));
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        Stretch(rect, anchorMin, anchorMax);

        Slider slider = go.AddComponent<Slider>();
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 1f;

        CreatePanel(name + " Background", rect, new Color(0.2f, 0.2f, 0.22f, 1f), Vector2.zero, Vector2.one);

        var fillAreaGo = new GameObject("Fill Area", typeof(RectTransform));
        RectTransform fillAreaRect = fillAreaGo.GetComponent<RectTransform>();
        fillAreaRect.SetParent(rect, false);
        Stretch(fillAreaRect, Vector2.zero, Vector2.one);

        var fillGo = new GameObject("Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform fillRect = fillGo.GetComponent<RectTransform>();
        fillRect.SetParent(fillAreaRect, false);
        fillRect.anchorMin = new Vector2(0f, 0f);
        fillRect.anchorMax = new Vector2(0f, 1f);
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        fillGo.GetComponent<Image>().color = accentColor;

        var handleAreaGo = new GameObject("Handle Slide Area", typeof(RectTransform));
        RectTransform handleAreaRect = handleAreaGo.GetComponent<RectTransform>();
        handleAreaRect.SetParent(rect, false);
        Stretch(handleAreaRect, Vector2.zero, Vector2.one);

        var handleGo = new GameObject("Handle", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform handleRect = handleGo.GetComponent<RectTransform>();
        handleRect.SetParent(handleAreaRect, false);
        handleRect.sizeDelta = new Vector2(14f, 0f);
        handleRect.anchorMin = new Vector2(0f, 0f);
        handleRect.anchorMax = new Vector2(0f, 1f);
        Image handleImage = handleGo.GetComponent<Image>();
        handleImage.color = Color.white;

        slider.fillRect = fillRect;
        slider.handleRect = handleRect;
        slider.targetGraphic = handleImage;
        slider.SetValueWithoutNotify(value);
        slider.onValueChanged.AddListener(onChange);

        return slider;
    }

    Toggle CreateToggle(string name, RectTransform parent, Vector2 anchorMin, Vector2 anchorMax,
        UnityEngine.Events.UnityAction<bool> onChange)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Toggle));
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        Stretch(rect, anchorMin, anchorMax);

        Image bgImage = go.GetComponent<Image>();
        bgImage.color = new Color(0.2f, 0.2f, 0.22f, 1f);

        var checkGo = new GameObject("Checkmark", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform checkRect = checkGo.GetComponent<RectTransform>();
        checkRect.SetParent(rect, false);
        Stretch(checkRect, new Vector2(0.2f, 0.2f), new Vector2(0.8f, 0.8f));
        Image checkImage = checkGo.GetComponent<Image>();
        checkImage.color = accentColor;

        Toggle toggle = go.GetComponent<Toggle>();
        toggle.targetGraphic = bgImage;
        toggle.graphic = checkImage;
        toggle.onValueChanged.AddListener(onChange);

        return toggle;
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
