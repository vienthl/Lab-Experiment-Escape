using System;
using System.Collections;
using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [Header("Máu")]
    public float maxHealth = 100f;

    [Tooltip("Hiện thanh máu phía trên đầu (tắt thì quái vẫn có máu bình thường)")]
    public bool showBar = true;

    [Tooltip("Ẩn thanh máu khi còn đủ máu (100%)")]
    public bool hideBarWhenFull = false;

    [Header("Chết")]
    [Tooltip("Thời gian mờ dần rồi biến mất (giây)")]
    public float deathFadeDuration = 0.25f;

    float currentHealth;
    WorldHealthBar healthBar;

    public float CurrentHealth => currentHealth;
    public float HealthPercent => maxHealth > 0f ? currentHealth / maxHealth : 0f;
    public bool  IsDead        => currentHealth <= 0f;

    public event Action<EnemyHealth> OnDamaged;
    public event Action<EnemyHealth> OnDied;

    void Awake()
    {
        currentHealth = maxHealth;
    }

    // Tạo thanh máu ở Start (không phải Awake) để Quai1AutoMove
    // kịp set showBar sau khi AddComponent lúc runtime
    void Start()
    {
        if (showBar)
        {
            CreateHealthBar();
            RefreshBar();
        }
    }

    public void Configure(float health)
    {
        maxHealth     = Mathf.Max(1f, health);
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
        if (IsDead || amount <= 0f) return;

        currentHealth = Mathf.Max(0f, currentHealth - amount);
        RefreshBar();
        OnDamaged?.Invoke(this);

        if (IsDead) Die();
    }

    public void Heal(float amount)
    {
        if (IsDead || amount <= 0f) return;
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        RefreshBar();
    }

    void RefreshBar()
    {
        if (healthBar == null) return;
        healthBar.gameObject.SetActive(!hideBarWhenFull || HealthPercent < 1f);
        healthBar.SetFill(HealthPercent);
    }

    void Die()
    {
        OnDied?.Invoke(this);

        // Tắt ngay mọi hành vi: xác không đuổi, không cắn, không cản đường
        var move = GetComponent<Quai1AutoMove>();
        if (move != null) move.enabled = false;

        var damage = GetComponent<EnemyDamage>();
        if (damage != null) damage.enabled = false;

        foreach (var col in GetComponentsInChildren<Collider2D>())
            col.enabled = false;

        var rb = GetComponent<Rigidbody2D>();
        if (rb != null) rb.linearVelocity = Vector2.zero;

        StartCoroutine(FadeOutAndDeactivate());
    }

    // Mờ dần toàn bộ sprite (kể cả thanh máu) rồi tắt object
    IEnumerator FadeOutAndDeactivate()
    {
        var renderers   = GetComponentsInChildren<SpriteRenderer>();
        var startColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
            startColors[i] = renderers[i].color;

        float elapsed = 0f;
        while (elapsed < deathFadeDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Clamp01(1f - elapsed / deathFadeDuration);

            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null) continue;
                var c = startColors[i];
                renderers[i].color = new Color(c.r, c.g, c.b, c.a * alpha);
            }

            yield return null;
        }

        gameObject.SetActive(false);
    }
}
