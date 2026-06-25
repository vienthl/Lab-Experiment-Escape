using UnityEngine;

// Script này chỉ quản lý DỮ LIỆU máu — không vẽ UI, không điều khiển animation.
// Tách riêng ra để PlayerHealthUI và GameOverUI có thể đọc dữ liệu độc lập.
public class PlayerHealth : MonoBehaviour
{
    [Header("Máu")]
    public float maxHealth = 100f; // Máu tối đa — chỉnh trong Inspector

    float currentHealth;       // Máu hiện tại — private, chỉ thay đổi qua TakeDamage/Heal
    PlayerMovement movement;   // Dùng để gọi animation chết khi HP về 0

    // Properties (thuộc tính chỉ đọc từ bên ngoài — bên ngoài không thể gán trực tiếp)
    public float CurrentHealth => currentHealth;                                        // Máu hiện tại (số thực)
    public float HealthPercent => maxHealth > 0f ? currentHealth / maxHealth : 0f;     // Tỉ lệ 0.0 → 1.0 dùng để vẽ thanh
    public bool  IsDead        => currentHealth <= 0f;                                  // true khi HP = 0

    void Awake() // Chạy 1 lần ngay khi GameObject được tạo
    {
        movement      = GetComponent<PlayerMovement>(); // Lấy script PlayerMovement trên cùng GameObject
        currentHealth = maxHealth;                      // Bắt đầu game với máu đầy
    }

    // Gọi khi quái chạm vào player (từ EnemyDamage.cs)
    public void TakeDamage(float amount)
    {
        if (IsDead || amount <= 0f) return;                       // Đã chết hoặc damage âm → bỏ qua
        currentHealth = Mathf.Max(0f, currentHealth - amount);    // Trừ máu, không cho xuống dưới 0
        if (IsDead) Die();                                        // Nếu máu vừa về 0 → kích hoạt chết
    }

    // Gọi khi cần hồi máu (hiện chưa dùng nhưng để sẵn cho tương lai)
    public void Heal(float amount)
    {
        if (IsDead || amount <= 0f) return;
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount); // Hồi máu, không vượt quá tối đa
    }

    void Die()
    {
        if (movement != null)
            movement.Die(); // Báo sang PlayerMovement để phát animation chết và khóa input
    }
}
