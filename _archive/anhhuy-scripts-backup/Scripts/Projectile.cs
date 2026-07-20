using UnityEngine;

public enum PotionType
{
    Fire,
    Lightning // Hiện tại dùng loại này làm bình băng
}

public class Projectile : MonoBehaviour
{
    [Header("Loại bình")]
    public PotionType potionType = PotionType.Fire;

    [Header("Hiệu ứng khi trúng")]
    [Tooltip("Kéo prefab FireEffect hoặc LightningEffect2 vào đây.")]
    public GameObject hitEffectPrefab;

    [Header("Tầm bay")]
    public float maxRange = 18f;

    [Header("Sát thương Boss")]
    [Tooltip("Bình lửa gây 5% máu tối đa của boss.")]
    [Range(0f, 1f)]
    public float fireBossDamagePercent = 0.05f;

    [Tooltip("Bình băng gây 10% máu tối đa của boss.")]
    [Range(0f, 1f)]
    public float iceBossDamagePercent = 0.10f;

    [Header("Làm chậm Boss")]
    [Tooltip("0.5 nghĩa là boss chỉ còn 50% tốc độ.")]
    [Range(0.1f, 1f)]
    public float bossSlowMultiplier = 0.5f;

    [Tooltip("Thời gian boss bị làm chậm.")]
    public float bossSlowDuration = 2f;

    private Rigidbody2D rb;
    private Vector2 startPosition;
    private bool hasHit;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        startPosition = transform.position;
    }

    public void Launch(Vector2 direction, float speed)
    {
        if (rb == null)
        {
            return;
        }

        rb.linearVelocity = direction.normalized * speed;
    }

    private void Update()
    {
        float distance = Vector2.Distance(
            transform.position,
            startPosition
        );

        if (distance >= maxRange)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasHit)
        {
            return;
        }

        // Không va chạm với Player.
        if (other.CompareTag("Player"))
        {
            return;
        }

        // Không va chạm với hiệu ứng.
        if (other.GetComponentInParent<HitEffect>() != null)
        {
            return;
        }

        // Hai bình thuốc không phá nhau.
        if (other.GetComponentInParent<Projectile>() != null)
        {
            return;
        }

        hasHit = true;

        BossHealth bossHealth =
            other.GetComponentInParent<BossHealth>();

        if (bossHealth != null && !bossHealth.IsDead)
        {
            DamageBoss(bossHealth);
        }
        else
        {
            DamageNormalEnemy(other);
        }

        SpawnHitEffect();

        Destroy(gameObject);
    }

    private void DamageBoss(BossHealth bossHealth)
    {
        if (potionType == PotionType.Fire)
        {
            float damage =
                bossHealth.MaxHealth * fireBossDamagePercent;

            bossHealth.TakeDamage(damage);

            UnityEngine.Debug.Log(
                $"Bình lửa gây {damage} sát thương lên boss.",
                bossHealth
            );

            return;
        }

        // Bình Lightning hiện được dùng làm bình băng.
        float iceDamage =
            bossHealth.MaxHealth * iceBossDamagePercent;

        bossHealth.TakeDamage(iceDamage);

        TestBoss bossAI = bossHealth.GetComponent<TestBoss>();

        if (bossAI != null)
        {
            bossAI.ApplySlow(
                bossSlowMultiplier,
                bossSlowDuration
            );
        }

        UnityEngine.Debug.Log(
            $"Bình băng gây {iceDamage} sát thương và làm chậm boss.",
            bossHealth
        );
    }

    private void DamageNormalEnemy(Collider2D other)
    {
        EnemyHealth enemy =
            other.GetComponentInParent<EnemyHealth>();

        if (enemy == null || enemy.IsDead)
        {
            return;
        }

        // Giữ sát thương quái thường như logic cũ.
        float damage = potionType == PotionType.Lightning
            ? enemy.maxHealth * 0.5f
            : enemy.maxHealth * 0.25f;

        enemy.TakeDamage(damage);
    }

    private void SpawnHitEffect()
    {
        if (hitEffectPrefab == null)
        {
            return;
        }

        Instantiate(
            hitEffectPrefab,
            transform.position,
            Quaternion.identity
        );
    }
}