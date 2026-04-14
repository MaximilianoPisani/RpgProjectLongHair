using UnityEngine;

[CreateAssetMenu(fileName = "NewItemVFX", menuName = "Inventory/ItemVFX")]
public class ItemVFXConfig : ScriptableObject
{
    [Header("VFX Settings")]
    public GameObject vfxPrefab;
    public Vector3 vfxOffset = Vector3.zero;
    public Vector3 vfxScale = Vector3.one;

    [Header("Animation")]
    public bool enableFloating = true;
    public float floatingSpeed = 1f;
    public float floatingHeight = 0.3f;

    public bool enableRotation = true;
    public Vector3 rotationSpeed = new Vector3(0, 50, 0);

    [Header("Glow/Pulse")]
    public bool enablePulse = false;
    public float pulseSpeed = 2f;
    public float pulseMinIntensity = 0.5f;
    public float pulseMaxIntensity = 1.5f;
}