using Fusion;

public struct ItemData : INetworkStruct
{
    public int id;       
    public ItemType type;
}
public enum ItemType
{
    Weapon,
    Armor,
    Consumable,
    QuestItem,
    Misc
}
