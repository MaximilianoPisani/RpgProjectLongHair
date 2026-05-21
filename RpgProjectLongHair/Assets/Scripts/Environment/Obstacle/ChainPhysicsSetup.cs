using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Configura automáticamente los ConfigurableJoint en cada hueso de la cadena.
/// Poner en el GameObject raíz del armature.
/// </summary>
public class ChainPhysicsSetup : MonoBehaviour
{
    [System.Serializable]
    public class ChainLink
    {
        public Rigidbody rb;
        [HideInInspector] public ConfigurableJoint joint;
    }

    [Header("Links — arrastrar en orden, raíz primero")]
    [SerializeField] private ChainLink[] links;

    [Header("Joint Settings")]
    [SerializeField] private float angularLimit = 35f;
    [SerializeField] private float spring = 5f;
    [SerializeField] private float damper = 2f;
    [SerializeField] private float linkMass = 0.5f;
    [SerializeField] private float linearDamping = 1f;
    [SerializeField] private float angularDamping = 5f;

    private List<Transform> _freedTransforms = new List<Transform>();

    private void Awake()
    {
        SetupChain();
    }

    private void SetupChain()
    {
        if (links == null || links.Length == 0) return;

        if (links[0].rb != null)
            links[0].rb.isKinematic = true;

        for (int i = 1; i < links.Length; i++)
        {
            if (links[i].rb == null) continue;

            Rigidbody rb = links[i].rb;
            rb.mass = linkMass;
            rb.linearDamping = linearDamping;
            rb.angularDamping = angularDamping;
            rb.isKinematic = false;

            var joint = rb.gameObject.AddComponent<ConfigurableJoint>();
            joint.connectedBody = links[i - 1].rb;

            joint.xMotion = ConfigurableJointMotion.Locked;
            joint.yMotion = ConfigurableJointMotion.Locked;
            joint.zMotion = ConfigurableJointMotion.Locked;

            joint.angularXMotion = ConfigurableJointMotion.Limited;
            joint.angularYMotion = ConfigurableJointMotion.Limited;
            joint.angularZMotion = ConfigurableJointMotion.Locked;

            var angularXLimit = new SoftJointLimit { limit = angularLimit };
            joint.lowAngularXLimit = new SoftJointLimit { limit = -angularLimit };
            joint.highAngularXLimit = angularXLimit;
            joint.angularYLimit = angularXLimit;

            var angularSpring = new SoftJointLimitSpring { spring = spring, damper = damper };
            joint.angularXLimitSpring = angularSpring;
            joint.angularYZLimitSpring = angularSpring;

            links[i].joint = joint;
        }
    }

    /// <summary>
    /// Rompe la cadena en el índice indicado.
    /// Todo lo que está por debajo cae libre.
    /// </summary>
    public void BreakAtIndex(int linkIndex, Vector3 impulse)
    {
        if (linkIndex < 0 || linkIndex >= links.Length) return;

        Transform armature = links[0].rb.transform.parent;
        if (armature != null)
        {
            armature.SetParent(null, true);
            _freedTransforms.Add(armature); // guardar referencia al armature completo
        }


        for (int i = linkIndex; i < links.Length; i++)
        {
            if (links[i].rb == null) continue;

            Rigidbody rb = links[i].rb;

            // 1. Destruir TODOS los ConfigurableJoint del objeto
            //    (GetComponents cubre joints huérfanos del editor)
            foreach (var j in rb.gameObject.GetComponents<ConfigurableJoint>())
                Destroy(j);

            links[i].joint = null;

            // 2. Desparentar
            rb.transform.SetParent(null);

            // 3. Liberar Rigidbody completamente
            rb.isKinematic = false;
            rb.useGravity  = true;
            rb.constraints = RigidbodyConstraints.None;

            // 4. Despertar el RB — puede estar dormido si no hubo movimiento
            rb.WakeUp();

            // 5. Impulso solo al primer link liberado
            if (i == linkIndex && impulse != Vector3.zero)
                rb.AddForce(impulse, ForceMode.Impulse);
        }
    }

    public void DestroyAll(float delay = 0f)
    {
        // Destruir el armature completo (que tiene los bones como hijos)
        foreach (var t in _freedTransforms)
        {
            if (t != null)
                Destroy(t.gameObject, delay);
        }
        _freedTransforms.Clear();

        // Destruir el objeto padre original también
        Destroy(gameObject, delay);
    }

    private void OnDrawGizmosSelected()
    {
        if (links == null) return;

        for (int i = 0; i < links.Length; i++)
        {
            if (links[i].rb == null) continue;

            Gizmos.color = i == 0 ? Color.green : Color.cyan;
            Gizmos.DrawWireSphere(links[i].rb.transform.position, 0.05f);

            if (i < links.Length - 1 && links[i + 1].rb != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(
                    links[i].rb.transform.position,
                    links[i + 1].rb.transform.position
                );
            }
        }
    }
}