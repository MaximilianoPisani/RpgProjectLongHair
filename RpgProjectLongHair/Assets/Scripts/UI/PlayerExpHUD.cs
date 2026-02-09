using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerExpHUD : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image expFill;
    [SerializeField] private TMP_Text expText;
    [SerializeField] private TMP_Text levelText;

    public void Bind(PlayerExp playerExp)
    {
        OnExpUpdated(
            playerExp.CurrentExp,
            playerExp.ExpToNextLevel,
            playerExp.Level
        );
    }

    public void OnExpUpdated(int currentExp, int expToNext, int level)
    {
        float normalized = Mathf.Clamp01((float)currentExp / expToNext);

        if (expFill != null)
            expFill.fillAmount = normalized;

        if (expText != null)
            expText.text = $"{currentExp} / {expToNext}";

        if (levelText != null)
            levelText.text = $"Lv {level}";
    }
}