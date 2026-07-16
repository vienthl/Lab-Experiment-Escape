using UnityEngine;

public enum PotionType { Fire, Lightning }

public class Projectile : MonoBehaviour
{
    [Header("Loại bình")]
    public PotionType potionType = PotionType.Fire;

    [Header("Hiệu ứng khi trúng")]
    [Tooltip("Kéo Prefab FireEffect hoặc LightningEffect2 vào đây")]
    public GameObject hitEffectPrefab;

    [Header("Tầm bay")]
    public float maxRange = 18f;

    Rigidbody2D rb;
    Vector2 startPos;

    void Awake()
    {
        rb       = GetComponent<Rigidbody2D>();
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
        if (other.CompareTag("Player")) return;
        if (other.GetComponentInParent<HitEffect>() != null) return;

        // 2 bình thuốc bay xuyên qua nhau, không tự hủy lẫn nhau
        if (other.GetComponentInParent<Projectile>() != null) return;

        var enemy = other.GetComponentInParent<EnemyHealth>();
        if (enemy != null && !enemy.IsDead)
        {
            float dmg = potionType == PotionType.Lightning
                ? enemy.maxHealth * 0.5f
                : enemy.maxHealth * 0.25f;

            enemy.TakeDamage(dmg);
        }

        if (hitEffectPrefab != null)
            Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);

        Destroy(gameObject);
    }
}
