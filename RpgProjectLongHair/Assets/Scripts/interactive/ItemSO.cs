using UnityEngine;

public enum WeaponCategory
{
    Axe,
    Hammer,
    Rifle,
    Gatling,
    CraftItem
}

[CreateAssetMenu(fileName = "NewItem", menuName = "Inventory/Item")]
public class ItemSO : ScriptableObject
{
    public int id;
    public string itemName;
    public Sprite icon;
    public ItemType type;
    public WeaponCategory weaponCategory;

    public int amount; // for quest
    public GameObject slotPrefab;
    public bool isDualWield;

    [Header("Single")]
    public GameObject equipPrefab;

    [Header("Dual Wield")]
    public GameObject rightHandPrefab;
    public GameObject leftHandPrefab;


    [Header("Armor")]
    public Mesh armorMesh;                    //mesh que reemplaza al base
    public Material[] armorMaterials;         //materiales del mesh
    // Si la armadura trae bones propios (mismo rig, distinto mesh):
    public GameObject armorSkinnedPrefab; // prefab con SkinnedMeshRenderer ya rigged

    [Header("VFX")]
    public ItemVFXConfig vfxConfig;

    [Header("Stats")]
    public int baseDamage;
    public int baseDefense;

    [Header("Restricciones")]
    public CharacterType owner;
}