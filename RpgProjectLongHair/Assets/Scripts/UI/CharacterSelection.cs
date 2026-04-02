using UnityEngine;
using UnityEngine.UI;

public class CharacterSelection : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button _player1Button;
    [SerializeField] private Button _player2Button;
    [SerializeField] private Button _player3Button;

    [SerializeField] private Button _logoutButton;


    public static int SelectedPlayer = -1;

    private void Start()
    {
        _player1Button.onClick.AddListener(() => SelectPlayer(1));
        _player2Button.onClick.AddListener(() => SelectPlayer(2));
        _player3Button.onClick.AddListener(() => SelectPlayer(3));

        _logoutButton.onClick.AddListener(OnLogoutClicked);

    }
    private void OnLogoutClicked()
    {
        Debug.Log("[CharacterSelection] Logout clicked");

        GameFlowManager.Instance.Logout();
    }
    private void SelectPlayer(int index)
    {
        if (!GameFlowManager.Instance.IsLoggedIn)
        {
            Debug.LogError("[CharacterSelection] No logueado");
            return;
        }

        SelectedPlayer = index;

        PlayerPrefs.SetInt("SelectedCharacter", index);

        Debug.Log("[CharacterSelection] Player selected: " + index);

        GameFlowManager.Instance.OnCharacterSelected(index);
    }
}