using UnityEngine;
using Fusion;

[CreateAssetMenu(fileName = "RangedAttack_000_Data", menuName = "Data/RangedAttack")]
public class RangedAttackData : AttackData
{
    [Header("Proyectil")]
    [Tooltip("Prefab de NetworkObject que se spawnea como proyectil.")]
    [SerializeField] private NetworkObject _projectilePrefab;

    [Tooltip("Velocidad del proyectil en unidades/segundo.")]
    [Min(0f)]
    [SerializeField] private float _projectileSpeed = 25f;

    [Header("Alcance y colisi�n")]
    [Tooltip("Segundos de vida m�xima del proyectil (fallback al alcance).")]
    [Min(0f)]
    [SerializeField] private float _lifetimeSeconds = 5f;

    [Header("Detecci�n de objetivos")]
    [Tooltip("LayerMask para detectar candidatos locales (debe incluir la capa de Hitbox).")]
    [SerializeField] private LayerMask _targetLayer;

    public NetworkObject ProjectilePrefab => _projectilePrefab;
    public float ProjectileSpeed => _projectileSpeed;
    public float LifetimeSeconds => _lifetimeSeconds;
    public LayerMask TargetLayer => _targetLayer;

    protected override void OnValidate()
    {
        base.OnValidate(); 

        if (_projectileSpeed < 0f) _projectileSpeed = 0f;
        if (_lifetimeSeconds < 0f) _lifetimeSeconds = 0f;


        if (AttackRange <= 0f && _lifetimeSeconds <= 0f)
            _lifetimeSeconds = 5f;
    }
}