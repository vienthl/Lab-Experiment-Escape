using UnityEngine;

// AI của Boss2 (Level 2): đứng yên canh phòng, Player vào tầm phát hiện thì đuổi theo;
// đủ gần thì bấm Trigger "Attack" (chỉ để đổi animation — sát thương thật vẫn do
// component EnemyDamage xử lý qua va chạm, giống hệt cách Quai1 đang hoạt động).
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

    [Header("Tấn công (chỉ đổi animation, sát thương do EnemyDamage lo)")]
    public float attackRange = 1.3f;
    public float attackCooldown = 1.2f;

    [Header("Va chạm")]
    public LayerMask wallLayer;

    Rigidbody2D rb;
    Animator animator;
    Collider2D bodyCollider;
    Boss2Health health;

    Transform player;
    PlayerHealth playerHealth;

    bool isChasing;
    float repathTimer;
    float lastAttackTime = -999f;
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

    bool HasLineOfSightToPlayer()
    {
        Vector2 origin = transform.position;
        Vector2 toPlayer = (Vector2)player.position - origin;
        RaycastHit2D hit = Physics2D.Raycast(origin, toPlayer.normalized, toPlayer.magnitude, wallLayer);
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
    }
}
