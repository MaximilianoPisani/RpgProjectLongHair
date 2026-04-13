using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class QuestFailedUI : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject _panel;

    [Header("Texts")]
    [SerializeField] private TextMeshProUGUI _txtTitle;

    [Header("Button")]
    [SerializeField] private Button _btnContinue;

    private void Start()
    {
        _btnContinue.onClick.AddListener(OnContinue);
    }

    public void Show()
    {
        Debug.Log("[QuestFailedUI] Show llamado");
        _txtTitle.text = "¡MISIÓN FALLIDA!";
        _panel.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void OnContinue()
    {
        _panel.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
    }

    private void OnDisable()
    {
        Debug.Log($"[QuestFailedUI] Panel desactivado!\n{System.Environment.StackTrace}");
    }
}
    
