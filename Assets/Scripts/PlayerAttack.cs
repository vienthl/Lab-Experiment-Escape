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

    [Header("Âm thanh")]
    public AudioClip throwSound;

    PlayerMovement movement;
    PlayerHealth   health;
    PlayerInventory inventory;
    float lastThrowTime = -999f;

    void Awake()
    {
        movement  = GetComponent<PlayerMovement>();
        health    = GetComponent<PlayerHealth>();
        inventory = GetComponent<PlayerInventory>();
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
            TryThrow(firePotionPrefab, ItemType.FirePotion);
        else if (Input.GetMouseButtonDown(1) && lightningPotionPrefab != null)
            TryThrow(lightningPotionPrefab, ItemType.LightningPotion);
    }

    // Tiêu hao 1 bình trong inventory trước khi ném — hết bình thì không ném, không tốn cooldown.
    // inventory == null (chưa gắn component) → fallback ném vô hạn như cũ, không chặn gameplay cũ.
    void TryThrow(GameObject prefab, ItemType ammoType)
    {
        if (inventory != null && !inventory.TryConsume(ammoType, 1))
        {
            Debug.Log($"Hết bình {ammoType} — nhặt thêm trước khi ném tiếp.");
            return;
        }

        Throw(prefab);
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
        movement?.TriggerThrow(dir);
        AudioOneShot.Play(throwSound, transform.position);
        lastThrowTime = Time.time;
    }
}
