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

    float currentHealth;
    float lastHitTime = -999f;
    PlayerMovement movement;
    SpriteRenderer spriteRenderer;
    Coroutine flashRoutine;
    Color baseColor = Color.white;

    public float CurrentHealth => currentHealth;
    public float MaxHealth     => maxHealth; // alias viết hoa — TestBoss.cs (Mr.X) của Anh Huy dùng tên này
    public float HealthPercent => maxHealth > 0f ? currentHealth / maxHealth : 0f;
    public bool  IsDead        => currentHealth <= 0f;

    void Awake()
    {
        movement       = GetComponent<PlayerMovement>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        currentHealth  = maxHealth;

        if (spriteRenderer != null)
            baseColor = spriteRenderer.color;
    }

    public void TakeDamage(float amount)
    {
        // Không có vị trí nguồn đánh → không knockback (dir = zero)
        TakeDamage(amount, transform.position);
    }

    public void TakeDamage(float amount, Vector2 sourcePosition)
    {
        if (IsDead || amount <= 0f) return;

        // I-FRAME: đang bất tử thì bỏ qua đòn đánh
        if (Time.time - lastHitTime < invincibilityDuration) return;

        lastHitTime   = Time.time;
        currentHealth = Mathf.Max(0f, currentHealth - amount);

        if (IsDead)
        {
            Die();
            return;
        }

        // KNOCKBACK: đẩy lùi theo hướng từ nguồn đánh về phía player
        Vector2 dir = ((Vector2)transform.position - sourcePosition).normalized;
        if (dir != Vector2.zero && movement != null)
            movement.ApplyKnockback(dir * knockbackForce);

        StartFlash();
    }

    // Sát thương "thật" bỏ qua i-frame — dùng cho skill hút máu ẩn của boss Mr.X (TestBoss.cs):
    // đòn hút máu luôn phải trúng, không được né bằng thời gian bất tử thông thường.
    public void TakeTrueDamage(float amount)
    {
        if (IsDead || amount <= 0f) return;

        currentHealth = Mathf.Max(0f, currentHealth - amount);

        if (IsDead)
        {
            Die();
            return;
        }

        StartFlash();
    }

    // Đổi màu áo "nền" của Player — dùng cho hiệu ứng nhiễm độc (PlayerInfection).
    // Tương thích với FlashRoutine: baseColor là màu được flash trả về sau mỗi lần trúng đòn.
    public void SetBaseTint(Color tint)
    {
        baseColor = tint;
        if (spriteRenderer != null && flashRoutine == null)
            spriteRenderer.color = baseColor;
    }

    public void Heal(float amount)
    {
        if (IsDead || amount <= 0f) return;
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
    }

    // FLASH — nhấp nháy sprite trong suốt thời gian bất tử
    void StartFlash()
    {
        if (spriteRenderer == null) return;

        // Bị đánh liên tiếp: dừng flash cũ + trả màu gốc trước khi flash mới
        if (flashRoutine != null)
        {
            StopCoroutine(flashRoutine);
            spriteRenderer.color = baseColor;
        }

        flashRoutine = StartCoroutine(FlashRoutine());
    }

    IEnumerator FlashRoutine()
    {
        var faded   = new Color(baseColor.r, baseColor.g, baseColor.b, 0.35f);
        float elapsed = 0f;
        bool  dim     = false;

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

    void Die()
    {
        // Trả sprite về màu gốc để animation chết không bị kẹt alpha
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
