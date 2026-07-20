using UnityEngine;

// Đặt trigger tại cửa thoát. Chỉ hoàn thành level khi cửa đã mở (lockdown đã xong).
[RequireComponent(typeof(Collider2D))]
public class ExitZone : MonoBehaviour
{
    [Tooltip("Cửa thoát cần đang mở thì mới cho hoàn thành level (để trống nếu không cần kiểm tra)")]
    public DoorController watchedDoor;

    [Tooltip("Tên scene sẽ load sau khi qua cửa này (vd Level2, Level3, MainMenu...)")]
    public string nextSceneName = "MainMenu";

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (watchedDoor != null && !watchedDoor.IsOpen) return;

        GameManager.Instance?.CompleteLevel(nextSceneName);
    }
}
