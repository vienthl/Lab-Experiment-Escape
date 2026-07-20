using UnityEngine;

// Thanh máu boss kiểu OnGUI — cùng phong cách PlayerHealthUI.cs nhưng đặt giữa-trên màn hình.
// Chỉ hiện khi BossHealth.HealthBarVisible = true (TestBoss tự bật lúc phát hiện player).
public class BossHealthUI : MonoBehaviour
{
    [Header("Kích thước (pixel) — bằng đúng PlayerHealthUI")]
    public float barWidth  = 220f;
    public float barHeight = 22f;
    public float topOffset = 24f;

    [Header("Màu sắc")]
    public Color backgroundColor = new Color(0.08f, 0.08f, 0.08f, 0.85f);
    public Color fillColor       = new Color(0.75f, 0.15f, 0.15f, 1f);
    public Color lowColor        = new Color(1f, 0.65f, 0.1f, 1f);

    [Range(0f, 1f)]
    public float lowHealthThreshold = 0.3f;

    BossHealth bossHealth;
    Texture2D bgTex;
    Texture2D fillTex;
    Texture2D lowTex;

    void Awake()
    {
        bossHealth = GetComponent<BossHealth>();
        bgTex   = MakeTex(backgroundColor);
        fillTex = MakeTex(fillColor);
        lowTex  = MakeTex(lowColor);
    }

    void OnGUI()
    {
        // Ẩn lúc chưa phát hiện player, đã chết, hoặc đang Pause (timeScale=0) — đồng bộ PlayerHealthUI/LevelHUD.
        if (bossHealth == null || !bossHealth.HealthBarVisible || Time.timeScale <= 0f) return;

        float pct = bossHealth.HealthPercent;
        float barX = (Screen.width - barWidth) * 0.5f;

        GUI.DrawTexture(new Rect(barX, topOffset, barWidth, barHeight), bgTex);

        float fillW = (barWidth - 4f) * pct;
        if (fillW > 0f)
        {
            var tex = pct <= lowHealthThreshold ? lowTex : fillTex;
            GUI.DrawTexture(new Rect(barX + 2f, topOffset + 2f, fillW, barHeight - 4f), tex);
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
