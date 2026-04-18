using UnityEngine;

public class PlayerDeadState : IPlayerState
{
    private PlayerStateMachine _sm;
    private float _timer;

    public PlayerDeadState(PlayerStateMachine sm)
    {
        _sm = sm;
    }

    public void Enter()
    {
        if (!_sm.Object.HasStateAuthority) return;
        _timer = 0f;

        if (_sm.Object.HasInputAuthority)
            RunnerManager.SetInputBlocked(true);
    }

    public void Exit()
    {
        if (!_sm.Object.HasInputAuthority) return;
        RunnerManager.SetInputBlocked(false);
    }

    public void Tick(NetworkInputData input)
    {
        if (!_sm.Object.HasStateAuthority) return;
        _timer += _sm.Runner.DeltaTime;

        if (_timer > 2f)
        {
            var checkpoint = _sm.GetComponent<PlayerCheckpoint>();
            Vector3 spawnPos = checkpoint != null ? checkpoint.LastCheckpoint : _sm.transform.position;
            _sm.Player.TeleportTo(spawnPos);

            if (_sm.Health != null)
                _sm.Health.ResetHealth();

            _sm.ChangeState(new PlayerIdleState(_sm));
        }
    }
}
