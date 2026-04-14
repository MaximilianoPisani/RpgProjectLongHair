using System.Collections;
using Fusion;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
public class PlayerHealth : NetworkBehaviour
{
    [Header("Health")]
    [SerializeField] private int _maxHealth = 100;
    [Networked] private int CurrentHealth { get; set; }

    [Header("Flash Effect")]
    [SerializeField] private Color _flashColor = Color.red;
    [SerializeField] private int _flashCount = 3;
    [SerializeField] private float _flashDuration = 0.1f;

    private SkinnedMeshRenderer[] _skinnedRenderers;
    private Color[] _originalColors;
    private Coroutine _flashCoroutine;

    public bool IsDead => CurrentHealth <= 0;

    public override void Spawned()
    {
        if (HasStateAuthority)
            CurrentHealth = _maxHealth;

        _skinnedRenderers = GetComponentsInChildren<SkinnedMeshRenderer>();
        _originalColors = new Color[_skinnedRenderers.Length];

        for (int i = 0; i < _skinnedRenderers.Length; i++)
            _originalColors[i] = _skinnedRenderers[i].material.color;

        Debug.Log($"[PlayerHealth] Encontró {_skinnedRenderers.Length} SkinnedMeshRenderers");
    }

    public void TakeDamage(int damage, Vector3 hitPoint)
    {
        if (!HasStateAuthority) return;
        if (damage <= 0) return;
        if (IsDead) return;

        CurrentHealth -= damage;
        Debug.Log($"[Player] {CurrentHealth}/{_maxHealth} HP");

        if (!IsDead)
            RPC_Flash();
        else
            OnDeath();
    }

    private void OnDeath()
    {
        Debug.Log("[Player] Dead");
        Debug.Log("[PlayerHealth] OnDeath llamado");
        // Avisar al sistema de misiones que el player murió
        var questController = GetComponent<QuestController>();
        if (questController != null && questController.CurrentQuest != null)
            questController.FailureQuest();

        var sm = GetComponent<PlayerStateMachine>();
        if (sm != null)
            sm.ChangeState(new PlayerDeadState(sm));
        RPC_OnDeath();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_OnDeath()
    {
        GetComponent<PlayerNetworkSync>()?.TriggerDie();
    }

    public void ResetHealth()
    {
        if (!HasStateAuthority) return;
        CurrentHealth = _maxHealth;
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_Flash()
    {
        if (_skinnedRenderers == null || _skinnedRenderers.Length == 0) return;

        if (_flashCoroutine != null)
            StopCoroutine(_flashCoroutine);
        _flashCoroutine = StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        for (int i = 0; i < _flashCount; i++)
        {
            foreach (var r in _skinnedRenderers)
                r.material.color = _flashColor;

            yield return new WaitForSeconds(_flashDuration);

            for (int j = 0; j < _skinnedRenderers.Length; j++)
                _skinnedRenderers[j].material.color = _originalColors[j];

            yield return new WaitForSeconds(_flashDuration);
        }
    }
}