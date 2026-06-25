using UnityEngine;

// Enum định nghĩa 2 loại bình — rõ ràng hơn dùng số 0/1
public enum PotionType { Fire, Lightning }

// Script gắn lên Prefab bình (FirePotion / LightningPotion).
// Bình tự bay theo hướng được truyền vào, phát hiện va chạm qua trigger, rồi tự hủy.
public class Projectile : MonoBehaviour
{
    [Header("Loại bình")]
    public PotionType potionType = PotionType.Fire; // Chọn Fire hoặc Lightning trong Inspector

    [Header("Hiệu ứng khi trúng")]
    [Tooltip("Kéo Prefab FireEffect hoặc LightningEffect2 vào đây")]
    public GameObject hitEffectPrefab; // Hiệu ứng lửa/điện sẽ spawn tại điểm va chạm

    [Header("Tầm bay")]
    public float maxRange = 18f; // Bình tự hủy khi bay quá khoảng cách này

    Rigidbody2D rb;       // Dùng để đặt vận tốc vật lý cho bình bay
    Vector2 startPos;     // Lưu vị trí lúc spawn để tính khoảng cách đã bay

    void Awake()
    {
        rb       = GetComponent<Rigidbody2D>();
        startPos = transform.position; // Ghi nhớ điểm xuất phát
    }

    // Gọi từ PlayerAttack.Throw() ngay sau khi Instantiate
    public void Launch(Vector2 direction, float speed)
    {
        if (rb != null)
            rb.linearVelocity = direction * speed; // Đặt vận tốc → Unity tự di chuyển mỗi frame
    }

    void Update()
    {
        // Kiểm tra mỗi frame: nếu bay quá tầm thì tự hủy (tránh bình bay mãi ra ngoài màn hình)
        if (Vector2.Distance(transform.position, startPos) >= maxRange)
            Destroy(gameObject);
    }

    // OnTriggerEnter2D được gọi khi CircleCollider2D (Is Trigger = true) của bình
    // giao nhau với bất kỳ Collider2D nào khác trong scene
    void OnTriggerEnter2D(Collider2D other)
    {
        // Bỏ qua player — bình không tự trúng người ném
        if (other.CompareTag("Player")) return;

        // Bỏ qua hiệu ứng lửa/điện đang hiện trong scene (tránh bình bị hủy bởi hiệu ứng của chính mình)
        if (other.GetComponentInParent<HitEffect>() != null) return;

        // Kiểm tra có phải quái không (GetComponentInParent vì collider có thể nằm trên child object)
        var enemy = other.GetComponentInParent<EnemyHealth>();
        if (enemy != null && !enemy.IsDead)
        {
            // Tính sát thương theo % MÁU TỐI ĐA của quái (không phải máu hiện tại)
            // → Bình điện luôn trừ 50% maxHP, bình lửa luôn trừ 25% maxHP dù quái còn bao nhiêu máu
            float dmg = potionType == PotionType.Lightning
                ? enemy.maxHealth * 0.5f    // Bình điện: 50% max HP
                : enemy.maxHealth * 0.25f;  // Bình lửa:  25% max HP

            enemy.TakeDamage(dmg); // Trừ máu quái
        }

        // Spawn hiệu ứng tại điểm bình vỡ — dù trúng quái hay tường đều có hiệu ứng
        if (hitEffectPrefab != null)
            Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);

        Destroy(gameObject); // Hủy bình sau khi xử lý xong
    }
}
