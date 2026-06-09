using UnityEngine;
using Fusion;
using System;
using System.Collections.Generic;

[RequireComponent(typeof(NetworkObject))]
public class EnemyHealth : NetworkBehaviour
{
    [Header("Life")]
    [SerializeField] private int _maxHealth = 100;

    [Networked, HideInInspector] public int currentHealth { get; set; }

    private PlayerRef _lastAttacker;

    public int MaxHealth => _maxHealth;
    public bool IsDead => currentHealth <= 0;

    [Header("Feedback")]
    [SerializeField] private Renderer _meshRenderer;
    [SerializeField] private Color _flashColor = Color.red;
    [SerializeField] private float _flashDuration = 0.1f;
    [SerializeField] private EnemyVFXController _vfxController;

    [Header("Enemy Controller")]
    [SerializeField] private EnemyBaseController enemyController;

    [Header("Reward")]
    [SerializeField] private ExpConfigSO _expConfig;

    public event Action<int, int> OnHealthChanged;
    public event Action OnDeath;

    private Color _originalColor;
    private Coroutine _flashCoroutine;

    private readonly HashSet<PlayerRef> _participants = new();

    private ChangeDetector _changeDetector;

    [SerializeField] private bool _isQuestEnemy = false;

    public override void Spawned()
    {
        if (Object.HasStateAuthority)
        {
            currentHealth = _maxHealth;
            _participants.Clear();
        }

        if (enemyController == null)
        {
            enemyController = GetComponent<EnemyMeleeController>();

            if (enemyController == null)
                enemyController = GetComponent<EnemyRangedController>();

            if (enemyController == null)
                enemyController = GetComponent<EnemyKamikazeController>();
        }


        _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);

        if (_meshRenderer != null)
            _originalColor = _meshRenderer.material.color;

        OnHealthChanged?.Invoke(currentHealth, _maxHealth);
    }

    public override void Render()
    {
        foreach (var change in _changeDetector.DetectChanges(this))
        {
            if (change == nameof(currentHealth))
            {
                OnHealthChanged?.Invoke(currentHealth, _maxHealth);

                if (currentHealth <= 0)
                    OnDeath?.Invoke();
            }
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority, Channel = RpcChannel.Reliable)]
    public void RPC_ApplyDamage(int damage, RpcInfo info = default)
    {
        TakeDamageServer(damage, info.Source);
    }

    public void ApplyDamageServer(int damage, PlayerRef attacker)
    {
        TakeDamageServer(damage, attacker);
    }

    private void TakeDamageServer(int damage, PlayerRef attacker)
    {
        Debug.Log($"[EnemyHealth] Damage {damage} from {attacker}, authority={Object.HasStateAuthority}");

        if (!Object.IsValid) return; //  objeto ya despawneado

        if (!Object.HasStateAuthority) return;

        if (damage <= 0) return;

        if (currentHealth <= 0) return;

        if (attacker != PlayerRef.None)
        {
            _participants.Add(attacker);
            _lastAttacker = attacker;
        }

        currentHealth = Mathf.Max(0, currentHealth - damage);

        if (Runner.TryGetPlayerObject(attacker, out NetworkObject playerObj))
        {
            Vector3 playerPos = playerObj.transform.position;
            Vector3 enemyPos = transform.position;

            // Dirección desde el PLAYER hacia el ENEMIGO (hacia afuera)
            Vector3 hitNormal = (enemyPos - playerPos).normalized;

            // No necesitamos modificar la posición aquí, 
            // el VFXController ahora usa transform.position del enemigo
            RPC_SpawnHitVFX(transform.position, hitNormal);
        }

        if (enemyController is EnemyMeleeController meleeController)
        {
            meleeController.TriggerHitAnimation();
        }
        else if (enemyController is EnemyRangedController rangedController)
        {
            rangedController.TriggerHitAnimation();
        }
        else if (enemyController is EnemyKamikazeController) 
        {
            var networkSync = GetComponent<EnemyNetworkSync>();
            networkSync?.TriggerHit(); 
        }

        RPC_Flash();

        if(currentHealth <= 0)
{
            GiveKillExp();

            if (enemyController is EnemyKamikazeController kamikazeController)
            {
                kamikazeController.ChangeState(new EnemyKamikazeExplodeState(kamikazeController));
                return;
            }

            //Delegar al death state — él hace el despawn retrasado
            enemyController?.ChangeState(new EnemyDeathState(enemyController));
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_Flash()
    {
        if (_meshRenderer == null) return;

        if (_flashCoroutine != null)
            StopCoroutine(_flashCoroutine);

        _flashCoroutine = StartCoroutine(FlashRoutine());
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_SpawnHitVFX(Vector3 pos, Vector3 normal)
    {
        if (_vfxController != null)
        {
            _vfxController.SpawnHitVFX(pos, normal);
        }
    }

    private System.Collections.IEnumerator FlashRoutine()
    {
        _meshRenderer.material.color = _flashColor;
        yield return new WaitForSeconds(_flashDuration);
        _meshRenderer.material.color = _originalColor;
    }

    public static bool TryApplyFromHitbox(Hitbox hb, int damage, PlayerRef attacker)
    {
        if (hb == null || hb.Root == null) return false;

        var health = hb.Root.GetComponentInChildren<EnemyHealth>();
        if (health == null) return false;
        if (!health.Object || !health.Object.HasStateAuthority) return false;

        health.ApplyDamageServer(damage, attacker);
        return true;
    }
    private void GiveKillExp()
    {
        if (_lastAttacker == PlayerRef.None) return;

        if (!Runner.TryGetPlayerObject(_lastAttacker, out NetworkObject playerObj)) return;

        var playerExp = playerObj.GetComponent<PlayerExp>();
        if (playerExp == null) return;

        int exp = _expConfig.GetExp(ExpEvent.Kill);

        playerExp.AddExperience(exp);

        if (_isQuestEnemy) {
            // FIX: Enviar el kill directamente al QuestController owner para filtrado de party
            var missionOwner = QuestController.GetMissionOwner();
            if (missionOwner != null)
                missionOwner.ReportKill(_lastAttacker);
            else
                TrackEvents.OnTrackEvent?.Invoke(QuestIds.KILL_MISSION_ENEMY, 1);
        }
        else
        {
            TrackEvents.OnTrackEvent?.Invoke(QuestIds.KILL_ENEMY, 1);
        }
    }

}