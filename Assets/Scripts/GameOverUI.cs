using UnityEngine;
using UnityEngine.SceneManagement; // Cần để dùng SceneManager.LoadScene()

// Hiển thị màn hình GAME OVER khi player chết, cho phép bấm R để chơi lại.
// Dùng OnGUI() như PlayerHealthUI — không cần Canvas hay package UI.
// Gán script này lên Player cùng với PlayerHealth.
public class GameOverUI : MonoBehaviour
{
    [Header("Màu sắc")]
    public Color overlayColor  = new Color(0f, 0f, 0f, 0.78f);    // Màu overlay tối phủ màn hình
    public Color titleColor    = new Color(0.92f, 0.15f, 0.15f, 1f); // Màu đỏ chữ GAME OVER
    public Color subtitleColor = new Color(1f, 1f, 1f, 0.9f);     // Màu trắng chữ hướng dẫn

    [Header("Thời gian trễ trước khi hiện (giây)")]
    public float showDelay = 0.8f; // Chờ animation chết phát xong mới hiện màn hình

    PlayerHealth playerHealth; // Đọc IsDead để biết khi nào player chết
    Texture2D    overlayTex;   // Texture màu tối phủ toàn màn hình
    GUIStyle     titleStyle;   // Style chữ lớn "GAME OVER"
    GUIStyle     hintStyle;    // Style chữ nhỏ "Nhấn R để chơi lại"

    float diedAt = -1f;  // Thời điểm (Time.time) khi player chết — dùng để tính showDelay
    bool  isDead = false; // Cờ xác nhận đã ghi nhận cái chết (tránh ghi nhiều lần)

    void Awake()
    {
        playerHealth = GetComponent<PlayerHealth>();
        overlayTex   = MakeTex(overlayColor); // Tạo texture 1px màu tối, dùng để vẽ overlay
    }

    void Update()
    {
        if (playerHealth == null) return;

        // Chỉ ghi nhận thời điểm chết 1 lần duy nhất (dùng !isDead làm khóa)
        if (!isDead && playerHealth.IsDead)
        {
            isDead = true;
            diedAt = Time.time; // Time.time = số giây từ khi game bắt đầu chạy
        }

        // Bấm R khi màn hình Game Over đang hiện → tải lại scene hiện tại
        if (isDead && IsVisible() && Input.GetKeyDown(KeyCode.R))
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            // buildIndex = số thứ tự scene trong Build Settings — GetActiveScene() lấy scene đang chạy
    }

    // Trả về true khi đã chết VÀ đã chờ đủ showDelay giây
    bool IsVisible() => isDead && Time.time - diedAt >= showDelay;

    // OnGUI() được Unity gọi mỗi frame để vẽ UI — chạy sau Update()
    void OnGUI()
    {
        if (!IsVisible()) return; // Chưa đủ điều kiện hiện → bỏ qua

        // GUIStyle phải khởi tạo trong OnGUI vì GUI.skin chỉ sẵn sàng ở đây, không phải Awake/Start
        if (titleStyle == null)
        {
            titleStyle = new GUIStyle(GUI.skin.label) // Kế thừa từ style mặc định
            {
                fontSize  = 72,                      // Cỡ chữ pixel
                fontStyle = FontStyle.Bold,           // In đậm
                alignment = TextAnchor.MiddleCenter   // Căn giữa ngang + dọc
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

        // Vẽ overlay đen tối phủ TOÀN MÀN HÌNH (Rect từ (0,0) đến (width, height))
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), overlayTex);

        // Tính tọa độ tâm màn hình để đặt chữ vào giữa
        float cx = Screen.width  * 0.5f;
        float cy = Screen.height * 0.5f;

        // Vẽ chữ "GAME OVER" — Rect(x, y, width, height), x/y là góc trên trái của vùng chữ
        GUI.Label(new Rect(cx - 300f, cy - 70f, 600f, 90f), "GAME OVER", titleStyle);

        // Vẽ hướng dẫn bên dưới
        GUI.Label(new Rect(cx - 220f, cy + 30f, 440f, 40f), "Nhấn  R  để chơi lại", hintStyle);
    }

    // Tạo texture 1x1 pixel với màu chỉ định — kéo giãn thành màu solid bất kỳ kích thước
    static Texture2D MakeTex(Color color)
    {
        var tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, color);
        tex.Apply(); // Bắt buộc gọi Apply() sau khi SetPixel để lưu thay đổi vào GPU
        return tex;
    }
}
