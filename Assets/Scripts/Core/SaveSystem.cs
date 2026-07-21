using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// Lưu tiến trình xuống đĩa (persistentDataPath) — dùng chung cho "mang đồ sang Level 2"
// và yêu cầu đề bài "storage system để mở lại game".
public static class SaveSystem
{
    static readonly string SavePath = Path.Combine(Application.persistentDataPath, "save.json");

    [Serializable]
    public class SaveData
    {
        public int firePotions;
        public int lightningPotions;
        public int healPotions;
        public int curePotions;
        public bool isInfected;
        public List<string> keyIds = new List<string>();
        public string levelReached = "Level1";
    }

    public static bool Exists() => File.Exists(SavePath);

    public static void Save(SaveData data)
    {
        try
        {
            File.WriteAllText(SavePath, JsonUtility.ToJson(data, true));
        }
        catch (Exception e)
        {
            Debug.LogError($"SaveSystem: lưu game thất bại — {e.Message}");
        }
    }

    public static SaveData Load()
    {
        if (!Exists()) return new SaveData();

        try
        {
            return JsonUtility.FromJson<SaveData>(File.ReadAllText(SavePath)) ?? new SaveData();
        }
        catch (Exception e)
        {
            Debug.LogError($"SaveSystem: đọc save thất bại, dùng dữ liệu mặc định — {e.Message}");
            return new SaveData();
        }
    }
}
