using UnityEngine;

// Script gắn lên Prefab hiệu ứng (FireEffect / LightningEffect2).
// Nhiệm vụ: phát animation rồi tự hủy — nếu không có script này, hiệu ứng sẽ tồn tại mãi trong scene.
public class HitEffect : MonoBehaviour
{
    [Tooltip("Thời gian tồn tại (giây) — nên đặt bằng độ dài animation clip")]
    public float lifetime = 1f;

    [Tooltip("Tốc độ animation — 1 = bình thường, 2 = nhanh gấp đôi, 3 = nhanh gấp 3")]
    public float animatorSpeed = 1f;

    void Start() // Chạy 1 lần ngay frame đầu tiên sau khi được Instantiate
    {
        // Chỉnh tốc độ animation nếu cần (VD: LightningEffect2 mặc định chậm → đặt 2.5)
        var anim = GetComponent<Animator>();
        if (anim != null)
            anim.speed = animatorSpeed;

        // Đặt lịch tự hủy sau `lifetime` giây — Unity tự gọi, không cần viết thêm gì
        Destroy(gameObject, lifetime);
    }
}
