using TMPro;
using UnityEngine;
using Fusion;

public class PlayerHealthTextUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI healthText;

    private PlayerHealth _playerHealth;

    private void Start()
    {
        _playerHealth = GetComponentInParent<PlayerHealth>();

        if (_playerHealth == null)
        {
            Debug.LogWarning("[PlayerHealthTextUI] No se encontró PlayerHealth");
            return;
        }

        _playerHealth.OnHealthChanged.AddListener(UpdateHealthText);

        UpdateHealthText(_playerHealth.CurrentHealth, _playerHealth.MaxHealth);
    }

    private void OnDestroy()
    {
        if (_playerHealth != null)
        {
            _playerHealth.OnHealthChanged.RemoveListener(UpdateHealthText);
        }
    }

    private void UpdateHealthText(int currentHealth, int maxHealth)
    {
        healthText.text = currentHealth.ToString();
    }
}