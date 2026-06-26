using UnityEngine;

public class GateSound : MonoBehaviour
{
    private DamageableObject _damageable;

    private void Awake()
    {
        _damageable = GetComponent<DamageableObject>();
        if (_damageable != null)
            _damageable.OnActivated.AddListener(OnGateDestroyed);
    }

    private void OnGateDestroyed()
    {
        AudioManager.Instance.PlayGate();
    }

    private void OnDestroy()
    {
        if (_damageable != null)
            _damageable.OnActivated.RemoveListener(OnGateDestroyed);
    }
}
