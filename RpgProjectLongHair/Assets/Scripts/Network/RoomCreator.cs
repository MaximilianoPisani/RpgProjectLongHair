using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RoomCreator : MonoBehaviour
{
    public event Action<string> OnRoomNameConfirmed;

    [Header("UI")]
    [SerializeField] private GameObject _panel;
    [SerializeField] private TMP_InputField _roomNameInput;
    [SerializeField] private Button _confirmButton;
    [SerializeField] private Button _cancelButton;
    [SerializeField] private TextMeshProUGUI _errorLabel;

    [Header("Referencias")]
    [SerializeField] private RoomBrowser _roomBrowser;

    private const int MIN_NAME_LENGTH = 3;
    private const int MAX_NAME_LENGTH = 24;

    private void Start()
    {
        _confirmButton.onClick.AddListener(OnConfirmClicked);
        _cancelButton.onClick.AddListener(OnCancelClicked);
        _roomNameInput.onValueChanged.AddListener(_ => ClearError());

        Hide();
    }

    private void OnConfirmClicked()
    {
        string roomName = _roomNameInput.text.Trim();
        if (!ValidateRoomName(roomName)) return;

        Debug.Log($"[RoomCreator] Sala confirmada: '{roomName}'");
        Hide();
        OnRoomNameConfirmed?.Invoke(roomName);
    }

    private bool ValidateRoomName(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length < MIN_NAME_LENGTH)
        {
            ShowError($"El nombre debe tener al menos {MIN_NAME_LENGTH} caracteres.");
            return false;
        }

        if (name.Length > MAX_NAME_LENGTH)
        {
            ShowError($"El nombre no puede superar {MAX_NAME_LENGTH} caracteres.");
            return false;
        }

        foreach (char c in name)
        {
            if (!char.IsLetterOrDigit(c) &&
                c != ' ' &&
                c != '_' &&
                c != '-')
            {
                ShowError("Solo se permiten letras, números, espacios, guiones y guiones bajos.");
                return false;
            }
        }

        if (_roomBrowser != null &&
            _roomBrowser.RoomExists(name))
        {
            ShowError("Ese nombre de sala ya está en uso.");
            return false;
        }

        return true;
    }

    private void OnCancelClicked()
    {
        Hide();
        _roomBrowser.Show();
    }

    private void ShowError(string message)
    {
        if (_errorLabel != null)
        {
            _errorLabel.text = message;
            _errorLabel.gameObject.SetActive(true);
        }
    }

    private void ClearError()
    {
        if (_errorLabel != null)
            _errorLabel.gameObject.SetActive(false);
    }

    public void Show()
    {
        _roomNameInput.text = string.Empty;
        ClearError();
        _panel.SetActive(true);
    }

    public void Hide() => _panel.SetActive(false);
}