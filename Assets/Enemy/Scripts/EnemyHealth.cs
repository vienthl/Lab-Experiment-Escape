using System;
using UnityEngine;

// Quản lý máu quái và thanh máu phía trên đầu.
// Script này được tạo tự động bởi Quai1AutoMove khi showHealthBar = true.
public class EnemyHealth : MonoBehaviour
{
    [Header("Máu")]
    public float maxHealth = 100f; // Máu tối đa — Quai1AutoMove ghi đè giá trị này khi khởi động

    [Tooltip("Ẩn thanh máu khi còn đủ máu (100%) — tắt để thanh luôn hiện")]
    public bool hideBarWhenFull = false;

    float currentHealth;     // Máu hiện tại
    WorldHealthBar healthBar; // Tham chiếu đến thanh máu world-space phía trên đầu quái

    // Properties chỉ đọc
    public float CurrentHealth => currentHealth;
    public float HealthPercent => maxHealth > 0f ? currentHealth / maxHealth : 0f; // 0.0 → 1.0
    public bool  IsDead        => currentHealth <= 0f;

    // Events — các script khác có thể đăng ký lắng nghe khi quái bị damage hoặc chết
    // Ví dụ: OnDied?.Invoke(this) → thông báo cho hệ thống quest, điểm số,...
    public event Action<EnemyHealth> OnDamaged;
    public event Action<EnemyHealth> OnDied;

    void Awake()
    {
        currentHealth = maxHealth;
        CreateHealthBar(); // Tạo thanh máu ngay khi quái xuất hiện
        RefreshBar();      // Cập nhật lần đầu (đang đầy máu)
    }

    // Gọi từ Quai1AutoMove để đặt lại maxHealth sau khi Awake chạy
    public void Configure(float health)
    {
        maxHealth     = Mathf.Max(1f, health); // Không cho maxHealth < 1
        currentHealth = maxHealth;
        RefreshBar();
    }

    // Tạo GameObject con "HealthBar" và gắn WorldHealthBar vào
    void CreateHealthBar()
    {
        // Tìm xem đã có child "HealthBar" chưa (tránh tạo trùng)
        var barGo = transform.Find("HealthBar");
        if (barGo == null)
        {
            barGo = new GameObject("HealthBar").transform;
            barGo.SetParent(transform, false); // false → giữ nguyên localPosition/scale
        }

        healthBar = barGo.GetComponent<WorldHealthBar>();
        if (healthBar == null)
            healthBar = barGo.gameObject.AddComponent<WorldHealthBar>(); // Thêm component nếu chưa có

        healthBar.Build(); // Tạo các SpriteRenderer con (nền + fill)
    }

    // Gọi từ Projectile khi bình trúng quái
    public void TakeDamage(float amount)
    {
        if (IsDead || amount <= 0f) return;

        currentHealth = Mathf.Max(0f, currentHealth - amount); // Trừ máu, không xuống dưới 0
        RefreshBar();             // Cập nhật độ dài thanh máu ngay lập tức
        OnDamaged?.Invoke(this);  // Kích hoạt event (nếu có script nào đang lắng nghe)

        if (IsDead) Die();
    }

    public void Heal(float amount)
    {
        if (IsDead || amount <= 0f) return;
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        RefreshBar();
    }

    // Đồng bộ dữ liệu máu lên thanh máu visual
    void RefreshBar()
    {
        if (healthBar == null) return;

        // Ẩn/hiện thanh theo cài đặt hideBarWhenFull
        // !hideBarWhenFull || HealthPercent < 1f → luôn hiện NẾU không ẩn, hoặc hiện khi máu không đầy
        healthBar.gameObject.SetActive(!hideBarWhenFull || HealthPercent < 1f);
        healthBar.SetFill(HealthPercent); // Cập nhật độ dài fill (0.0 → 1.0)
    }

    void Die()
    {
        OnDied?.Invoke(this); // Thông báo cho các listener

        // Tắt script di chuyển → quái đứng yên khi chết
        var move = GetComponent<Quai1AutoMove>();
        if (move != null) move.enabled = false;

        // Dừng vật lý → không trượt thêm sau khi chết
        var rb = GetComponent<Rigidbody2D>();
        if (rb != null) rb.linearVelocity = Vector2.zero;

        gameObject.SetActive(false); // Ẩn quái khỏi scene
    }
}
