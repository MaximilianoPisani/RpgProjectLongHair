using Fusion;
using UnityEngine;

public interface IEnemyState
{
    void EnterState();
    void ExitState();
    void UpdateState();
}