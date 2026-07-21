using UnityEngine;

// Thanh máu Boss2 kiểu OnGUI giống hệt PlayerHealthUI, đặt Top-Center thay vì Top-Left.
// Gắn lên chính GameObject Boss2 (script tự GetComponent<Boss2Health>).
public class Boss2HealthUI : MonoBehaviour
{
    [Header("Kích thước (pixel) — khớp PlayerHealthUI")]
    public float barY = 20f;
    public float barWidth = 220f;
    public float barHeight = 22f;

    [Header("Màu sắc")]
    public Color backgroundColor = new Color(0.08f, 0.08f, 0.08f, 0.85f);
    public Color fillColor = new Color(0.75f, 0.15f, 0.2f, 1f);

    Boss2Health bossHealth;
    Texture2D bgTex;
    Texture2D fillTex;

    void Awake()
    {
        bossHealth = GetComponent<Boss2Health>();
        bgTex = MakeTex(backgroundColor);
        fillTex = MakeTex(fillColor);
    }

    void OnGUI()
    {
        if (bossHealth == null || bossHealth.IsDead || Time.timeScale <= 0f) return;
        if (!gameObject.activeInHierarchy) return;

        float pct = bossHealth.HealthPercent;
        float barX = (Screen.width - barWidth) / 2f;

        GUI.DrawTexture(new Rect(barX, barY, barWidth, barHeight), bgTex);

        float fillW = (barWidth - 4f) * pct;
        if (fillW > 0f)
            GUI.DrawTexture(new Rect(barX + 2f, barY + 2f, fillW, barHeight - 4f), fillTex);
    }

    static Texture2D MakeTex(Color color)
    {
        var tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, color);
        tex.Apply();
        return tex;
    }
}
