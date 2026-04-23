using Fusion;
using UnityEngine;

public class EnemyRangedController : EnemyBaseController
{
    [Header("Ranged Settings")]
    [SerializeField] private RangedAttackData _rangedAttackData;
    [SerializeField] private float _preferredMinRange = 4f;
    [SerializeField] private float _preferredMaxRange = 7f;

    [Header("Animation & VFX")]
    [SerializeField] private EnemyAnimationController animationController;
    [SerializeField] private EnemyVFXController vfxController;

    // Tracking de disparo
    [Networked] public float NextRangedAttackTime { get; set; }
    [Networked] public bool IsReloading { get; set; }

    // Propiedades públicas
    public RangedAttackData RangedAttackData => _rangedAttackData;
    public float PreferredMinRange => _preferredMinRange;
    public float PreferredMaxRange => _preferredMaxRange;
    public EnemyAnimationController AnimationController => animationController;
    public EnemyVFXController VFXController => vfxController;
    public override void Spawned()
    {
        base.Spawned();

        if (animationController == null)
            animationController = GetComponent<EnemyAnimationController>();

        if (vfxController == null)
            vfxController = GetComponent<EnemyVFXController>();

        IsReloading = false;
    }

    protected override void InitStateMachine()
    {
        ChangeState(new EnemyIdleRangedState(this));
    }

    protected override IEnemyState GetIdleState() => new EnemyIdleRangedState(this);

    /// <summary>
    /// Dispara la animación ranged pasando ambos VFX configs desde el data.
    /// El timing de cada VFX viene embebido en su AttackVFXConfig.
    /// </summary>

    public void ExecuteShot()
    {
        if (animationController == null || _rangedAttackData == null) return;

        animationController.PlayRangedAttack(
            _rangedAttackData.FireEjectionVFX,
            _rangedAttackData.ShellEjectionVFX
        );
    }

    /// <summary>
    /// Spawnea el proyectil hacia el objetivo.
    /// </summary>
    public void FireProjectile()
    {
        if (_rangedAttackData?.ProjectilePrefab == null || TargetPlayer == null) return;

        Vector3 spawnPos = attackOrigin != null
            ? attackOrigin.position
            : transform.position + Vector3.up * 1.2f;

        Vector3 targetPos = GetTargetChestPosition();
        Vector3 direction = (targetPos - spawnPos).normalized;

        Runner.Spawn(
            _rangedAttackData.ProjectilePrefab,
            spawnPos,
            Quaternion.LookRotation(direction),
            PlayerRef.None,
            (runner, spawned) =>
            {
                var proj = spawned.GetComponent<EnemyProjectile>();
                if (proj != null)
                    proj.InitServer(direction, _rangedAttackData, spawnPos);
            }
        );
    }

    /// <summary>
    /// Ejecuta la animación de recarga.
    /// </summary>
    public void ExecuteReload()
    {
        if (animationController == null) return;

        IsReloading = true;
        animationController.PlayReloadAnimation();
    }

    /// <summary>
    /// Marca la recarga como completada.
    /// </summary>
    public void CompleteReload()
    {
        IsReloading = false;
    }

    private Vector3 GetTargetChestPosition()
    {
        if (TargetPlayer == null)
            return transform.position + transform.forward * 5f;

        if (TargetPlayer.TryGetComponent<Collider>(out var col))
            return col.bounds.center;

        return TargetPlayer.position + Vector3.up * 1.2f;
    }

    public void TriggerHitAnimation()
    {
        if (animationController != null)
            animationController.PlayHitReaction();
    }

    private void OnValidate()
    {
        if (animationController == null)
            animationController = GetComponent<EnemyAnimationController>();

        if (vfxController == null)
            vfxController = GetComponent<EnemyVFXController>();
    }
}