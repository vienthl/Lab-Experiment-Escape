using UnityEngine;

/// <summary>
/// Gắn lên Prefab hiệu ứng (lửa / tia điện).
/// Tự hủy sau khi animation phát xong.
/// </summary>
public class HitEffect : MonoBehaviour
{
    [Tooltip("Thời gian tồn tại (giây) — đặt bằng độ dài animation clip")]
    public float lifetime = 1f;

    void Start()
    {
        Destroy(gameObject, lifetime);
    }
}
