using Fusion;
using UnityEngine;

public class DamageableChainAnchor : NetworkBehaviour
{
    [Header("Vida del ancla")]
    [SerializeField] private float maxHealth = 100f;

    [Header("Referencia a la cadena")]
    [SerializeField] private ChainPhysicsSetup chainSetup;
    [SerializeField] private int breakAtLinkIndex = 2;
    [SerializeField] private Vector3 breakImpulse = new Vector3(0f, -10f, 0f);

    [Header("Feedback")]
    [SerializeField] private GameObject breakVFX;
    [SerializeField] private AudioClip breakSFX;

    [Networked] private float CurrentHealth { get; set; }
    [Networked] private NetworkBool IsBroken { get; set; }

    // Flag local — lo lee el FixedUpdate nativo de Unity
    private bool _pendingBreak = false;
    private bool _breakExecuted = false;

    public override void Spawned()
    {
        if (Object.HasStateAuthority)
        {
            CurrentHealth = maxHealth;
            IsBroken = false;
        }
    }

    // Fusion detecta el cambio y levanta el flag
    public override void FixedUpdateNetwork()
    {
        if (IsBroken && !_breakExecuted)
            _pendingBreak = true;
    }

    // Unity procesa física DESPUÉS de Fusion — aquí sí funcionan los Rigidbodies
    private void FixedUpdate()
    {
        if (_pendingBreak && !_breakExecuted)
        {
            _breakExecuted = true;
            _pendingBreak = false;
            chainSetup?.BreakAtIndex(breakAtLinkIndex, breakImpulse);
            PlayBreakFeedback();

            chainSetup?.DestroyAll(10f);
        }
    }

    public void ApplyDamageServer(float amount, PlayerRef attacker)
    {
        if (!Object.HasStateAuthority || IsBroken) return;

        CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);

        if (CurrentHealth <= 0f)
            IsBroken = true;
    }

    private void PlayBreakFeedback()
    {
        if (breakVFX != null)
            Instantiate(breakVFX, transform.position, Quaternion.identity);

        if (breakSFX != null)
            AudioSource.PlayClipAtPoint(breakSFX, transform.position);
    }

    public float HealthPercent => CurrentHealth / maxHealth;

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(transform.position, Vector3.one * 0.15f);
    }
}