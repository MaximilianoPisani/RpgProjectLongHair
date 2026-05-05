using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewCraftRecipe", menuName = "Crafting/CraftRecipe")]
public class CraftRecipeSO : ScriptableObject
{
    public string recipeName; // "Rifle" ó "Gatling"
    public List<CraftItemSO> requiredItems; // los 3 items necesarios
    public ItemSO resultItem;      //el arma que se craftea,
    //acá no va el resultante que está hecho? y la pensé mal al itemSO que no me andaba,
    //o sea no necesito llenar con datos nuevos como hacía con los item para craftear, antes de este codigo

    //test 
    public string resultWeaponName;
    public Sprite resultIcon;
}
