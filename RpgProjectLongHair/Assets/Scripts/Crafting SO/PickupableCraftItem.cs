using UnityEngine;
using Fusion;
using TMPro;
using System.Collections;

public class PickupableCraftItem : NetworkBehaviour
{
    [SerializeField] private CraftItemSO _craftItemSO;
    [SerializeField] private Transform _visualRoot;
    [SerializeField] private TextMeshProUGUI _txtFeedback;

    public CraftItemSO CraftItemSO => _craftItemSO;
    public int ItemId => _craftItemSO != null ? _craftItemSO.id : 0;

    public override void Spawned()
    {
        SetupVisual();
        if (_txtFeedback != null)
        {
            _txtFeedback.gameObject.SetActive(false);
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

    public void ShowFeedback(string message)
    {
        if (_txtFeedback == null) return;
        _txtFeedback.text = message;
        _txtFeedback.gameObject.SetActive(true);

        if (_feedbackCoroutine != null)
            StopCoroutine(_feedbackCoroutine);
        _feedbackCoroutine = StartCoroutine(HideFeedback());
    }

    private Coroutine _feedbackCoroutine;

    private IEnumerator HideFeedback()
    {
        yield return new WaitForSeconds(3f);
        if (_txtFeedback != null)
            _txtFeedback.gameObject.SetActive(false);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_RequestDespawn()
    {
        if (Object == null || !Object.IsValid) return;
        Runner.Despawn(Object);
    }
}