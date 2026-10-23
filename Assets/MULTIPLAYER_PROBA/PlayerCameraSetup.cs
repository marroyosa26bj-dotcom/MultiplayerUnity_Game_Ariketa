
using Unity.Netcode;
using Unity.Cinemachine;
using UnityEngine;

public class PlayerCameraSetup : NetworkBehaviour
{
    [SerializeField] private CinemachineCamera playerCamera;
    [SerializeField] private Transform cameraTarget;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            playerCamera.gameObject.SetActive(false);
            return;
        }

        playerCamera.Follow = cameraTarget;
        playerCamera.LookAt = cameraTarget;
    }
}