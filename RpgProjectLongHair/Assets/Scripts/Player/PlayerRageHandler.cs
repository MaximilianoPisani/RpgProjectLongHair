using Fusion;
using UnityEngine;
using System;

[RequireComponent(typeof(NetworkObject))]
public class PlayerRageHandler : NetworkBehaviour
{
    [SerializeField] private RageData _rageData;
    public RageData RageData => _rageData; 

    [Networked, HideInInspector] public float CurrentCharge { get; private set; }
    [Networked, HideInInspector] public bool IsRageActive { get; private set; }
    [Networked, HideInInspector] public float RageTimeLeft { get; private set; }

    public static event Action<float, float> OnChargeChanged;
    public static event Action OnRageActivated;
    public static event Action OnRageDeactivated;
    private static event Action<PlayerRef, int> OnDamageDealt;

    private ChangeDetector _changeDetector;

    public static void NotifyDamageDealt(PlayerRef attacker, int damage)
    {
        OnDamageDealt?.Invoke(attacker, damage);
    }

    public override void Spawned()
    {
        if (Object.HasStateAuthority)
        {
            CurrentCharge = 0f;
            IsRageActive = false;
            RageTimeLeft = 0f;
        }

        _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);

        OnDamageDealt -= HandleDamageDealt;
        OnDamageDealt += HandleDamageDealt;
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        OnDamageDealt -= HandleDamageDealt;
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority) return;

        if (IsRageActive)
        {
            RageTimeLeft -= Runner.DeltaTime;
            if (RageTimeLeft <= 0f)
                DeactivateRage();
        }
    }

    public override void Render()
    {
        if (!Object.HasInputAuthority) return;

        foreach (var change in _changeDetector.DetectChanges(this))
        {
            switch (change)
            {
                case nameof(IsRageActive) when IsRageActive:
                    OnRageActivated?.Invoke();
                    break;
                case nameof(IsRageActive) when !IsRageActive:
                    OnRageDeactivated?.Invoke();
                    break;
            }
        }
    }

    private void Update()
    {
        if (Object == null || !Object.HasInputAuthority) return;

        if (Input.GetKeyDown(_rageData.activationKey) && IsChargeFull() && !IsRageActive)
            RPC_RequestActivateRage();
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority, Channel = RpcChannel.Reliable)]
    private void RPC_RequestActivateRage()
    {
        if (!IsChargeFull() || IsRageActive) return;
        ActivateRage();
    }

    private void HandleDamageDealt(PlayerRef attacker, int damage)
    {
        if (!Object.HasStateAuthority)
            return;

        if (Object.InputAuthority != attacker)
            return;

        if (IsRageActive)
            return;

        float amount = _rageData.chargePerHit;

        if (!_rageData.useHitsInsteadOfDamage)
        {
            amount = damage * _rageData.chargePerDamage;
        }
        else
        {
            amount += damage * _rageData.extraChargeFromDamage;
        }

        if (Runner.TryGetPlayerObject(attacker, out NetworkObject playerObj))
        {
            var combat = playerObj.GetComponent<PlayerCombat>();

            if (combat != null)
            {
                WeaponCategory weapon = combat.CurrentWeapon;

                bool isAutomaticWeapon =
                    weapon == WeaponCategory.Gatling;

                if (isAutomaticWeapon)
                {
                    amount *= _rageData.automaticWeaponChargeMultiplier;
                }
            }
        }

        AddCharge(amount);
    }

    private void AddCharge(float amount)
    {
        float previous = CurrentCharge;

        CurrentCharge = Mathf.Min(
            CurrentCharge + amount,
            _rageData.maxCharge
        );

        if (!Mathf.Approximately(previous, CurrentCharge))
            OnChargeChanged?.Invoke(CurrentCharge, _rageData.maxCharge);
    }

    private void ActivateRage()
    {
        IsRageActive = true;
        RageTimeLeft = _rageData.activeDuration;
        CurrentCharge = _rageData.maxCharge;

        OnRageActivated?.Invoke();
        Debug.Log("[Rage] Activada");
    }

    private void DeactivateRage()
    {
        IsRageActive = false;
        RageTimeLeft = 0f;
        CurrentCharge = 0f;

        OnRageDeactivated?.Invoke();
        OnChargeChanged?.Invoke(CurrentCharge, _rageData.maxCharge);
        Debug.Log("[Rage] Desactivada");
    }

    public bool IsChargeFull() => CurrentCharge >= _rageData.maxCharge;
    public float GetDamageMultiplier() => IsRageActive ? _rageData.damageMultiplier : 1f;

    public float GetNormalizedBar()
    {
        if (IsRageActive) return RageTimeLeft / _rageData.activeDuration;
        return CurrentCharge / _rageData.maxCharge;
    }
}