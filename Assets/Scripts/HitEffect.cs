using UnityEngine;

/// <summary>
/// Gắn lên Prefab hiệu ứng (lửa / tia điện).
/// Tự hủy sau khi animation phát xong.
/// </summary>
public class HitEffect : MonoBehaviour
{
    [Tooltip("Thời gian tồn tại (giây) — đặt bằng độ dài animation clip")]
    public float lifetime = 1f;

    [Tooltip("Tốc độ animation — tăng lên 2 hoặc 3 nếu animation quá chậm")]
    public float animatorSpeed = 1f;

    void Start()
    {
        var anim = GetComponent<Animator>();
        if (anim != null)
            anim.speed = animatorSpeed;

        Destroy(gameObject, lifetime);
    }
}
