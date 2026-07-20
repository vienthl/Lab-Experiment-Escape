using UnityEngine;

/// <summary>
/// Quái tự di chuyển 4 hướng (lên / xuống / trái / phải),
/// đổi hướng ngẫu nhiên theo thời gian hoặc khi gặp vật cản.
/// </summary>
[RequireComponent(typeof(EnemyHealth))]
public class Quai1AutoMove : MonoBehaviour
{
    public enum Direction { Up, Down, Left, Right }

    [Header("Di chuyển")]
    [Tooltip("Tốc độ di chuyển (đơn vị Unity/giây)")]
    public float moveSpeed = 2f;

    [Tooltip("Sau bao nhiêu giây thì thử đổi hướng ngẫu nhiên")]
    public float changeDirectionInterval = 2f;

    [Header("Va chạm")]
    [Tooltip("Layer chứa tường / vật cản (trong scene đang dùng Default)")]
    public LayerMask wallLayer;

    [Tooltip("Độ dài tia kiểm tra phía trước — càng lớn càng phát hiện tường sớm hơn")]
    public float wallCheckDistance = 0.4f;

    [Header("Đuổi theo player")]
    [Tooltip("Player vào trong bán kính này (và không bị tường che) thì quái đuổi theo")]
    public float detectRange = 4f;

    [Tooltip("Player chạy xa hơn khoảng này thì quái bỏ cuộc — nên > detectRange để tránh nhấp nháy trạng thái")]
    public float loseRange = 5.5f;

    [Tooltip("Tốc độ khi đang đuổi")]
    public float chaseSpeed = 2.5f;

    [Tooltip("Bao lâu tính lại hướng đuổi một lần — chống giật hướng khi player ở đường chéo")]
    public float chaseRepathInterval = 0.2f;

    [Tooltip("Đứng lại bao lâu sau khi cắn trúng player (giây)")]
    public float attackPauseDuration = 0.35f;

    [Header("Máu")]
    [Tooltip("Hiện thanh máu phía trên đầu quái (tắt thì quái VẪN có máu, chỉ ẩn thanh)")]
    public bool showHealthBar = true;
    public float maxHealth = 100f;

    private Rigidbody2D rb;
    private Collider2D bodyCollider;
    private Animator animator;          // 1 Animator duy nhất, đổi hướng qua Blend Tree (MoveX/MoveY)
    private Direction currentDirection; // hướng đang đi hiện tại
    private float directionTimer;       // đếm thời gian để đổi hướng định kỳ

    private Transform playerTransform;  // cache player để đuổi theo
    private PlayerHealth playerHealth;  // để ngừng đuổi khi player chết
    private bool isChasing;             // đang đuổi player hay đi lang thang
    private bool isStuck;               // bị kẹt cả 4 hướng → đứng yên
    private float repathTimer;          // đếm nhịp tính lại hướng đuổi
    private float attackPauseTimer;     // đứng lại sau khi cắn player

    // ── Khởi tạo ──────────────────────────────────────────────────────────

    void Awake()
    {
        // Lấy hoặc tạo Rigidbody2D để di chuyển bằng vật lý (va chạm tường chính xác)
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
            rb = gameObject.AddComponent<Rigidbody2D>();

        rb.gravityScale = 0f;                                          // top-down: không rơi
        rb.freezeRotation = true;                                      // không bị xoay khi va chạm
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous; // tránh xuyên tường khi đi nhanh
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;       // mượt trên màn hình tần số cao

        bodyCollider = GetComponent<Collider2D>();
        animator = GetComponent<Animator>();
        EnsureHealthComponent();
    }

    void EnsureHealthComponent()
    {
        // LUÔN gắn máu cho quái — showHealthBar chỉ quyết định hiển thị thanh máu
        var health = GetComponent<EnemyHealth>();
        if (health == null)
            health = gameObject.AddComponent<EnemyHealth>();

        health.showBar = showHealthBar;
        health.Configure(maxHealth);
    }

    void Start()
    {
        // Cache player để đuổi theo — không tìm thấy thì quái chỉ đi lang thang
        var playerGo = GameObject.FindGameObjectWithTag("Player");
        if (playerGo != null)
        {
            playerTransform = playerGo.transform;
            playerHealth = playerGo.GetComponent<PlayerHealth>();
        }

        PickValidDirection(); // chọn hướng ban đầu (hướng nào không bị tường chặn)
        UpdateAnimator();     // đặt Blend Tree đúng hướng ban đầu
    }

    // ── Vòng lặp game ─────────────────────────────────────────────────────

    void Update()
    {
        // Vừa cắn player → đứng lại một nhịp (player kịp bị đẩy ra + i-frame)
        if (attackPauseTimer > 0f)
        {
            attackPauseTimer -= Time.deltaTime;
            UpdateAnimator();
            return;
        }

        UpdateChaseState();

        if (isChasing)
        {
            // Tính lại hướng đuổi theo nhịp — chống giật hướng khi |dx| ≈ |dy|
            repathTimer -= Time.deltaTime;
            if (repathTimer <= 0f || IsBlocked(currentDirection))
            {
                PickChaseDirection();
                repathTimer = chaseRepathInterval;
            }
        }
        else
        {
            // WANDER: đếm thời gian → đến lúc thì thử đổi hướng ngẫu nhiên
            directionTimer += Time.deltaTime;
            if (directionTimer >= changeDirectionInterval)
            {
                PickValidDirection();
                directionTimer = 0f;
            }

            // Mỗi frame kiểm tra: hướng hiện tại còn đi được không?
            // (phát hiện tường TRƯỚC khi va chạm thật sự xảy ra)
            if (IsBlocked(currentDirection))
                PickValidDirection();
        }

        UpdateAnimator();
    }

    void FixedUpdate()
    {
        // Kẹt 4 phía hoặc đang đứng lại sau khi cắn → đứng yên thật sự
        if (isStuck || attackPauseTimer > 0f)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        // FixedUpdate chạy theo nhịp vật lý (50 lần/giây) — di chuyển mượt và ổn định
        float speed = isChasing ? chaseSpeed : moveSpeed;
        rb.linearVelocity = DirectionToVector(currentDirection) * speed;
    }

    // Unity gọi khi collider của quái CHẠM collider khác (player, tường, quái khác...)
    void OnCollisionEnter2D(Collision2D collision)
    {
        // Chạm player → đứng lại một nhịp thay vì nghiến liên tục vào người
        if (collision.gameObject.CompareTag("Player"))
        {
            attackPauseTimer = attackPauseDuration;
            return;
        }

        PickValidDirection(); // va chạm thật → đổi hướng ngay
        directionTimer = 0f;
    }

    // ── Đuổi theo player ──────────────────────────────────────────────────

    void UpdateChaseState()
    {
        // Không có player hoặc player đã chết → quay về lang thang
        if (playerTransform == null || (playerHealth != null && playerHealth.IsDead))
        {
            StopChasing();
            return;
        }

        float dist = Vector2.Distance(transform.position, playerTransform.position);

        if (!isChasing)
        {
            // Vào tầm + nhìn thấy (không bị tường che) → bắt đầu đuổi
            if (dist <= detectRange && HasLineOfSightToPlayer())
            {
                isChasing = true;
                repathTimer = 0f; // tính hướng ngay frame này
            }
        }
        else
        {
            // Ra khỏi tầm hoặc mất tầm nhìn → bỏ cuộc
            if (dist > loseRange || !HasLineOfSightToPlayer())
                StopChasing();
        }
    }

    void StopChasing()
    {
        if (!isChasing)
            return;

        isChasing = false;
        directionTimer = 0f;
        PickValidDirection();
    }

    // Có nhìn thấy player không — tia từ quái đến player không vướng tường
    bool HasLineOfSightToPlayer()
    {
        Vector2 origin = transform.position;
        Vector2 toPlayer = (Vector2)playerTransform.position - origin;

        RaycastHit2D hit = Physics2D.Raycast(origin, toPlayer.normalized, toPlayer.magnitude, wallLayer);
        return hit.collider == null;
    }

    // Chọn hướng đuổi 4 hướng: ưu tiên trục có khoảng cách lớn hơn,
    // bị chặn thì thử trục còn lại, cả 2 bị chặn thì lách như wander
    void PickChaseDirection()
    {
        Vector2 delta = playerTransform.position - transform.position;
        bool horizontalFirst = Mathf.Abs(delta.x) >= Mathf.Abs(delta.y);

        Direction primary = horizontalFirst
            ? (delta.x >= 0f ? Direction.Right : Direction.Left)
            : (delta.y >= 0f ? Direction.Up : Direction.Down);

        Direction secondary = horizontalFirst
            ? (delta.y >= 0f ? Direction.Up : Direction.Down)
            : (delta.x >= 0f ? Direction.Right : Direction.Left);

        if (!IsBlocked(primary))
        {
            currentDirection = primary;
            isStuck = false;
        }
        else if (!IsBlocked(secondary))
        {
            currentDirection = secondary;
            isStuck = false;
        }
        else
        {
            PickValidDirection();
        }
    }

    // ── Animator (Blend Tree 4 hướng: MoveX/MoveY) ──────────────────────────

    // Đẩy hướng đang đi vào Animator — Blend Tree tự chọn đúng animation,
    // không SetActive/tắt-bật GameObject nên không bị giật/reset animation khi đổi hướng liên tục.
    void UpdateAnimator()
    {
        if (animator == null) return;

        Vector2 dir = DirectionToVector(currentDirection);
        animator.SetFloat("MoveX", dir.x);
        animator.SetFloat("MoveY", dir.y);
    }

    // ── Chọn hướng ────────────────────────────────────────────────────────

    // Xáo trộn ngẫu nhiên 4 hướng, chọn hướng đầu tiên KHÔNG bị tường chặn
    void PickValidDirection()
    {
        Direction[] directions =
        {
            Direction.Up,
            Direction.Down,
            Direction.Left,
            Direction.Right
        };

        Shuffle(directions);

        foreach (Direction dir in directions)
        {
            if (!IsBlocked(dir))
            {
                currentDirection = dir;
                isStuck = false;
                return;
            }
        }

        // Bị kẹt 4 phía → FixedUpdate giữ quái đứng yên,
        // directionTimer vẫn chạy nên sẽ tự thử lại sau changeDirectionInterval
        isStuck = true;
    }

    // ── Phát hiện vật cản (RAYCAST) ───────────────────────────────────────
    //
    // Cách hoạt động:
    //
    //   [Quái] ----tia ray----> [Tường]
    //      ↑                        ↑
    //   điểm bắt đầu            hit.collider
    //   (ngoài collider)        nếu khoảng cách ≤ wallCheckDistance
    //                           → hướng đó bị chặn
    //
    // Physics2D.RaycastAll bắn 1 tia thẳng theo hướng cần kiểm tra.
    // Tham số wallCheckDistance = độ DÀI tối đa của tia (không phải khoảng cách giữa 2 object).
    // Unity trả về hit.distance = khoảng cách thực từ điểm bắt đầu đến vật cản.
    // Nếu có collider trong phạm vi tia → IsBlocked = true.

    bool IsBlocked(Direction dir)
    {
        Vector2 checkDir = DirectionToVector(dir);

        // Bắt đầu tia từ NGOÀI collider quái (tránh tia tự chạm vào chính mình)
        float castOriginOffset = bodyCollider != null
            ? bodyCollider.bounds.extents.magnitude + 0.05f
            : 0.2f;

        Vector2 origin = (Vector2)transform.position + checkDir * castOriginOffset;

        // Bắn tia dài wallCheckDistance, chỉ quét layer tường (wallLayer)
        RaycastHit2D[] hits = Physics2D.RaycastAll(origin, checkDir, wallCheckDistance, wallLayer);

        foreach (RaycastHit2D hit in hits)
        {
            // Bỏ qua collider của chính quái này
            if (hit.collider == null || IsOwnCollider(hit.collider))
                continue;

            // hit.distance cho biết tường cách bao xa (0 → wallCheckDistance)
            return true;
        }

        return false;
    }

    bool IsOwnCollider(Collider2D col)
    {
        return col.transform == transform || col.transform.IsChildOf(transform);
    }

    // ── Hàm tiện ích ──────────────────────────────────────────────────────

    static void Shuffle(Direction[] array)
    {
        for (int i = array.Length - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (array[i], array[j]) = (array[j], array[i]);
        }
    }

    static Vector2 DirectionToVector(Direction dir)
    {
        return dir switch
        {
            Direction.Up => Vector2.up,
            Direction.Down => Vector2.down,
            Direction.Left => Vector2.left,
            Direction.Right => Vector2.right,
            _ => Vector2.zero
        };
    }

    // Vẽ tia kiểm tra trong Scene view khi chọn Quai1 (chỉ khi đang Play)
    void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) return;

        Vector2 dir = DirectionToVector(currentDirection);
        float offset = bodyCollider != null
            ? bodyCollider.bounds.extents.magnitude + 0.05f
            : 0.2f;
        Vector2 origin = (Vector2)transform.position + dir * offset;

        Gizmos.color = Color.red;
        Gizmos.DrawLine(origin, origin + dir * wallCheckDistance);
        Gizmos.DrawWireSphere(origin, 0.05f);
    }
}
