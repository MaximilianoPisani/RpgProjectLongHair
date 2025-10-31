using UnityEngine;
//using Unity.Cinemachine;   
using Fusion;

public class LocalCameraSpawner : MonoBehaviour
{
    [SerializeField] private GameObject _camPrefab;    // prefab CinemachineVirtualCamera 
    [SerializeField] private Transform _cameraTarget; // prefab CameraTarget dentro del player

    void Start()
    {
        if (!GetComponent<NetworkObject>().HasInputAuthority) return;

        GameObject newCam = Instantiate(_camPrefab);
        //var cmc = newCam.GetComponent<CinemachineCamera>();
        //cmc.Follow = _cameraTarget;
        //cmc.LookAt = _cameraTarget;
    }
}