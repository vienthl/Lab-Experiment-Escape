using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    [Header("Bình lửa — chuột TRÁI")]
    public GameObject firePotionPrefab;

    [Header("Bình băng — chuột PHẢI")]
    public GameObject lightningPotionPrefab;

    [Header("Cài đặt ném")]
    public float projectileSpeed = 14f;

    [Tooltip("Khoảng cách spawn tính từ tâm Player.")]
    public float spawnOffset = 0.4f;

    [Tooltip("Kích thước bình khi spawn.")]
    public float projectileScale = 0.35f;

    [Header("Cooldown")]
    [Tooltip("Thời gian chờ giữa hai lần ném bình lửa.")]
    public float fireCooldown = 0.5f;

    [Tooltip("Thời gian reload bình băng.")]
    public float iceCooldown = 1f;

    private PlayerMovement movement;
    private PlayerHealth health;

    private float lastFireThrowTime = -999f;
    private float lastIceThrowTime = -999f;

    public float IceCooldownRemaining
    {
        get
        {
            float remaining =
                iceCooldown - (Time.time - lastIceThrowTime);

            return Mathf.Max(0f, remaining);
        }
    }

    public bool IsIceReady => IceCooldownRemaining <= 0f;

    private void Awake()
    {
        movement = GetComponent<PlayerMovement>();
        health = GetComponent<PlayerHealth>();
    }

    private void Update()
    {
        if (health != null && health.IsDead)
        {
            return;
        }

        if (movement != null && movement.IsBusy)
        {
            return;
        }

        // Chuột trái: bình lửa.
        if (Input.GetMouseButtonDown(0))
        {
            TryThrowFire();
        }
        // Chuột phải: bình băng.
        else if (Input.GetMouseButtonDown(1))
        {
            TryThrowIce();
        }
    }

    private void TryThrowFire()
    {
        if (firePotionPrefab == null)
        {
            return;
        }

        if (Time.time - lastFireThrowTime < fireCooldown)
        {
            return;
        }

        if (Throw(firePotionPrefab))
        {
            lastFireThrowTime = Time.time;
        }
    }

    private void TryThrowIce()
    {
        if (lightningPotionPrefab == null)
        {
            return;
        }

        if (Time.time - lastIceThrowTime < iceCooldown)
        {
            UnityEngine.Debug.Log(
                $"Bình băng đang reload: {IceCooldownRemaining:F1} giây.",
                this
            );

            return;
        }

        if (Throw(lightningPotionPrefab))
        {
            lastIceThrowTime = Time.time;
        }
    }

    private bool Throw(GameObject prefab)
    {
        if (Camera.main == null)
        {
            UnityEngine.Debug.LogWarning(
                "Không tìm thấy Main Camera.",
                this
            );

            return false;
        }

        Vector3 mouseScreenPosition = Input.mousePosition;

        mouseScreenPosition.z =
            -Camera.main.transform.position.z;

        Vector3 mouseWorldPosition =
            Camera.main.ScreenToWorldPoint(
                mouseScreenPosition
            );

        mouseWorldPosition.z = 0f;

        Vector2 direction =
            ((Vector2)mouseWorldPosition -
             (Vector2)transform.position).normalized;

        if (direction == Vector2.zero)
        {
            return false;
        }

        Vector3 spawnPosition =
            transform.position +
            (Vector3)(direction * spawnOffset);

        spawnPosition.z = 0f;

        float angle =
            Mathf.Atan2(direction.y, direction.x) *
            Mathf.Rad2Deg;

        GameObject projectileObject = Instantiate(
            prefab,
            spawnPosition,
            Quaternion.Euler(0f, 0f, angle)
        );

        projectileObject.transform.localScale =
            Vector3.one * projectileScale;

        Projectile projectile =
            projectileObject.GetComponent<Projectile>();

        if (projectile == null)
        {
            UnityEngine.Debug.LogWarning(
                $"Prefab {prefab.name} thiếu script Projectile.",
                prefab
            );

            Destroy(projectileObject);
            return false;
        }

        projectile.Launch(direction, projectileSpeed);

        if (movement != null)
        {
            movement.TriggerThrow(direction);
        }

        return true;
    }
}