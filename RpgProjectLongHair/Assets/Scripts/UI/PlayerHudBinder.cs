using Fusion;
using UnityEngine;
public class PlayerHudBinder : MonoBehaviour
{
    [SerializeField] private PlayerExpHUD hud;

    private bool _bound;
    private NetworkRunner _runner;

    private void Awake()
    {
        if (hud == null)
            hud = FindObjectOfType<PlayerExpHUD>(includeInactive: true);
    }

    private void Start()
    {
        _runner = FindObjectOfType<NetworkRunner>();
        // Intentos periódicos hasta encontrar al player local
        InvokeRepeating(nameof(TryBind), 0.1f, 0.25f);
    }

    private void TryBind()
    {
        if (_bound || hud == null || _runner == null) return;

        var all = FindObjectsOfType<PlayerProgression>(includeInactive: true);
        for (int i = 0; i < all.Length; i++)
        {
            var prog = all[i];
            var no = prog.Object;
            if (no == null) continue;

            // Player local: tiene InputAuthority y coincide con LocalPlayer
            if (no.HasInputAuthority && no.InputAuthority == _runner.LocalPlayer)
            {
                prog.BindHUD(hud);   // <-- Aquí se enlaza con tu PlayerProgression
                _bound = true;
                CancelInvoke(nameof(TryBind));
                return;
            }
        }
    }
}
