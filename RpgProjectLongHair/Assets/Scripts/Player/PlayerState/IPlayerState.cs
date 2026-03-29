
public interface IPlayerState
{
    void Enter();
    void Exit();
    void Tick(NetworkInputData input);
}