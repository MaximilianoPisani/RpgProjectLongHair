using UnityEngine;

public class PlayerAnimationEvents : MonoBehaviour
{
    private PlayerStateMachine _sm;

    private void Awake()
    {
        _sm = GetComponent<PlayerStateMachine>();
    }
    public void PlayAttackSound(string soundName)
    {
        // Reproducir sonido
    }

    public void SpawnHitVFX()
    {
        // Efectos visuales
    }
}