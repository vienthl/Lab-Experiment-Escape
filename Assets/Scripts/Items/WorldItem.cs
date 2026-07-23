using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class WorldItem : MonoBehaviour
{
    [Header("Dữ liệu vật phẩm")]
    public ItemData itemData;

    [Tooltip("Ghi đè số lượng của ItemData nếu > 0 (để 0 = dùng amount mặc định trong ItemData)")]
    public int quantityOverride = 0;

    [Header("Hiệu ứng nhấp nhô")]
    public float bobHeight = 0.08f;
    public float bobSpeed = 2.5f;

    [Header("Highlight khi player đứng gần")]
    public Color highlightColor = new Color(1f, 0.95f, 0.6f, 1f);

    [Header("Âm thanh")]
    public AudioClip collectSound;
    [Range(0f, 1f)] public float collectVolume = 1f;

    Vector3 basePos;
    SpriteRenderer spriteRenderer;
    Color baseColor = Color.white;

    public int Quantity => quantityOverride > 0 ? quantityOverride : (itemData != null ? itemData.amount : 0);

    void Awake()
    {
        basePos = transform.position;
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
            baseColor = spriteRenderer.color;
    }

    void Update()
    {
        float y = Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        transform.position = basePos + new Vector3(0f, y, 0f);
    }

    public void SetHighlighted(bool highlighted)
    {
        if (spriteRenderer == null) return;
        spriteRenderer.color = highlighted ? highlightColor : baseColor;
    }

    // Trả về data để PlayerInventory cộng vào, rồi tự hủy — gọi từ PlayerInteractor.Interact()
    public ItemData Collect(out int quantity)
    {
        quantity = Quantity;
        var data = itemData;
        AudioOneShot.Play(collectSound, transform.position, collectVolume);
        Destroy(gameObject);
        return data;
    }
}
