using Fusion;
using UnityEngine;

public class PlayerStateMachine : NetworkBehaviour
{
    private IPlayerState _currentState;
    public IPlayerState CurrentState => _currentState;

    [Header("References")]
    public Animator Animator;
    public PlayerCombat Combat;
    public PlayerHealth Health;
    public Player Player;

    [Header("Config")]
    public float moveSpeed = 5f;
    public bool IsBusy =>_currentState is PlayerMeleeState || _currentState is PlayerRangeState;
    [Networked] public TickTimer AttackCooldown { get; set; }
    [Networked] public int NetworkedComboIndex { get; set; }

    public bool IsJumping { get; set; } = false;
    public Vector3 LastShootDirection { get; set; } = Vector3.forward;
    public bool IsInputLocked => _currentState is PlayerRangeState rangeState && rangeState.IsLockingInput;

    public override void Spawned()
    {
        Player = GetComponent<Player>();
        IsJumping = false;
        ChangeState(new PlayerIdleState(this));
    }

    public override void FixedUpdateNetwork()
    {
        if (Player.IsGrounded())
            IsJumping = false;

        if (_currentState is PlayerDeadState)
        {
            _currentState.Tick(default);
            return;
        }

        if (!GetInput(out NetworkInputData input)) return;
        LastShootDirection = input.shootDirection;

        if (_currentState is PlayerIdleState || _currentState is PlayerMoveState)
        {
            if (PlayerFallState.ShouldFall(this))
            {
                ChangeState(new PlayerFallState(this));
                return;
            }
        }

        if (input.jump && !IsJumping && Player.IsGrounded() && !IsInputLocked)
        {
            IsJumping = true;
            ChangeState(new PlayerJumpState(this));
            return;
        }

        _currentState?.Tick(input);
    }

    public void ChangeState(IPlayerState newState)
    {
        _currentState?.Exit();
        _currentState = newState;
        _currentState.Enter();
    }

    public void UpdateState(NetworkInputData input)
    {
        _currentState?.Tick(input);
    }

    public Vector3 GetSpawnPosition()
    {
        var checkpoint = GetComponent<PlayerCheckpoint>();
        return checkpoint != null ? checkpoint.LastCheckpoint : Vector3.zero;
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority, Channel = RpcChannel.Reliable)]
    public void RPC_RequestMeleeDamage(Vector3 origin, Vector3 forward, int damage)
    {
        if (Combat == null || Combat.meleeData == null) return;

        Vector3 attackOrigin = Combat.meleeOrigin != null
            ? Combat.meleeOrigin.position
            : origin + forward * 0.5f + Vector3.up;

        Collider[] hits = Physics.OverlapSphere(
            attackOrigin,
            Combat.meleeData.HitRadius,
            Combat.enemyLayer
        );

        foreach (var hit in hits)
        {
            var enemyHealth = hit.GetComponentInParent<EnemyHealth>();
            if (enemyHealth != null && enemyHealth.Object.HasStateAuthority)
            {
                enemyHealth.ApplyDamageServer(
                    damage,
                    Object.InputAuthority
                );
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (Combat == null || Combat.meleeData == null) return;

        Vector3 direction = Vector3.right;
        Vector3 origin = Combat.meleeOrigin != null
            ? Combat.meleeOrigin.position
            : transform.position + direction * 0.5f + Vector3.up;

        float radius = Combat.meleeData.HitRadius;

        if (_currentState is PlayerMeleeState)
            Gizmos.color = Color.red;
        else
            Gizmos.color = new Color(1, 0, 0, 0.2f);

        Gizmos.DrawWireSphere(origin, radius);

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(origin, origin + direction * 1.5f);
    }
}