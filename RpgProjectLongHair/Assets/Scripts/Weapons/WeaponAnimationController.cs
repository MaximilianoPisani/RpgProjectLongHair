using UnityEngine;

/// <summary>
/// Interfaz que expone las acciones de animación del arma.
/// Permite desacoplar completamente el state machine del componente concreto.
/// </summary>
public interface IWeaponAnimatable
{
    void PlayShoot();
    void PlayReload();
    void StopAll();
}

/// <summary>
/// Gestiona las animaciones de un arma ranged (Animator-based).
/// Coloca este componente en el GameObject del arma (hijo del jugador).
///
/// Parámetros esperados en el Animator del arma:
///   - Trigger  "Shoot"        disparo (single o ráfaga)
///   - Bool     "IsReloading"  true durante recarga
///   - Float    "Speed"        blend de movimiento (0..1), opcional
/// </summary>
[RequireComponent(typeof(Animator))]
public class WeaponAnimationController : MonoBehaviour, IWeaponAnimatable
{
    //  Hash de parámetros (evita string lookups en caliente) 
    private static readonly int HashShoot = Animator.StringToHash("Shoot");
    private static readonly int HashIsReloading = Animator.StringToHash("IsReloading");

    //Referencias
    private Animator _animator;

    //  Estado interno 
    private bool _isReloading;

    // Inspector 
    [Header("Debug")]
    [SerializeField] private bool _logStateChanges = false;

    // =========================================================================
    // Unity Lifecycle
    // =========================================================================

    private void Awake()
    {
        _animator = GetComponent<Animator>();
    }

    // =========================================================================
    // IWeaponAnimatable — API pública
    // =========================================================================

    /// <summary>
    /// Dispara el trigger de animación de disparo.
    /// Funciona tanto en SingleShot como en Automatic (se llama por ráfaga).
    /// </summary>
    public void PlayShoot()
    {
        if (_isReloading) return;           // no interrumpir recarga

        _animator.SetTrigger(HashShoot);
        Log("Shoot triggered");
    }

    /// <summary>
    /// Activa la animación de recarga.
    /// </summary>
    public void PlayReload()
    {
        if (_isReloading) return;

        _isReloading = true;
        _animator.ResetTrigger(HashShoot);  // cancelar posible trigger pendiente
        _animator.SetBool(HashIsReloading, true);
        Log("Reload started");
    }

    /// <summary>
    /// Detiene todas las animaciones activas y resetea el estado.
    /// Útil al salir del estado de combate.
    /// </summary>
    public void StopAll()
    {
        _isReloading = false;
        _animator.ResetTrigger(HashShoot);
        _animator.SetBool(HashIsReloading, false);
        Log("All animations stopped");
    }
    public void OnReloadAnimationComplete()
    {
        _isReloading = false;
        _animator.SetBool(HashIsReloading, false);
        Log("Reload animation complete (via AnimEvent)");
    }

    // =========================================================================
    // Helpers
    // =========================================================================

    private void Log(string message)
    {
        if (_logStateChanges)
            Debug.Log($"[WeaponAnim] {message}");
    }
}