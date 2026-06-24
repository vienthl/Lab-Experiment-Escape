using UnityEngine;

/// <summary>
/// Thanh máu UI góc trên trái màn hình — dùng OnGUI, không cần package UI.
/// Gán script này lên Player cùng với PlayerHealth.
/// </summary>
public class PlayerHealthUI : MonoBehaviour
{
    [Header("Vị trí & kích thước (pixel)")]
    public float barX      = 20f;
    public float barY      = 20f;
    public float barWidth  = 220f;
    public float barHeight = 22f;

    [Header("Màu sắc")]
    public Color backgroundColor = new Color(0.08f, 0.08f, 0.08f, 0.85f);
    public Color fillColor       = new Color(0.2f,  0.6f,  0.95f, 1f);
    public Color lowColor        = new Color(0.9f,  0.2f,  0.15f, 1f);

    [Range(0f, 1f)]
    public float lowHealthThreshold = 0.35f;

    PlayerHealth playerHealth;
    Texture2D bgTex;
    Texture2D fillTex;
    Texture2D lowTex;

    void Awake()
    {
        playerHealth = GetComponent<PlayerHealth>();
        bgTex   = MakeTex(backgroundColor);
        fillTex = MakeTex(fillColor);
        lowTex  = MakeTex(lowColor);
    }

    void OnGUI()
    {
        if (playerHealth == null) return;

        float pct = playerHealth.HealthPercent;

        // Nền
        GUI.DrawTexture(new Rect(barX, barY, barWidth, barHeight), bgTex);

        // Thanh fill
        float fillW = (barWidth - 4f) * pct;
        if (fillW > 0f)
        {
            var tex = pct <= lowHealthThreshold ? lowTex : fillTex;
            GUI.DrawTexture(new Rect(barX + 2f, barY + 2f, fillW, barHeight - 4f), tex);
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
