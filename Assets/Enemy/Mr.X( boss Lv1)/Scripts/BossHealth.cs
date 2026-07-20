
using System;

using UnityEngine;

/// <summary>
/// Quản lý máu, thanh máu và trạng thái chết của boss Mr.X.
/// </summary>
public class BossHealth : MonoBehaviour
{
    [Header("Máu Boss")]
    [Tooltip("Máu tối đa của boss.")]
    [SerializeField] private float maxHealth = 100f;

    [Header("UI Thanh Máu")]
    [Tooltip("Kéo object BossHealthBar trong Canvas vào đây.")]
    [SerializeField] private BossHealthBar bossHealthBar;

    [Header("Animation Chết")]
    [Tooltip("Animator của boss. Nếu để trống, script sẽ tự tìm.")]
    [SerializeField] private Animator animator;

    [Tooltip("Tên Trigger chết trong Animator.")]
    [SerializeField] private string deathTriggerName = "Die";

    private float currentHealth;
    private bool isDead;

    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public bool IsDead => isDead;
    public float HealthPercent => maxHealth > 0f ? currentHealth / maxHealth : 0f;

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
        RefreshHealthBar();
        SetHealthBarVisible(false);
    }

    public void SetHealthBarVisible(bool visible)
    {
        if (bossHealthBar == null)
            return;

        if (visible && !isDead)
            bossHealthBar.Show();
        else
            bossHealthBar.Hide();
    }

    public void TakeDamage(float amount)
    {
        if (isDead || amount <= 0f)
            return;

        SetHealthBarVisible(true);
        currentHealth = Mathf.Max(0f, currentHealth - amount);

        RefreshHealthBar();
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

        RefreshHealthBar();

        if (currentHealth > oldHealth)
            OnHealed?.Invoke(this);
    }

    public void HealToFull()
    {
        if (isDead)
            return;

        currentHealth = maxHealth;
        SetHealthBarVisible(true);
        RefreshHealthBar();
        OnHealed?.Invoke(this);

        Debug.Log("Mr.X đã hồi đầy máu.", this);
    }

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

    private void Die()
    {
        if (isDead)
            return;

        isDead = true;

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

        SetHealthBarVisible(false);
        OnDied?.Invoke(this);

        Debug.Log("Mr.X đã chết và bắt đầu animation chết.", this);
    }
}
