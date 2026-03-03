using UnityEngine;
using UnityEngine.UI;

public class CharacterSelection : MonoBehaviour
{
    [Header("Botones")]
    [SerializeField] private Button _player1Button;
    [SerializeField] private Button _player2Button;
    [SerializeField] private Button _player3Button;

    [Header("Canvas")]
    [SerializeField] private GameObject _selectionCanvas;
    [SerializeField] private GameObject _connectionCanvas;

    public static int SelectedPlayer = -1;

    private void Start()
    {
        _player1Button.onClick.AddListener(() => SelectPlayer(1));
        _player2Button.onClick.AddListener(() => SelectPlayer(2));
        _player3Button.onClick.AddListener(() => SelectPlayer(3));
    }

    private void SelectPlayer(int index)
    {
        Debug.Log("Selected player: Player " + index);

        SelectedPlayer = index;

        _selectionCanvas.SetActive(false);

        _connectionCanvas.SetActive(true);
    }
}