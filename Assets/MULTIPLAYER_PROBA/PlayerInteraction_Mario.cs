using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;

public class PlayerInteraction : NetworkBehaviour
{
    void Update()
    {
        if (!IsOwner) return;

        if (Keyboard.current != null &&
            Keyboard.current.eKey.wasPressedThisFrame)
        {
            ScoreManager.Instance.AddPointRpc();
        }
    }
    
}