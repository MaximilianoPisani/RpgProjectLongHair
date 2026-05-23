using UnityEngine;

public enum ExpEvent : byte { Kill, Chest, Quest, Craft }

[CreateAssetMenu(fileName = "ExpConfig", menuName = "RPG/Exp Config")]
public class ExpConfigSO : ScriptableObject
{
    [Header("EXP necesaria por nivel")]
    public int expPerLevel = 200;

    [Header("Recompensas de EXP")]
    public int killExp = 100;
    public int chestExp = 15;
    public int questExp = 120;
    public int craftExp = 10;

    [Header("Level Up VFX")]
    public AttackVFXConfig levelUpVFX;

    public int GetExp(ExpEvent evt, int hint = 0)
    {
        return evt switch
        {
            ExpEvent.Kill => killExp,
            ExpEvent.Chest => chestExp,
            ExpEvent.Quest => questExp,
            ExpEvent.Craft => craftExp,
            _ => 0
        };
    }

    public int CalcExpToNext(int level)
    {
        return expPerLevel;
    }
}