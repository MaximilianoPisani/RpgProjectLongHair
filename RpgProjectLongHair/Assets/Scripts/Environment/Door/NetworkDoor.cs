using Fusion;
using UnityEngine;

public class NetworkDoor : NetworkBehaviour
{
    [Header("Movimiento")]
    [SerializeField] private float openHeight = 4f;
    [SerializeField] private float duration = 1.5f;

    [Header("Sonido (opcional)")]
    [SerializeField] private AudioClip openSFX;

    [Networked] private NetworkBool IsOpen { get; set; }

    private ChangeDetector _changeDetector;
    private Vector3 _closedPosition;
    private bool _isAnimating = false;

    public override void Spawned()
    {
        _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);
        _closedPosition = transform.position;
    }

    // Llamado desde DoorOpenListener via DamageableObject.OnActivated
    public void Open()
    {
        if (_isAnimating || IsOpen) return;

        if (Object.HasStateAuthority)
            IsOpen = true;

        StartCoroutine(OpenCoroutine());
    }

    public override void Render()
    {
        foreach (var change in _changeDetector.DetectChanges(this))
        {
            if (change == nameof(IsOpen) && IsOpen && !_isAnimating)
                StartCoroutine(OpenCoroutine());
        }
    }

    private System.Collections.IEnumerator OpenCoroutine()
    {
        _isAnimating = true;
        Vector3 startPos = transform.position;
        Vector3 targetPos = _closedPosition + Vector3.up * openHeight;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            transform.position = Vector3.Lerp(startPos, targetPos, t);
            yield return null;
        }

        transform.position = targetPos;
        _isAnimating = false;

        if (openSFX != null)
            AudioSource.PlayClipAtPoint(openSFX, transform.position);
    }
}