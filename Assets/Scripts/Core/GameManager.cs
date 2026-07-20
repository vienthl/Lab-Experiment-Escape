using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// Singleton sống xuyên scene — giữ SavedData (đồ mang theo giữa các level) và
// xử lý hoàn thành level / chuyển scene. Chỉ cần tồn tại ở 1 scene bất kỳ (vd MainMenu),
// DontDestroyOnLoad tự giữ nó qua các lần load scene sau.
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public SaveSystem.SaveData SavedData { get; private set; }

    [Header("Chuyển cảnh (fade tối → sáng)")]
    [Tooltip("Thời gian fade mỗi chiều (giây) — tổng thời gian tối màn hình = 2 lần số này")]
    public float fadeDuration = 0.5f;

    [Header("Pause")]
    [Tooltip("Tên scene gameplay được phép bấm ESC để Pause — mở rộng thêm khi có Level2/3")]
    public string[] pausableScenes = { "Level1" };

    public enum GameContext { Fresh, Paused, Dead }
    public GameContext CurrentContext { get; private set; } = GameContext.Fresh;

    bool isLevelComplete;
    float fadeAlpha;
    Texture2D fadeTex;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        SavedData = SaveSystem.Load();
    }

    void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Escape)) return;

        if (CurrentContext == GameContext.Fresh && IsCurrentScenePausable())
            PauseGame();
        else if (CurrentContext == GameContext.Paused)
            ResumeGame();
    }

    bool IsCurrentScenePausable()
    {
        string active = SceneManager.GetActiveScene().name;
        foreach (var name in pausableScenes)
            if (name == active) return true;
        return false;
    }

    // ESC lúc đang chơi Level1 → đóng băng gameplay, mở đè MainMenu lên trên (Additive) làm màn Pause,
    // giữ nguyên state Level1 (vị trí player, quái, đồ đã nhặt...) không mất gì khi Resume.
    public void PauseGame()
    {
        if (CurrentContext != GameContext.Fresh) return;

        CurrentContext = GameContext.Paused;
        Time.timeScale = 0f;
        SceneManager.LoadScene("MainMenu", LoadSceneMode.Additive);
    }

    // Gọi từ nút PLAY (lúc đã đổi label thành "RESUME") trong MainMenuController.
    public void ResumeGame()
    {
        if (CurrentContext != GameContext.Paused) return;

        CurrentContext = GameContext.Fresh;
        Time.timeScale = 1f;
        SceneManager.UnloadSceneAsync("MainMenu");
    }

    // Gọi từ GameOverUI khi player chết và bấm phím xác nhận — thay Level1 bằng MainMenu hẳn
    // (không cần giữ state cũ vì đã thua), nút PLAY sẽ tự đổi label thành "RESTART".
    public void NotifyPlayerDied()
    {
        CurrentContext = GameContext.Dead;
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }

    // MainMenuController gọi ngay sau khi đã đọc context để hiện đúng label —
    // tiêu thụ trạng thái Dead, tránh lần vào MainMenu tiếp theo (từ Title) vẫn hiện "RESTART".
    public void ClearDeathContext()
    {
        if (CurrentContext == GameContext.Dead)
            CurrentContext = GameContext.Fresh;
    }

    // Gọi từ ExitZone khi player thoát level thành công — tự lưu game rồi fade tối/sáng
    // sang scene kế tiếp, không cần bấm nút gì cả.
    public void CompleteLevel(string nextScene)
    {
        if (isLevelComplete) return;
        isLevelComplete = true;

        var inventory = FindFirstObjectByType<PlayerInventory>();
        if (inventory != null)
        {
            SavedData.firePotions = inventory.firePotions;
            SavedData.lightningPotions = inventory.lightningPotions;
            SavedData.healPotions = inventory.healPotions;
            SavedData.keyIds = new List<string>(inventory.KeyIds);
        }

        SavedData.levelReached = SceneManager.GetActiveScene().name;
        SaveSystem.Save(SavedData);

        StartCoroutine(FadeAndLoad(nextScene));
    }

    IEnumerator FadeAndLoad(string nextScene)
    {
        // Tối dần
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            fadeAlpha = Mathf.Clamp01(t / fadeDuration);
            yield return null;
        }
        fadeAlpha = 1f;

        SceneManager.LoadScene(nextScene);
        yield return null; // đợi 1 frame cho scene mới load xong trước khi sáng dần lại

        // Sáng dần lại
        t = 0f;
        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            fadeAlpha = 1f - Mathf.Clamp01(t / fadeDuration);
            yield return null;
        }
        fadeAlpha = 0f;
        isLevelComplete = false; // sẵn sàng cho lần CompleteLevel kế tiếp (Level2 → Level3...)
    }

    public void RestartLevel()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void LoadLevel(string sceneName)
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(sceneName);
    }

    void OnGUI()
    {
        if (fadeAlpha <= 0f) return;

        fadeTex ??= MakeTex(Color.black);

        Color prev = GUI.color;
        GUI.color = new Color(1f, 1f, 1f, fadeAlpha);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), fadeTex);
        GUI.color = prev;
    }

    static Texture2D MakeTex(Color color)
    {
        var tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, color);
        tex.Apply();
        return tex;
    }
}
