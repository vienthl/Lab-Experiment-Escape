
using System;

using UnityEngine;

/// <summary>
/// Quản lý máu, thanh máu và trạng thái chết của boss Mr.X.
/// Thanh máu hiển thị qua BossHealthUI (OnGUI, top-center) — không dùng Canvas/Image nữa.
/// </summary>
public class BossHealth : MonoBehaviour
{
    [Header("Máu Boss")]
    [Tooltip("Máu tối đa của boss.")]
    [SerializeField] private float maxHealth = 100f;

    [Header("Animation Chết")]
    [Tooltip("Animator của boss. Nếu để trống, script sẽ tự tìm.")]
    [SerializeField] private Animator animator;

    [Tooltip("Tên Trigger chết trong Animator.")]
    [SerializeField] private string deathTriggerName = "Die";

    [Header("Rơi đồ khi chết")]
    [Tooltip("Prefab WorldItem chứa mũ khiên thủy tinh — luôn rơi khi MrX chết")]
    [SerializeField] private WorldItem shieldItemPrefab;

    private float currentHealth;
    private bool isDead;
    private bool healthBarVisible;

    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public bool IsDead => isDead;
    public float HealthPercent => maxHealth > 0f ? currentHealth / maxHealth : 0f;

    // Đọc bởi BossHealthUI (OnGUI) — thay cho việc SetActive Canvas/Image như bản cũ.
    public bool HealthBarVisible => healthBarVisible;

    public event Action<BossHealth> OnDamaged;
    public event Action<BossHealth> OnHealed;
    public event Action<BossHealth> OnDied;

    private void Awake()
    {
        maxHealth = Mathf.Max(1f, maxHealth);
        currentHealth = maxHealth;

        if (animator == null)
            animator = GetComponent<Animator>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    private void Start()
    {
        SetHealthBarVisible(false);
    }

    public void SetHealthBarVisible(bool visible)
    {
        healthBarVisible = visible && !isDead;
    }

    public void TakeDamage(float amount)
    {
        if (isDead || amount <= 0f)
            return;

        SetHealthBarVisible(true);
        currentHealth = Mathf.Max(0f, currentHealth - amount);

        OnDamaged?.Invoke(this);

        Debug.Log(
            $"Mr.X nhận {amount} sát thương. HP: {currentHealth}/{maxHealth}",
            this
        );

        if (currentHealth <= 0f)
            Die();
    }

    public void Heal(float amount)
    {
        if (isDead || amount <= 0f)
            return;

        float oldHealth = currentHealth;
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);

        if (currentHealth > oldHealth)
            OnHealed?.Invoke(this);
    }

    public void HealToFull()
    {
        if (isDead)
            return;

        currentHealth = maxHealth;
        SetHealthBarVisible(true);
        OnHealed?.Invoke(this);

        Debug.Log("Mr.X đã hồi đầy máu.", this);
    }

    private void Die()
    {
        if (isDead)
            return;

        isDead = true;

        GameManager.Instance?.AddScore(50);

        TestBoss bossAI = GetComponent<TestBoss>();
        if (bossAI != null)
        {
            bossAI.StopAllCoroutines();
            bossAI.enabled = false;
        }

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        Collider2D bossCollider = GetComponent<Collider2D>();
        if (bossCollider != null)
            bossCollider.enabled = false;

        if (animator != null)
        {
            animator.ResetTrigger("Attack");
            animator.SetBool("IsMoving", false);
            animator.SetTrigger(deathTriggerName);
        }
        else
        {
            Debug.LogWarning(
                "BossHealth không tìm thấy Animator của Mr.X.",
                this
            );
        }

        if (shieldItemPrefab != null)
            Instantiate(shieldItemPrefab, transform.position, Quaternion.identity);

        SetHealthBarVisible(false);
        OnDied?.Invoke(this);

        Debug.Log("Mr.X đã chết và bắt đầu animation chết.", this);
    }
}
