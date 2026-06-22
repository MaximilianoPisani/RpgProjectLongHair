using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class ButtonSound : MonoBehaviour
{
    private Button _button;

    void Start()
    {
        _button = GetComponent<Button>();

        _button.onClick.AddListener(() =>
        {
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlayUIClick();
        });
    }

    void OnDestroy()
    {
        if (_button != null)
            _button.onClick.RemoveAllListeners();
    }
}
