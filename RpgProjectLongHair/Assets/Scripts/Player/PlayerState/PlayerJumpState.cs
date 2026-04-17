public class PlayerJumpState : IPlayerState
{
    private PlayerStateMachine _sm;
    private float _airTime;
    private const float MinAirTime = 0.2f;

    public PlayerJumpState(PlayerStateMachine sm) => _sm = sm;

    public void Enter()
    {
        _airTime = 0f;
        _sm.IsJumping = true;

        if (_sm.Player.IsPhysicallyGroundedPublic())
            _sm.Player.Jump();

        _sm.GetComponent<PlayerNetworkSync>()?.TriggerJump();

        if (_sm.Animator != null)
        {
            _sm.Animator.SetBool("isJumping", true);
            _sm.Animator.SetBool("isFalling", false);
        }
    }

    public void Exit()
    {
        if (_sm.Animator != null)
            _sm.Animator.SetBool("isJumping", false);
    }

    public void Tick(NetworkInputData input)
    {
        _airTime += _sm.Runner.DeltaTime;
        if (_airTime < MinAirTime) return;

        if (PlayerFallState.ShouldFall(_sm))
        {
            _sm.ChangeState(new PlayerFallState(_sm));
            return;
        }

        if (_sm.Player.IsPhysicallyGroundedPublic())
        {
            _sm.ChangeState(new PlayerLandState(_sm));
        }
    }
}