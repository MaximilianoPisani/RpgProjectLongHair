using UnityEngine;

/*public class PlayerHudBinder : MonoBehaviour
{
    [Header("Tags")]
    [SerializeField] private string hudTag = "HUD_XP";
    [SerializeField] private string localPlayerTag = "Player"; // ahora "Player"

    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;

    private PlayerExpHUD _hud;
    private bool _bound;

    private void Start()
    {
        InvokeRepeating(nameof(TryBind), 0.1f, 0.25f);
    }

    private void TryBind()
    {
        if (_bound) return;

        // HUD por tag
        if (_hud == null)
        {
            var hudGo = GameObject.FindWithTag(hudTag);
            if (hudGo) _hud = hudGo.GetComponent<PlayerExpHUD>();
            if (debugLogs) Debug.Log(_hud ? "[Binder] HUD ok" : "[Binder] HUD no encontrado");
        }
        if (_hud == null) return;

        // Player local por tag "Player"
        var playerGo = GameObject.FindWithTag(localPlayerTag);
        if (!playerGo) { if (debugLogs) Debug.Log("[Binder] Player (local) no encontrado"); return; }

        var prog = playerGo.GetComponent<PlayerProgression>();
        if (!prog) { if (debugLogs) Debug.LogWarning("[Binder] Player sin PlayerProgression"); return; }

        prog.BindHUD(_hud);
        _bound = true;
        CancelInvoke(nameof(TryBind));

        if (debugLogs) Debug.Log("[Binder] Enlace HUD↔PlayerProgression OK (tag=Player).");
    }
}*/
