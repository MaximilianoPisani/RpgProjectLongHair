using UnityEngine;
using UnityEngine.UI;

public class EnemyHealthUI : MonoBehaviour
{
    [SerializeField] private Slider _healthSlider;

    private EnemyHealth _enemyHealth;

    private void Awake()
    {
        if (_healthSlider == null)
            _healthSlider = GetComponentInChildren<Slider>();

        _enemyHealth = GetComponentInParent<EnemyHealth>();
    }

    private void OnEnable()
    {
        if (_enemyHealth == null) return;
        _enemyHealth.OnHealthChanged += UpdateHealth;
        _enemyHealth.OnDeath += HandleDeath;

        // Solo leer currentHealth si Photon ya inicializó el objeto
        if (_enemyHealth.Object != null && _enemyHealth.Object.IsValid)
            UpdateHealth(_enemyHealth.currentHealth, _enemyHealth.MaxHealth);
    }

    private void OnDisable()
    {
        if (_enemyHealth == null) return;

        _enemyHealth.OnHealthChanged -= UpdateHealth;
        _enemyHealth.OnDeath -= HandleDeath;
    }

    private void UpdateHealth(int current, int max)
    {
        _healthSlider.maxValue = max;
        _healthSlider.value = current;
    }

    private void HandleDeath()
    {
        gameObject.SetActive(false);
    }
}