using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerExpHUD : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Slider expBar;     // Configurar 0..1 en el inspector
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text expText;

    [Header("Animación")]
    [SerializeField] private float fillLerpSpeed = 8f;

    private float _targetFill;   // 0..1
    private int _lastLevel = -1;
    private int _lastCur = -1;
    private int _lastNext = -1;

    private void Awake()
    {
        if (expBar)
        {
            expBar.minValue = 0f;
            expBar.maxValue = 1f;
            expBar.value = 0f;
        }
        if (levelText) levelText.text = "Lv -";
        if (expText) expText.text = "- / -";
    }

    private void Update()
    {
        if (!expBar) return;
        // Animación suave hacia el objetivo
        expBar.value = Mathf.MoveTowards(expBar.value, _targetFill, fillLerpSpeed * Time.unscaledDeltaTime);
    }

    /// Llamado por PlayerProgression.BindHUD(...) y/o sus OnChanged
    public void Set(int level, int current, int toNext)
    {
        if (toNext <= 0) toNext = 1;

        // Evitar trabajo si no cambió nada
        if (level == _lastLevel && current == _lastCur && toNext == _lastNext)
            return;

        _targetFill = Mathf.Clamp01(current / (float)toNext);

        if (levelText) levelText.text = $"Lv {level}";
        if (expText) expText.text = $"{current} / {toNext}";

        _lastLevel = level;
        _lastCur = current;
        _lastNext = toNext;
    }

    /// Útil para forzar estado inicial sin animación (opcional).
    public void SetInstant(int level, int current, int toNext)
    {
        Set(level, current, toNext);
        if (expBar) expBar.value = _targetFill;
    }

    /// Ping visual opcional al subir de nivel.
    public void FlashLevelUp()
    {
        // Ejemplo simple: pequeño “punch” al texto de nivel (completar si querés)
        // StartCoroutine(Punch(levelText.rectTransform));
    }
}
