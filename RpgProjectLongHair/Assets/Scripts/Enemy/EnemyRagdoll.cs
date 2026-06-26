using UnityEngine;

public class EnemyRagdoll : MonoBehaviour
{
    [Header("Ragdoll Settings")]
    [SerializeField] private float ragdollDuration = 8f;
    [SerializeField] private float deathForceMultiplier = 5f;

    [Header("Hit Reaction")]
    [SerializeField] private float hitForceMultiplier = 40f;
    [SerializeField] private float hitUpwardForce = 12f;
    [SerializeField] private float spreadForce = 8f;
    [SerializeField] private float spreadRadius = 0.5f;

    [Header("References")]
    [SerializeField] private Animator animator;
    [Tooltip("Collider principal que recibe daño — NO se desactiva hasta la muerte")]
    [SerializeField] private Collider mainCollider;

    private Rigidbody[] ragdollRigidbodies;
    private Collider[] ragdollColliders;
    private bool isRagdollActive = false;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (mainCollider == null)
            mainCollider = GetComponent<Collider>();

        ragdollRigidbodies = GetComponentsInChildren<Rigidbody>();

        var allColliders = GetComponentsInChildren<Collider>();
        var ragdollList = new System.Collections.Generic.List<Collider>();
        foreach (var col in allColliders)
        {
            if (col != mainCollider)
                ragdollList.Add(col);
        }
        ragdollColliders = ragdollList.ToArray();

        Debug.Log($"[Ragdoll] Setup: {ragdollRigidbodies.Length} rigidbodies, " +
                  $"{ragdollColliders.Length} ragdoll colliders (excluded main)");

        DeactivateRagdoll();
    }

    public void ActivateRagdoll(Vector3 deathForce = default)
    {
        if (isRagdollActive) return;

        // 1. Desactivar Animator (con culling mode para evitar invisibilidad)
        if (animator != null)
        {
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.enabled = false;
        }

        // 2. Evitar culling en SkinnedMeshRenderers
        foreach (var skinnedMesh in GetComponentsInChildren<SkinnedMeshRenderer>())
            skinnedMesh.updateWhenOffscreen = true;

        // 3. Desactivar collider principal
        if (mainCollider != null)
            mainCollider.enabled = false;

        // 4. Activar Rigidbodies y poner en layer Damageable
        foreach (Rigidbody rb in ragdollRigidbodies)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.gameObject.layer = LayerMask.NameToLayer("Damageable");

            if (deathForce != Vector3.zero)
                rb.AddForce(deathForce * deathForceMultiplier, ForceMode.Impulse);
        }

        // 5. Activar colliders del ragdoll
        foreach (Collider col in ragdollColliders)
            col.enabled = true;

        isRagdollActive = true;
        Debug.Log($"[Ragdoll] Activated on {gameObject.name}");
    }

    private void DeactivateRagdoll()
    {
        foreach (Rigidbody rb in ragdollRigidbodies)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        foreach (Collider col in ragdollColliders)
            col.enabled = false;

        if (mainCollider != null)
            mainCollider.enabled = true;

        isRagdollActive = false;
    }

    public void ApplyHitForce(Vector3 hitPoint, Vector3 hitDirection, float forceScale = 1f)
    {
        if (!isRagdollActive) return;

        Rigidbody closestRb = GetClosestRigidbody(hitPoint);
        if (closestRb == null) return;

        // Fuerza principal en el hueso más cercano
        Vector3 mainForce = (hitDirection.normalized * hitForceMultiplier
                          + Vector3.up * hitUpwardForce) * forceScale;
        closestRb.AddForce(mainForce, ForceMode.Impulse);

        // Spread solo a huesos cercanos al punto de impacto
        foreach (Rigidbody rb in ragdollRigidbodies)
        {
            if (rb == closestRb) continue;

            float distanceToHit = Vector3.Distance(rb.position, hitPoint);
            if (distanceToHit > spreadRadius) continue;

            // Solo fuerza hacia arriba para evitar deslizamiento
            Vector3 secondaryForce = Vector3.up * (hitUpwardForce * 0.5f) * forceScale;
            rb.AddForce(secondaryForce, ForceMode.Impulse);
        }

        Debug.Log($"[Ragdoll] Hit! Main bone: {closestRb.name}, Force: {mainForce}");
    }

    private Rigidbody GetClosestRigidbody(Vector3 point)
    {
        Rigidbody closest = null;
        float closestDistance = float.MaxValue;

        foreach (Rigidbody rb in ragdollRigidbodies)
        {
            float distance = Vector3.Distance(rb.position, point);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closest = rb;
            }
        }

        return closest;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, spreadRadius);
    }

    public float RagdollDuration => ragdollDuration;
    public bool IsActive => isRagdollActive;
}