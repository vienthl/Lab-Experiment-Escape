using System.Collections;

using UnityEngine;

public class TestBoss : MonoBehaviour
{
    [Header("Target")]
    public Transform player;

    [Header("Detection")]
    public float detectRange = 6f;
    public float loseRange = 9f;

    [Header("Movement")]
    public float moveSpeed = 2.5f;

    [Header("Attack")]
    public float attackRange = 2f;
    public float attackDuration = 0.8f;
    public float attackCooldown = 0.5f;

    [Tooltip("Mỗi đòn đánh thường gây 15% máu tối đa của Player.")]
    [Range(0f, 1f)]
    public float normalAttackDamagePercent = 0.15f;

    [Header("Animator State Name")]
    public string attackStateName = "BT_Attack";

    [Header("Hidden Skill")]
    [Tooltip("Kích hoạt khi Boss còn 30% máu hoặc thấp hơn.")]
    [Range(0f, 1f)]
    public float hiddenSkillTriggerPercent = 0.30f;

    [Tooltip("Tốc độ Boss lao tới Player.")]
    public float drainRushSpeed = 7f;

    [Tooltip("Khoảng cách để bắt đầu hút máu.")]
    public float drainRange = 1.2f;

    [Tooltip("Thời gian hút máu.")]
    public float drainDuration = 1.2f;

    [Tooltip("Skill hút 30% máu tối đa của Player.")]
    [Range(0f, 1f)]
    public float drainDamagePercent = 0.30f;

    [Tooltip("Thời gian nghỉ sau khi hút máu.")]
    public float drainEndDelay = 0.4f;

    [Header("Drain Beam")]
    [Tooltip("Kéo LineRenderer của object DrainBeam vào đây.")]
    [SerializeField] private LineRenderer drainBeam;

    [Tooltip("Điểm bắt đầu tia trên Boss. Để trống sẽ dùng vị trí Boss.")]
    [SerializeField] private Transform beamStartPoint;

    [Tooltip("Điểm kết thúc tia trên Player. Để trống sẽ dùng vị trí Player.")]
    [SerializeField] private Transform beamEndPoint;

    private Rigidbody2D rb;
    private Animator animator;
    private BossHealth bossHealth;
    private PlayerHealth playerHealth;

    private Vector2 moveDirection;
    private Vector2 lastMoveDirection = Vector2.down;

    private bool hasSeenPlayer;
    private bool isAttacking;
    private bool isAttackLoopRunning;
    private bool hiddenSkillUsed;
    private bool isUsingHiddenSkill;

    private float currentSpeedMultiplier = 1f;

    private Coroutine slowCoroutine;
    private Coroutine attackCoroutine;
    private Coroutine hiddenSkillCoroutine;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        bossHealth = GetComponent<BossHealth>();

        if (drainBeam != null)
        {
            drainBeam.enabled = false;
            drainBeam.useWorldSpace = true;
            drainBeam.positionCount = 2;
        }
    }

    private void Start()
    {
        FindPlayer();
        bossHealth?.SetHealthBarVisible(false);
    }

    private void Update()
    {
        if (bossHealth != null && bossHealth.IsDead)
        {
            StopMoving();
            DisableDrainBeam();
            return;
        }

        if (player == null)
            FindPlayer();

        if (player == null)
        {
            StopMoving();
            SetIdleAnimation();
            bossHealth?.SetHealthBarVisible(false);
            return;
        }

        if (playerHealth == null)
            playerHealth = player.GetComponent<PlayerHealth>();

        if (playerHealth != null && playerHealth.IsDead)
        {
            StopMoving();
            SetIdleAnimation();
            DisableDrainBeam();
            return;
        }

        if (isUsingHiddenSkill)
            return;

        CheckHiddenSkill();

        if (isUsingHiddenSkill)
            return;

        float distance = Vector2.Distance(
            transform.position,
            player.position
        );

        if (!hasSeenPlayer)
        {
            if (distance <= detectRange)
            {
                hasSeenPlayer = true;
                bossHealth?.SetHealthBarVisible(true);
            }
            else
            {
                StopMoving();
                SetIdleAnimation();
                bossHealth?.SetHealthBarVisible(false);
                return;
            }
        }

        if (distance > loseRange)
        {
            LosePlayer();
            return;
        }

        bossHealth?.SetHealthBarVisible(true);

        if (distance <= attackRange)
        {
            StopMoving();
            FacePlayer();
            SetIdleAnimation();

            if (!isAttackLoopRunning)
            {
                attackCoroutine = StartCoroutine(AttackLoop());
            }

            return;
        }

        StopAttackLoop();

        if (!isAttacking)
        {
            ChasePlayer();
            SetRunAnimation();
        }
    }

    private void FixedUpdate()
    {
        if (rb == null)
            return;

        if (bossHealth != null && bossHealth.IsDead)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        if (isUsingHiddenSkill)
            return;

        if (isAttacking || isAttackLoopRunning)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        float currentMoveSpeed =
            moveSpeed * currentSpeedMultiplier;

        rb.linearVelocity =
            moveDirection * currentMoveSpeed;
    }

    private void LateUpdate()
    {
        if (drainBeam != null && drainBeam.enabled)
            UpdateDrainBeam();
    }

    private void FindPlayer()
    {
        if (player == null)
        {
            GameObject playerObject =
                GameObject.FindGameObjectWithTag("Player");

            if (playerObject != null)
                player = playerObject.transform;
        }

        if (player != null)
            playerHealth = player.GetComponent<PlayerHealth>();
    }

    private void CheckHiddenSkill()
    {
        if (hiddenSkillUsed || isUsingHiddenSkill)
            return;

        if (bossHealth == null || bossHealth.IsDead)
            return;

        if (player == null || playerHealth == null || playerHealth.IsDead)
            return;

        if (bossHealth.HealthPercent > hiddenSkillTriggerPercent)
            return;

        hiddenSkillUsed = true;
        hiddenSkillCoroutine = StartCoroutine(HiddenDrainSkill());
    }

    private IEnumerator HiddenDrainSkill()
    {
        isUsingHiddenSkill = true;
        StopAttackLoop();
        StopMoving();

        if (slowCoroutine != null)
        {
            StopCoroutine(slowCoroutine);
            slowCoroutine = null;
        }

        currentSpeedMultiplier = 1f;

        while (
            player != null &&
            playerHealth != null &&
            !playerHealth.IsDead &&
            bossHealth != null &&
            !bossHealth.IsDead &&
            Vector2.Distance(transform.position, player.position) > drainRange
        )
        {
            Vector2 direction =
                ((Vector2)player.position -
                 (Vector2)transform.position).normalized;

            if (direction != Vector2.zero)
            {
                lastMoveDirection = direction;
                SetRunAnimation();
            }

            if (rb != null)
                rb.linearVelocity = direction * drainRushSpeed;

            yield return new WaitForFixedUpdate();
        }

        StopMoving();
        SetIdleAnimation();

        if (
            player == null ||
            playerHealth == null ||
            playerHealth.IsDead ||
            bossHealth == null ||
            bossHealth.IsDead
        )
        {
            FinishHiddenSkill();
            yield break;
        }

        EnableDrainBeam();

        float elapsed = 0f;
        while (elapsed < drainDuration)
        {
            if (
                player == null ||
                playerHealth == null ||
                playerHealth.IsDead ||
                bossHealth == null ||
                bossHealth.IsDead
            )
            {
                break;
            }

            UpdateDrainBeam();
            elapsed += Time.deltaTime;
            yield return null;
        }

        DisableDrainBeam();

        if (
            playerHealth != null &&
            !playerHealth.IsDead &&
            bossHealth != null &&
            !bossHealth.IsDead
        )
        {
            float drainDamage =
                playerHealth.MaxHealth * drainDamagePercent;

            playerHealth.TakeTrueDamage(drainDamage);
            bossHealth.HealToFull();

            Debug.Log(
                $"Boss hút {drainDamage} HP của Player và hồi đầy máu.",
                this
            );
        }

        yield return new WaitForSeconds(drainEndDelay);
        FinishHiddenSkill();
    }

    private void FinishHiddenSkill()
    {
        DisableDrainBeam();
        StopMoving();

        isUsingHiddenSkill = false;
        hiddenSkillCoroutine = null;
    }

    private void EnableDrainBeam()
    {
        if (drainBeam == null)
            return;

        drainBeam.positionCount = 2;
        drainBeam.useWorldSpace = true;
        drainBeam.enabled = true;
        UpdateDrainBeam();
    }

    private void DisableDrainBeam()
    {
        if (drainBeam != null)
            drainBeam.enabled = false;
    }

    private void UpdateDrainBeam()
    {
        if (drainBeam == null || player == null)
            return;

        Vector3 startPosition =
            beamStartPoint != null
                ? beamStartPoint.position
                : transform.position;

        Vector3 endPosition =
            beamEndPoint != null
                ? beamEndPoint.position
                : player.position;

        drainBeam.SetPosition(0, startPosition);
        drainBeam.SetPosition(1, endPosition);
    }

    public void ApplySlow(float multiplier, float duration)
    {
        if (
            bossHealth != null &&
            bossHealth.IsDead
        )
        {
            return;
        }

        if (isUsingHiddenSkill)
            return;

        multiplier = Mathf.Clamp(multiplier, 0.1f, 1f);
        duration = Mathf.Max(0.1f, duration);

        if (slowCoroutine != null)
            StopCoroutine(slowCoroutine);

        slowCoroutine = StartCoroutine(
            SlowRoutine(multiplier, duration)
        );
    }

    private IEnumerator SlowRoutine(
        float multiplier,
        float duration
    )
    {
        currentSpeedMultiplier = multiplier;

        Debug.Log(
            $"Boss bị làm chậm còn {multiplier * 100f}% tốc độ trong {duration} giây.",
            this
        );

        yield return new WaitForSeconds(duration);

        currentSpeedMultiplier = 1f;
        slowCoroutine = null;

        Debug.Log("Boss đã hết hiệu ứng làm chậm.", this);
    }

    private void ChasePlayer()
    {
        if (player == null)
            return;

        Vector2 direction =
            (Vector2)player.position -
            (Vector2)transform.position;

        moveDirection = direction.normalized;

        if (moveDirection != Vector2.zero)
            lastMoveDirection = moveDirection;
    }

    private void FacePlayer()
    {
        if (player == null)
            return;

        Vector2 direction =
            (Vector2)player.position -
            (Vector2)transform.position;

        if (direction != Vector2.zero)
            lastMoveDirection = direction.normalized;
    }

    private IEnumerator AttackLoop()
    {
        isAttackLoopRunning = true;

        while (
            player != null &&
            playerHealth != null &&
            !playerHealth.IsDead &&
            bossHealth != null &&
            !bossHealth.IsDead &&
            !isUsingHiddenSkill &&
            Vector2.Distance(transform.position, player.position) <= attackRange
        )
        {
            isAttacking = true;
            StopMoving();
            FacePlayer();

            if (animator != null)
            {
                animator.SetFloat("MoveX", lastMoveDirection.x);
                animator.SetFloat("MoveY", lastMoveDirection.y);
                animator.SetBool("IsMoving", false);
                animator.Play(attackStateName, 0, 0f);
            }

            yield return new WaitForSeconds(attackDuration);

            if (
                player != null &&
                playerHealth != null &&
                !playerHealth.IsDead &&
                bossHealth != null &&
                !bossHealth.IsDead &&
                !isUsingHiddenSkill &&
                Vector2.Distance(transform.position, player.position) <= attackRange
            )
            {
                float damage =
                    playerHealth.MaxHealth *
                    normalAttackDamagePercent;

                playerHealth.TakeDamage(
                    damage,
                    transform.position
                );
            }

            isAttacking = false;

            yield return new WaitForSeconds(attackCooldown);
        }

        isAttacking = false;
        isAttackLoopRunning = false;
        attackCoroutine = null;
    }

    private void StopAttackLoop()
    {
        if (attackCoroutine != null)
        {
            StopCoroutine(attackCoroutine);
            attackCoroutine = null;
        }

        isAttacking = false;
        isAttackLoopRunning = false;
    }

    private void LosePlayer()
    {
        hasSeenPlayer = false;

        StopAttackLoop();

        if (slowCoroutine != null)
        {
            StopCoroutine(slowCoroutine);
            slowCoroutine = null;
        }

        currentSpeedMultiplier = 1f;

        StopMoving();
        SetIdleAnimation();
        bossHealth?.SetHealthBarVisible(false);
    }

    private void StopMoving()
    {
        moveDirection = Vector2.zero;

        if (rb != null)
            rb.linearVelocity = Vector2.zero;
    }

    private void SetIdleAnimation()
    {
        if (animator == null)
            return;

        animator.SetFloat("MoveX", lastMoveDirection.x);
        animator.SetFloat("MoveY", lastMoveDirection.y);
        animator.SetBool("IsMoving", false);
    }

    private void SetRunAnimation()
    {
        if (animator == null)
            return;

        animator.SetFloat("MoveX", lastMoveDirection.x);
        animator.SetFloat("MoveY", lastMoveDirection.y);
        animator.SetBool("IsMoving", true);
    }

    private void OnDisable()
    {
        StopAllCoroutines();

        attackCoroutine = null;
        hiddenSkillCoroutine = null;
        slowCoroutine = null;

        currentSpeedMultiplier = 1f;
        isAttacking = false;
        isAttackLoopRunning = false;
        isUsingHiddenSkill = false;

        DisableDrainBeam();
        StopMoving();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, drainRange);

        Gizmos.color = Color.gray;
        Gizmos.DrawWireSphere(transform.position, loseRange);
    }
}
