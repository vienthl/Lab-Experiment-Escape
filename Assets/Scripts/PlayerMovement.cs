using System.Collections;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 3f;

    // DRINK
    public float drinkDuration = 0.8f;

    // PICKUP
    public float pickUpDuration = 0.8f;

    // THROW
    public float throwDuration = 0.5f;

    // KNOCKBACK
    [Tooltip("Tốc độ tắt dần của lực đẩy lùi (đơn vị/giây)")]
    public float knockbackDecay = 25f;

    private Rigidbody2D rb;
    private Animator animator;
    private Vector2 movement;
    private Vector2 lastMoveDirection = Vector2.down;
    private Vector2 knockbackVelocity;

    // ACTION STATE
    private bool isDrinking = false;

    // PICKUP
    private bool isPickingUp = false;

    // THROW
    private bool isThrowing = false;

    // DEATH
    private bool isDead = false;

    public bool IsDead => isDead;
    public bool IsThrowing => isThrowing;
    public bool IsBusy => isDrinking || isPickingUp;
    public Vector2 LastMoveDirection => lastMoveDirection;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        // DEATH
        // Nếu đã chết thì không cho nhập gì nữa
        if (isDead)
        {
            movement = Vector2.zero;

            animator.SetFloat("MoveX", 0);
            animator.SetFloat("MoveY", 0);
            animator.SetFloat("Speed", 0);

            return;
        }

        // ACTION LOCK
        // Nếu đang uống nước hoặc nhặt đồ thì không cho nhập di chuyển
        // Lưu ý: KHÔNG có isThrowing ở đây, vì ném vẫn được di chuyển
        if (isDrinking || isPickingUp)
        {
            movement = Vector2.zero;

            animator.SetFloat("MoveX", 0);
            animator.SetFloat("MoveY", 0);
            animator.SetFloat("Speed", 0);

            animator.SetFloat("LastMoveX", lastMoveDirection.x);
            animator.SetFloat("LastMoveY", lastMoveDirection.y);

            return;
        }

        // MOVEMENT INPUT
        movement.x = Input.GetAxisRaw("Horizontal");
        movement.y = Input.GetAxisRaw("Vertical");

        movement = movement.normalized;

        if (movement != Vector2.zero)
        {
            lastMoveDirection = movement;
        }

        // ANIMATOR PARAMETERS
        animator.SetFloat("MoveX", movement.x);
        animator.SetFloat("MoveY", movement.y);
        animator.SetFloat("Speed", movement.sqrMagnitude);
        animator.SetFloat("LastMoveX", lastMoveDirection.x);
        animator.SetFloat("LastMoveY", lastMoveDirection.y);

        // DRINK / PICKUP
        // Không cho uống/nhặt khi đang ném (tránh chồng animation)
        // E và F bấm cùng frame thì ưu tiên E (else if)
        if (!isThrowing)
        {
            if (Input.GetKeyDown(KeyCode.E))
            {
                StartCoroutine(DrinkRoutine());
            }
            else if (Input.GetKeyDown(KeyCode.F))
            {
                StartCoroutine(PickUpRoutine());
            }
        }
    }

    void FixedUpdate()
    {
        // DEATH
        // Khi chết thì đứng yên tuyệt đối, không bị đẩy trôi
        if (isDead)
        {
            knockbackVelocity = Vector2.zero;
            rb.linearVelocity = Vector2.zero;
            return;
        }

        // ACTION LOCK
        // Khi đang uống nước hoặc nhặt đồ thì không tự di chuyển,
        // nhưng vẫn nhận knockback khi bị đánh
        // Lưu ý: KHÔNG có isThrowing ở đây, vì ném vẫn được di chuyển
        Vector2 moveVelocity = (isDrinking || isPickingUp)
            ? Vector2.zero
            : movement * moveSpeed;

        rb.linearVelocity = moveVelocity + knockbackVelocity;

        knockbackVelocity = Vector2.MoveTowards(
            knockbackVelocity, Vector2.zero, knockbackDecay * Time.fixedDeltaTime);
    }

    // KNOCKBACK — gọi từ PlayerHealth khi trúng đòn
    // Set trực tiếp (không cộng dồn) để bị đánh liên tiếp không văng quá xa
    public void ApplyKnockback(Vector2 velocity)
    {
        if (isDead)
        {
            return;
        }

        knockbackVelocity = velocity;
    }

    // DEATH
    public void Die()
    {
        if (isDead)
        {
            return;
        }

        isDead = true;
        movement = Vector2.zero;
        knockbackVelocity = Vector2.zero;
        rb.linearVelocity = Vector2.zero;

        animator.SetFloat("Speed", 0);
        animator.SetFloat("MoveX", 0);
        animator.SetFloat("MoveY", 0);
        animator.SetFloat("LastMoveX", lastMoveDirection.x);
        animator.SetFloat("LastMoveY", lastMoveDirection.y);

        animator.SetTrigger("Die");
    }

    // DRINK
    IEnumerator DrinkRoutine()
    {
        isDrinking = true;

        movement = Vector2.zero;

        animator.SetFloat("Speed", 0);
        animator.SetFloat("MoveX", 0);
        animator.SetFloat("MoveY", 0);
        animator.SetFloat("LastMoveX", lastMoveDirection.x);
        animator.SetFloat("LastMoveY", lastMoveDirection.y);

        animator.SetTrigger("Drink");

        yield return new WaitForSeconds(drinkDuration);

        isDrinking = false;
    }

    // PICKUP
    IEnumerator PickUpRoutine()
    {
        isPickingUp = true;

        movement = Vector2.zero;

        animator.SetFloat("Speed", 0);
        animator.SetFloat("MoveX", 0);
        animator.SetFloat("MoveY", 0);
        animator.SetFloat("LastMoveX", lastMoveDirection.x);
        animator.SetFloat("LastMoveY", lastMoveDirection.y);

        animator.SetTrigger("PickUp");

        yield return new WaitForSeconds(pickUpDuration);

        isPickingUp = false;
    }

    // THROW — gọi từ PlayerAttack khi bấm chuột trái/phải
    public void TriggerThrow()
    {
        if (!isThrowing && !isDead && !IsBusy)
            StartCoroutine(ThrowRoutine());
    }

    IEnumerator ThrowRoutine()
    {
        isThrowing = true;
        animator.SetFloat("LastMoveX", lastMoveDirection.x);
        animator.SetFloat("LastMoveY", lastMoveDirection.y);
        animator.SetTrigger("Throw");
        yield return new WaitForSeconds(throwDuration);
        isThrowing = false;
    }
}