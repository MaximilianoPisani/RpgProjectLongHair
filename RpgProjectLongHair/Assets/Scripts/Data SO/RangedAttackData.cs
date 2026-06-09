using UnityEngine;
using Fusion;
public enum FireMode
{
    SingleShot,  // Un disparo por click (Mosquete)
    Automatic    // Disparo continuo mientras se mantiene presionado (Gatling)
}
[CreateAssetMenu(fileName = "RangedAttack_Data", menuName = "Data/RangedAttack")]
public class RangedAttackData : AttackData
{
    [Header("Projectile")]
    [SerializeField] private NetworkObject _projectilePrefab;
    [Min(0f)]
    [SerializeField] private float _projectileSpeed = 25f;
    [Min(0f)]
    [SerializeField] private float _lifetimeSeconds = 5f;
    [SerializeField] private LayerMask _targetLayer;
    [Header("Fire Mode")]
    [SerializeField] private FireMode _fireMode = FireMode.SingleShot;
    [SerializeField] private bool _requireReleaseToShootAgain = true;
    [Header("Timing")]
    [Tooltip("Tiempo desde el inicio del disparo hasta que se spawnea el proyectil")]
    [SerializeField] private float _shootFrameTime = 0.2f;
    [Tooltip("Duración total de la animación de disparo")]
    [SerializeField] private float _shootDuration = 0.4f;
    [Tooltip("Tiempo que tarda la recarga completa")]
    [SerializeField] private float _reloadDuration = 1.0f;
    [Header("Automatic Fire (Solo para FireMode.Automatic)")]
    [Tooltip("Tiempo entre disparos en modo automático")]
    [SerializeField] private float _fireRate = 0.3f;
    [Tooltip("Tiempo máximo que puede disparar antes de recargar (0 = infinito mientras tenga munición)")]
    [SerializeField] private float _maxContinuousFireTime = 3f;
    [Header("VFX")]
    [Tooltip("VFX del fogonazo / disparo. El timing está incluido en el config.")]
    [SerializeField] private AttackVFXConfig _fireEjectionVFX;
    [Tooltip("VFX de expulsión de casquillo. El timing está incluido en el config.")]
    [SerializeField] private AttackVFXConfig _shellEjectionVFX;
    // Propiedades públicas
    public NetworkObject ProjectilePrefab => _projectilePrefab;
    public float ProjectileSpeed => _projectileSpeed;
    public float LifetimeSeconds => _lifetimeSeconds;
    public LayerMask TargetLayer => _targetLayer;
    public FireMode Mode => _fireMode;
    public bool RequireReleaseToShootAgain => _requireReleaseToShootAgain;
    public float ShootFrameTime => _shootFrameTime;
    public float ShootDuration => _shootDuration;
    public float ReloadDuration => _reloadDuration;
    public float FireRate => _fireRate;
    public float MaxContinuousFireTime => _maxContinuousFireTime;
    // VFX — ahora devuelven AttackVFXConfig completo (timing incluido)
    public AttackVFXConfig FireEjectionVFX => _fireEjectionVFX;
    public AttackVFXConfig ShellEjectionVFX => _shellEjectionVFX;
    protected override void OnValidate()
    {
        base.OnValidate();
        if (_projectileSpeed < 0f) _projectileSpeed = 0f;
        if (_lifetimeSeconds < 0f) _lifetimeSeconds = 0f;
        if (_shootFrameTime < 0f) _shootFrameTime = 0f;
        if (_shootDuration < 0f) _shootDuration = 0f;
        if (_reloadDuration < 0f) _reloadDuration = 0f;
        if (_fireRate < 0.1f) _fireRate = 0.1f;
        if (_maxContinuousFireTime < 0f) _maxContinuousFireTime = 0f;
        if (_shootFrameTime > _shootDuration)
            _shootFrameTime = _shootDuration * 0.5f;
    }
}