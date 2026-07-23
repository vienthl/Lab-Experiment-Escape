using System.Collections;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 3f;

    [Header("Âm thanh")]
    public AudioClip drinkSound;
    [Range(0f, 1f)] public float drinkVolume = 1f;
    public AudioClip pickUpSound;
    [Range(0f, 1f)] public float pickUpVolume = 1f;

    [Header("Âm thanh bước chân (loop khi đang di chuyển, tắt ngay khi dừng)")]
    [Tooltip("1 clip liên tục (vd 6 giây) — tự Play() khi bắt đầu đi, tự Stop() ngay khi dừng, không cắt nhịp giữa chừng")]
    public AudioClip footstepLoop;
    [Range(0f, 1f)]
    public float footstepVolume = 0.6f;

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
    private PlayerInteractor interactor;
    private PlayerInfection infection;
    private Vector2 movement;
    private Vector2 lastMoveDirection = Vector2.down;
    private Vector2 knockbackVelocity;
    private AudioSource footstepSource;

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
        interactor = GetComponent<PlayerInteractor>();
        infection = GetComponent<PlayerInfection>();

        if (footstepLoop != null)
        {
            footstepSource = gameObject.AddComponent<AudioSource>();
            footstepSource.clip = footstepLoop;
            footstepSource.volume = footstepVolume;
            footstepSource.loop = true;
            footstepSource.playOnAwake = false;
        }
    }

    void StopFootsteps()
    {
        if (footstepSource != null && footstepSource.isPlaying)
            footstepSource.Stop();
    }

    void Update()
    {
        // DEATH
        // Nếu đã chết thì không cho nhập gì nữa
        if (isDead)
        {
            movement = Vector2.zero;
            StopFootsteps();

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
            StopFootsteps();

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

        // FOOTSTEP — loop nguyên clip trong lúc di chuyển, dừng ngay khi đứng yên (không cắt giữa chừng)
        if (footstepSource != null)
        {
            if (movement != Vector2.zero && !footstepSource.isPlaying)
                footstepSource.Play();
            else if (movement == Vector2.zero && footstepSource.isPlaying)
                footstepSource.Stop();
        }

        // Khi đang ném: GIỮ hướng mặt về phía ném (đã set ở TriggerThrow),
        // không cho hướng di chuyển ghi đè → animation ném không đổi hướng giữa chừng
        if (!isThrowing)
        {
            animator.SetFloat("LastMoveX", lastMoveDirection.x);
            animator.SetFloat("LastMoveY", lastMoveDirection.y);
        }

        // DRINK / PICKUP
        // Không cho uống/nhặt khi đang ném (tránh chồng animation)
        // E và F bấm cùng frame thì ưu tiên E (else if)
        if (!isThrowing)
        {
            if (Input.GetKeyDown(KeyCode.E))
            {
                // Đang nhiễm độc + có bình cure → ưu tiên uống cure để chữa; không thì uống bình hồi máu như bình thường.
                if (infection != null && infection.IsInfected && infection.HasCure)
                    StartCoroutine(CureRoutine());
                else
                    StartCoroutine(DrinkRoutine());
            }
            else if (Input.GetKeyDown(KeyCode.F) && interactor != null && interactor.HasTarget)
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

        try
        {
            movement = Vector2.zero;

            animator.SetFloat("Speed", 0);
            animator.SetFloat("MoveX", 0);
            animator.SetFloat("MoveY", 0);
            animator.SetFloat("LastMoveX", lastMoveDirection.x);
            animator.SetFloat("LastMoveY", lastMoveDirection.y);

            animator.SetTrigger("Drink");
            AudioOneShot.Play(drinkSound, transform.position, drinkVolume);

            yield return new WaitForSeconds(drinkDuration);
        }
        finally
        {
            // finally đảm bảo cờ luôn được nhả dù có exception hay coroutine bị Stop giữa chừng —
            // tránh Update() bị kẹt return sớm mãi mãi (đơ toàn bộ input di chuyển).
            isDrinking = false;
        }
    }

    // CURE — uống bình thuốc xanh (Boss2 rơi ra) để hết nhiễm độc, phím C
    IEnumerator CureRoutine()
    {
        isDrinking = true;

        try
        {
            movement = Vector2.zero;

            animator.SetFloat("Speed", 0);
            animator.SetFloat("MoveX", 0);
            animator.SetFloat("MoveY", 0);
            animator.SetFloat("LastMoveX", lastMoveDirection.x);
            animator.SetFloat("LastMoveY", lastMoveDirection.y);

            animator.SetTrigger("Drink");
            AudioOneShot.Play(drinkSound, transform.position, drinkVolume);

            yield return new WaitForSeconds(drinkDuration);

            infection?.ConsumeAndCure();
        }
        finally
        {
            isDrinking = false;
        }
    }

    // PICKUP
    IEnumerator PickUpRoutine()
    {
        isPickingUp = true;

        try
        {
            movement = Vector2.zero;

            animator.SetFloat("Speed", 0);
            animator.SetFloat("MoveX", 0);
            animator.SetFloat("MoveY", 0);
            animator.SetFloat("LastMoveX", lastMoveDirection.x);
            animator.SetFloat("LastMoveY", lastMoveDirection.y);

            animator.SetTrigger("PickUp");

            yield return new WaitForSeconds(pickUpDuration);

            interactor?.Interact();
            AudioOneShot.Play(pickUpSound, transform.position, pickUpVolume);
        }
        finally
        {
            isPickingUp = false;
        }
    }

    // THROW — gọi từ PlayerAttack khi bấm chuột trái/phải
    // Overload cũ: ném theo hướng đi cuối (giữ tương thích nếu nơi khác còn gọi)
    public void TriggerThrow() => TriggerThrow(Vector2.zero);

    // aimDir: hướng ném (hướng chuột) — quay mặt nhân vật về đúng phía ném
    public void TriggerThrow(Vector2 aimDir)
    {
        if (isThrowing || isDead || IsBusy) return;

        if (aimDir != Vector2.zero)
            lastMoveDirection = aimDir;

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