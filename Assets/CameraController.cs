using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Transform target;

    [Tooltip("Thời gian camera đuổi kịp player (giây) — nhỏ hơn = bám sát hơn")]
    public float smoothTime = 0.15f;

    public Vector3 offset = new Vector3(0, 0, -10);

    Vector3 velocity;

    void LateUpdate()
    {
        if (target == null) return;
        Vector3 desired = target.position + offset;

        // SmoothDamp không phụ thuộc framerate (Lerp với deltaTime thì có)
        transform.position = Vector3.SmoothDamp(transform.position, desired, ref velocity, smoothTime);
    }
}
