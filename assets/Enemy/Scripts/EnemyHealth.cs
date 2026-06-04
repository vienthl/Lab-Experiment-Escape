using System;
using UnityEngine;

/// <summary>
/// Quản lý máu quái + cập nhật thanh máu phía trên đầu.
/// </summary>
public class EnemyHealth : MonoBehaviour
{
    [Header("Máu")]
    public float maxHealth = 100f;

    [Tooltip("Ẩn thanh máu khi còn đủ máu (100%)")]
    public bool hideBarWhenFull = false;

    float currentHealth;
    WorldHealthBar healthBar;

    public float CurrentHealth => currentHealth;
    public float HealthPercent => maxHealth > 0f ? currentHealth / maxHealth : 0f;
    public bool IsDead => currentHealth <= 0f;

    public event Action<EnemyHealth> OnDamaged;
    public event Action<EnemyHealth> OnDied;

    void Awake()
    {
        currentHealth = maxHealth;
        CreateHealthBar();
        RefreshBar();
    }

    public void Configure(float health)
    {
        maxHealth = Mathf.Max(1f, health);
        currentHealth = maxHealth;
        RefreshBar();
    }

    void CreateHealthBar()
    {
        var barGo = transform.Find("HealthBar");
        if (barGo == null)
        {
            barGo = new GameObject("HealthBar").transform;
            barGo.SetParent(transform, false);
        }

        healthBar = barGo.GetComponent<WorldHealthBar>();
        if (healthBar == null)
            healthBar = barGo.gameObject.AddComponent<WorldHealthBar>();

        healthBar.Build();
    }

    public void TakeDamage(float amount)
    {
        if (IsDead || amount <= 0f)
            return;

        currentHealth = Mathf.Max(0f, currentHealth - amount);
        RefreshBar();
        OnDamaged?.Invoke(this);

        if (IsDead)
            Die();
    }

    public void Heal(float amount)
    {
        if (IsDead || amount <= 0f)
            return;

        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        RefreshBar();
    }

    void RefreshBar()
    {
        if (healthBar == null)
            return;

        healthBar.gameObject.SetActive(!hideBarWhenFull || HealthPercent < 1f);
        healthBar.SetFill(HealthPercent);
    }

    void Die()
    {
        OnDied?.Invoke(this);

        var move = GetComponent<Quai1AutoMove>();
        if (move != null)
            move.enabled = false;

        var rb = GetComponent<Rigidbody2D>();
        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        gameObject.SetActive(false);
    }
}
