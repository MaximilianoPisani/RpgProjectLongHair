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

    [Tooltip("Daño base que aplica el proyectil por impacto validado.")]
    [Min(0)]
    [SerializeField] private int _damage = 25;

    [Header("Alcance y colisión")]
    [Tooltip("Distancia máxima que puede recorrer el proyectil antes de auto-destruirse.")]
    [Min(0f)]
    [SerializeField] private float _attackRange = 30f;

    [Tooltip("Radio de verificación de impacto (se usa localmente y en LagComp OverlapSphere).")]
    [Min(0f)]
    [SerializeField] private float _hitRadius = 0.3f;

    [Tooltip("Segundos de vida máxima del proyectil (fallback al alcance).")]
    [Min(0f)]
    [SerializeField] private float _lifetimeSeconds = 5f;

    [Header("Detección de objetivos")]
    [Tooltip("LayerMask para detectar candidatos locales (debe incluir la capa de Hitbox).")]
    [SerializeField] private LayerMask _targetLayer;

    // --- Getters públicos (inmutables en runtime) ---
    public NetworkObject ProjectilePrefab => _projectilePrefab;
    public float ProjectileSpeed => _projectileSpeed;
    public int Damage => _damage;
    public float AttackRange => _attackRange;
    public float HitRadius => _hitRadius;
    public float LifetimeSeconds => _lifetimeSeconds;
    public LayerMask TargetLayer => _targetLayer;

    protected override void OnValidate()
    {
        base.OnValidate();

        if (_projectileSpeed < 0f) _projectileSpeed = 0f;
        if (_damage < 0) _damage = 0;
        if (_attackRange < 0f) _attackRange = 0f;
        if (_hitRadius < 0f) _hitRadius = 0f;
        if (_lifetimeSeconds < 0f) _lifetimeSeconds = 0f;

        // Sugerencia: mantener coherencia entre vida y alcance
        // (no obligatorio, solo conveniente)
        if (_attackRange <= 0f && _lifetimeSeconds <= 0f)
            _lifetimeSeconds = 5f;
    }
}
