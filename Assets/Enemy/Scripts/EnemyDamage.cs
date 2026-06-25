using UnityEngine;

public class EnemyDamage : MonoBehaviour
{
    [Header("Sát thương")]
    [Tooltip("Lượng máu trừ mỗi lần chạm")]
    public float damagePerHit = 10f;

    [Tooltip("Thời gian chờ giữa 2 lần gây sát thương liên tiếp (giây)")]
    public float damageCooldown = 1f;

    float lastDamageTime = -999f;

    void OnCollisionEnter2D(Collision2D collision)
    {
        TryDamagePlayer(collision.gameObject);
    }

    void OnCollisionStay2D(Collision2D collision)
    {
        TryDamagePlayer(collision.gameObject);
    }

    void TryDamagePlayer(GameObject other)
    {
        if (!other.CompareTag("Player")) return;
        if (Time.time - lastDamageTime < damageCooldown) return;

        var playerHealth = other.GetComponent<PlayerHealth>();
        if (playerHealth == null || playerHealth.IsDead) return;

        playerHealth.TakeDamage(damagePerHit);
        lastDamageTime = Time.time;
    }
}
