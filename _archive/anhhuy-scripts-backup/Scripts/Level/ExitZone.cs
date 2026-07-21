using UnityEngine;

// Đặt trigger tại cửa thoát. Chỉ hoàn thành level khi cửa đã mở (lockdown đã xong).
[RequireComponent(typeof(Collider2D))]
public class ExitZone : MonoBehaviour
{
    [Tooltip("Cửa thoát cần đang mở thì mới cho hoàn thành level (để trống nếu không cần kiểm tra)")]
    public DoorController watchedDoor;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (watchedDoor != null && !watchedDoor.IsOpen) return;

        GameManager.Instance?.CompleteLevel();
    }
}
