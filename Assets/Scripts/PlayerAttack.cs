using UnityEngine;

// Xử lý tấn công của player bằng chuột.
// Chuột TRÁI → ném bình lửa (25% máu quái).
// Chuột PHẢI → ném bình điện (50% máu quái).
// Gán script này lên Player cùng với PlayerMovement và PlayerHealth.
public class PlayerAttack : MonoBehaviour
{
    [Header("Bình lửa — chuột TRÁI (25% HP quái)")]
    public GameObject firePotionPrefab;      // Kéo Prefab FirePotion vào đây trong Inspector

    [Header("Bình điện — chuột PHẢI (50% HP quái)")]
    public GameObject lightningPotionPrefab; // Kéo Prefab LightningPotion vào đây

    [Header("Cài đặt ném")]
    public float projectileSpeed = 14f; // Tốc độ bay của bình (unit/giây)

    [Tooltip("Khoảng cách spawn tính từ tâm player (phải > half-size collider player)")]
    public float spawnOffset = 1.8f;    // Bình xuất hiện cách tâm player 1.8 unit — đủ xa để không va chạm ngay

    [Tooltip("Scale bình khi spawn — chỉnh cho khớp kích thước nhân vật")]
    public float projectileScale = 0.35f; // Thu nhỏ bình so với kích thước gốc trong prefab

    PlayerMovement movement; // Dùng để gọi TriggerThrow() → phát animation ném
    PlayerHealth   health;   // Dùng để kiểm tra player còn sống không trước khi cho ném

    void Awake()
    {
        movement = GetComponent<PlayerMovement>();
        health   = GetComponent<PlayerHealth>();
    }

    void Update()
    {
        if (health != null && health.IsDead) return; // Đã chết → không cho ném

        // GetMouseButtonDown chỉ trả về true đúng 1 frame khi nhấn xuống (không spam)
        if (Input.GetMouseButtonDown(0) && firePotionPrefab != null)
            Throw(firePotionPrefab);       // Chuột trái → bình lửa

        if (Input.GetMouseButtonDown(1) && lightningPotionPrefab != null)
            Throw(lightningPotionPrefab);  // Chuột phải → bình điện
    }

    void Throw(GameObject prefab)
    {
        // Bước 1: Chuyển vị trí chuột từ tọa độ màn hình (pixel) sang tọa độ thế giới (world units)
        // Camera ở z = -10, cần đặt z = 10 để ScreenToWorldPoint chiếu đúng xuống mặt phẳng z=0
        Vector3 mouseScreenPos = Input.mousePosition;
        mouseScreenPos.z = -Camera.main.transform.position.z; // = 10f
        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(mouseScreenPos);
        mouseWorld.z = 0f; // Đảm bảo tọa độ nằm trên mặt phẳng 2D

        // Bước 2: Tính vector hướng từ player đến con trỏ chuột
        // .normalized → đưa vector về độ dài = 1 để tốc độ không phụ thuộc khoảng cách
        Vector2 dir = ((Vector2)mouseWorld - (Vector2)transform.position).normalized;
        if (dir == Vector2.zero) return; // Chuột đúng vị trí player → bỏ qua

        // Bước 3: Spawn bình tại vị trí lệch ra theo hướng ném (để bình không nằm trong collider player)
        Vector3 spawnPos = transform.position + (Vector3)(dir * spawnOffset);
        spawnPos.z = 0f;

        // Bước 4: Xoay sprite bình theo hướng bay (Atan2 tính góc của vector 2D)
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        var go = Instantiate(prefab, spawnPos, Quaternion.Euler(0f, 0f, angle));

        go.transform.localScale = Vector3.one * projectileScale; // Thu nhỏ bình về kích thước phù hợp

        go.GetComponent<Projectile>()?.Launch(dir, projectileSpeed); // Truyền hướng + tốc độ cho Projectile
        movement?.TriggerThrow();  // Kích hoạt animation ném (dấu ?. → bỏ qua nếu null)
    }
}
