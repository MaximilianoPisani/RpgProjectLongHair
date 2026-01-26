using UnityEngine;
using UnityEngine.UI;

public class EnemyHealthUI : MonoBehaviour
{
    [SerializeField] private Slider healthSlider;

    private EnemyHealth enemyHealth;

    private void Awake()
    {
        if (healthSlider == null)
            healthSlider = GetComponentInChildren<Slider>();

        enemyHealth = GetComponentInParent<EnemyHealth>();
    }

    private void OnEnable()
    {
        if (enemyHealth == null) return;

        enemyHealth.OnHealthChanged += UpdateHealth;
        enemyHealth.OnDeath += HandleDeath;

        UpdateHealth(enemyHealth.currentHealth, enemyHealth.MaxHealth);
    }

    private void OnDisable()
    {
        if (enemyHealth == null) return;

        enemyHealth.OnHealthChanged -= UpdateHealth;
        enemyHealth.OnDeath -= HandleDeath;
    }

    private void UpdateHealth(int current, int max)
    {
        healthSlider.maxValue = max;
        healthSlider.value = current;
    }

    private void HandleDeath()
    {
        gameObject.SetActive(false);
    }
}