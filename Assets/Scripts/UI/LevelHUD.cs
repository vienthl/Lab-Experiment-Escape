using UnityEngine;

// HUD phụ: số bình mang theo + tiến độ lockdown (đợt/thời gian).
// Theo cùng phong cách OnGUI với PlayerHealthUI/GameOverUI — không dùng Canvas trong scope này.
public class LevelHUD : MonoBehaviour
{
    [Header("Vị trí (pixel, dưới thanh máu)")]
    public float panelX = 20f;
    public float panelY = 50f;
    public float panelWidth = 280f;

    [Header("Màu sắc")]
    public Color backgroundColor = new Color(0.08f, 0.08f, 0.08f, 0.75f);
    public Color textColor = Color.white;
    public Color warningColor = new Color(0.95f, 0.35f, 0.2f, 1f);

    PlayerInventory inventory;
    LockdownRoomController lockdown;
    Texture2D bgTex;
    GUIStyle textStyle;
    GUIStyle warningStyle;

    void Awake()
    {
        inventory = GetComponent<PlayerInventory>();
        lockdown = FindFirstObjectByType<LockdownRoomController>();
        bgTex = MakeTex(backgroundColor);
    }

    void OnGUI()
    {
        if (inventory == null) return;

        textStyle ??= new GUIStyle(GUI.skin.label) { fontSize = 16, normal = { textColor = textColor } };
        warningStyle ??= new GUIStyle(GUI.skin.label) { fontSize = 16, fontStyle = FontStyle.Bold, normal = { textColor = warningColor } };

        int lineCount = lockdown != null && !lockdown.IsCleared ? 3 : 1;
        float lineHeight = 22f;
        float panelHeight = lineCount * lineHeight + 10f;

        GUI.DrawTexture(new Rect(panelX, panelY, panelWidth, panelHeight), bgTex);

        float y = panelY + 5f;
        GUI.Label(new Rect(panelX + 8f, y, panelWidth - 16f, lineHeight),
            $"Lửa: {inventory.firePotions}   Điện: {inventory.lightningPotions}   Hồi máu: {inventory.healPotions}",
            textStyle);

        if (lockdown != null && !lockdown.IsCleared)
        {
            y += lineHeight;
            GUI.Label(new Rect(panelX + 8f, y, panelWidth - 16f, lineHeight),
                $"Đợt {lockdown.CurrentWaveNumber}/{lockdown.TotalWaves} — còn {lockdown.EnemiesRemainingInWave} quái",
                textStyle);

            y += lineHeight;
            if (lockdown.HasTimeLimit)
            {
                int seconds = Mathf.CeilToInt(lockdown.RemainingTime);
                bool low = seconds <= 10;
                GUI.Label(new Rect(panelX + 8f, y, panelWidth - 16f, lineHeight),
                    $"Thời gian: {seconds / 60:00}:{seconds % 60:00}",
                    low ? warningStyle : textStyle);
            }
        }
    }

    static Texture2D MakeTex(Color color)
    {
        var tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, color);
        tex.Apply();
        return tex;
    }
}
