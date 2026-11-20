
using UnityEngine;
using Unity.Netcode;

public class PlayerAppearance : NetworkBehaviour
{
    [Header("Materiales")]
    [SerializeField] private Material hostMaterial;
    [SerializeField] private Material clientMaterial;
    [SerializeField] private Material frozenMaterial;

    [Header("Partes que cambiarán de material")]
    [SerializeField] private Renderer[] renderersToChange;

    private NetworkPlayerMovement playerMovement;
    private PlayerModelSelector playerModelSelector;

    public override void OnNetworkSpawn()
    {
        playerMovement = GetComponent<NetworkPlayerMovement>();
        playerModelSelector = GetComponent<PlayerModelSelector>();

        if (playerMovement != null)
        {
            playerMovement.IsFrozen.OnValueChanged += OnFrozenChanged;
        }

        if (playerModelSelector != null)
        {
            playerModelSelector.ModelInitialized += OnModelInitialized;
        }

        UpdateMaterial();
    }

    public override void OnNetworkDespawn()
    {
        if (playerMovement != null)
        {
            playerMovement.IsFrozen.OnValueChanged -= OnFrozenChanged;
        }

        if (playerModelSelector != null)
        {
            playerModelSelector.ModelInitialized -= OnModelInitialized;
        }
    }

    private void OnFrozenChanged(bool previousValue, bool newValue)
    {
        UpdateMaterial();
    }

    private void OnModelInitialized(GameObject selectedModel)
    {
        UpdateMaterial();
    }

    private void UpdateMaterial()
    {
        bool isHostPlayer =
            OwnerClientId == NetworkManager.ServerClientId;

        Material selectedMaterial;

        if (playerMovement != null &&
            playerMovement.IsFrozen.Value &&
            !isHostPlayer)
        {
            selectedMaterial = frozenMaterial;
        }
        else if (isHostPlayer)
        {
            selectedMaterial = hostMaterial;
        }
        else
        {
            selectedMaterial = clientMaterial;
        }

        Renderer[] renderers = renderersToChange;

        if (playerModelSelector != null)
        {
            if (playerModelSelector.SelectedModel == null)
                return;

            renderers =
                playerModelSelector.SelectedModel
                    .GetComponentsInChildren<Renderer>(true);
        }

        if (renderers == null)
            return;

        foreach (Renderer rend in renderers)
        {
            if (rend != null)
            {
                rend.material = selectedMaterial;
            }
        }
    }
}
