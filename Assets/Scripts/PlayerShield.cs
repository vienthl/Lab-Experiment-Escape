using System.Collections;
using UnityEngine;

// Mũ khiên thủy tinh — MrX rơi ra khi chết ở Level1 (ItemType.ShieldHelmet, nhặt 1 lần là
// mở khóa vĩnh viễn khả năng này, không tiêu hao). Bấm phím R để bật khiên: miễn nhiễm
// TUYỆT ĐỐI mọi sát thương trong "Shield Duration" giây, có "Shield Cooldown" giây giữa 2 lần bật.
// Lúc khiên đang bật, tự sinh 1 sprite mũ thủy tinh làm CHILD của Player (yêu cầu #13 đề bài).
[RequireComponent(typeof(PlayerHealth))]
public class PlayerShield : MonoBehaviour
{
    [Header("Thời gian")]
    public float shieldDuration = 20f;
    public float shieldCooldown = 30f;

    [Header("Hiển thị (spawn làm Child của Player lúc khiên bật)")]
    [Tooltip("Prefab sprite mũ thủy tinh — để trống vẫn hoạt động, chỉ là không có hiệu ứng hình")]
    public GameObject shieldVisualPrefab;
    [Tooltip("Vị trí tự tính = top-center của Collider2D trên Player. Chỉnh thêm số này nếu muốn nhích lệch (mặc định 0,0,0 = không nhích)")]
    public Vector3 shieldVisualExtraOffset = Vector3.zero;

    [Header("Âm thanh")]
    public AudioClip activateSound;
    [Range(0f, 1f)] public float activateVolume = 1f;

    PlayerInventory inventory;
    Collider2D playerCollider;
    GameObject activeVisual;
    float lastActivatedAt = -999f;

    public bool IsShieldActive { get; private set; }
    public bool HasShieldItem => inventory != null && inventory.hasShieldItem;
    public bool IsOnCooldown => Time.time - lastActivatedAt < shieldCooldown;

    void Awake()
    {
        inventory = GetComponent<PlayerInventory>();
        playerCollider = GetComponent<Collider2D>();
    }

    // Top-center của Collider2D trên Player, quy đổi từ world sang local (giả định Player không xoay,
    // đúng với mọi nhân vật top-down trong game này — PlayerMovement luôn freezeRotation).
    Vector3 GetTopCenterLocalOffset()
    {
        if (playerCollider == null) return Vector3.zero;

        Vector3 topCenterWorld = new Vector3(
            playerCollider.bounds.center.x,
            playerCollider.bounds.max.y,
            transform.position.z);

        return topCenterWorld - transform.position;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R) && HasShieldItem && !IsShieldActive && !IsOnCooldown)
            StartCoroutine(ShieldRoutine());
    }

    IEnumerator ShieldRoutine()
    {
        IsShieldActive = true;
        lastActivatedAt = Time.time;

        if (shieldVisualPrefab != null)
        {
            activeVisual = Instantiate(shieldVisualPrefab, transform);
            activeVisual.transform.localPosition = GetTopCenterLocalOffset() + shieldVisualExtraOffset;
        }

        AudioOneShot.Play(activateSound, transform.position, activateVolume);

        yield return new WaitForSeconds(shieldDuration);

        IsShieldActive = false;
        if (activeVisual != null)
        {
            Destroy(activeVisual);
            activeVisual = null;
        }
    }
}
