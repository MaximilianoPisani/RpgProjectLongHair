using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerExpHUD : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image expFill;
    [SerializeField] private TMP_Text expText;

    [Header("Config")]
    [SerializeField] private int expToNextLevel = 100;

    private int _cachedExp;
    private PlayerExp _playerExp;

    public void Bind(PlayerExp playerExp)
    {
        _cachedExp = playerExp.TotalExp;
        UpdateBar();
    }

    public void OnNetworkExpChanged(int newTotalExp)
    {
        _cachedExp = newTotalExp;
        UpdateBar();
    }

    private void UpdateBar()
    {
        float normalized = Mathf.Clamp01((float)_cachedExp / expToNextLevel);

        if (expFill != null)
            expFill.fillAmount = normalized;

        if (expText != null)
            expText.text = $"{_cachedExp} / {expToNextLevel}";
    }

    private void RefreshText()
    {
        if (expText != null)
            expText.text = $"{_cachedExp} / {expToNextLevel}";
    }
}