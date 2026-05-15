using TMPro;
using UnityEngine;

public class PlayerDamageTextUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI damageText;

    private PlayerStats _playerStats;

    private void Start()
    {
        _playerStats = GetComponentInParent<PlayerStats>();

        if (_playerStats == null)
        {
            Debug.LogWarning("[PlayerDamageTextUI] No se encontró PlayerStats");
            return;
        }

        _playerStats.OnDamageChanged.AddListener(UpdateDamageText);

        UpdateDamageText(_playerStats.CurrentDamage);
    }

    private void OnDestroy()
    {
        if (_playerStats != null)
            _playerStats.OnDamageChanged.RemoveListener(UpdateDamageText);
    }

    private void UpdateDamageText(int damage)
    {
        damageText.text = damage.ToString();
    }
}