using UnityEngine;

// AI của Boss2 (Level 2): đứng yên canh phòng, Player vào tầm phát hiện thì đuổi theo;
// đủ gần thì bấm Trigger "Attack" (chỉ để đổi animation — sát thương thật vẫn do
// component EnemyDamage xử lý qua va chạm, giống hệt cách Quai1 đang hoạt động).
// Player ở khoảng giữa Attack Range và Shoot Range thì boss bắn đạn tầm xa (BossProjectile).
[RequireComponent(typeof(Boss2Health))]
public class Boss2AI : MonoBehaviour
{
    [Header("Đuổi theo Player")]
    [Tooltip("Player vào trong bán kính này (và không bị tường che) thì boss bắt đầu đuổi")]
    public float detectRange = 6f;

    [Tooltip("Player chạy xa hơn khoảng này thì boss bỏ cuộc, quay về canh chỗ cũ")]
    public float loseRange = 8f;

    public float chaseSpeed = 2.2f;

    [Tooltip("Bao lâu tính lại hướng đuổi 1 lần — chống giật hướng")]
    public float chaseRepathInterval = 0.2f;

    [Header("Tấn công cận chiến (chỉ đổi animation, sát thương do EnemyDamage lo)")]
    public float attackRange = 1.3f;
    public float attackCooldown = 1.2f;

    [Header("Bắn đạn tầm xa (Player ở giữa Attack Range và Shoot Range thì bắn)")]
    [Tooltip("Để trống = không có kỹ năng bắn đạn")]
    public BossProjectile projectilePrefab;
    [Tooltip("Trong tầm này (và ngoài Attack Range cận chiến) thì boss bắn đạn")]
    public float shootRange = 4f;
    public float shootCooldown = 2f;
    [Tooltip("Khoảng cách spawn đạn tính từ tâm boss, theo hướng Player")]
    public float projectileSpawnOffset = 0.5f;

    [Header("Âm thanh bắn đạn")]
    public AudioClip shootSound;
    [Range(0f, 1f)] public float shootVolume = 1f;

    [Header("Va chạm")]
    public LayerMask wallLayer;

    [Header("Âm thanh")]
    public AudioClip attackSound;
    [Range(0f, 1f)] public float attackVolume = 1f;

    Rigidbody2D rb;
    Animator animator;
    Collider2D bodyCollider;
    Boss2Health health;

    Transform player;
    PlayerHealth playerHealth;

    bool isChasing;
    float repathTimer;
    float lastAttackTime = -999f;
    float lastShootTime = -999f;
    Vector2 moveDir;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        animator = GetComponent<Animator>();
        bodyCollider = GetComponent<Collider2D>();
        health = GetComponent<Boss2Health>();
    }

    void Start()
    {
        var playerGo = GameObject.FindGameObjectWithTag("Player");
        if (playerGo != null)
        {
            player = playerGo.transform;
            playerHealth = playerGo.GetComponent<PlayerHealth>();
        }
    }

    void Update()
    {
        if (health != null && health.IsDead) return;

        UpdateChaseState();

        if (isChasing)
        {
            repathTimer -= Time.deltaTime;
            if (repathTimer <= 0f)
            {
                RepathTowardsPlayer();
                repathTimer = chaseRepathInterval;
            }

            TryAttack();
            TryShoot();
        }
        else
        {
            moveDir = Vector2.zero;
        }

        if (animator != null)
        {
            animator.SetFloat("MoveX", moveDir.x);
            animator.SetFloat("MoveY", moveDir.y);
        }
    }

    void FixedUpdate()
    {
        if (health != null && health.IsDead)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        bool tooClose = isChasing && player != null &&
            Vector2.Distance(transform.position, player.position) <= attackRange;

        rb.linearVelocity = (isChasing && !tooClose) ? moveDir * chaseSpeed : Vector2.zero;
    }

    void UpdateChaseState()
    {
        if (player == null || (playerHealth != null && playerHealth.IsDead))
        {
            isChasing = false;
            return;
        }

        float dist = Vector2.Distance(transform.position, player.position);

        if (!isChasing)
        {
            if (dist <= detectRange && HasLineOfSightToPlayer())
            {
                isChasing = true;
                repathTimer = 0f;
            }
        }
        else if (dist > loseRange || !HasLineOfSightToPlayer())
        {
            isChasing = false;
        }
    }

    // Bắn tia từ NGOÀI collider của chính boss (không phải từ tâm) — tránh tia tự trúng ngay
    // chính nó nếu Wall Layer lỡ trùng layer của Boss2, khiến isChasing không bao giờ bật được.
    bool HasLineOfSightToPlayer()
    {
        Vector2 toPlayer = (Vector2)player.position - (Vector2)transform.position;
        if (toPlayer == Vector2.zero) return true;

        float originOffset = bodyCollider != null
            ? bodyCollider.bounds.extents.magnitude + 0.05f
            : 0.2f;

        Vector2 dir = toPlayer.normalized;
        Vector2 origin = (Vector2)transform.position + dir * originOffset;
        float castDistance = Mathf.Max(0f, toPlayer.magnitude - originOffset);

        RaycastHit2D hit = Physics2D.Raycast(origin, dir, castDistance, wallLayer);
        return hit.collider == null;
    }

    void RepathTowardsPlayer()
    {
        moveDir = ((Vector2)player.position - (Vector2)transform.position).normalized;
    }

    void TryAttack()
    {
        if (player == null) return;
        if (Vector2.Distance(transform.position, player.position) > attackRange) return;
        if (Time.time - lastAttackTime < attackCooldown) return;

        lastAttackTime = Time.time;
        animator?.SetTrigger("Attack");
        AudioOneShot.Play(attackSound, transform.position, attackVolume);
    }

    // Bắn đạn khi Player ở khoảng giữa Attack Range (đấm được) và Shoot Range (phát hiện được)
    // — quá gần thì ưu tiên đấm (TryAttack), không cần bắn đạn chồng lên.
    void TryShoot()
    {
        if (player == null || projectilePrefab == null) return;

        float dist = Vector2.Distance(transform.position, player.position);
        if (dist <= attackRange || dist > shootRange) return;
        if (Time.time - lastShootTime < shootCooldown) return;

        lastShootTime = Time.time;

        Vector2 dir = ((Vector2)player.position - (Vector2)transform.position).normalized;
        if (dir == Vector2.zero) return;

        // Xoay sprite viên đạn theo đúng hướng bắn — giống cách PlayerAttack.Throw() làm với bình ném
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        Vector3 spawnPos = transform.position + (Vector3)(dir * projectileSpawnOffset);
        var proj = Instantiate(projectilePrefab, spawnPos, Quaternion.Euler(0f, 0f, angle));
        proj.Launch(dir);

        animator?.SetTrigger("Shoot");
        AudioOneShot.Play(shootSound, transform.position, shootVolume);
    }
}
