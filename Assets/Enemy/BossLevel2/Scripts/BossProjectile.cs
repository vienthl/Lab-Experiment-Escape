using UnityEngine;

// Đạn Boss2 bắn ra — chỉ gây sát thương lên Player, không ảnh hưởng enemy/boss khác
// (ngược hướng với Projectile.cs của Player: cái đó chỉ gây sát thương lên enemy/boss).
public class BossProjectile : MonoBehaviour
{
    [Header("Sát thương")]
    public float damage = 10f;

    [Header("Di chuyển")]
    public float speed = 8f;
    public float maxRange = 10f;

    [Header("Hiệu ứng khi trúng")]
    public GameObject hitEffectPrefab;

    [Header("Âm thanh khi trúng Player")]
    public AudioClip impactSound;
    [Range(0f, 1f)] public float impactVolume = 1f;

    Rigidbody2D rb;
    Vector2 startPos;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        startPos = transform.position;
    }

    public void Launch(Vector2 direction)
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
        // Bỏ qua: đạn khác, chính Boss2/quái khác — tránh tự trúng "đồng đội" hoặc chính nó lúc vừa spawn
        if (other.GetComponentInParent<BossProjectile>() != null) return;
        if (other.GetComponentInParent<Boss2Health>() != null) return;
        if (other.GetComponentInParent<EnemyHealth>() != null) return;
        if (other.GetComponentInParent<BossHealth>() != null) return;

        var playerHealth = other.GetComponentInParent<PlayerHealth>();
        if (playerHealth != null && !playerHealth.IsDead)
            playerHealth.TakeDamage(damage, transform.position);

        // Trúng Player HOẶC bất kỳ thứ gì khác (tường, vật cản...) đều dừng đạn tại đây —
        // giống hệt cách Projectile.cs của Player hoạt động, chỉ khác chiều gây sát thương.
        if (hitEffectPrefab != null)
            Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);

        AudioOneShot.Play(impactSound, transform.position, impactVolume);

        Destroy(gameObject);
    }
}
