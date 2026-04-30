using System.Collections;
using Fusion;
using UnityEngine;

[RequireComponent(typeof(PlayerHealth))]
public class PlayerHealthRegeneration : NetworkBehaviour
{
    [Header("Regeneración")]
    [SerializeField] private bool _regenerationEnabled = true;
    [SerializeField] private int _healthPerTick = 5;
    [SerializeField] private float _tickInterval = 2f;
    [SerializeField] private float _delayAfterDamage = 3f;

    [Header("Límites")]
    [SerializeField] private bool _onlyWhenNotFull = true;
    [SerializeField] private bool _stopWhenDead = true;

    private PlayerHealth _playerHealth;
    private float _lastDamageTime;
    private Coroutine _regenCoroutine;

    public override void Spawned()
    {
        _playerHealth = GetComponent<PlayerHealth>();

        if (HasStateAuthority && _regenerationEnabled)
        {
            StartRegeneration();
        }
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        StopRegeneration();
    }

    private void StartRegeneration()
    {
        if (_regenCoroutine == null)
        {
            _regenCoroutine = StartCoroutine(RegenerationRoutine());
        }
    }

    private void StopRegeneration()
    {
        if (_regenCoroutine != null)
        {
            StopCoroutine(_regenCoroutine);
            _regenCoroutine = null;
        }
    }

    private IEnumerator RegenerationRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(_tickInterval);

            // Solo regenerar si tenemos autoridad
            if (!HasStateAuthority)
                continue;

            // No regenerar si está muerto
            if (_stopWhenDead && _playerHealth.IsDead)
                continue;

            // Esperar después de recibir daño
            if (Time.time - _lastDamageTime < _delayAfterDamage)
                continue;

            // No regenerar si ya está a vida máxima
            if (_onlyWhenNotFull && _playerHealth.CurrentHealth >= _playerHealth.MaxHealth)
                continue;

            // Aplicar regeneración
            ApplyRegeneration();
        }
    }

    private void ApplyRegeneration()
    {
        _playerHealth.Heal(_healthPerTick);
    }

    /// <summary>
    /// Llamar esto cuando el jugador reciba daño para resetear el delay
    /// </summary>
    public void OnDamageTaken()
    {
        _lastDamageTime = Time.time;
    }

    #region Public Methods

    /// <summary>
    /// Activar o desactivar la regeneración
    /// </summary>
    public void SetRegenerationEnabled(bool enabled)
    {
        _regenerationEnabled = enabled;

        if (enabled && HasStateAuthority)
            StartRegeneration();
        else
            StopRegeneration();
    }

    /// <summary>
    /// Cambiar la cantidad de salud regenerada por tick
    /// </summary>
    public void SetHealthPerTick(int amount)
    {
        _healthPerTick = Mathf.Max(0, amount);
    }

    /// <summary>
    /// Cambiar el intervalo entre ticks de regeneración
    /// </summary>
    public void SetTickInterval(float interval)
    {
        _tickInterval = Mathf.Max(0.1f, interval);
    }

    /// <summary>
    /// Cambiar el delay después de recibir daño
    /// </summary>
    public void SetDelayAfterDamage(float delay)
    {
        _delayAfterDamage = Mathf.Max(0f, delay);
    }

    #endregion
}