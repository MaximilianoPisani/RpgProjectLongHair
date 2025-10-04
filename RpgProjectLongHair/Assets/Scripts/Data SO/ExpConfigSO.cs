using UnityEngine;

public enum ExpEvent : byte { Kill, Chest, Quest, Craft }

[CreateAssetMenu(fileName = "ExpConfig", menuName = "RPG/Exp Config")]
public class ExpConfigSO : ScriptableObject
{
    [Header("Curva de Nivel")]
    public int baseLevelThreshold = 100; // EXP de 1->2
    public float levelCurve = 1.35f;     // umbral *= levelCurve por nivel

    [Header("Recompensas de EXP")]
    public int killExp = 100;
    public int chestExp = 15;
    public int questExp = 120;
    public int craftExp = 10;

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
        return Mathf.FloorToInt(baseLevelThreshold * Mathf.Pow(levelCurve, level - 1));
    }
}
