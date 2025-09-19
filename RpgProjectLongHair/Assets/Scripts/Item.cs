using UnityEngine;

[System.Serializable]
public class Item
{
    public string name;
    public string description;
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