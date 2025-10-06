using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerExpHUD : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image expBar;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private TextMeshProUGUI expText;

    [Header("Exp")]
    [SerializeField] AnimationCurve expCurve;

    [Header("Comportamiento")]
    //[SerializeField] private bool normalized = true;

    [Header("Animación")]
    //[SerializeField] private float fillLerpSpeed = 8f;

    int currentLevel, totalExp;
    int previousLevelExp, nextLevelsExp;

    //private float _targetFill01;
    //private float _targetRaw;
    //private int _lastLevel = -1, _lastCur = -1, _lastNext = -1;

    private void Start()
    {
        UpdateLevel();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            AddExperience(20);
        }
    }

    public void AddExperience(int amount)
    {
        totalExp += amount;
        CheckForLevelUp();
        UpdateInterface();
    }

    void CheckForLevelUp()
    {
        if(totalExp >= nextLevelsExp)
        {
            currentLevel++;
            UpdateLevel();
        }
    }
    void UpdateLevel()
    {
        previousLevelExp = (int)expCurve.Evaluate(currentLevel);
        nextLevelsExp = (int)expCurve.Evaluate(currentLevel + 1);
        UpdateInterface();

    }

    void UpdateInterface()
    {
        int start = totalExp - previousLevelExp;
        int end = nextLevelsExp - previousLevelExp;

        levelText.text = currentLevel.ToString();
        expText.text = start + "exp / " + end + "exp";
        expBar.fillAmount = (float)start / (float)end;

    }
}

    /*public void Set(int level, int current, int toNext)
    {
        if (toNext <= 0) toNext = 1;

        if (level == _lastLevel && current == _lastCur && toNext == _lastNext)
            return;

        if (levelText) levelText.text = $"Lv {level}";
        if (expText) expText.text = $"{current} / {toNext}";

        if (expBar)
        {
            if (normalized)
            {
                if (expBar.maxValue != 1f) { expBar.minValue = 0f; expBar.maxValue = 1f; }
                _targetFill01 = Mathf.Clamp01(current / (float)toNext);
            }
            else
            {
                if (Mathf.Abs(expBar.maxValue - toNext) > 0.001f)
                {
                    expBar.minValue = 0f;
                    expBar.maxValue = toNext;
                }
                _targetRaw = Mathf.Clamp(current, 0, toNext);
            }
        }

        if (debugLogs)
            Debug.Log($"[PlayerExpHUD] Set ? L={level} XP={current}/{toNext} | mode={(normalized ? "norm" : "abs")}");

        _lastLevel = level; _lastCur = current; _lastNext = toNext;
    }

    public void SetInstant(int level, int current, int toNext)
    {
        Set(level, current, toNext);
        if (!expBar) return;

        if (toNext <= 0) toNext = 1;
        if (normalized)
        {
            _targetFill01 = Mathf.Clamp01(current / (float)toNext);
            expBar.value = _targetFill01;
        }
        else
        {
            expBar.minValue = 0f;
            expBar.maxValue = toNext;
            _targetRaw = Mathf.Clamp(current, 0, toNext);
            expBar.value = _targetRaw;
        }

        if (debugLogs)
            Debug.Log($"[PlayerExpHUD] SetInstant ? Slider={expBar.value:0.000}");
    }

    public void ShowXpGain(int amount, Vector3 worldPos)
    {
        // TODO: spawn de un texto flotante, animación, etc.
        // Por ahora, un log para validar que llega:
        Debug.Log($"[PlayerExpHUD] +{amount} XP at {worldPos}");
    }

    public void FlashLevelUp()
    {
        if (debugLogs)
            Debug.Log("[PlayerExpHUD] FlashLevelUp()");
    }
}*/

