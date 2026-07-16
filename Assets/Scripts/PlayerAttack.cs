using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    [Header("Bình lửa — chuột TRÁI (25% HP quái)")]
    public GameObject firePotionPrefab;

    [Header("Bình điện — chuột PHẢI (50% HP quái)")]
    public GameObject lightningPotionPrefab;

    [Header("Cài đặt ném")]
    public float projectileSpeed = 14f;

    [Tooltip("Thời gian chờ giữa 2 lần ném (giây)")]
    public float throwCooldown = 0.5f;

    [Tooltip("Khoảng cách spawn tính từ tâm player — nhỏ để đánh được quái áp sát")]
    public float spawnOffset = 0.4f;

    [Tooltip("Scale bình khi spawn — chỉnh cho khớp kích thước nhân vật")]
    public float projectileScale = 0.35f;

    PlayerMovement movement;
    PlayerHealth   health;
    float lastThrowTime = -999f;

    void Awake()
    {
        movement = GetComponent<PlayerMovement>();
        health   = GetComponent<PlayerHealth>();
    }

    void Update()
    {
        if (health != null && health.IsDead) return;

        // Không ném khi đang uống nước / nhặt đồ
        if (movement != null && movement.IsBusy) return;

        // Cooldown giữa 2 lần ném
        if (Time.time - lastThrowTime < throwCooldown) return;

        // else if: bấm 2 chuột cùng frame thì chỉ ném bình lửa
        if (Input.GetMouseButtonDown(0) && firePotionPrefab != null)
            Throw(firePotionPrefab);
        else if (Input.GetMouseButtonDown(1) && lightningPotionPrefab != null)
            Throw(lightningPotionPrefab);
    }

    void Throw(GameObject prefab)
    {
        if (Camera.main == null) return;

        Vector3 mouseScreenPos = Input.mousePosition;
        mouseScreenPos.z = -Camera.main.transform.position.z;
        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(mouseScreenPos);
        mouseWorld.z = 0f;

        Vector2 dir = ((Vector2)mouseWorld - (Vector2)transform.position).normalized;
        if (dir == Vector2.zero) return;

        Vector3 spawnPos = transform.position + (Vector3)(dir * spawnOffset);
        spawnPos.z = 0f;

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        var go = Instantiate(prefab, spawnPos, Quaternion.Euler(0f, 0f, angle));

        go.transform.localScale = Vector3.one * projectileScale;

        go.GetComponent<Projectile>()?.Launch(dir, projectileSpeed);
        movement?.TriggerThrow();
        lastThrowTime = Time.time;
    }
}
