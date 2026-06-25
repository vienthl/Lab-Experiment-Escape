using UnityEngine;

public class HitEffect : MonoBehaviour
{
    [Tooltip("Thời gian tồn tại (giây) — nên đặt bằng độ dài animation clip")]
    public float lifetime = 1f;

    [Tooltip("Tốc độ animation — 1 = bình thường, 2 = nhanh gấp đôi")]
    public float animatorSpeed = 1f;

    void Start()
    {
        var anim = GetComponent<Animator>();
        if (anim != null)
            anim.speed = animatorSpeed;

        Destroy(gameObject, lifetime);
    }
}
