using UnityEngine;
using Fusion;
using System.Collections;
using TMPro;

// Componente item que puede ser recogido
public class PickupableItem : NetworkBehaviour
{
    [SerializeField] private ItemSO itemDataSO;
    [SerializeField] private Transform _visualRoot;
    [SerializeField] private TextMeshProUGUI _txtFeedback;

    [Networked] private NetworkBool IsPicked { get; set; }

    private Collider _collider;
    private bool _localPicked;
    private PickupVFXController _vfxController;
    private Coroutine _feedbackCoroutine;
    public ItemSO ItemDataSO => itemDataSO;

    public override void Spawned()
    {
        _collider = GetComponent<Collider>();
        SetupVisual();
        SetupVFX();

        if (_txtFeedback != null)
            _txtFeedback.gameObject.SetActive(false);
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

    private void SetupVisual()
    {
        if (itemDataSO == null) return;

        // Limpiar hijos
        foreach (Transform child in _visualRoot)
            Destroy(child.gameObject);

        if (itemDataSO.isDualWield)
        {
            Instantiate(itemDataSO.rightHandPrefab, _visualRoot);
            Instantiate(itemDataSO.leftHandPrefab, _visualRoot);
        }
        else if (itemDataSO.equipPrefab != null)
        {
            Instantiate(itemDataSO.equipPrefab, _visualRoot);
        }
    }

    public void ShowFeedback(string message)
    {
        Debug.Log($"[Feedback] mensaje='{message}' | txtFeedback null={_txtFeedback == null} | gameObject={gameObject.name}");
        if (_txtFeedback == null) return;
        _txtFeedback.text = message;
        _txtFeedback.gameObject.SetActive(true);
        if (_feedbackCoroutine != null)
            StopCoroutine(_feedbackCoroutine);
        _feedbackCoroutine = StartCoroutine(HideFeedback());
    }

    private void SetupVFX()
    {
        if (itemDataSO == null || itemDataSO.vfxConfig == null) return;

        // Crear el controlador de VFX
        _vfxController = gameObject.AddComponent<PickupVFXController>();
        _vfxController.Initialize(_visualRoot, itemDataSO.vfxConfig);
    }
    public ItemData ItemData => new ItemData // Datos que se envían al inventario (NetworkArray)
    {
        id = itemDataSO != null ? itemDataSO.id : 0,
        type = itemDataSO != null ? itemDataSO.type : ItemType.Consumable
    };

    private void Reset() // Asegura que el collider sea trigger si fue agregado al prefab
    {
        var col = GetComponent<Collider>();
        if (col != null)
            col.isTrigger = true;
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
        if (IsPicked) return;         //NUEVO: guard en el servidor
        IsPicked = true;

        if (Object == null || !Object.IsValid) return;

        if (_vfxController != null)
        {
            _vfxController.DestroyVFX();
        }

        Runner.Despawn(Object);
    }
}