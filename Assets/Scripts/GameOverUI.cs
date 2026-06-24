using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Hiển thị màn hình Game Over khi player chết.
/// Gắn lên cùng GameObject với PlayerHealth.
/// </summary>
public class GameOverUI : MonoBehaviour
{
    [Header("Màu sắc")]
    public Color overlayColor  = new Color(0f, 0f, 0f, 0.78f);
    public Color titleColor    = new Color(0.92f, 0.15f, 0.15f, 1f);
    public Color subtitleColor = new Color(1f, 1f, 1f, 0.9f);

    [Header("Thời gian trễ trước khi hiện (giây)")]
    public float showDelay = 0.8f;

    PlayerHealth playerHealth;
    Texture2D    overlayTex;
    GUIStyle     titleStyle;
    GUIStyle     hintStyle;

    float diedAt = -1f;
    bool  isDead = false;

    void Awake()
    {
        playerHealth = GetComponent<PlayerHealth>();
        overlayTex   = MakeTex(overlayColor);
    }

    void Update()
    {
        if (playerHealth == null) return;

        // Ghi nhận thời điểm chết (chỉ 1 lần)
        if (!isDead && playerHealth.IsDead)
        {
            isDead = true;
            diedAt = Time.time;
        }

        // Bấm R để chơi lại
        if (isDead && IsVisible() && Input.GetKeyDown(KeyCode.R))
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    bool IsVisible() => isDead && Time.time - diedAt >= showDelay;

    void OnGUI()
    {
        if (!IsVisible()) return;

        // Khởi tạo style lần đầu (phải nằm trong OnGUI)
        if (titleStyle == null)
        {
            titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize  = 72,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            titleStyle.normal.textColor = titleColor;
        }

        if (hintStyle == null)
        {
            hintStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize  = 26,
                alignment = TextAnchor.MiddleCenter
            };
            hintStyle.normal.textColor = subtitleColor;
        }

        // Overlay tối phủ toàn màn hình
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), overlayTex);

        float cx = Screen.width  * 0.5f;
        float cy = Screen.height * 0.5f;

        // Tiêu đề GAME OVER
        GUI.Label(new Rect(cx - 300f, cy - 70f, 600f, 90f), "GAME OVER", titleStyle);

        // Hướng dẫn chơi lại
        GUI.Label(new Rect(cx - 220f, cy + 30f, 440f, 40f), "Nhấn  R  để chơi lại", hintStyle);
    }

    static Texture2D MakeTex(Color color)
    {
        var tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, color);
        tex.Apply();
        return tex;
    }
}
