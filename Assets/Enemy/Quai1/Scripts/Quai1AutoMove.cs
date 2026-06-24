using UnityEngine;

/// <summary>
/// Quái tự di chuyển 4 hướng (lên / xuống / trái / phải),
/// đổi hướng ngẫu nhiên theo thời gian hoặc khi gặp vật cản.
/// </summary>
public class Quai1AutoMove : MonoBehaviour
{
    public enum Direction { Up, Down, Left, Right }

    [Header("Di chuyển")]
    [Tooltip("Tốc độ di chuyển (đơn vị Unity/giây)")]
    public float moveSpeed = 2f;

    [Tooltip("Sau bao nhiêu giây thì thử đổi hướng ngẫu nhiên")]
    public float changeDirectionInterval = 2f;

    [Header("Tên child trong Hierarchy (tự tìm nếu để trống reference)")]
    public string normalChildName = "Quai1_normal"; // sprite hướng xuống
    public string leftChildName = "Quai1_left";
    public string rightChildName = "Quai1_right";
    public string backChildName = "Quai1_back";    // sprite hướng lên

    [Header("Sprite theo hướng (tự gán nếu để trống)")]
    public GameObject spriteNormal;
    public GameObject spriteLeft;
    public GameObject spriteRight;
    public GameObject spriteBack;

    [Header("Va chạm")]
    [Tooltip("Layer chứa tường / vật cản (trong scene đang dùng Default)")]
    public LayerMask wallLayer;

    [Tooltip("Độ dài tia kiểm tra phía trước — càng lớn càng phát hiện tường sớm hơn")]
    public float wallCheckDistance = 0.4f;

    [Header("Máu")]
    [Tooltip("Tự tạo thanh máu phía trên đầu quái")]
    public bool showHealthBar = true;
    public float maxHealth = 100f;

    private Rigidbody2D rb;
    private Collider2D bodyCollider;
    private Direction currentDirection; // hướng đang đi hiện tại
    private float directionTimer;       // đếm thời gian để đổi hướng định kỳ

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

        bodyCollider = GetComponent<Collider2D>();
        ResolveDirectionSprites(); // tự tìm 4 child sprite nếu chưa kéo thả trong Inspector
        EnsureHealthComponent();
    }

    void EnsureHealthComponent()
    {
        if (!showHealthBar)
            return;

        var health = GetComponent<EnemyHealth>();
        if (health == null)
            health = gameObject.AddComponent<EnemyHealth>();

        health.Configure(maxHealth);
    }

    void Start()
    {
        PickValidDirection(); // chọn hướng ban đầu (hướng nào không bị tường chặn)
        UpdateSprite();       // bật đúng sprite theo hướng
    }

    // ── Vòng lặp game ─────────────────────────────────────────────────────

    void Update()
    {
        // Đếm thời gian → đến lúc thì thử đổi hướng ngẫu nhiên
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

        UpdateSprite();
    }

    void FixedUpdate()
    {
        // FixedUpdate chạy theo nhịp vật lý (50 lần/giây) — di chuyển mượt và ổn định
        rb.linearVelocity = DirectionToVector(currentDirection) * moveSpeed;
    }

    // Unity gọi khi collider của quái CHẠM collider khác (tường, quái khác...)
    void OnCollisionEnter2D(Collision2D collision)
    {
        PickValidDirection(); // va chạm thật → đổi hướng ngay
        directionTimer = 0f;
    }

    // ── Sprite 4 hướng ────────────────────────────────────────────────────

    void ResolveDirectionSprites()
    {
        if (spriteNormal == null) spriteNormal = FindChild(normalChildName);
        if (spriteLeft == null) spriteLeft = FindChild(leftChildName);
        if (spriteRight == null) spriteRight = FindChild(rightChildName);
        if (spriteBack == null) spriteBack = FindChild(backChildName);
    }

    // Tìm child theo tên bên trong object cha Quai1
    GameObject FindChild(string childName)
    {
        if (string.IsNullOrWhiteSpace(childName))
            return null;

        Transform child = transform.Find(childName);
        if (child != null)
            return child.gameObject;

        foreach (Transform descendant in transform.GetComponentsInChildren<Transform>(true))
        {
            if (descendant != transform && descendant.name == childName)
                return descendant.gameObject;
        }

        return null;
    }

    // Bật 1 sprite đúng hướng, tắt 3 sprite còn lại
    void UpdateSprite()
    {
        if (spriteNormal != null) spriteNormal.SetActive(currentDirection == Direction.Down);
        if (spriteLeft != null) spriteLeft.SetActive(currentDirection == Direction.Left);
        if (spriteRight != null) spriteRight.SetActive(currentDirection == Direction.Right);
        if (spriteBack != null) spriteBack.SetActive(currentDirection == Direction.Up);
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
                return;
            }
        }

        // Bị kẹt 4 phía → dừng lại
        rb.linearVelocity = Vector2.zero;
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
