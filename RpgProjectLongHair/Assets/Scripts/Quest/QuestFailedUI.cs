using UnityEngine;
using TMPro;
using System.Collections;

public class QuestFailedUI : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject _panel;
    [Header("Texts")]
    [SerializeField] private TextMeshProUGUI _txtTitle;
    [Header("Duration")]
    [SerializeField] private float _displayDuration = 3f;

    public bool IsVisible => _panel != null && _panel.activeSelf;
    private Coroutine _hideCoroutine;

    public void Show()
    {
        _txtTitle.text = "¡MISIÓN FALLIDA!";
        _panel.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (_hideCoroutine != null)
            StopCoroutine(_hideCoroutine);
        _hideCoroutine = StartCoroutine(HideAfterDelay());
    }

    private IEnumerator HideAfterDelay()
    {
        yield return new WaitForSeconds(_displayDuration);
        Hide();
    }

    public void Hide()
    {
        _panel.SetActive(false);
        _hideCoroutine = null;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}