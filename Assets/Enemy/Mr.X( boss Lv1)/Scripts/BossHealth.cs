using System;

using UnityEngine;

/// <summary>
/// Quản lý máu riêng của boss Mr.X.
/// Đồng bộ lượng máu với BossHealthBar trên Canvas
/// và xử lý khi boss chết.
/// </summary>
public class BossHealth : MonoBehaviour
{
    [Header("Máu Boss")]
    [Tooltip("Máu tối đa của boss.")]
    [SerializeField] private float maxHealth = 100f;

    [Header("UI Thanh Máu")]
    [Tooltip("Kéo object Canvas/BossHealthBar trong Hierarchy vào đây.")]
    [SerializeField] private BossHealthBar bossHealthBar;

    [Header("Animation Chết")]
    [Tooltip("Animator của Mr.X. Nếu để trống, script sẽ tự tìm trên object boss.")]
    [SerializeField] private Animator animator;

    [Tooltip("Tên Trigger chạy animation chết trong Animator.")]
    [SerializeField] private string deathTriggerName = "Die";

    [Header("Testing")]
    [Tooltip("Bật để dùng bàn phím test thanh máu.")]
    [SerializeField] private bool enableHealthTest = true;

    [Tooltip("Lượng máu bị trừ hoặc hồi mỗi lần test.")]
    [SerializeField] private float testDamage = 10f;

    // Máu hiện tại của boss.
    private float currentHealth;

    // Boss đã chết hay chưa.
    private bool isDead;

    // Cho script khác đọc thông tin máu nhưng không sửa trực tiếp.
    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;

    public float HealthPercent =>
        maxHealth > 0f ? currentHealth / maxHealth : 0f;

    public bool IsDead => isDead;

    // Sự kiện để sau này kết nối âm thanh, nhiệm vụ hoặc hiệu ứng.
    public event Action<BossHealth> OnDamaged;
    public event Action<BossHealth> OnDied;

    private void Awake()
    {
        // Không cho maxHealth nhỏ hơn 1.
        maxHealth = Mathf.Max(1f, maxHealth);

        // Khi bắt đầu, boss có đầy máu.
        currentHealth = maxHealth;

        // Nếu chưa kéo Animator vào Inspector,
        // script sẽ tự lấy Animator trên object Mr.X.
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
    }

    private void Start()
    {
        // Hiển thị thanh máu đầy khi bắt đầu scene.
        RefreshHealthBar();
    }

    private void Update()
    {
        // Đoạn này chỉ dùng để test.
        // Có thể bỏ tick Enable Health Test sau khi kiểm tra xong.
        if (!enableHealthTest || isDead)
        {
            return;
        }

        // Nhấn H để boss mất máu.
        if (Input.GetKeyDown(KeyCode.H))
        {
            TakeDamage(testDamage);
        }

        // Nhấn J để boss hồi máu.
        if (Input.GetKeyDown(KeyCode.J))
        {
            Heal(testDamage);
        }
    }

    /// <summary>
    /// Gây sát thương lên boss.
    /// Sau này script tấn công của Player sẽ gọi hàm này.
    /// </summary>
    public void TakeDamage(float amount)
    {
        // Boss chết rồi hoặc damage không hợp lệ thì không xử lý.
        if (isDead || amount <= 0f)
        {
            return;
        }

        // Trừ máu và không cho máu xuống dưới 0.
        currentHealth = Mathf.Max(0f, currentHealth - amount);

        // Cập nhật sprite thanh máu.
        RefreshHealthBar();

        // Thông báo boss vừa nhận sát thương.
        OnDamaged?.Invoke(this);

        Debug.Log(
            $"Mr.X nhận {amount} sát thương. HP: {currentHealth}/{maxHealth}",
            this
        );

        // Nếu hết máu thì xử lý chết.
        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    /// <summary>
    /// Hồi máu cho boss.
    /// </summary>
    public void Heal(float amount)
    {
        // Boss chết rồi hoặc lượng hồi không hợp lệ thì bỏ qua.
        if (isDead || amount <= 0f)
        {
            return;
        }

        // Hồi máu nhưng không vượt quá maxHealth.
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);

        // Cập nhật lại thanh máu.
        RefreshHealthBar();

        Debug.Log(
            $"Mr.X hồi {amount} máu. HP: {currentHealth}/{maxHealth}",
            this
        );
    }

    /// <summary>
    /// Gửi máu hiện tại sang script BossHealthBar
    /// để đổi sprite thanh máu.
    /// </summary>
    private void RefreshHealthBar()
    {
        if (bossHealthBar == null)
        {
            Debug.LogWarning(
                "BossHealth chưa được gán BossHealthBar trong Inspector.",
                this
            );

            return;
        }

        bossHealthBar.SetHealth(currentHealth, maxHealth);
    }

    /// <summary>
    /// Xử lý khi boss hết máu.
    /// </summary>
    private void Die()
    {
        // Tránh chạy hàm chết nhiều lần.
        if (isDead)
        {
            return;
        }

        isDead = true;

        // Dừng AI đuổi và đánh Player.
        TestBoss bossAI = GetComponent<TestBoss>();

        if (bossAI != null)
        {
            bossAI.StopAllCoroutines();
            bossAI.enabled = false;
        }

        // Dừng boss di chuyển.
        Rigidbody2D rb = GetComponent<Rigidbody2D>();

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }

        // Chạy animation chết.
        if (animator != null)
        {
            // Dừng trạng thái chạy trước khi chết.
            animator.SetBool("IsMoving", false);

            // Kích hoạt Trigger chết.
            animator.SetTrigger(deathTriggerName);
        }
        else
        {
            Debug.LogWarning(
                "BossHealth không tìm thấy Animator trên Mr.X.",
                this
            );
        }

        // Thông báo boss đã chết.
        OnDied?.Invoke(this);

        Debug.Log("Mr.X đã hết máu và bắt đầu animation chết.", this);

        // Không tắt gameObject tại đây,
        // vì nếu tắt ngay thì animation chết sẽ không kịp chạy.
    }
}