using UnityEngine;

public enum PotionType { Fire, Lightning }

public class Projectile : MonoBehaviour
{
    [Header("Loại bình")]
    public PotionType potionType = PotionType.Fire;

    [Header("Hiệu ứng khi trúng quái")]
    [Tooltip("Kéo Prefab FireEffect hoặc LightningEffect vào đây")]
    public GameObject hitEffectPrefab;

    [Header("Tầm bay")]
    public float maxRange = 18f;

    Rigidbody2D rb;
    Vector2 startPos;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        startPos = transform.position;
    }

    public void Launch(Vector2 direction, float speed)
    {
        if (rb != null)
            rb.linearVelocity = direction * speed;
    }

    void Update()
    {
        if (Vector2.Distance(transform.position, startPos) >= maxRange)
            Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Bỏ qua player
        if (other.CompareTag("Player")) return;

        // Bỏ qua HitEffect đang tồn tại (hiệu ứng lửa/điện không làm bể bình)
        if (other.GetComponentInParent<HitEffect>() != null) return;

        var enemy = other.GetComponentInParent<EnemyHealth>();
        if (enemy != null && !enemy.IsDead)
        {
            float dmg = potionType == PotionType.Lightning
                ? enemy.maxHealth * 0.5f    // điện: 50% max HP
                : enemy.maxHealth * 0.25f;  // lửa:  25% max HP

            enemy.TakeDamage(dmg);
        }

        // Hiệu ứng tại điểm va chạm — cả khi trúng tường lẫn quái
        if (hitEffectPrefab != null)
            Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);

        Destroy(gameObject);
    }
}
