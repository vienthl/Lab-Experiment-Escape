using UnityEngine;

// Hiệu ứng "vùng tối" kiểu Among Us: màn hình tối đen TUYỆT ĐỐI ngoài 1 vùng tròn quanh Player
// (biên cứng, không mờ dần như sương mù — ra khỏi bán kính là tối đen ngay).
// Luôn bật liên tục (không cần Activate() từ bên ngoài) — bán kính vùng sáng tự tăng khi Player
// có đủ bình lửa trong túi đồ (đại diện cho có đèn/lửa soi sáng), nhỏ lại khi chưa đủ.
// Gắn trực tiếp lên GameObject Player (script tự tìm PlayerInventory trên chính nó).
[RequireComponent(typeof(PlayerInventory))]
public class VisionLimiter : MonoBehaviour
{
    public static VisionLimiter Instance { get; private set; }

    [Header("Bán kính vùng sáng (pixel màn hình)")]
    [Tooltip("Bán kính khi CHƯA đủ bình lửa")]
    public float smallRadius = 160f;
    [Tooltip("Bán kính khi ĐÃ đủ bình lửa (>= Fire Potion Threshold)")]
    public float largeRadius = 260f;
    [Tooltip("Số bình lửa tối thiểu để có vùng sáng lớn hơn")]
    public int firePotionThreshold = 10;

    [Range(0.05f, 0.9f)]
    [Tooltip("Tỉ lệ nội bộ dùng để tính kích thước texture — càng lớn thì hình tròn càng mượt (ít răng cưa)")]
    public float innerFraction = 0.1f;

    [Range(0f, 0.15f)]
    [Tooltip("Độ mềm CHỈ ở sát viền tròn (dải rất mỏng, chống răng cưa) — 0 = cắt cứng tuyệt đối. KHÔNG phải hiệu ứng sương mù rộng như bản cũ, phần tối xung quanh vẫn đen 100%")]
    public float edgeSoftness = 0.03f;

    const int TexSize = 512;

    Texture2D maskTex;
    PlayerInventory inventory;

    float CurrentRadius => (inventory != null && inventory.firePotions >= firePotionThreshold) ? largeRadius : smallRadius;

    void Awake()
    {
        Instance = this;
        inventory = GetComponent<PlayerInventory>();
        BuildMask();
    }

    // Biên gần như cứng kiểu Among Us: trong bán kính thì sáng hoàn toàn (alpha 0), ra khỏi 1 dải
    // rất mỏng (Edge Softness) là tối đen tuyệt đối (alpha 1) — khác bản mờ-sương-mù cũ ở chỗ dải
    // chuyển màu chỉ rộng vài % thay vì trải dài tới tận mép texture.
    void BuildMask()
    {
        maskTex = new Texture2D(TexSize, TexSize, TextureFormat.ARGB32, false);
        maskTex.filterMode = FilterMode.Bilinear;
        Vector2 center = new Vector2(TexSize / 2f, TexSize / 2f);
        float outerR = TexSize / 2f;
        float holeR = outerR * innerFraction;
        float edgeR = holeR + outerR * edgeSoftness;

        for (int y = 0; y < TexSize; y++)
        {
            for (int x = 0; x < TexSize; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                float alpha = edgeSoftness > 0f
                    ? Mathf.Clamp01(Mathf.InverseLerp(holeR, edgeR, dist))
                    : (dist <= holeR ? 0f : 1f);
                maskTex.SetPixel(x, y, new Color(0f, 0f, 0f, alpha));
            }
        }
        maskTex.Apply();
    }

    void OnGUI()
    {
        if (maskTex == null || Time.timeScale <= 0f) return;

        var cam = Camera.main;
        if (cam == null) return;

        Vector3 sp = cam.WorldToScreenPoint(transform.position);
        float guiY = Screen.height - sp.y;

        float radius = CurrentRadius;

        // coverSize suy ra từ radius/innerFraction để hình tròn sáng luôn đúng bán kính mong muốn,
        // đồng thời đảm bảo phủ hết góc màn hình xa nhất.
        float coverSize = Mathf.Max(
            radius * 2f / innerFraction,
            Mathf.Max(Screen.width, Screen.height) * 2.5f);

        // Depth cao hơn (mặc định 0) → vẽ Ở DƯỚI, để các HUD khác (máu, đồng hồ...) vẫn hiện trên vùng tối.
        GUI.depth = 1000;
        GUI.DrawTexture(new Rect(sp.x - coverSize / 2f, guiY - coverSize / 2f, coverSize, coverSize), maskTex);
    }
}
