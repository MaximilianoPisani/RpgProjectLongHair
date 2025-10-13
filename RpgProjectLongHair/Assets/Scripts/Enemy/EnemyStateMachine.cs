using System.Collections.Generic;
using UnityEngine;

public class EnemyStateMachine
{
    private IEnemyState _currentState;
    public IEnemyState CurrentState => _currentState;

    public void ChangeState(IEnemyState newState)
    {
        _currentState?.ExitState();
        _currentState = newState;
        _currentState?.EnterState();
    }

    public void Update()
    {
        _currentState?.UpdateState();
    }
}