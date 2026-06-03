using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    public Transform player;

    public Transform pointA;
    public Transform pointB;

    public float moveSpeed = 2f;

    public float detectRange = 5f;
    public float attackRange = 1f;
    public float loseRange = 8f;

    private Transform currentTarget;
    private Vector3 startPosition;

    enum State
    {
        Patrol,
        Chase,
        Attack,
        Return
    }

    State state;

    void Start()
    {
        startPosition = transform.position;

        currentTarget = pointA;

        state = State.Patrol;
    }

    void Update()
    {
        float distance =
            Vector2.Distance(
                transform.position,
                player.position);

        switch (state)
        {
            case State.Patrol:

                Patrol();

                if (distance <= detectRange)
                {
                    state = State.Chase;
                }

                break;

            case State.Chase:

                Chase();

                if (distance <= attackRange)
                {
                    state = State.Attack;
                }

                if (distance >= loseRange)
                {
                    state = State.Return;
                }

                break;

            case State.Attack:

                Attack();

                if (distance > attackRange)
                {
                    state = State.Chase;
                }

                if (distance >= loseRange)
                {
                    state = State.Return;
                }

                break;

            case State.Return:

                ReturnHome();

                break;
        }
    }

    void Patrol()
    {
        transform.position =
            Vector2.MoveTowards(
                transform.position,
                currentTarget.position,
                moveSpeed * Time.deltaTime);

        if (Vector2.Distance(
            transform.position,
            currentTarget.position) < 0.1f)
        {
            currentTarget =
                currentTarget == pointA
                ? pointB
                : pointA;
        }
    }

    void Chase()
    {
        transform.position =
            Vector2.MoveTowards(
                transform.position,
                player.position,
                moveSpeed * Time.deltaTime);
    }

    void Attack()
    {
        Debug.Log("ATTACK");
    }

    void ReturnHome()
    {
        transform.position =
            Vector2.MoveTowards(
                transform.position,
                startPosition,
                moveSpeed * Time.deltaTime);

        if (Vector2.Distance(
            transform.position,
            startPosition) < 0.1f)
        {
            state = State.Patrol;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(
            transform.position,
            detectRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(
            transform.position,
            attackRange);

        Gizmos.color = Color.gray;
        Gizmos.DrawWireSphere(
            transform.position,
            loseRange);
    }
}