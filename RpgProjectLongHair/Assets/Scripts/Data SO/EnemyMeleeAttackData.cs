using UnityEngine;

[CreateAssetMenu(fileName = "EnemyMeleeAttack_Data", menuName = "Data/EnemyMeleeAttack")]
public class EnemyMeleeAttackData : MeleeAttackData
{
    [Header("Enemy Sequence")]
    [SerializeField] private bool _loopSequence = true;
    [SerializeField] private float _sequenceCooldown = 1f;

    public bool LoopSequence => _loopSequence;
    public float SequenceCooldown => _sequenceCooldown;

    // Helpers de conveniencia para el EnemyMeleeController
    // Usan ComboAttackConfig directamente — sin arrays paralelos

    public ComboAttackConfig GetAttackConfig(int index)
    {
        if (ComboAttacks == null || index < 0 || index >= ComboAttacks.Length)
            return null;
        return ComboAttacks[index];
    }

    public float GetDamageFrameTime(int index) =>
        GetAttackConfig(index)?.hitFrameTime ?? 0f;      // mismo nombre que usa el player

    public float GetAttackDuration(int index) =>
        GetAttackConfig(index)?.attackDuration ?? 0f;

    public AttackVFXConfig GetVFXConfig(int index) =>
        GetAttackConfig(index)?.attackVFX;               // ya está dentro del config, sin array paralelo

    public int AttackCount => MaxComboCount;             // alias de lo que hereda
}