using Fusion;
using UnityEngine;
using UnityEngine.Rendering;

public class CameraUnderwaterDetector : NetworkBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Volume _volume;
    [SerializeField] private VolumeProfile _underwaterProfile;

    [Header("Transición")]
    [SerializeField] private float transitionSpeed = 3f;

    private bool _isUnderwater = false;
    private float _targetWeight = 0f;

    private float _currentWaterLevel = float.MinValue;
    private bool _hasWaterLevel = false;

    private float _lastZoneConfirmTime = -1f;
    private const float _zoneTimeout = 0.15f;

    public override void Spawned()
    {
        if (!Object.HasInputAuthority)
            return;

        if (_volume == null)
            _volume = GetComponent<Volume>();

        if (_volume != null)
        {
            _volume.isGlobal = true;
            _volume.weight = 0f;
            _volume.priority = 10;
            _volume.profile = _underwaterProfile;

            Debug.Log("[UnderwaterDetector] Inicializado para jugador local");
        }
        else
        {
            Debug.LogError("[UnderwaterDetector] No se encontró Volume");
        }
    }

    public void ConfirmInsideZone()
    {
        if (!Object.HasInputAuthority)
            return;

        _lastZoneConfirmTime = Time.time;
    }

    private void Update()
    {
        if (!Object.HasInputAuthority || _volume == null)
            return;

        // Transición suave
        _volume.weight = Mathf.MoveTowards(
            _volume.weight,
            _targetWeight,
            transitionSpeed * Time.deltaTime
        );

        // Si no hay agua registrada no seguimos
        if (!_hasWaterLevel)
            return;

        bool insideZone = (Time.time - _lastZoneConfirmTime) < _zoneTimeout;

        if (!insideZone)
        {
            UnregisterWaterSurface();
            return;
        }

        // Verificar si la cámara está bajo el agua
        bool cameraUnderwater = transform.position.y < _currentWaterLevel;

        if (cameraUnderwater != _isUnderwater)
        {
            SetUnderwater(cameraUnderwater);
        }
    }

    public void RegisterWaterSurface(float waterLevel)
    {
        if (!Object.HasInputAuthority)
            return;

        _currentWaterLevel = waterLevel;
        _hasWaterLevel = true;

        bool underwater = transform.position.y < _currentWaterLevel;

        SetUnderwater(underwater);

        Debug.Log(
            $"[UnderwaterDetector] WaterLevel={waterLevel} | CameraY={transform.position.y} | Underwater={underwater}"
        );
    }

    public void UnregisterWaterSurface()
    {
        if (!Object.HasInputAuthority)
            return;

        _hasWaterLevel = false;

        SetUnderwater(false);

        Debug.Log("[UnderwaterDetector] Superficie desregistrada");
    }

    private void SetUnderwater(bool underwater)
    {
        if (_isUnderwater == underwater)
            return;

        _isUnderwater = underwater;

        _targetWeight = underwater ? 1f : 0f;

        Debug.Log(
            $"[UnderwaterDetector] Underwater={underwater} | TargetWeight={_targetWeight}"
        );
    }

    public bool IsUnderwater => _isUnderwater;

    public float CurrentWeight => _volume != null ? _volume.weight : 0f;
}