using System.Collections;
using UnityEngine;
using Fusion;
using UnityEngine.InputSystem;

public class PlayerMeleeAttack : NetworkBehaviour
{
    [SerializeField] private MeleeAttackData _attackData;

    private float _nextAttackTime;

    public void OnAttack(InputAction.CallbackContext context)
    {
        if (!HasInputAuthority || !context.started) return;
        TryAttack();
    }

    private void TryAttack()
    {
        if (Time.time < _nextAttackTime) return;

        _nextAttackTime = Time.time + _attackData.Cooldown;


        Debug.Log($"[Player] Melee Attack - Range: {_attackData.AttackRange}, Damage: {_attackData.Damage}");


        StartCoroutine(ShowDebugAttackArea());
    }

    private IEnumerator ShowDebugAttackArea()
    {
        float duration = 0.2f;
        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            Debug.DrawRay(transform.position, transform.forward * _attackData.AttackRange, Color.red);
            yield return null;
        }
    }
}