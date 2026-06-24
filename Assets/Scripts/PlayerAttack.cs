using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    [Header("Projectile")]
    [Tooltip("Kéo thả Prefab projectile vào đây trong Inspector")]
    public GameObject projectilePrefab;

    [Tooltip("Tốc độ bay của projectile")]
    public float projectileSpeed = 14f;

    [Tooltip("Khoảng cách spawn projectile tính từ tâm player")]
    public float spawnOffset = 0.6f;

    PlayerMovement movement;
    PlayerHealth health;

    void Awake()
    {
        movement = GetComponent<PlayerMovement>();
        health   = GetComponent<PlayerHealth>();
    }

    void Update()
    {
        if (health != null && health.IsDead) return;
        if (projectilePrefab == null) return;

        if (Input.GetMouseButtonDown(0))
            Throw();
    }

    void Throw()
    {
        // Chuyển vị trí chuột từ screen sang world
        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = 0f;

        Vector2 dir = ((Vector2)mouseWorld - (Vector2)transform.position).normalized;
        if (dir == Vector2.zero) return;

        // Spawn tại vị trí lệch ra theo hướng ném để không va chạm player ngay
        Vector3 spawnPos = transform.position + (Vector3)(dir * spawnOffset);
        spawnPos.z = 0f;

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        var go = Instantiate(projectilePrefab, spawnPos, Quaternion.Euler(0f, 0f, angle));
        go.GetComponent<Projectile>()?.Launch(dir, projectileSpeed);

        // Trigger animation ném trên nhân vật
        movement?.TriggerThrow();
    }
}
