using System.Collections;
using UnityEngine;

public class TestBoss : MonoBehaviour
{
    [Header("Target")]
    // Player mà boss sẽ phát hiện, đuổi theo và tấn công
    public Transform player;

    [Header("Detection")]
    // Khoảng cách boss bắt đầu phát hiện Player
    public float detectRange = 6f;

    // Khoảng cách Player chạy quá xa thì boss mất dấu
    public float loseRange = 9f;

    [Header("Movement")]
    // Tốc độ di chuyển của boss khi đuổi Player
    public float moveSpeed = 2.5f;

    [Header("Attack")]
    // Khoảng cách đủ gần để boss bắt đầu đánh
    public float attackRange = 2f;

    // Thời gian chạy một lần animation đánh
    public float attackDuration = 0.8f;

    // Thời gian nghỉ giữa các lần đánh
    public float attackCooldown = 0.5f;

    [Header("Animator State Name")]
    // Tên state attack trong Animator
    // Tên này phải trùng chính xác với state trong Animator
    public string attackStateName = "BT_Attack";

    private Rigidbody2D rb;
    private Animator animator;

    // Hướng di chuyển hiện tại của boss
    private Vector2 moveDirection;

    // Hướng nhìn cuối cùng của boss
    // Mặc định nhìn xuống
    private Vector2 lastMoveDirection = Vector2.down;

    // Boss đã từng thấy Player chưa
    private bool hasSeenPlayer = false;

    // Boss đang thực hiện một cú đánh
    private bool isAttacking = false;

    // Coroutine đánh liên tục có đang chạy không
    private bool isAttackLoopRunning = false;

    void Awake()
    {
        // Lấy Rigidbody2D và Animator gắn trên boss
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
    }

    void Start()
    {
        // Nếu chưa kéo Player vào Inspector,
        // boss sẽ tự tìm object có Tag là "Player"
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");

            if (playerObj != null)
            {
                player = playerObj.transform;
            }
        }
    }

    void Update()
    {
        // Nếu không tìm thấy Player thì boss đứng yên
        if (player == null)
        {
            StopMoving();
            SetIdleAnimation();
            return;
        }

        // Tính khoảng cách từ boss đến Player
        float distance = Vector2.Distance(transform.position, player.position);

        // Nếu boss chưa thấy Player
        if (!hasSeenPlayer)
        {
            // Player vào vùng phát hiện thì boss bắt đầu nhận mục tiêu
            if (distance <= detectRange)
            {
                hasSeenPlayer = true;
            }
            else
            {
                // Player chưa vào tầm phát hiện thì boss đứng yên
                StopMoving();
                SetIdleAnimation();
                return;
            }
        }

        // Nếu Player chạy ra quá xa thì boss mất dấu
        if (distance > loseRange)
        {
            hasSeenPlayer = false;
            StopMoving();
            SetIdleAnimation();
            return;
        }

        // Nếu Player nằm trong vùng đánh
        if (distance <= attackRange)
        {
            // Boss đứng lại
            StopMoving();

            // Boss xoay hướng nhìn về phía Player
            FacePlayer();

            // Set animation đứng theo hướng nhìn
            SetIdleAnimation();

            // Nếu chưa chạy vòng lặp đánh thì bắt đầu đánh liên tục
            if (!isAttackLoopRunning)
            {
                StartCoroutine(AttackLoop());
            }

            return;
        }

        // Nếu Player chưa trong vùng đánh thì boss tiếp tục đuổi
        // Chỉ đuổi khi boss không đang đánh
        if (!isAttacking)
        {
            ChasePlayer();
            SetRunAnimation();
        }
    }

    void FixedUpdate()
    {
        // Nếu boss đang đánh thì không cho di chuyển
        if (isAttacking || isAttackLoopRunning)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        // Di chuyển boss theo hướng đã tính trong Update
        rb.linearVelocity = moveDirection * moveSpeed;
    }

    void ChasePlayer()
    {
        // Tính hướng từ boss tới Player
        Vector2 direction = player.position - transform.position;

        // Chuẩn hóa hướng để boss chạy đều tốc độ
        moveDirection = direction.normalized;

        // Cập nhật hướng nhìn cuối cùng của boss
        if (moveDirection != Vector2.zero)
        {
            lastMoveDirection = moveDirection;
        }
    }

    void FacePlayer()
    {
        // Tính hướng từ boss tới Player
        Vector2 direction = player.position - transform.position;

        // Cập nhật hướng nhìn để boss quay mặt về phía Player khi đánh
        if (direction != Vector2.zero)
        {
            lastMoveDirection = direction.normalized;
        }
    }

    IEnumerator AttackLoop()
    {
        // Đánh dấu vòng lặp attack đang chạy
        isAttackLoopRunning = true;

        // Chừng nào Player còn tồn tại và vẫn nằm trong attackRange,
        // boss sẽ tiếp tục đánh
        while (player != null && Vector2.Distance(transform.position, player.position) <= attackRange)
        {
            // Boss bắt đầu một cú đánh
            isAttacking = true;

            // Đứng yên trước khi đánh
            StopMoving();

            // Quay mặt về phía Player
            FacePlayer();

            // Cập nhật hướng animation theo hướng nhìn
            animator.SetFloat("MoveX", lastMoveDirection.x);
            animator.SetFloat("MoveY", lastMoveDirection.y);

            // Boss không chạy trong lúc đánh
            animator.SetBool("IsMoving", false);

            // Ép state attack chạy lại từ frame đầu
            // Dùng để boss đánh nhiều lần liên tục thay vì chỉ đánh một lần
            animator.Play(attackStateName, 0, 0f);

            // Chờ animation đánh chạy xong
            yield return new WaitForSeconds(attackDuration);

            // Kết thúc một cú đánh
            isAttacking = false;

            // Chờ cooldown trước khi đánh tiếp
            yield return new WaitForSeconds(attackCooldown);
        }

        // Khi Player ra khỏi vùng đánh thì dừng vòng lặp attack
        isAttacking = false;
        isAttackLoopRunning = false;
    }

    void StopMoving()
    {
        // Xóa hướng di chuyển
        moveDirection = Vector2.zero;

        // Dừng Rigidbody2D lại ngay lập tức
        rb.linearVelocity = Vector2.zero;
    }

    void SetIdleAnimation()
    {
        // Set hướng nhìn cho animation idle
        animator.SetFloat("MoveX", lastMoveDirection.x);
        animator.SetFloat("MoveY", lastMoveDirection.y);

        // Boss không di chuyển
        animator.SetBool("IsMoving", false);
    }

    void SetRunAnimation()
    {
        // Set hướng chạy theo hướng nhìn cuối cùng
        animator.SetFloat("MoveX", lastMoveDirection.x);
        animator.SetFloat("MoveY", lastMoveDirection.y);

        // Boss đang chạy
        animator.SetBool("IsMoving", true);
    }

    void OnDrawGizmosSelected()
    {
        // Vòng màu vàng: vùng boss phát hiện Player
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectRange);

        // Vòng màu đỏ: vùng boss có thể đánh Player
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // Vòng màu xám: vùng Player chạy quá xa thì boss mất dấu
        Gizmos.color = Color.gray;
        Gizmos.DrawWireSphere(transform.position, loseRange);
    }
}