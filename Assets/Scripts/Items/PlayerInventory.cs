using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    [Header("Số lượng hiện có (đọc để debug — đừng sửa tay lúc Play)")]
    public int firePotions;
    public int lightningPotions;
    public int healPotions;
    public int curePotions;
    public bool hasShieldItem;

    readonly List<string> keyIds = new List<string>();

    public IReadOnlyList<string> KeyIds => keyIds;

    // HUD đăng ký nghe để tự refresh, không cần polling mỗi frame.
    public event Action OnChanged;

    void Start()
    {
        var saved = GameManager.Instance != null ? GameManager.Instance.SavedData : null;
        if (saved == null) return;

        firePotions = saved.firePotions;
        lightningPotions = saved.lightningPotions;
        healPotions = saved.healPotions;
        curePotions = saved.curePotions;
        hasShieldItem = saved.hasShieldItem;
        keyIds.Clear();
        keyIds.AddRange(saved.keyIds);

        OnChanged?.Invoke();
    }

    public void Add(ItemData data, int quantity)
    {
        if (data == null || quantity <= 0) return;

        switch (data.type)
        {
            case ItemType.FirePotion:
                firePotions += quantity;
                break;
            case ItemType.LightningPotion:
                lightningPotions += quantity;
                break;
            case ItemType.HealPotion:
                healPotions += quantity;
                break;
            case ItemType.CurePotion:
                curePotions += quantity;
                break;
            case ItemType.ShieldHelmet:
                hasShieldItem = true;
                break;
            case ItemType.KeyCard:
                if (!string.IsNullOrEmpty(data.id) && !keyIds.Contains(data.id))
                    keyIds.Add(data.id);
                break;
            case ItemType.LoreNote:
                // Chưa có hệ lore riêng — để trống, mở rộng sau khi cần.
                break;
        }

        OnChanged?.Invoke();
    }

    public bool TryConsume(ItemType type, int quantity)
    {
        if (quantity <= 0) return true;

        switch (type)
        {
            case ItemType.FirePotion:
                if (firePotions < quantity) return false;
                firePotions -= quantity;
                break;
            case ItemType.LightningPotion:
                if (lightningPotions < quantity) return false;
                lightningPotions -= quantity;
                break;
            case ItemType.HealPotion:
                if (healPotions < quantity) return false;
                healPotions -= quantity;
                break;
            case ItemType.CurePotion:
                if (curePotions < quantity) return false;
                curePotions -= quantity;
                break;
            default:
                return false;
        }

        OnChanged?.Invoke();
        return true;
    }

    public bool HasKey(string id) => !string.IsNullOrEmpty(id) && keyIds.Contains(id);
}
