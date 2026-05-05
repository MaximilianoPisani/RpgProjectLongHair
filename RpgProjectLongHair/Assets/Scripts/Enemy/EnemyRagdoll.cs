using UnityEngine;

public class EnemyRagdoll : MonoBehaviour
{
    [Header("Ragdoll Settings")]
    [SerializeField] private float ragdollDuration = 3f;
    [SerializeField] private float deathForceMultiplier = 5f;

    [Header("References")]
    [SerializeField] private Animator animator;
    [Tooltip("Collider principal que NO debe desactivarse (recibe daño)")]
    [SerializeField] private Collider mainCollider; // NUEVO

    private Rigidbody[] ragdollRigidbodies;
    private Collider[] ragdollColliders;
    private bool isRagdollActive = false;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        // Si no se asignó, buscar el collider en el root
        if (mainCollider == null)
            mainCollider = GetComponent<Collider>();

        // Guardar Rigidbodies
        ragdollRigidbodies = GetComponentsInChildren<Rigidbody>();

        // Guardar colliders EXCEPTO el principal
        var allColliders = GetComponentsInChildren<Collider>();
        var ragdollList = new System.Collections.Generic.List<Collider>();

        foreach (var col in allColliders)
        {
            // Excluir el collider principal
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

        Debug.Log($"[Ragdoll] Activating ragdoll on {gameObject.name}");

        // 1. Desactivar Animator
        if (animator != null)
            animator.enabled = false;

        // 2. Desactivar el collider principal (ya no necesita recibir daño)
        if (mainCollider != null)
            mainCollider.enabled = false;

        // 3. Activar Rigidbodies del ragdoll
        foreach (Rigidbody rb in ragdollRigidbodies)
        {
            rb.isKinematic = false;
            rb.useGravity = true;

            if (deathForce != Vector3.zero)
            {
                rb.AddForce(deathForce * deathForceMultiplier, ForceMode.Impulse);
            }
        }

        // 4. Activar colliders del ragdoll
        foreach (Collider col in ragdollColliders)
        {
            col.enabled = true;
        }

        isRagdollActive = true;
    }

    private void DeactivateRagdoll()
    {
        // Solo desactivar los colliders del ragdoll, NO el principal
        foreach (Rigidbody rb in ragdollRigidbodies)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        foreach (Collider col in ragdollColliders)
        {
            col.enabled = false;
        }

        // El mainCollider se mantiene activo para recibir daño
        if (mainCollider != null)
            mainCollider.enabled = true;

        isRagdollActive = false;
    }

    public float RagdollDuration => ragdollDuration;
}