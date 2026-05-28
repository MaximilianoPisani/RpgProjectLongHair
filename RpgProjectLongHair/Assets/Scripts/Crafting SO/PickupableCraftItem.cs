using UnityEngine;
using Fusion;
using TMPro;
using System.Collections;

public class PickupableCraftItem : NetworkBehaviour
{
    [SerializeField] private CraftItemSO _craftItemSO;
    [SerializeField] private Transform _visualRoot;
    [SerializeField] private TextMeshProUGUI _txtFeedback;

    [Networked] private NetworkBool IsPicked { get; set; }

    private Collider _collider;
    private bool _localPicked;
    private Coroutine _feedbackCoroutine;
    private PickupVFXController _vfxController;
    private GameObject _spawnedMinimapIcon;


    [Header("Minimap")]
    [SerializeField] private GameObject _minimapIconPrefab;
    [SerializeField] private Vector3 _iconOffset = new Vector3(0, 3f, 0);
    [SerializeField] private Vector3 _iconRotation;

    public CraftItemSO CraftItemSO => _craftItemSO;
    public int ItemId => _craftItemSO != null ? _craftItemSO.id : 0;

    public override void Spawned()
    {
        _collider = GetComponent<Collider>();

        SetupVisual();
        SetupVFX();
        SpawnMinimapIcon();

        if (_txtFeedback != null)
            _txtFeedback.gameObject.SetActive(false);
    }

    private void SetupVisual()
    {
        if (_craftItemSO == null || _visualRoot == null) return;
        if (_craftItemSO.visualPrefab == null) return;

        foreach (Transform child in _visualRoot)
            Destroy(child.gameObject);

        Instantiate(_craftItemSO.visualPrefab, _visualRoot);
    }

    private void SpawnMinimapIcon()
    {
        if (_minimapIconPrefab == null) return;

        _spawnedMinimapIcon = Instantiate(
            _minimapIconPrefab,
            transform
        );

        _spawnedMinimapIcon.transform.localPosition = _iconOffset;

        _spawnedMinimapIcon.transform.localRotation =
            Quaternion.Euler(_iconRotation);
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
    public void RPC_RequestDespawn()
    {
        if (IsPicked) return;
        IsPicked = true;

        if (Object == null || !Object.IsValid) return;

        if (_vfxController != null)
            _vfxController.DestroyVFX();

        if (_spawnedMinimapIcon != null)
        {
            Destroy(_spawnedMinimapIcon);
        }

        Runner.Despawn(Object);
    }
}