using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("Máu")]
    public float maxHealth = 100f;

    [Tooltip("Ẩn thanh máu khi còn đủ máu 100%")]
    public bool hideBarWhenFull = true;

    float currentHealth;
    WorldHealthBar healthBar;
    PlayerMovement movement;

    public float CurrentHealth => currentHealth;
    public float HealthPercent => maxHealth > 0f ? currentHealth / maxHealth : 0f;
    public bool IsDead => currentHealth <= 0f;

    void Awake()
    {
        movement = GetComponent<PlayerMovement>();
        currentHealth = maxHealth;
        CreateHealthBar();
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

        // Thanh máu player: rộng hơn, màu xanh, hiện cao hơn đầu
        healthBar.barWidth = 1f;
        healthBar.barHeight = 0.1f;
        healthBar.localOffset = new Vector3(0f, 1.4f, 0f);
        healthBar.fullColor = new Color(0.2f, 0.55f, 0.95f, 1f);
        healthBar.lowColor = new Color(0.9f, 0.2f, 0.15f, 1f);
        healthBar.lowHealthThreshold = 0.35f;
        healthBar.sortingOrder = 150;
        healthBar.Build();
    }

    public void TakeDamage(float amount)
    {
        if (IsDead || amount <= 0f) return;

        currentHealth = Mathf.Max(0f, currentHealth - amount);
        RefreshBar();

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
        if (movement != null)
            movement.Die();
    }
}
