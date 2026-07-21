using UnityEngine;

// Đặt trigger tại cửa thoát. Chỉ hoàn thành level khi cửa đã mở (lockdown đã xong).
[RequireComponent(typeof(Collider2D))]
public class ExitZone : MonoBehaviour
{
    [Tooltip("Cửa thoát cần đang mở thì mới cho hoàn thành level (để trống nếu không cần kiểm tra)")]
    public DoorController watchedDoor;

    [Tooltip("Tên scene sẽ load sau khi qua cửa này (vd Level2, Level3, MainMenu...)")]
    public string nextSceneName = "MainMenu";

    [Tooltip("Bật nếu cửa này chỉ cho qua khi Player đã hết nhiễm độc (vd Door_Extrance ở Level2 dẫn tới Happy Ending)")]
    public bool requireCured = false;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (watchedDoor != null && !watchedDoor.IsOpen) return;

        if (requireCured)
        {
            var infection = other.GetComponent<PlayerInfection>();
            if (infection != null && infection.IsInfected) return; // còn nhiễm độc — chưa được ra
        }

        GameManager.Instance?.CompleteLevel(nextSceneName);
    }
}
