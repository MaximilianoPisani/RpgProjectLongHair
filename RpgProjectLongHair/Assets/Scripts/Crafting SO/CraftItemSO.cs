using UnityEngine;

[CreateAssetMenu(fileName = "NewCraftItem", menuName = "Crafting/CraftItem")]
public class CraftItemSO : ScriptableObject
{
    public int id;
    public string itemName;
    public Sprite icon;
    public GameObject visualPrefab;
}
