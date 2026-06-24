using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    [Header("Bình lửa — chuột TRÁI (25% HP quái)")]
    public GameObject firePotionPrefab;

    [Header("Bình điện — chuột PHẢI (50% HP quái)")]
    public GameObject lightningPotionPrefab;

    [Header("Cài đặt ném")]
    public float projectileSpeed = 14f;
    public float spawnOffset     = 0.6f;

    PlayerMovement movement;
    PlayerHealth   health;

    void Awake()
    {
        movement = GetComponent<PlayerMovement>();
        health   = GetComponent<PlayerHealth>();
    }

    void Update()
    {
        if (health != null && health.IsDead) return;

        if (Input.GetMouseButtonDown(0) && firePotionPrefab != null)
            Throw(firePotionPrefab);

        if (Input.GetMouseButtonDown(1) && lightningPotionPrefab != null)
            Throw(lightningPotionPrefab);
    }

    void Throw(GameObject prefab)
    {
        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = 0f;

        Vector2 dir = ((Vector2)mouseWorld - (Vector2)transform.position).normalized;
        if (dir == Vector2.zero) return;

        Vector3 spawnPos = transform.position + (Vector3)(dir * spawnOffset);
        spawnPos.z = 0f;

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        var go = Instantiate(prefab, spawnPos, Quaternion.Euler(0f, 0f, angle));
        go.GetComponent<Projectile>()?.Launch(dir, projectileSpeed);

        movement?.TriggerThrow();
    }
}
