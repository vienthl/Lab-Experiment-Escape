using UnityEngine;

/// <summary>
/// Thanh máu hiển thị phía trên đầu quái (dùng SpriteRenderer, không cần Canvas UI).
/// </summary>
public class WorldHealthBar : MonoBehaviour
{
    [Header("Kích thước & vị trí")]
    public Vector3 localOffset = new Vector3(0f, 0.75f, 0f);
    public float barWidth = 0.7f;
    public float barHeight = 0.08f;

    [Header("Màu sắc")]
    public Color backgroundColor = new Color(0.15f, 0.15f, 0.15f, 0.9f);
    public Color fullColor = new Color(0.2f, 0.85f, 0.25f, 1f);
    public Color lowColor = new Color(0.9f, 0.2f, 0.15f, 1f);
    public float lowHealthThreshold = 0.3f;

    [Header("Hiển thị")]
    public int sortingOrder = 100;

    Transform fillTransform;
    SpriteRenderer fillRenderer;
    static Sprite whiteSprite;

    public void Build()
    {
        if (fillTransform != null)
            return;

        whiteSprite ??= CreateWhiteSprite();

        var bgGo = new GameObject("Background");
        bgGo.transform.SetParent(transform, false);
        bgGo.transform.localPosition = localOffset;
        bgGo.transform.localScale = new Vector3(barWidth, barHeight, 1f);

        var bgRenderer = bgGo.AddComponent<SpriteRenderer>();
        bgRenderer.sprite = whiteSprite;
        bgRenderer.color = backgroundColor;
        bgRenderer.sortingOrder = sortingOrder;

        var fillGo = new GameObject("Fill");
        fillGo.transform.SetParent(transform, false);

        fillRenderer = fillGo.AddComponent<SpriteRenderer>();
        fillRenderer.sprite = whiteSprite;
        fillRenderer.color = fullColor;
        fillRenderer.sortingOrder = sortingOrder + 1;

        ApplySortingFromEnemy(
            transform.parent?.GetComponentInChildren<SpriteRenderer>(true),
            bgRenderer,
            fillRenderer);

        fillTransform = fillGo.transform;
        SetFill(1f);
    }

    void ApplySortingFromEnemy(SpriteRenderer spriteRef, SpriteRenderer bg, SpriteRenderer fill)
    {
        if (spriteRef == null)
            return;

        bg.sortingLayerID = spriteRef.sortingLayerID;
        fill.sortingLayerID = spriteRef.sortingLayerID;
        bg.sortingOrder = spriteRef.sortingOrder + sortingOrder;
        fill.sortingOrder = spriteRef.sortingOrder + sortingOrder + 1;
    }

    public void SetFill(float normalized)
    {
        if (fillTransform == null)
            return;

        normalized = Mathf.Clamp01(normalized);

        float fillWidth = barWidth * normalized;
        fillTransform.localPosition = new Vector3(
            localOffset.x - barWidth * 0.5f + fillWidth * 0.5f,
            localOffset.y,
            localOffset.z - 0.01f);

        fillTransform.localScale = new Vector3(fillWidth, barHeight, 1f);

        if (fillRenderer != null)
            fillRenderer.color = normalized <= lowHealthThreshold ? lowColor : fullColor;
    }

    static Sprite CreateWhiteSprite()
    {
        var tex = Texture2D.whiteTexture;
        return Sprite.Create(
            tex,
            new Rect(0f, 0f, tex.width, tex.height),
            new Vector2(0.5f, 0.5f),
            100f);
    }
}
