using UnityEngine;

[CreateAssetMenu(fileName = "MeleeAttack_Data", menuName = "Data/MeleeAttack")]
public class MeleeAttackData : AttackData
{
    [Header("Combo Settings")]
    [SerializeField] private ComboAttackConfig[] _comboAttacks;
    [SerializeField] private float _comboResetTime = 1f;

    public ComboAttackConfig[] ComboAttacks => _comboAttacks;
    public float ComboResetTime => _comboResetTime;
    public int MaxComboCount => _comboAttacks?.Length ?? 0;

    protected override void OnValidate()
    {
        base.OnValidate();
        if (_comboResetTime < 0) _comboResetTime = 0;
    }
}

[System.Serializable]
public class ComboAttackConfig
{
    [Header("Timing")]
    [Tooltip("Duración total de la animación de ataque")]
    public float attackDuration = 0.5f;

    [Tooltip("Tiempo desde el inicio hasta que se aplica el daño")]
    public float hitFrameTime = 0.25f;

    [Tooltip("Tiempo desde el inicio hasta que se abre la ventana de combo")]
    public float comboWindowOpenTime = 0.3f;

    [Tooltip("Tiempo desde el inicio hasta que se cierra la ventana de combo")]
    public float comboWindowCloseTime = 0.6f;

    [Header("Damage")]
    [Tooltip("Daño específico de este ataque del combo")]
    public int damage = 10;

    [Header("Animation")]
    [Tooltip("Nombre del estado de animación (opcional, para debugging)")]
    public string animationStateName = "";
}
