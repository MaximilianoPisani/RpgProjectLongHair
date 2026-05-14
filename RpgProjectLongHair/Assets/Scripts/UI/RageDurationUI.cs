using TMPro;
using UnityEngine;

public class RageDurationUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI durationText;

    private PlayerRageHandler _rageHandler;

    private void Start()
    {
        _rageHandler = GetLocalRageHandler();

        if (_rageHandler == null)
        {
            Debug.LogWarning("[RageDurationUI] No se encontró PlayerRageHandler");
            enabled = false;
        }
    }

    private void Update()
    {
        if (_rageHandler == null)
            return;

        if (_rageHandler.IsRageActive)
        {
            durationText.gameObject.SetActive(true);

            int seconds = Mathf.CeilToInt(_rageHandler.RageTimeLeft);

            durationText.text = $"{seconds}s";
        }
        else
        {
            durationText.gameObject.SetActive(false);
        }
    }

    private PlayerRageHandler GetLocalRageHandler()
    {
        var handlers = FindObjectsByType<PlayerRageHandler>(FindObjectsSortMode.None);

        foreach (var h in handlers)
        {
            if (h.Object != null && h.Object.HasInputAuthority)
                return h;
        }

        return null;
    }
}