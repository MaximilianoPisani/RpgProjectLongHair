using UnityEngine;
using Unity.Cinemachine; 
using Fusion;

public class PlayerCameraSetup : NetworkBehaviour
{
    [SerializeField] private GameObject _cameraParentPrefab; 

    private void Start()
    {
        if (!HasInputAuthority) return;

        if (_cameraParentPrefab == null)
        {
            Debug.LogWarning("[PlayerCameraSetup] Camera prefab not assigned!");
            return;
        }

        GameObject camInstance = Instantiate(_cameraParentPrefab);
        camInstance.transform.SetParent(null); 
        camInstance.name = $"VCamLocalPlayer_{gameObject.name}";

        camInstance.SetActive(true);


        var vCam = camInstance.GetComponentInChildren<CinemachineVirtualCamera>();
        if (vCam != null)
        {

        }
    }
}