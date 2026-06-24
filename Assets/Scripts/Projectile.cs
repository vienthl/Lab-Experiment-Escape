using UnityEngine;

public class Projectile : MonoBehaviour
{
    [Header("Chỉ số")]
    public float damage   = 25f;
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
        // Tự hủy khi bay quá tầm
        if (Vector2.Distance(transform.position, startPos) >= maxRange)
            Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Bỏ qua chính Player
        if (other.CompareTag("Player")) return;

        // Bỏ qua các trigger khác (ví dụ: trigger zone)
        if (other.isTrigger) return;

        // Kiểm tra có phải quái không (tìm EnemyHealth trên root)
        var enemy = other.GetComponentInParent<EnemyHealth>();
        if (enemy != null && !enemy.IsDead)
            enemy.TakeDamage(damage);

        // Hủy projectile dù trúng quái hay tường
        Destroy(gameObject);
    }
}
