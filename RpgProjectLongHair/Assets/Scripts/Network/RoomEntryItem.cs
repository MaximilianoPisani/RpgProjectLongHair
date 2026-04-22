using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RoomEntryItem : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _roomNameText;
    [SerializeField] private TextMeshProUGUI _playerCountText;
    [SerializeField] private Image _selectedBackground;
    [SerializeField] private Button _button;

    public string SessionName { get; private set; }

    private Action<string> _onSelected;

    public void Setup(string roomName, int playerCount, int maxPlayers, Action<string> onSelected)
    {
        SessionName = roomName;
        _onSelected = onSelected;

        _roomNameText.text = roomName;
        _playerCountText.text = $"{playerCount} / {maxPlayers}";

        if (_selectedBackground != null)
            _selectedBackground.enabled = true;

        _button.onClick.RemoveAllListeners(); 
        _button.onClick.AddListener(() => _onSelected?.Invoke(SessionName));
    }

    public void SetSelected(bool selected)
    {

        if (_selectedBackground != null)
        {
            _selectedBackground.color = selected
                ? new Color(1f, 1f, 1f, 1f)    
                : new Color(1f, 1f, 1f, 0.5f); 
        }
    }
}
