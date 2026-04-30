using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerHealthBar : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Slider _healthSlider;
    [SerializeField] private Image _fillImage;
    [SerializeField] private TextMeshProUGUI _healthText;

    [Header("Animación")]
    [SerializeField] private bool _smoothTransition = true;
    [SerializeField] private float _lerpSpeed = 5f;

    [Header("Colores según Vida")]
    [SerializeField] private bool _useColorGradient = true;
    [SerializeField] private Color _highHealthColor = Color.green;
    [SerializeField] private Color _mediumHealthColor = Color.yellow;
    [SerializeField] private Color _lowHealthColor = Color.red;
    [SerializeField] private float _mediumHealthThreshold = 0.5f;
    [SerializeField] private float _lowHealthThreshold = 0.25f;

    [Header("Opciones de Texto")]
    [SerializeField] private bool _showText = true;
    [SerializeField] private bool _showPercentage = false;
    [SerializeField] private bool _showMaxHealth = true;

    private PlayerHealth _playerHealth;
    private float _targetValue;
    private int _currentHealth;
    private int _maxHealth;

    private void Start()
    {
        // Intentar encontrar el PlayerHealth
        FindPlayerHealth();

        // Configurar el slider
        if (_healthSlider != null)
        {
            _healthSlider.minValue = 0;
            _healthSlider.maxValue = 1;
            _healthSlider.value = 1;
        }
    }

    private void FindPlayerHealth()
    {
        // Buscar el PlayerHealth del jugador local
        var allPlayers = FindObjectsOfType<PlayerHealth>();

        foreach (var player in allPlayers)
        {
            // Si tiene HasInputAuthority significa que es el jugador local
            if (player.Object != null && player.Object.HasInputAuthority)
            {
                SetPlayerHealth(player);
                return;
            }
        }

        // Si no se encontró, intentar de nuevo en el próximo frame
        Invoke(nameof(FindPlayerHealth), 0.5f);
    }

    public void SetPlayerHealth(PlayerHealth playerHealth)
    {
        // Desuscribir del anterior si existe
        if (_playerHealth != null)
        {
            _playerHealth.OnHealthChanged.RemoveListener(OnHealthChanged);
            _playerHealth.OnPlayerDied.RemoveListener(OnPlayerDied);
        }

        _playerHealth = playerHealth;

        if (_playerHealth != null)
        {
            // Suscribir a eventos
            _playerHealth.OnHealthChanged.AddListener(OnHealthChanged);
            _playerHealth.OnPlayerDied.AddListener(OnPlayerDied);

            // Inicializar valores
            _currentHealth = _playerHealth.CurrentHealth;
            _maxHealth = _playerHealth.MaxHealth;
            _targetValue = _playerHealth.HealthPercentage;

            UpdateHealthBar(true);

            Debug.Log($"[HealthBar] Conectada a PlayerHealth: {_currentHealth}/{_maxHealth}");
        }
    }

    private void Update()
    {
        if (_healthSlider == null) return;

        // Animar suavemente hacia el valor objetivo
        if (_smoothTransition)
        {
            _healthSlider.value = Mathf.Lerp(_healthSlider.value, _targetValue, Time.deltaTime * _lerpSpeed);
        }
    }

    private void OnHealthChanged(int currentHealth, int maxHealth)
    {
        _currentHealth = currentHealth;
        _maxHealth = maxHealth;
        _targetValue = maxHealth > 0 ? (float)currentHealth / maxHealth : 0f;

        UpdateHealthBar(false);
    }

    private void OnPlayerDied()
    {
        _currentHealth = 0;
        _targetValue = 0f;
        UpdateHealthBar(false);
    }

    private void UpdateHealthBar(bool instant)
    {
        if (_healthSlider == null) return;

        // Actualizar slider
        if (instant || !_smoothTransition)
        {
            _healthSlider.value = _targetValue;
        }

        // Actualizar color
        if (_useColorGradient && _fillImage != null)
        {
            _fillImage.color = GetHealthColor(_targetValue);
        }

        // Actualizar texto
        if (_showText && _healthText != null)
        {
            _healthText.text = GetHealthText();
        }
    }

    private Color GetHealthColor(float healthPercentage)
    {
        if (healthPercentage > _mediumHealthThreshold)
        {
            // Verde a Amarillo
            float t = (healthPercentage - _mediumHealthThreshold) / (1f - _mediumHealthThreshold);
            return Color.Lerp(_mediumHealthColor, _highHealthColor, t);
        }
        else if (healthPercentage > _lowHealthThreshold)
        {
            // Amarillo a Rojo
            float t = (healthPercentage - _lowHealthThreshold) / (_mediumHealthThreshold - _lowHealthThreshold);
            return Color.Lerp(_lowHealthColor, _mediumHealthColor, t);
        }
        else
        {
            // Rojo
            return _lowHealthColor;
        }
    }

    private string GetHealthText()
    {
        if (_showPercentage)
        {
            return $"{Mathf.RoundToInt(_targetValue * 100)}%";
        }
        else if (_showMaxHealth)
        {
            return $"{_currentHealth}/{_maxHealth}";
        }
        else
        {
            return _currentHealth.ToString();
        }
    }

    private void OnDestroy()
    {
        // Limpiar eventos
        if (_playerHealth != null)
        {
            _playerHealth.OnHealthChanged.RemoveListener(OnHealthChanged);
            _playerHealth.OnPlayerDied.RemoveListener(OnPlayerDied);
        }
    }

    #region Public Methods

    /// <summary>
    /// Cambiar la velocidad de la animación
    /// </summary>
    public void SetLerpSpeed(float speed)
    {
        _lerpSpeed = Mathf.Max(0.1f, speed);
    }

    /// <summary>
    /// Activar/desactivar transición suave
    /// </summary>
    public void SetSmoothTransition(bool smooth)
    {
        _smoothTransition = smooth;
    }

    /// <summary>
    /// Forzar actualización inmediata
    /// </summary>
    public void ForceUpdate()
    {
        if (_playerHealth != null)
        {
            OnHealthChanged(_playerHealth.CurrentHealth, _playerHealth.MaxHealth);
            UpdateHealthBar(true);
        }
    }

    #endregion
}