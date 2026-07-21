using UnityEngine;

// Thông báo "đã bị nhiễm độc" hiện ở Bottom-Center lúc vào Level 2 — chỉ hiện nếu Player
// đang nhiễm độc (PlayerInfection.IsInfected). Tự ẩn sau vài giây hoặc khi bấm phím bất kỳ.
// Gắn script này lên 1 GameObject bất kỳ tồn tại sẵn trong scene Level2 (vd chung với LevelHUD).
public class InfectionNoticeUI : MonoBehaviour
{
    [Header("Nội dung")]
    [TextArea]
    public string message =
        "Bạn đã bị nhiễm độc nhẹ sau trận chiến ở Level 1.\nHãy đánh hạ Boss để lấy bình thuốc giải độc.";

    [Header("Thời gian (giây)")]
    [Tooltip("Không cho bấm phím tắt trong khoảng này — tránh việc lỡ tay bấm WASD lúc scene vừa vào làm mất thông báo ngay")]
    public float minDisplayTime = 1.5f;
    [Tooltip("Tự ẩn sau bằng này giây, 0 = không tự ẩn (chỉ ẩn khi bấm phím)")]
    public float autoHideAfter = 6f;

    [Header("Kích thước & vị trí (pixel)")]
    public float panelWidth = 640f;
    public float panelHeight = 90f;
    public float bottomMargin = 40f;

    [Header("Hình khung (để trống = dùng màu phẳng mặc định)")]
    public Texture2D panelTexture;

    [Header("Màu sắc (khi không có panelTexture)")]
    public Color backgroundColor = new Color(0.08f, 0.08f, 0.08f, 0.9f);
    public Color textColor = new Color(0.95f, 0.35f, 0.2f, 1f);

    PlayerInfection infection;
    Texture2D fallbackTex;
    GUIStyle textStyle;

    bool shown;
    bool dismissed;
    float shownAt;

    void Awake()
    {
        infection = FindFirstObjectByType<PlayerInfection>();
        fallbackTex = MakeTex(backgroundColor);
    }

    void Update()
    {
        if (dismissed || infection == null) return;

        if (!shown)
        {
            if (!infection.IsInfected) { dismissed = true; return; }
            shown = true;
            shownAt = Time.time;
            return;
        }

        float elapsed = Time.time - shownAt;
        bool canDismissByKey = elapsed >= minDisplayTime && Input.anyKeyDown;
        bool timedOut = autoHideAfter > 0f && elapsed >= autoHideAfter;

        if (canDismissByKey || timedOut)
            dismissed = true;
    }

    void OnGUI()
    {
        if (!shown || dismissed || Time.timeScale <= 0f) return;

        textStyle ??= new GUIStyle(GUI.skin.label)
        {
            fontSize = 20,
            alignment = TextAnchor.MiddleCenter,
            wordWrap = true
        };
        textStyle.normal.textColor = textColor;

        float x = (Screen.width - panelWidth) / 2f;
        float y = Screen.height - panelHeight - bottomMargin;

        GUI.DrawTexture(new Rect(x, y, panelWidth, panelHeight), panelTexture != null ? panelTexture : fallbackTex);
        GUI.Label(new Rect(x + 20f, y + 10f, panelWidth - 40f, panelHeight - 20f), message, textStyle);
    }

    static Texture2D MakeTex(Color color)
    {
        var tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, color);
        tex.Apply();
        return tex;
    }
}
