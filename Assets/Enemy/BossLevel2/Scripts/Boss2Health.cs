using System;
using System.Collections;
using UnityEngine;

// Máu + chết cho Boss2 (Level 2). Tương tự BossHealth cũ của Mr.X nhưng viết lại mới:
// - Ở mốc 50% máu, kích hoạt hiệu ứng tối màn hình quanh Player (VisionLimiter) đúng 1 lần.
// - Khi chết, NẾU lockdown phòng vẫn còn thời gian (chưa hết giờ) thì rơi bình "cure" cho Player nhặt.
public class Boss2Health : MonoBehaviour
{
    [Header("Máu")]
    public float maxHealth = 400f;

    [Header("Hiệu ứng tối màn hình khi máu ≤ 50%")]
    [Range(0f, 1f)]
    public float visionLimitThreshold = 0.5f;

    [Header("Chết")]
    [Tooltip("Thời gian mờ dần rồi biến mất (giây)")]
    public float deathFadeDuration = 0.25f;

    [Header("Rơi bình \"cure\" khi chết")]
    [Tooltip("Prefab WorldItem chứa bình thuốc giải nhiễm độc")]
    public WorldItem curePotionPrefab;

    [Tooltip("Chỉ rơi bình cure nếu lockdown phòng CÒN thời gian (chưa hết giờ). Không tìm thấy LockdownRoomController thì mặc định coi như còn thời gian.")]
    public bool onlyDropIfTimeRemaining = true;

    [Header("Âm thanh")]
    public AudioClip hitSound;
    public AudioClip deathSound;
    [Tooltip("Phát 1 lần khi máu xuống mốc kích hoạt vùng tối (vd sting/gầm báo hiệu đổi pha)")]
    public AudioClip visionTriggerSound;

    float currentHealth;
    bool visionLimitTriggered;

    public float CurrentHealth => currentHealth;
    public float HealthPercent => maxHealth > 0f ? currentHealth / maxHealth : 0f;
    public bool IsDead => currentHealth <= 0f;

    public event Action<Boss2Health> OnDamaged;
    public event Action<Boss2Health> OnDied;

    void Awake()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(float amount)
    {
        if (IsDead || amount <= 0f) return;

        currentHealth = Mathf.Max(0f, currentHealth - amount);
        AudioOneShot.Play(hitSound, transform.position);
        OnDamaged?.Invoke(this);

        if (!visionLimitTriggered && HealthPercent <= visionLimitThreshold)
        {
            visionLimitTriggered = true;
            VisionLimiter.Instance?.Activate();
            AudioOneShot.Play(visionTriggerSound, transform.position);
        }

        if (IsDead) Die();
    }

    void Die()
    {
        AudioOneShot.Play(deathSound, transform.position);
        OnDied?.Invoke(this);

        if (curePotionPrefab != null && ShouldDropCure())
            Instantiate(curePotionPrefab, transform.position, Quaternion.identity);

        var ai = GetComponent<Boss2AI>();
        if (ai != null) ai.enabled = false;

        var damage = GetComponent<EnemyDamage>();
        if (damage != null) damage.enabled = false;

        foreach (var col in GetComponentsInChildren<Collider2D>())
            col.enabled = false;

        var rb = GetComponent<Rigidbody2D>();
        if (rb != null) rb.linearVelocity = Vector2.zero;

        var animator = GetComponent<Animator>();
        if (animator != null) animator.SetTrigger("Die");

        StartCoroutine(FadeOutAndDeactivate());
    }

    bool ShouldDropCure()
    {
        if (!onlyDropIfTimeRemaining) return true;

        var lockdown = FindFirstObjectByType<LockdownRoomController>();
        if (lockdown == null) return true; // không có lockdown → không chặn, cứ rơi bình

        return !lockdown.HasTimeLimit || lockdown.RemainingTime > 0f;
    }

    IEnumerator FadeOutAndDeactivate()
    {
        var renderers = GetComponentsInChildren<SpriteRenderer>();
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
