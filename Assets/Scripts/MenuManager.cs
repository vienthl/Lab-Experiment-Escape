using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuManager : MonoBehaviour
{
    private const string MasterVolumeKey = "MasterVolume";
    private const string MusicVolumeKey = "MusicVolume";
    private const string SFXVolumeKey = "SFXVolume";
    private const string FullscreenKey = "Fullscreen";
    private const string ResolutionIndexKey = "ResolutionIndex";

    public GameObject optionsPanel;
    public Slider masterVolumeSlider;
    public Slider musicVolumeSlider;
    public Slider sfxVolumeSlider;
    public Toggle fullscreenToggle;
    public TMP_Dropdown resolutionDropdown;
    //public CanvasScaler canvasScaler;

    private readonly List<Resolution> resolutions = new();

    private void Awake()
    {
        CenterMenuBackground();

        if (optionsPanel != null)
        {
            Transform settingsLayer = optionsPanel.transform.parent;
            if (settingsLayer != null)
            {
                // The supplied options image already contains its own backdrop.
                // This legacy image otherwise covers the neon menu after closing.
                Transform legacyBackground = settingsLayer.Find("UI_Background");
                if (legacyBackground != null)
                {
                    legacyBackground.gameObject.SetActive(false);
                }
            }

            optionsPanel.SetActive(false);
        }
    }

    private void Start()
    {
        //if (canvasScaler == null)
        //{
        //    canvasScaler = FindFirstObjectByType<CanvasScaler>();
        //}

        BuildResolutionDropdown();
        LoadSavedSettings();
        RegisterCallbacks();
    }

    public void OpenOptions()
    {
        if (optionsPanel != null)
        {
            // The options artwork and controls live inside Settings_Panel. Bring
            // that whole layer above the full-screen neon menu before opening it.
            Transform settingsLayer = optionsPanel.transform.parent;
            if (settingsLayer != null)
            {
                settingsLayer.SetAsLastSibling();
            }

            optionsPanel.SetActive(true);
        }
    }

    private void CenterMenuBackground()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        Transform background = canvas != null
            ? canvas.transform.Find("NeonMenu/BlueNeonBackground")
            : null;

        if (background == null || !background.TryGetComponent(out Image backgroundImage))
        {
            return;
        }

        RectTransform backgroundRect = backgroundImage.rectTransform;
        backgroundRect.anchorMin = new Vector2(0.5f, 0.5f);
        backgroundRect.anchorMax = new Vector2(0.5f, 0.5f);
        backgroundRect.pivot = new Vector2(0.5f, 0.5f);
        backgroundRect.anchoredPosition = Vector2.zero;

        AspectRatioFitter aspectFitter = background.GetComponent<AspectRatioFitter>();
        if (aspectFitter == null)
        {
            aspectFitter = background.gameObject.AddComponent<AspectRatioFitter>();
        }

        aspectFitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        if (backgroundImage.sprite != null)
        {
            Rect spriteRect = backgroundImage.sprite.rect;
            aspectFitter.aspectRatio = spriteRect.width / spriteRect.height;
        }

        backgroundImage.preserveAspect = true;
    }

    public void CloseOptions()
    {
        SaveSettings();

        if (optionsPanel != null)
        {
            optionsPanel.SetActive(false);

            Transform settingsLayer = optionsPanel.transform.parent;
            Transform canvasRoot = settingsLayer != null ? settingsLayer.parent : null;
            Transform neonMenu = canvasRoot != null ? canvasRoot.Find("NeonMenu") : null;
            if (neonMenu != null)
            {
                neonMenu.SetAsLastSibling();
            }
        }
    }

    public void PlayGame()
    {
        SceneManager.LoadScene("Level1");
    }

    public void QuitGame()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    public void ResetData()
    {
        PlayerPrefs.DeleteAll();
        BuildResolutionDropdown();
        LoadSavedSettings();
    }

    public void SetMasterVolume(float value)
    {
        ApplyMasterVolume(value);
    }

    public void SetMusicVolume(float value)
    {
        ApplyMusicVolume(value);
    }

    public void SetSFXVolume(float value)
    {
        ApplySFXVolume(value);
    }

    public void SetFullscreen(bool isFullscreen)
    {
        ApplyFullscreen(isFullscreen);

    }

    public void SetResolution(int index)
    {
        ApplyResolution(index);
    }

    private void BuildResolutionDropdown()
    {
        if (resolutionDropdown == null)
        {
            return;
        }

        resolutions.Clear();
        resolutionDropdown.ClearOptions();

        Resolution[] availableResolutions = Screen.resolutions;
        List<string> options = new();
        for (int i = 0; i < availableResolutions.Length; i++)
        {
            Resolution resolution = availableResolutions[i];
            resolutions.Add(resolution);
            options.Add(resolution.width + " x " + resolution.height + " @ " + resolution.refreshRateRatio.value.ToString("0") + "Hz");
        }

        resolutionDropdown.AddOptions(options);
    }

    private void LoadSavedSettings()
    {
        float masterVolume = PlayerPrefs.GetFloat(MasterVolumeKey, 1f);
        float musicVolume = PlayerPrefs.GetFloat(MusicVolumeKey, 1f);
        float sfxVolume = PlayerPrefs.GetFloat(SFXVolumeKey, 1f);
        bool isFullscreen = PlayerPrefs.GetInt(FullscreenKey, 1) == 1;
        int resolutionIndex = PlayerPrefs.GetInt(ResolutionIndexKey, GetCurrentResolutionIndex());

        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.SetValueWithoutNotify(masterVolume);
        }

        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.SetValueWithoutNotify(musicVolume);
        }

        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.SetValueWithoutNotify(sfxVolume);
        }

        if (fullscreenToggle != null)
        {
            fullscreenToggle.SetIsOnWithoutNotify(isFullscreen);
        }

        if (resolutionDropdown != null && resolutions.Count > 0)
        {
            resolutionIndex = Mathf.Clamp(resolutionIndex, 0, resolutions.Count - 1);
            resolutionDropdown.SetValueWithoutNotify(resolutionIndex);
            resolutionDropdown.RefreshShownValue();
        }

        ApplyMasterVolume(masterVolume);
        ApplyMusicVolume(musicVolume);
        ApplySFXVolume(sfxVolume);
      

        if (resolutions.Count > 0)
        {
            ApplyResolution(Mathf.Clamp(resolutionIndex, 0, resolutions.Count - 1));
        }
        else
        {
            ApplyFullscreen(isFullscreen);
        }
    }

    private void RegisterCallbacks()
    {
        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.onValueChanged.RemoveAllListeners();
            masterVolumeSlider.onValueChanged.AddListener(ApplyMasterVolume);
        }

        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.onValueChanged.RemoveAllListeners();
            musicVolumeSlider.onValueChanged.AddListener(ApplyMusicVolume);
        }

        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.onValueChanged.RemoveAllListeners();
            sfxVolumeSlider.onValueChanged.AddListener(ApplySFXVolume);
        }

        if (fullscreenToggle != null)
        {
            fullscreenToggle.onValueChanged.RemoveAllListeners();
            fullscreenToggle.onValueChanged.AddListener(ApplyFullscreen);
        }

        if (resolutionDropdown != null)
        {
            resolutionDropdown.onValueChanged.RemoveAllListeners();
            resolutionDropdown.onValueChanged.AddListener(ApplyResolution);
        }
    }

    private void SaveSettings()
    {
        if (masterVolumeSlider != null)
        {
            PlayerPrefs.SetFloat(MasterVolumeKey, masterVolumeSlider.value);
        }

        if (musicVolumeSlider != null)
        {
            PlayerPrefs.SetFloat(MusicVolumeKey, musicVolumeSlider.value);
        }

        if (sfxVolumeSlider != null)
        {
            PlayerPrefs.SetFloat(SFXVolumeKey, sfxVolumeSlider.value);
        }

        if (fullscreenToggle != null)
        {
            PlayerPrefs.SetInt(FullscreenKey, fullscreenToggle.isOn ? 1 : 0);
        }

        if (resolutionDropdown != null)
        {
            PlayerPrefs.SetInt(ResolutionIndexKey, resolutionDropdown.value);
        }

        PlayerPrefs.Save();
    }

    private void ApplyMasterVolume(float value)
    {
        AudioListener.volume = Mathf.Clamp01(value);
    }

    private void ApplyMusicVolume(float value)
    {
        SetTaggedAudioSourceVolume("Music", value);
    }

    private void ApplySFXVolume(float value)
    {
        SetTaggedAudioSourceVolume("SFX", value);
    }

    private void ApplyFullscreen(bool isFullscreen)
    {
        if (resolutionDropdown != null && resolutions.Count > 0)
        {
            ApplyResolution(resolutionDropdown.value);
        }
        else
        {
            Screen.fullScreenMode = isFullscreen
                ? FullScreenMode.ExclusiveFullScreen
                : FullScreenMode.Windowed;
        }

        PlayerPrefs.SetInt(FullscreenKey, isFullscreen ? 1 : 0);
    }

    private void ApplyResolution(int index)
    {
        if (index < 0 || index >= resolutions.Count)
        {
            return;
        }

        Resolution resolution = resolutions[index];

        bool isFullscreen = fullscreenToggle != null && fullscreenToggle.isOn;

        FullScreenMode mode = isFullscreen
            ? FullScreenMode.ExclusiveFullScreen
            : FullScreenMode.Windowed;

        Screen.SetResolution(
            resolution.width,
            resolution.height,
            mode,
            resolution.refreshRateRatio
        );

        PlayerPrefs.SetInt(ResolutionIndexKey, index);
        PlayerPrefs.SetInt(FullscreenKey, isFullscreen ? 1 : 0);
    }

    private void SetTaggedAudioSourceVolume(string tagName, float volume)
    {
        float clampedVolume = Mathf.Clamp01(volume);
        AudioSource[] audioSources = FindObjectsByType<AudioSource>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < audioSources.Length; i++)
        {
            AudioSource source = audioSources[i];
            if (source != null && source.gameObject.tag == tagName)
            {
                source.volume = clampedVolume;
            }
        }
    }

    private int GetCurrentResolutionIndex()
    {
        for (int i = 0; i < resolutions.Count; i++)
        {
            Resolution resolution = resolutions[i];
            if (resolution.width == Screen.currentResolution.width &&
                resolution.height == Screen.currentResolution.height &&
                Mathf.Approximately((float)resolution.refreshRateRatio.value, (float)Screen.currentResolution.refreshRateRatio.value))
            {
                return i;
            }
        }

        for (int i = 0; i < resolutions.Count; i++)
        {
            Resolution resolution = resolutions[i];
            if (resolution.width == Screen.width && resolution.height == Screen.height)
            {
                return i;
            }
        }

        return 0;
    }
}
