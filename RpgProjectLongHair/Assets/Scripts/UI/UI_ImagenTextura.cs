using UnityEngine;
using UnityEngine.UI;
using Fusion;

public class UI_ImagenTextura : NetworkBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Camera _playerCamera;
    [SerializeField] private RawImage _rawImage;

    [Header("Configuración")]
    [SerializeField] private int _width = 512;
    [SerializeField] private int _height = 512;
    [SerializeField] private int _depth = 24;

    private RenderTexture _rt;

    public override void Spawned()
    {
        if (!HasInputAuthority)
        {
            if (_playerCamera != null)
                _playerCamera.gameObject.SetActive(false);

            enabled = false;
            return;
        }

        CrearRenderTextureLocal();
    }

    private void CrearRenderTextureLocal()
    {
        _rt = new RenderTexture(_width, _height, _depth, RenderTextureFormat.ARGB32);
        _rt.name = $"RT_Player{Runner.UserId}";
        _rt.Create();

        if (_playerCamera != null)
            _playerCamera.targetTexture = _rt;
        else
            Debug.LogError("[UI_ImagenTextura] No hay cámara asignada");

        if (_rawImage != null)
            _rawImage.texture = _rt;
        else
            Debug.LogError("[UI_ImagenTextura] No hay RawImage asignado");
    }

    private void OnDestroy()
    {
        if (_playerCamera != null)
            _playerCamera.targetTexture = null;

        if (_rt != null)
        {
            _rt.Release();
            Destroy(_rt);
        }
    }
}