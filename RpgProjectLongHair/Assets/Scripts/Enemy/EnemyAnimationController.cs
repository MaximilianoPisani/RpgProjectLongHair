using UnityEngine;

/// <summary>
/// Controlador de animaciones genérico para enemigos (Melee y Ranged).
/// </summary>
public class EnemyAnimationController : MonoBehaviour
{
    [Header("Animator")]
    [SerializeField] private Animator animator;

    [Header("Animation Parameters")]
    [SerializeField] private string speedParameter = "Speed";
    [SerializeField] private string attackIndexParameter = "AttackIndex";

    [Header("Attack Triggers")]
    [Tooltip("Trigger para ataque melee")]
    [SerializeField] private string meleeAttackTrigger = "MeleeAttack";

    [Tooltip("Trigger para ataque ranged")]
    [SerializeField] private string rangedAttackTrigger = "RangedAttack";

    [Tooltip("Trigger para recarga")]
    [SerializeField] private string reloadTrigger = "Reload";

    [Tooltip("Trigger para recibir daño")]
    [SerializeField] private string hitTrigger = "Hit";

    [Tooltip("Trigger para muerte")]
    [SerializeField] private string deathTrigger = "Death";

    [Header("Idle Settings")]
    [Tooltip("Cantidad de animaciones idle disponibles (0 = solo una idle)")]
    [SerializeField] private int idleVariationsCount = 2;

    [Tooltip("Tiempo mínimo antes de cambiar idle animation")]
    [SerializeField] private float minIdleChangeTime = 3f;

    [Tooltip("Tiempo máximo antes de cambiar idle animation")]
    [SerializeField] private float maxIdleChangeTime = 8f;

    [SerializeField] private string idleIndexParameter = "IdleIndex";

    [Header("References")]
    [SerializeField] private UnityEngine.AI.NavMeshAgent agent;
    [SerializeField] private EnemyVFXController vfxController;

    // Control interno
    private float nextIdleChangeTime;
    private int currentIdleIndex = 0;
    private bool isAttacking = false;
    private bool isReloading = false;
    private bool isDead = false;
    private float lastSpeed = 0f;

    // Hash de parámetros para optimización
    private int speedHash;
    private int meleeAttackHash;
    private int attackIndexHash;
    private int rangedAttackHash;
    private int reloadHash;
    private int hitHash;
    private int deathHash;
    private int idleIndexHash;

    private EnemyNetworkSync _networkSync;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (agent == null)
            agent = GetComponent<UnityEngine.AI.NavMeshAgent>();

        if (vfxController == null)
            vfxController = GetComponent<EnemyVFXController>();

        speedHash = Animator.StringToHash(speedParameter);
        meleeAttackHash = Animator.StringToHash(meleeAttackTrigger);
        attackIndexHash = Animator.StringToHash(attackIndexParameter);
        rangedAttackHash = Animator.StringToHash(rangedAttackTrigger);
        reloadHash = Animator.StringToHash(reloadTrigger);
        hitHash = Animator.StringToHash(hitTrigger);
        deathHash = Animator.StringToHash(deathTrigger);
        idleIndexHash = Animator.StringToHash(idleIndexParameter);

        ScheduleNextIdleChange();

        _networkSync = GetComponent<EnemyNetworkSync>();
    }

    private void Update()
    {
        if (_networkSync != null && !_networkSync.Object.HasStateAuthority) return;

        if (animator == null || isDead) return;

        UpdateMovementAnimation();
        UpdateIdleVariation();
        UpdateVFX();
    }

    private void UpdateMovementAnimation()
    {
        if (agent == null || !agent.isOnNavMesh) return;

        float currentSpeed = agent.velocity.magnitude;
        lastSpeed = Mathf.Lerp(lastSpeed, currentSpeed, Time.deltaTime * 10f);
        animator.SetFloat(speedHash, lastSpeed);
    }

    private void UpdateVFX()
    {
        if (vfxController == null) return;
        vfxController.UpdateThrusters(lastSpeed);
    }

    private void UpdateIdleVariation()
    {
        if (isAttacking || isReloading || idleVariationsCount <= 1) return;

        if (lastSpeed < 0.1f && Time.time >= nextIdleChangeTime)
        {
            int newIdleIndex;
            do
            {
                newIdleIndex = Random.Range(0, idleVariationsCount);
            }
            while (newIdleIndex == currentIdleIndex && idleVariationsCount > 1);

            currentIdleIndex = newIdleIndex;
            animator.SetInteger(idleIndexHash, currentIdleIndex);

            ScheduleNextIdleChange();
        }
    }

    private void ScheduleNextIdleChange()
    {
        nextIdleChangeTime = Time.time + Random.Range(minIdleChangeTime, maxIdleChangeTime);
    }

    #region Attack Animations

    /// <summary>
    /// Ejecuta animación de ataque melee con VFX opcional.
    /// </summary>
    // PlayMeleeAttack recibe el índice además del VFX
    public void PlayMeleeAttack(int attackIndex = 0, AttackVFXConfig vfxConfig = null)
    {
        if (animator == null || isDead) return;

        Debug.Log($"[AnimController] PlayMeleeAttack — index:{attackIndex} animator:{animator.name} isDead:{isDead}");
        animator.SetInteger(attackIndexHash, attackIndex);
        animator.SetTrigger(meleeAttackHash);

        if (vfxConfig != null && vfxController != null)
            vfxController.SpawnVFXDelayed(vfxConfig, vfxConfig.vfxSpawnTime);
    }

    /// <summary>
    /// Ejecuta animación de ataque ranged con VFX de disparo y/o casquillo.
    /// Ambos configs son opcionales; el timing viene embebido en cada AttackVFXConfig.
    /// </summary>
    public void PlayRangedAttack(AttackVFXConfig fireVFX = null, AttackVFXConfig shellVFX = null)
    {
        if (animator == null || isDead) return;

        animator.SetTrigger(rangedAttackHash);
        isAttacking = true;

        if (vfxController != null)
        {
            if (fireVFX != null)
                vfxController.SpawnVFXDelayed(fireVFX, fireVFX.vfxSpawnTime);

            if (shellVFX != null)
                vfxController.SpawnVFXDelayed(shellVFX, shellVFX.vfxSpawnTime);
        }

        Invoke(nameof(ResetAttackFlag), 0.1f);
    }

    /// <summary>
    /// Ejecuta animación de recarga.
    /// </summary>
    public void PlayReloadAnimation()
    {
        if (animator == null || isDead) return;

        animator.SetTrigger(reloadHash);
        isReloading = true;

        // Resetear el flag de recarga después de un breve delay
        Invoke(nameof(ResetReloadFlag), 0.1f);
    }

    private void ResetAttackFlag()
    {
        isAttacking = false;
    }

    private void ResetReloadFlag()
    {
        isReloading = false;
    }

    #endregion

    #region Hit & Death

    /// <summary>
    /// Ejecuta animación de recibir daño.
    /// </summary>
    public void PlayHitReaction()
    {
        if (animator == null || isDead) return;
        animator.SetTrigger(hitHash);
    }

    /// <summary>
    /// Ejecuta animación de muerte y detiene propulsores.
    /// </summary>
    public void PlayDeath()
    {
        if (animator == null || isDead) return;

        isDead = true;
        animator.SetTrigger(deathHash);

        if (vfxController != null)
            vfxController.ForceStopThrusters();
    }

    #endregion

    #region Utility

    /// <summary>
    /// Fuerza un idle específico (útil para debugging).
    /// </summary>
    public void SetIdleVariation(int index)
    {
        if (index < 0 || index >= idleVariationsCount || isDead) return;

        currentIdleIndex = index;
        animator.SetInteger(idleIndexHash, currentIdleIndex);
    }

    public int GetCurrentIdleIndex() => currentIdleIndex;
    public bool IsDead => isDead;
    public bool IsReloading => isReloading;

    #endregion

    private void OnValidate()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (agent == null)
            agent = GetComponent<UnityEngine.AI.NavMeshAgent>();

        if (vfxController == null)
            vfxController = GetComponent<EnemyVFXController>();
    }
}