using UnityEngine;

// Thanh máu hiển thị TRONG THẾ GIỚI GAME (world-space) phía trên đầu quái.
// Khác PlayerHealthUI (vẽ lên màn hình), script này dùng SpriteRenderer — thanh máu di chuyển theo quái.
// Được tạo tự động bởi EnemyHealth, không cần gắn thủ công.
public class WorldHealthBar : MonoBehaviour
{
    [Header("Kích thước & vị trí (đơn vị world unit)")]
    public Vector3 localOffset = new Vector3(0f, 1.4f, 0f); // Vị trí thanh máu so với tâm quái (lên 1.4 unit)
    public float barWidth  = 1.5f;  // Chiều ngang thanh (world units)
    public float barHeight = 0.14f; // Chiều cao thanh (world units)

    [Header("Màu sắc")]
    public Color backgroundColor = new Color(0.15f, 0.15f, 0.15f, 0.9f); // Màu nền tối
    public Color fullColor       = new Color(0.2f,  0.85f, 0.25f, 1f);   // Màu xanh lá khi máu cao
    public Color lowColor        = new Color(0.9f,  0.2f,  0.15f, 1f);   // Màu đỏ khi máu thấp
    public float lowHealthThreshold = 0.3f; // Dưới 30% → đổi sang màu đỏ

    [Header("Thứ tự render")]
    public int sortingOrder = 100; // Số càng cao càng vẽ đè lên trên — 100 để thanh máu không bị che

    Transform fillTransform;      // Transform của phần fill (để co giãn theo HP)
    SpriteRenderer fillRenderer;  // Renderer của fill (để đổi màu)
    static Sprite whiteSprite;    // Sprite 1px trắng dùng chung cho tất cả thanh máu trong scene

    // Tạo 2 child GameObject: "Background" (nền) và "Fill" (thanh màu)
    // Gọi từ EnemyHealth.CreateHealthBar() khi quái xuất hiện
    public void Build()
    {
        if (fillTransform != null) return; // Đã build rồi → bỏ qua (tránh tạo trùng)

        // Tạo sprite trắng 1 pixel nếu chưa có (static → dùng chung, tiết kiệm memory)
        whiteSprite ??= CreateWhiteSprite();

        // ── Tạo thanh NỀN ──
        var bgGo = new GameObject("Background");
        bgGo.transform.SetParent(transform, false);
        bgGo.transform.localPosition = localOffset; // Đặt phía trên đầu quái
        bgGo.transform.localScale    = new Vector3(barWidth, barHeight, 1f); // Scale = kích thước thanh

        var bgRenderer = bgGo.AddComponent<SpriteRenderer>();
        bgRenderer.sprite = whiteSprite;
        bgRenderer.color  = backgroundColor;
        bgRenderer.sortingOrder = sortingOrder;

        // ── Tạo thanh FILL (phần màu thể hiện % HP) ──
        var fillGo = new GameObject("Fill");
        fillGo.transform.SetParent(transform, false);

        fillRenderer = fillGo.AddComponent<SpriteRenderer>();
        fillRenderer.sprite = whiteSprite;
        fillRenderer.color  = fullColor;
        fillRenderer.sortingOrder = sortingOrder + 1; // Vẽ đè lên nền

        // Đồng bộ Sorting Layer với sprite của quái (để thanh máu cùng layer render)
        ApplySortingFromEnemy(
            transform.parent?.GetComponentInChildren<SpriteRenderer>(true),
            bgRenderer, fillRenderer);

        fillTransform = fillGo.transform;
        SetFill(1f); // Khởi tạo đầy máu
    }

    // Sao chép Sorting Layer từ sprite của quái sang thanh máu — để không bị render sai thứ tự
    void ApplySortingFromEnemy(SpriteRenderer spriteRef, SpriteRenderer bg, SpriteRenderer fill)
    {
        if (spriteRef == null) return;
        bg.sortingLayerID   = spriteRef.sortingLayerID;
        fill.sortingLayerID = spriteRef.sortingLayerID;
        bg.sortingOrder     = spriteRef.sortingOrder + sortingOrder;
        fill.sortingOrder   = spriteRef.sortingOrder + sortingOrder + 1;
    }

    // Cập nhật độ dài fill theo tỉ lệ HP — gọi từ EnemyHealth.RefreshBar() mỗi lần bị damage
    public void SetFill(float normalized) // normalized = 0.0 (chết) → 1.0 (đầy máu)
    {
        if (fillTransform == null) return;

        normalized = Mathf.Clamp01(normalized); // Giới hạn trong [0, 1]

        float fillWidth = barWidth * normalized; // Độ rộng fill tỉ lệ với HP

        // Dịch tâm fill sang trái khi co lại, để CẠNH TRÁI của thanh luôn cố định
        // Vì SpriteRenderer tính từ tâm, khi thu hẹp cần dịch tâm để cạnh trái không dịch chuyển
        fillTransform.localPosition = new Vector3(
            localOffset.x - barWidth * 0.5f + fillWidth * 0.5f, // Tâm fill = cạnh trái + nửa fill
            localOffset.y,
            localOffset.z - 0.01f); // z lùi 0.01 để fill hiện trước nền

        fillTransform.localScale = new Vector3(fillWidth, barHeight, 1f);

        // Đổi màu đỏ khi HP thấp
        if (fillRenderer != null)
            fillRenderer.color = normalized <= lowHealthThreshold ? lowColor : fullColor;
    }

    // Tạo Sprite từ texture trắng 1x1 — dùng làm "cọ màu" để DrawTexture bằng SpriteRenderer
    static Sprite CreateWhiteSprite()
    {
        var tex = Texture2D.whiteTexture; // Texture trắng 1x1 có sẵn trong Unity
        return Sprite.Create(
            tex,
            new Rect(0f, 0f, tex.width, tex.height), // Vùng cắt = toàn bộ texture
            new Vector2(0.5f, 0.5f),                  // Pivot = tâm
            100f);                                     // Pixels Per Unit
    }
}
