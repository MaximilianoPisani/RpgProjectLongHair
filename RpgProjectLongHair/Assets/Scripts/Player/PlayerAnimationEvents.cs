using UnityEngine;

public class PlayerAnimationEvents : MonoBehaviour
{
    private PlayerStateMachine _sm;

    private void Awake()
    {
        _sm = GetComponent<PlayerStateMachine>();
    }

    // ==================== Melee ====================
    // Evento principal - momento de impacto
    public void OnHitFrame()
    {
        if (_sm.CurrentState is IAnimationEventReceiver receiver)
        {
            receiver.OnHitFrame();
        }
    }

    public void OpenComboWindow()
    {
        if (_sm.CurrentState is IAnimationEventReceiver receiver)
        {
            receiver.OpenComboWindow();
        }
    }

    public void CloseComboWindow()
    {
        if (_sm.CurrentState is IAnimationEventReceiver receiver)
        {
            receiver.CloseComboWindow();
        }
    }

    public void EndAttack()
    {
        if (_sm.CurrentState is IAnimationEventReceiver receiver)
        {
            receiver.EndAttack();
        }
    }
    // ==================== Ranged ====================

    // ========== EVENTOS DE RANGED ==========

    public void OnShootFrame()
    {
        if (_sm.CurrentState is PlayerRangeState rangedState)
        {
            rangedState.OnShootFrame();
        }
    }

    public void OnShootAnimationEnd()
    {
        if (_sm.CurrentState is PlayerRangeState rangedState)
        {
            rangedState.OnShootAnimationEnd();
        }
    }

    public void OnReloadComplete()
    {
        if (_sm.CurrentState is PlayerRangeState rangedState)
        {
            rangedState.OnReloadComplete();
        }
    }

    // ==================== Extra ====================
    public void PlayAttackSound(string soundName)
    {
        // Reproducir sonido
    }

    public void SpawnHitVFX()
    {
        // Efectos visuales
    }
}