using UnityEngine;

public class StoneSound : MonoBehaviour
{
    private DamageableObject _damageable;

    private void Awake()
    {
        _damageable = GetComponent<DamageableObject>();
        if (_damageable != null)
            _damageable.OnActivated.AddListener(OnStoneDestroyed);
    }

    private void OnStoneDestroyed()
    {
        // Solo el jugador local escucha (o todos, como prefieras)
        AudioManager.Instance.PlayStone();
    }

    private void OnDestroy()
    {
        if (_damageable != null)
            _damageable.OnActivated.RemoveListener(OnStoneDestroyed);
    }
}
