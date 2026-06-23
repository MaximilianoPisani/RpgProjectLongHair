using UnityEngine;
using Fusion;
using TMPro;
using System.Collections;

public class PickupableCraftItem : NetworkBehaviour
{
    [SerializeField] private CraftItemSO _craftItemSO;
    [SerializeField] private Transform _visualRoot;
    [SerializeField] private TextMeshProUGUI _txtFeedback;

    [Header("Respawn items for network")]
    [SerializeField] private float _respawnTime = 15f; // tiempo de respawn

    [Networked] private NetworkBool IsPicked { get; set; }
    [Networked] private TickTimer _respawnTimer { get; set; } // - timer de Fusion

    private Collider _collider;
    private bool _localPicked;
    private Coroutine _feedbackCoroutine;
    private PickupVFXController _vfxController;

    // Cache de renderers visuales (NO UI)
    private Renderer[] _cachedRenderers;

    public CraftItemSO CraftItemSO => _craftItemSO;
    public int ItemId => _craftItemSO != null ? _craftItemSO.id : 0;
    public bool IsAlreadyPicked => IsPicked || _localPicked;

    [Header("Quest Tracking")]
    [SerializeField] private string _questTrackId = "";
    public string QuestTrackId => _questTrackId;

    public override void Spawned()
    {
        _collider = GetComponent<Collider>();

        //SetupVisual();

        // Cachear renderers UNA VEZ
        CacheVisualRenderers();

        SetupVFX();

        if (_txtFeedback != null)
            _txtFeedback.gameObject.SetActive(false);

        // si respawneó, asegurar que esté activo
        if (!IsPicked)
            SetVisualActive(true);
    }

    private void CacheVisualRenderers()
    {
        var allRenderers = GetComponentsInChildren<Renderer>(true);
        var visualList = new System.Collections.Generic.List<Renderer>();

        foreach (var rend in allRenderers)
        {
            // Ignorar CanvasRenderer (UI)
            if (rend is CanvasRenderer) continue;

            // Ignorar si está dentro de un Canvas
            if (rend.GetComponentInParent<Canvas>() != null) continue;

            visualList.Add(rend);
        }

        _cachedRenderers = visualList.ToArray();
    }

    public override void FixedUpdateNetwork()
    {
        // solo el StateAuthority maneja el timer
        if (!HasStateAuthority) return;

        if (IsPicked && _respawnTimer.Expired(Runner))
        {
            RPC_Respawn();
        }
    }

    private void SetupVisual()
    {
        if (_craftItemSO == null || _visualRoot == null) return;
        if (_craftItemSO.visualPrefab == null) return;

        foreach (Transform child in _visualRoot)
            Destroy(child.gameObject);

        Instantiate(_craftItemSO.visualPrefab, _visualRoot);
    }

    private void SetVisualActive(bool active)
    {
        // Activar/desactivar renderers cacheados
        if (_cachedRenderers != null)
        {
            foreach (var rend in _cachedRenderers)
            {
                if (rend != null)
                    rend.enabled = active;
            }
        }

        // VFX
        if (_vfxController != null)
        {
            if (active) _vfxController.RestoreVFX();
            else _vfxController.HideVFX();
        }
    }

    private void SetupVFX()
    {
        if (_craftItemSO == null || _craftItemSO.vfxConfig == null) return;

        _vfxController = gameObject.AddComponent<PickupVFXController>();
        _vfxController.Initialize(_visualRoot, _craftItemSO.vfxConfig);
    }

    public bool TryMarkPicked()
    {
        if (_localPicked) return false;
        if (IsPicked) return false;

        _localPicked = true;

        if (_collider != null)
            _collider.enabled = false;

        return true;
    }

    public void ShowFeedback(string message)
    {
        if (_txtFeedback == null) return;

        _txtFeedback.text = message;
        _txtFeedback.gameObject.SetActive(true);

        if (_feedbackCoroutine != null)
            StopCoroutine(_feedbackCoroutine);

        _feedbackCoroutine = StartCoroutine(HideFeedback());
    }

    private IEnumerator HideFeedback()
    {
        yield return new WaitForSeconds(3f);
        if (_txtFeedback != null)
            _txtFeedback.gameObject.SetActive(false);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_RequestPickup()
    {
        if (IsPicked) return;
        if (Object == null || !Object.IsValid) return;

        IsPicked = true;
        _respawnTimer = TickTimer.CreateFromSeconds(Runner, _respawnTime);
        RPC_SetPickedVisual(true);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_SetPickedVisual(bool picked)
    {
        SetVisualActive(!picked);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_Respawn()
    {
        IsPicked = false;
        _localPicked = false;

        if (_collider != null)
            _collider.enabled = true;

        SetVisualActive(true);
    }

    public void NotifyPickedForQuest()
    {
        if (!string.IsNullOrEmpty(_questTrackId))
            TrackEvents.OnTrackEvent?.Invoke(_questTrackId, 1);
    }

    //[Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    //public void RPC_RequestDespawn()
    //{
    //    if (IsPicked) return;
    //    IsPicked = true;

    //    if (Object == null || !Object.IsValid) return;

    //    if (_vfxController != null)
    //        _vfxController.DestroyVFX();

    //    Runner.Despawn(Object);
    //}
}