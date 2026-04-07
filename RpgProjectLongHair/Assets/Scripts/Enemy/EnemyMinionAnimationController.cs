using UnityEngine;

/// <summary>
/// Controlador de animaciones para el enemigo Minion Mecano.
/// Maneja las transiciones de animación basadas en estados.
/// </summary>
public class EnemyMinionAnimationController : MonoBehaviour
{
    [Header("Animator")]
    [SerializeField] private Animator animator;

    [Header("Animation Parameters")]
    [Tooltip("Nombre del parámetro float para la velocidad")]
    [SerializeField] private string speedParameter = "Speed";

    [Tooltip("Nombre del trigger para ataque melee")]
    [SerializeField] private string meleeAttackTrigger = "MeleeAttack";

    [Tooltip("Nombre del trigger para recibir daño")]
    [SerializeField] private string hitTrigger = "Hit";

    [Tooltip("Nombre del parámetro int para idle variations")]
    [SerializeField] private string idleIndexParameter = "IdleIndex";

    [Header("Idle Settings")]
    [Tooltip("Cantidad de animaciones idle disponibles (0 = solo una idle)")]
    [SerializeField] private int idleVariationsCount = 2;

    [Tooltip("Tiempo mínimo antes de cambiar idle animation")]
    [SerializeField] private float minIdleChangeTime = 3f;

    [Tooltip("Tiempo máximo antes de cambiar idle animation")]
    [SerializeField] private float maxIdleChangeTime = 8f;

    [Header("References")]
    [SerializeField] private UnityEngine.AI.NavMeshAgent agent;

    // Control interno
    private float nextIdleChangeTime;
    private int currentIdleIndex = 0;
    private bool isAttacking = false;
    private float lastSpeed = 0f;

    // Hash de parámetros para optimización
    private int speedHash;
    private int meleeAttackHash;
    private int hitHash;
    private int idleIndexHash;

    private void Awake()
    {
        // Auto-encontrar componentes si no están asignados
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (agent == null)
            agent = GetComponent<UnityEngine.AI.NavMeshAgent>();

        // Precalcular hashes para mejor performance
        speedHash = Animator.StringToHash(speedParameter);
        meleeAttackHash = Animator.StringToHash(meleeAttackTrigger);
        hitHash = Animator.StringToHash(hitTrigger);
        idleIndexHash = Animator.StringToHash(idleIndexParameter);

        // Inicializar el tiempo para el primer cambio de idle
        ScheduleNextIdleChange();
    }

    private void Update()
    {
        if (animator == null) return;

        UpdateMovementAnimation();
        UpdateIdleVariation();
    }

    /// <summary>
    /// Actualiza la animación de movimiento basada en la velocidad del NavMeshAgent
    /// </summary>
    private void UpdateMovementAnimation()
    {
        if (agent == null || !agent.isOnNavMesh) return;

        // Obtener la velocidad actual del agente
        float currentSpeed = agent.velocity.magnitude;

        // Suavizar la transición de velocidad
        lastSpeed = Mathf.Lerp(lastSpeed, currentSpeed, Time.deltaTime * 10f);

        // Actualizar el parámetro de velocidad en el animator
        animator.SetFloat(speedHash, lastSpeed);
    }

    /// <summary>
    /// Cambia aleatoriamente entre las variaciones de idle
    /// </summary>
    private void UpdateIdleVariation()
    {
        // Solo cambiar idle si no estamos atacando y tenemos variaciones
        if (isAttacking || idleVariationsCount <= 1) return;

        // Verificar si estamos en idle (velocidad casi cero)
        if (lastSpeed < 0.1f && Time.time >= nextIdleChangeTime)
        {
            // Seleccionar una nueva idle diferente a la actual
            int newIdleIndex;
            do
            {
                newIdleIndex = Random.Range(0, idleVariationsCount);
            } while (newIdleIndex == currentIdleIndex && idleVariationsCount > 1);

            currentIdleIndex = newIdleIndex;
            animator.SetInteger(idleIndexHash, currentIdleIndex);

            ScheduleNextIdleChange();
        }
    }

    /// <summary>
    /// Programa el próximo cambio de idle animation
    /// </summary>
    private void ScheduleNextIdleChange()
    {
        nextIdleChangeTime = Time.time + Random.Range(minIdleChangeTime, maxIdleChangeTime);
    }

    /// <summary>
    /// Ejecuta la animación de ataque melee
    /// </summary>
    public void PlayMeleeAttack()
    {
        if (animator == null) return;

        animator.SetTrigger(meleeAttackHash);
        isAttacking = true;

        // Resetear el flag después de un frame
        Invoke(nameof(ResetAttackFlag), 0.1f);
    }

    /// <summary>
    /// Ejecuta la animación de recibir daño
    /// </summary>
    public void PlayHitReaction()
    {
        if (animator == null) return;

        animator.SetTrigger(hitHash);
    }

    /// <summary>
    /// Resetea el flag de ataque
    /// </summary>
    private void ResetAttackFlag()
    {
        isAttacking = false;
    }

    /// <summary>
    /// Fuerza un idle específico (útil para debugging)
    /// </summary>
    public void SetIdleVariation(int index)
    {
        if (index < 0 || index >= idleVariationsCount) return;

        currentIdleIndex = index;
        animator.SetInteger(idleIndexHash, currentIdleIndex);
    }

    /// <summary>
    /// Obtiene el índice actual de idle
    /// </summary>
    public int GetCurrentIdleIndex() => currentIdleIndex;

    private void OnValidate()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (agent == null)
            agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
    }
}