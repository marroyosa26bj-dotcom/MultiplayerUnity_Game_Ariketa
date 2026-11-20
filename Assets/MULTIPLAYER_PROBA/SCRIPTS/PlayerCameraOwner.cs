
using Unity.Netcode;
using UnityEngine;

public class PlayerCameraOwner : NetworkBehaviour
{
    [SerializeField] private GameObject cameraRig;

    public override void OnNetworkSpawn()
    {
        if (cameraRig != null)
        {
            cameraRig.SetActive(IsOwner);
        }
    }
}
