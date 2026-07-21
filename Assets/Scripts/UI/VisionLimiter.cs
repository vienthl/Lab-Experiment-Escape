using UnityEngine;

// Hiệu ứng "vùng tối" kiểu Among Us: màn hình tối đen, chỉ sáng 1 vùng tròn quanh Player.
// Boss2Health gọi VisionLimiter.Instance.Activate() khi máu boss xuống mốc quy định (mặc định 50%).
// Đặt script này lên 1 GameObject có sẵn trong scene Level2 (vd chính GameObject đang giữ LevelHUD).
public class VisionLimiter : MonoBehaviour
{
    public static VisionLimiter Instance { get; private set; }

    [Header("Bán kính vùng sáng quanh Player (pixel màn hình)")]
    public float visionRadius = 160f;

    [Range(0.05f, 0.9f)]
    [Tooltip("Tỉ lệ (so với bán kính vùng sáng) nơi bắt đầu mờ dần sang tối hẳn")]
    public float innerFraction = 0.1f;

    const int TexSize = 512;

    Texture2D maskTex;
    Transform player;
    bool active;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        var playerGo = GameObject.FindGameObjectWithTag("Player");
        if (playerGo != null) player = playerGo.transform;
    }

    public void Activate()
    {
        if (active) return;
        active = true;
        if (maskTex == null) BuildMask();
    }

    public void Deactivate() => active = false;

    void BuildMask()
    {
        maskTex = new Texture2D(TexSize, TexSize, TextureFormat.ARGB32, false);
        Vector2 center = new Vector2(TexSize / 2f, TexSize / 2f);
        float outerR = TexSize / 2f;
        float innerR = outerR * innerFraction;

        for (int y = 0; y < TexSize; y++)
        {
            for (int x = 0; x < TexSize; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                float alpha = Mathf.Clamp01(Mathf.InverseLerp(innerR, outerR, dist));
                maskTex.SetPixel(x, y, new Color(0f, 0f, 0f, alpha));
            }
        }
        maskTex.Apply();
    }

    void OnGUI()
    {
        if (!active || player == null || maskTex == null || Time.timeScale <= 0f) return;

        var cam = Camera.main;
        if (cam == null) return;

        Vector3 sp = cam.WorldToScreenPoint(player.position);
        float guiY = Screen.height - sp.y;

        // coverSize suy ra từ visionRadius/innerFraction để hình tròn sáng luôn đúng bán kính mong muốn,
        // đồng thời đảm bảo phủ hết góc màn hình xa nhất.
        float coverSize = Mathf.Max(
            visionRadius * 2f / innerFraction,
            Mathf.Max(Screen.width, Screen.height) * 2.5f);

        // Depth cao hơn (mặc định 0) → vẽ Ở DƯỚI, để các HUD khác (máu, đồng hồ...) vẫn hiện trên vùng tối.
        GUI.depth = 1000;
        GUI.DrawTexture(new Rect(sp.x - coverSize / 2f, guiY - coverSize / 2f, coverSize, coverSize), maskTex);
    }
}
