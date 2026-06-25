using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("Máu")]
    public float maxHealth = 100f;

    float currentHealth;
    PlayerMovement movement;

    public float CurrentHealth => currentHealth;
    public float HealthPercent => maxHealth > 0f ? currentHealth / maxHealth : 0f;
    public bool  IsDead        => currentHealth <= 0f;

    void Awake()
    {
        movement      = GetComponent<PlayerMovement>();
        currentHealth = maxHealth;
    }

    public void TakeDamage(float amount)
    {
        if (IsDead || amount <= 0f) return;
        currentHealth = Mathf.Max(0f, currentHealth - amount);
        if (IsDead) Die();
    }

    public void Heal(float amount)
    {
        if (IsDead || amount <= 0f) return;
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
    }

    void Die()
    {
        if (movement != null)
            movement.Die();
    }
}
