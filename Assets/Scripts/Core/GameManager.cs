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

    [Header("Level Complete (tạm thời — Level 2 chưa có map)")]
    public string nextSceneWhenNoLevel2 = "MainMenu";

    bool isLevelComplete;
    GUIStyle titleStyle;
    GUIStyle bodyStyle;
    Texture2D overlayTex;

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

    // Gọi từ ExitZone khi player thoát level thành công.
    public void CompleteLevel()
    {
        if (isLevelComplete) return;

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

        isLevelComplete = true;
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
        if (!isLevelComplete) return;

        overlayTex ??= MakeTex(new Color(0f, 0f, 0f, 0.8f));
        titleStyle ??= new GUIStyle(GUI.skin.label)
        {
            fontSize = 56,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(0.2f, 0.9f, 0.6f) }
        };
        bodyStyle ??= new GUIStyle(GUI.skin.label)
        {
            fontSize = 22,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = Color.white }
        };

        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), overlayTex);

        float cx = Screen.width * 0.5f;
        float cy = Screen.height * 0.5f;

        GUI.Label(new Rect(cx - 300f, cy - 140f, 600f, 80f), "LEVEL COMPLETE", titleStyle);
        GUI.Label(new Rect(cx - 260f, cy - 60f, 520f, 40f),
            $"Bình lửa: {SavedData.firePotions}   Bình điện: {SavedData.lightningPotions}   Bình hồi máu: {SavedData.healPotions}",
            bodyStyle);

        if (GUI.Button(new Rect(cx - 100f, cy + 20f, 200f, 46f), "Về Main Menu"))
            LoadLevel(nextSceneWhenNoLevel2);
    }

    static Texture2D MakeTex(Color color)
    {
        var tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, color);
        tex.Apply();
        return tex;
    }
}
