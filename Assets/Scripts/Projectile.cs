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
        if (other.CompareTag("Player")) return;
        if (other.isTrigger) return;

        var enemy = other.GetComponentInParent<EnemyHealth>();
        if (enemy != null && !enemy.IsDead)
        {
            // Sát thương theo % máu tối đa của quái
            float dmg = potionType == PotionType.Lightning
                ? enemy.maxHealth * 0.5f    // điện: 50% max HP
                : enemy.maxHealth * 0.25f;  // lửa:  25% max HP

            enemy.TakeDamage(dmg);

            // Spawn hiệu ứng tại vị trí quái
            if (hitEffectPrefab != null)
                Instantiate(hitEffectPrefab, enemy.transform.position, Quaternion.identity);
        }

        Destroy(gameObject);
    }
}
