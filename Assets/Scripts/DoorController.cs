using UnityEngine;

public class DoorController : MonoBehaviour
{
    [Header("Tham chiếu")]
    [Tooltip("Collider vật lý chặn người chơi khi cửa đóng")]
    public BoxCollider2D blockCollider;

    [Header("Âm thanh")]
    public AudioClip openSound;
    public AudioClip closeSound;

    Animator animator;
    bool isOpen;
    bool hasStarted; // chặn tiếng "đóng cửa" giả lúc Start() tự đóng cửa ban đầu

    public bool IsOpen => isOpen;

    void Awake()
    {
        animator = GetComponent<Animator>();
    }

    void Start()
    {
        // Luôn đóng khi phòng bắt đầu, kể cả nếu quên set sẵn trong Inspector
        CloseDoor();
        hasStarted = true;
    }

    public void OpenDoor()
    {
        if (isOpen) return;
        isOpen = true;

        // Reset trigger đối lập trước — tránh trường hợp "Close" bị set từ trước (lúc đang đứng yên
        // ở Door_Closed, không có transition nào tiêu thụ nó) rồi treo armed tới khi vào Door_Open
        // thì lập tức bị tiêu thụ, khiến cửa vừa mở đã tự đóng lại ngay.
        animator.ResetTrigger("Close");
        animator.SetTrigger("Open");
        AudioOneShot.Play(openSound, transform.position);

        if (blockCollider != null) blockCollider.enabled = false;
    }

    public void CloseDoor()
    {
        isOpen = false;

        animator.ResetTrigger("Open");
        animator.SetTrigger("Close");

        if (blockCollider != null) blockCollider.enabled = true;

        // Chỉ phát tiếng khi đây là 1 lần đóng THẬT (lockdown/gameplay), không phải lần
        // tự đóng ban đầu ở Start() lúc mới vào scene.
        if (hasStarted)
            AudioOneShot.Play(closeSound, transform.position);
    }

    // API rõ nghĩa cho hệ lockdown/khóa theo điều kiện (LockdownRoomController, keycard...)
    public void SetLocked(bool locked)
    {
        if (locked) CloseDoor();
        else OpenDoor();
    }
}
