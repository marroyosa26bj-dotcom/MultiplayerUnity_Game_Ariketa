
using UnityEngine;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Barreras")]
    [SerializeField] private GameObject[] barrierVisuals;
    [SerializeField] private Collider[] barrierColliders;

    [SerializeField] private Collider zona_A;
    [SerializeField] private Collider zona_B;

    [SerializeField] TMP_Text Names;
    [SerializeField] TMP_Text Scores;


    // Indica si la partida ha comenzado.
    // Solo el servidor puede modificar su valor.
    public NetworkVariable<bool> GameStarted = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private void Awake()
    {
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        GameStarted.OnValueChanged += OnGameStartedChanged;

        // Aplicamos el estado actual de las barreras.
        ApplyBarrierState(GameStarted.Value);
    }

    public override void OnNetworkDespawn()
    {
        GameStarted.OnValueChanged -= OnGameStartedChanged;
    }

    // Este método solo puede iniciarlo el servidor.
    public void StartGame()
    {
        if (!IsServer)
            return;

        if (GameStarted.Value)
            return;

        GameStarted.Value = true;

        Debug.Log("¡LA PARTIDA HA COMENZADO!");
    }

    private void OnGameStartedChanged(bool previousValue, bool newValue)
    {
        ApplyBarrierState(newValue);
    }

    private void ApplyBarrierState(bool gameStarted)
    {
        // Si la partida ha comenzado, las barreras se abren.
        bool barriersOpen = gameStarted;

        foreach (GameObject visual in barrierVisuals)
        {
            if (visual != null)
                visual.SetActive(!barriersOpen);
        }

        foreach (Collider barrier in barrierColliders)
        {
            if (barrier != null)
                barrier.enabled = !barriersOpen;
        }
    }
}
