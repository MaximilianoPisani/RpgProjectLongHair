using System.Collections.Generic;
using UnityEngine;

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

    private readonly List<Transform> _freedTransforms = new();

    private void Awake() => SetupChain();

    private void SetupChain()
    {
        if (links == null || links.Length == 0) return;

        links[0].rb.isKinematic = true;

        for (int i = 1; i < links.Length; i++)
        {
            if (links[i].rb == null) continue;

            var rb = links[i].rb;
            rb.mass = linkMass;
            rb.linearDamping = linearDamping;
            rb.angularDamping = angularDamping;
            rb.useGravity = true;

            var joint = rb.gameObject.AddComponent<ConfigurableJoint>();
            joint.connectedBody = links[i - 1].rb;

            joint.xMotion = joint.yMotion = joint.zMotion = ConfigurableJointMotion.Locked;
            joint.angularXMotion = joint.angularYMotion = ConfigurableJointMotion.Limited;
            joint.angularZMotion = ConfigurableJointMotion.Locked;

            var limit = new SoftJointLimit { limit = angularLimit };
            joint.lowAngularXLimit = new SoftJointLimit { limit = -angularLimit };
            joint.highAngularXLimit = limit;
            joint.angularYLimit = limit;

            var sp = new SoftJointLimitSpring { spring = spring, damper = damper };
            joint.angularXLimitSpring = sp;
            joint.angularYZLimitSpring = sp;

            links[i].joint = joint;
        }
    }

    public void BreakAtIndex(int linkIndex, Vector3 impulse)
    {
        if (linkIndex < 0 || linkIndex >= links.Length) return;

        // Sacar el armature del NetworkObject para que Fusion no lo controle
        var armature = links[0].rb.transform.parent;
        if (armature != null)
        {
            armature.SetParent(null, true);
            _freedTransforms.Add(armature);
        }

        for (int i = linkIndex; i < links.Length; i++)
        {
            if (links[i].rb == null) continue;

            foreach (var j in links[i].rb.GetComponents<ConfigurableJoint>())
                Destroy(j);
            links[i].joint = null;

            var rb = links[i].rb;
            rb.transform.SetParent(null);
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.constraints = RigidbodyConstraints.None;
            rb.WakeUp();

            if (i == linkIndex && impulse != Vector3.zero)
                rb.AddForce(impulse, ForceMode.Impulse);
        }
    }

    public void DestroyAll(float delay = 0f)
    {
        foreach (var t in _freedTransforms)
            if (t != null) Destroy(t.gameObject, delay);

        _freedTransforms.Clear();
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
                Gizmos.DrawLine(links[i].rb.transform.position, links[i + 1].rb.transform.position);
            }
        }
    }
}