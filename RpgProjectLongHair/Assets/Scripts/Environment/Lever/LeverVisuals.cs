using UnityEngine;

public class LeverVisuals : MonoBehaviour
{
    [Header("Animación")]
    [SerializeField] private Animator leverAnimator;
    [SerializeField] private string blendParameter = "Enable"; // nombre del param

    [Header("Material indicador")]
    [SerializeField] private Renderer indicatorRenderer;
    [SerializeField] private int materialIndex = 1;           // índice del material rojo/verde
    [SerializeField] private Color colorOff = Color.red;
    [SerializeField] private Color colorOn = Color.green;

    private MaterialPropertyBlock _mpb;

    private void SetColor(Color color, bool emissive)
    {
        if (indicatorRenderer == null) return;
        indicatorRenderer.GetPropertyBlock(_mpb, materialIndex);
        _mpb.SetColor("_BaseColor", color);
        _mpb.SetColor("_EmissionColor", emissive ? color * 2f : Color.black);
        indicatorRenderer.SetPropertyBlock(_mpb, materialIndex);
    }

    private void Awake()
    {
        _mpb = new MaterialPropertyBlock();
        SetColor(colorOff, emissive: false);
    }

    public void Activate()
    {
        SetColor(colorOn, emissive: true);
        if (leverAnimator != null)
            leverAnimator.SetFloat(blendParameter, 1f);
    }
}