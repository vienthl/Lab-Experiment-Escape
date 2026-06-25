using UnityEngine;

// Gây sát thương cho player khi quái chạm vào.
// Gắn script này lên cùng GameObject với Rigidbody2D và Collider2D của quái.
public class EnemyDamage : MonoBehaviour
{
    [Header("Sát thương")]
    [Tooltip("Lượng máu trừ mỗi lần chạm")]
    public float damagePerHit = 10f;

    [Tooltip("Thời gian chờ giữa 2 lần gây sát thương (giây) — tránh trừ máu liên tục quá nhanh")]
    public float damageCooldown = 1f;

    // Khởi tạo là -999 để lần chạm đầu tiên luôn gây damage ngay (không phải chờ cooldown)
    float lastDamageTime = -999f;

    // OnCollisionEnter2D: gọi đúng 1 lần khi quái BẮT ĐẦU chạm vào object khác
    void OnCollisionEnter2D(Collision2D collision)
    {
        TryDamagePlayer(collision.gameObject);
    }

    // OnCollisionStay2D: gọi mỗi frame khi quái ĐANG ĐỨNG CHỒNG lên object khác
    // Cần cả 2 vì: Enter bắt lần chạm đầu, Stay bắt khi quái đứng trên player liên tục
    void OnCollisionStay2D(Collision2D collision)
    {
        TryDamagePlayer(collision.gameObject);
    }

    void TryDamagePlayer(GameObject other)
    {
        // Chỉ xử lý khi va chạm với Player (kiểm tra Tag "Player" trên GameObject)
        if (!other.CompareTag("Player")) return;

        // Kiểm tra cooldown: nếu chưa đủ thời gian chờ thì bỏ qua
        // Time.time = tổng số giây từ khi game bắt đầu
        if (Time.time - lastDamageTime < damageCooldown) return;

        var playerHealth = other.GetComponent<PlayerHealth>();
        if (playerHealth == null || playerHealth.IsDead) return; // Player không có máu hoặc đã chết

        playerHealth.TakeDamage(damagePerHit);  // Trừ máu player
        lastDamageTime = Time.time;              // Ghi lại thời điểm vừa gây damage để tính cooldown lần sau
    }
}
