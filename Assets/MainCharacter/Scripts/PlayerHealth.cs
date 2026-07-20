using System.Collections;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("Máu")]
    public float maxHealth = 100f;

    [Header("Trúng đòn")]
    [Tooltip("Thời gian bất tử sau mỗi lần trúng đòn (giây)")]
    public float invincibilityDuration = 0.6f;

    [Tooltip("Lực đẩy lùi khi trúng đòn")]
    public float knockbackForce = 7f;

    [Tooltip("Nhịp nhấp nháy sprite khi bất tử (giây)")]
    public float flashInterval = 0.08f;

    private float currentHealth;
    private float lastHitTime = -999f;
    private PlayerMovement movement;
    private SpriteRenderer spriteRenderer;
    private Coroutine flashRoutine;
    private Color baseColor = Color.white;

    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public float HealthPercent => maxHealth > 0f ? currentHealth / maxHealth : 0f;
    public bool IsDead => currentHealth <= 0f;

    private void Awake()
    {
        maxHealth = Mathf.Max(1f, maxHealth);
        movement = GetComponent<PlayerMovement>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        currentHealth = maxHealth;

        if (spriteRenderer != null)
            baseColor = spriteRenderer.color;
    }

    public void TakeDamage(float amount)
    {
        TakeDamage(amount, transform.position);
    }

    public void TakeDamage(float amount, Vector2 sourcePosition)
    {
        if (IsDead || amount <= 0f)
            return;

        if (Time.time - lastHitTime < invincibilityDuration)
            return;

        lastHitTime = Time.time;
        currentHealth = Mathf.Max(0f, currentHealth - amount);

        if (IsDead)
        {
            Die();
            return;
        }

        Vector2 direction =
            ((Vector2)transform.position - sourcePosition).normalized;

        if (direction != Vector2.zero && movement != null)
            movement.ApplyKnockback(direction * knockbackForce);

        StartFlash();
    }

    /// <summary>
    /// Sát thương bỏ qua i-frame và không gây knockback.
    /// Dùng cho skill hút máu của Boss.
    /// </summary>
    public void TakeTrueDamage(float amount)
    {
        if (IsDead || amount <= 0f)
            return;

        currentHealth = Mathf.Max(0f, currentHealth - amount);

        if (IsDead)
        {
            Die();
            return;
        }

        StartFlash();
    }

    public void Heal(float amount)
    {
        if (IsDead || amount <= 0f)
            return;

        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
    }

    private void StartFlash()
    {
        if (spriteRenderer == null)
            return;

        if (flashRoutine != null)
        {
            StopCoroutine(flashRoutine);
            spriteRenderer.color = baseColor;
        }

        flashRoutine = StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        Color faded = new Color(
            baseColor.r,
            baseColor.g,
            baseColor.b,
            0.35f
        );

        float elapsed = 0f;
        bool dim = false;

        while (elapsed < invincibilityDuration)
        {
            dim = !dim;
            spriteRenderer.color = dim ? faded : baseColor;

            yield return new WaitForSeconds(flashInterval);
            elapsed += flashInterval;
        }

        spriteRenderer.color = baseColor;
        flashRoutine = null;
    }

    private void Die()
    {
        if (flashRoutine != null)
        {
            StopCoroutine(flashRoutine);
            flashRoutine = null;
        }

        if (spriteRenderer != null)
            spriteRenderer.color = baseColor;

        if (movement != null)
            movement.Die();
    }
}
