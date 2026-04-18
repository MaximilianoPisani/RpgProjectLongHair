using UnityEngine;
using UnityEngine.UI;

public class CharacterSelection : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button _player1Button;
    [SerializeField] private Button _player2Button;

    [Header("Highlight visual (opcional)")]
    [SerializeField] private GameObject _highlight1;
    [SerializeField] private GameObject _highlight2;

    public static int SelectedCharacter = -1;

    private void Start()
    {
        _player1Button.onClick.AddListener(() => SelectCharacter(1));
        _player2Button.onClick.AddListener(() => SelectCharacter(2));
        SetHighlight(-1);
        Debug.Log("[CharacterSelection] Panel inicializado.");
    }

    private void SelectCharacter(int index)
    {
        SelectedCharacter = index;
        SetHighlight(index);
        Debug.Log($"[CharacterSelection] Personaje elegido: {index}");

        if (GameFlowManager.Instance != null)
            GameFlowManager.Instance.OnCharacterSelected();
        else
            Debug.LogWarning("[CharacterSelection] GameFlowManager no encontrado.");
    }

    private void SetHighlight(int index)
    {
        if (_highlight1 != null) _highlight1.SetActive(index == 1);
        if (_highlight2 != null) _highlight2.SetActive(index == 2);
    }
}