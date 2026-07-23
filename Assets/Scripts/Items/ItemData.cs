using UnityEngine;

public enum ItemType { FirePotion, LightningPotion, HealPotion, KeyCard, LoreNote, CurePotion, ShieldHelmet }

[CreateAssetMenu(fileName = "NewItemData", menuName = "Lab Escape/Item Data")]
public class ItemData : ScriptableObject
{
    [Header("Nhận dạng")]
    public string id;
    public string displayName;
    public Sprite icon;

    [Header("Loại & giá trị")]
    public ItemType type;

    [Tooltip("Số lượng cộng vào inventory khi nhặt (bình: số đạn; hồi máu: số lượng bình; keycard/lore: thường để 1)")]
    public int amount = 1;
}
