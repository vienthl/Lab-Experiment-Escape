using UnityEngine;

// Trạng thái "nhiễm độc nhẹ" sau khi hoàn thành Level 1 (đánh hạ boss Mr.X) — đổi màu áo Player
// sang đỏ. Hết nhiễm khi uống bình thuốc xanh (CurePotion) rơi ra từ Boss2 ở Level 2.
// Trạng thái đọc/ghi qua GameManager.SavedData nên giữ nguyên khi chuyển Level1 → Level2.
[RequireComponent(typeof(PlayerHealth))]
public class PlayerInfection : MonoBehaviour
{
    [Header("Màu áo khi đang nhiễm độc")]
    public Color infectedTint = new Color(1f, 0.55f, 0.55f, 1f);

    PlayerHealth health;
    PlayerInventory inventory;

    public bool IsInfected { get; private set; }
    public bool HasCure => inventory != null && inventory.curePotions > 0;

    void Awake()
    {
        health = GetComponent<PlayerHealth>();
        inventory = GetComponent<PlayerInventory>();
    }

    void Start()
    {
        var saved = GameManager.Instance != null ? GameManager.Instance.SavedData : null;
        IsInfected = saved != null && saved.isInfected;
        ApplyTint();
    }

    // Gọi từ PlayerMovement giữa CureRoutine (sau khi animation Drink chạy xong, phím C)
    public void ConsumeAndCure()
    {
        if (!IsInfected || !HasCure) return;
        if (!inventory.TryConsume(ItemType.CurePotion, 1)) return;

        IsInfected = false;
        ApplyTint();
        GameManager.Instance?.SetInfected(false);
    }

    void ApplyTint()
    {
        if (health != null)
            health.SetBaseTint(IsInfected ? infectedTint : Color.white);
    }
}
