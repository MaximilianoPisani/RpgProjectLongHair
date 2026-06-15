using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RageBarUI : MonoBehaviour
{
    [SerializeField] private Image _fillImage;
    [SerializeField] private GameObject _readyIndicator;
    [SerializeField] private Color _chargingColor = Color.yellow;
    [SerializeField] private Color _activeColor = Color.red;

    [Header("Smooth Fill")]
    [SerializeField] private float _fillSpeed = 3f;

    [Header("Key Hint")]
    [SerializeField] private GameObject _keyHintObject; 
    [SerializeField] private string _activationKey = "Q"; 

    private float _targetFill = 0f;
    private float _currentFill = 0f;
    private PlayerRageHandler _cachedHandler;

    private void OnEnable()
    {
        PlayerRageHandler.OnChargeChanged += HandleChargeChanged;
        PlayerRageHandler.OnRageActivated += HandleRageActivated;
        PlayerRageHandler.OnRageDeactivated += HandleRageDeactivated;

        if (_keyHintObject != null)
        {
            var tmp = _keyHintObject.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null) tmp.text = _activationKey;
            _keyHintObject.SetActive(false);
        }
    }

    private void OnDisable()
    {
        PlayerRageHandler.OnChargeChanged -= HandleChargeChanged;
        PlayerRageHandler.OnRageActivated -= HandleRageActivated;
        PlayerRageHandler.OnRageDeactivated -= HandleRageDeactivated;
        _cachedHandler = null;
    }

    private void Update()
    {
        var handler = GetLocalRageHandler();
        if (handler == null) return;

        _targetFill = handler.GetNormalizedBar();
        _currentFill = Mathf.Lerp(_currentFill, _targetFill, Time.deltaTime * _fillSpeed);
        _fillImage.fillAmount = _currentFill;
        _fillImage.color = handler.IsRageActive ? _activeColor : _chargingColor;

        bool showReady = handler.IsChargeFull() && !handler.IsRageActive;

        if (_readyIndicator != null)
            _readyIndicator.SetActive(showReady);

        if (_keyHintObject != null)
            _keyHintObject.SetActive(showReady);
    }

    private void HandleChargeChanged(float current, float max)
    {
        _targetFill = current / max;
        _fillImage.color = _chargingColor;

        bool full = current >= max;
        if (_readyIndicator != null) _readyIndicator.SetActive(full);
        if (_keyHintObject != null) _keyHintObject.SetActive(full);
    }

    private void HandleRageActivated()
    {
        _targetFill = 1f;
        _currentFill = 1f;
        _fillImage.fillAmount = 1f;
        _fillImage.color = _activeColor;

        if (_readyIndicator != null) _readyIndicator.SetActive(false);
        if (_keyHintObject != null) _keyHintObject.SetActive(false);
    }

    private void HandleRageDeactivated()
    {
        _targetFill = 0f;
        _currentFill = 0f;
        _fillImage.fillAmount = 0f;
        _fillImage.color = _chargingColor;

        if (_readyIndicator != null) _readyIndicator.SetActive(false);
        if (_keyHintObject != null) _keyHintObject.SetActive(false);
    }

    private PlayerRageHandler GetLocalRageHandler()
    {
        if (_cachedHandler != null && _cachedHandler.Object != null)
            return _cachedHandler;

        var handlers = FindObjectsByType<PlayerRageHandler>(FindObjectsSortMode.None);
        foreach (var h in handlers)
        {
            if (h.Object != null && h.Object.HasInputAuthority)
            {
                _cachedHandler = h;
                return h;
            }
        }
        return null;
    }
}