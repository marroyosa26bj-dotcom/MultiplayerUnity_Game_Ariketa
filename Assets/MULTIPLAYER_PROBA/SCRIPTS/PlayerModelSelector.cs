
using System;
using UnityEngine;
using Unity.Netcode;

public class PlayerModelSelector : NetworkBehaviour
{
    [Header("Modelos 3D")]
    [SerializeField] private GameObject hostModel;
    [SerializeField] private GameObject clientModel;

    private Animator currentAnimator;

    public Animator CurrentAnimator => currentAnimator;
    public GameObject SelectedModel { get; private set; }
    public event Action<GameObject> ModelInitialized;

    protected override void OnNetworkPostSpawn()
    {
        InitializeModel();
    }

    public Animator InitializeModel()
    {
        if (hostModel == null || clientModel == null)
        {
            Debug.LogError(
                "PlayerModelSelector needs both host and client models assigned.",
                this
            );
            return null;
        }

        bool isHostPlayer =
            OwnerClientId == NetworkManager.ServerClientId;

        GameObject selectedModel = isHostPlayer
            ? hostModel
            : clientModel;
        GameObject unusedModel = isHostPlayer
            ? clientModel
            : hostModel;

        SelectedModel = selectedModel;
        selectedModel.SetActive(true);
        unusedModel.SetActive(false);

        currentAnimator =
            selectedModel.GetComponentInChildren<Animator>(true);

        if (currentAnimator == null)
        {
            Debug.LogError(
                "No Animator was found in the selected model " +
                selectedModel.name + ".",
                selectedModel
            );
        }

        ModelInitialized?.Invoke(selectedModel);
        return currentAnimator;
    }
}
