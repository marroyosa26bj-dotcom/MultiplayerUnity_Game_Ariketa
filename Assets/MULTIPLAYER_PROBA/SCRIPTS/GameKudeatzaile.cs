using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using TMPro;

public class GameKudeatzaile : NetworkBehaviour
{
    [SerializeField] private SessionManager sessionManager;
    [SerializeField] private TMP_InputField joinCode;
    [SerializeField] private TMP_InputField playerNameInput;

    [SerializeField] private Transform hostSpawnPoint;
    [SerializeField] private Transform clientSpawnPoint_1;
    [SerializeField] private Transform clientSpawnPoint_2;


    private string pendingPlayerName;
    private bool waitingForLocalPlayerName;
    private readonly HashSet<ulong> positionedPlayerObjects = new();
    private bool spawnPointsErrorLogged;

    [SerializeField] GameObject hasiera_Canvas;

    [SerializeField] GameObject izena_Beharra_Abisu;

    void Start()
    {
        hasiera_Canvas.SetActive(true);
        izena_Beharra_Abisu.SetActive(false);
    }

    private void Update()
    {
        TrySetLocalPlayerName();
        PositionConnectedPlayers();
    }

    private void PositionConnectedPlayers()
    {
        NetworkManager manager = NetworkManager.Singleton;
        if (manager == null || !manager.IsServer)
            return;

        if (hostSpawnPoint == null || clientSpawnPoint_1 == null || clientSpawnPoint_2 == null)
        {
            if (!spawnPointsErrorLogged)
            {
                Debug.LogError("GameKudeatzaile: Assign the host and both client spawn points.", this);
                spawnPointsErrorLogged = true;
            }

            return;
        }

        foreach (NetworkClient client in manager.ConnectedClientsList)
        {
            NetworkObject playerObject = client.PlayerObject;
            if (playerObject == null || positionedPlayerObjects.Contains(playerObject.NetworkObjectId))
                continue;

            if (!playerObject.TryGetComponent(out NetworkPlayerMovement playerMovement))
            {
                Debug.LogError("The player prefab is missing NetworkPlayerMovement.", playerObject);
                positionedPlayerObjects.Add(playerObject.NetworkObjectId);
                continue;
            }

            if (client.ClientId == NetworkManager.ServerClientId)
            {
                playerMovement.SetInitialSpawnPosition(
                    hostSpawnPoint.position,
                    hostSpawnPoint.rotation
                );
            }
            else
            {
                Vector3 firstPoint = clientSpawnPoint_1.position;
                Vector3 secondPoint = clientSpawnPoint_2.position;
                Vector3 randomPosition = new(
                    UnityEngine.Random.Range(Mathf.Min(firstPoint.x, secondPoint.x), Mathf.Max(firstPoint.x, secondPoint.x)),
                    firstPoint.y,
                    UnityEngine.Random.Range(Mathf.Min(firstPoint.z, secondPoint.z), Mathf.Max(firstPoint.z, secondPoint.z))
                );

                playerMovement.SetInitialSpawnPosition(randomPosition, clientSpawnPoint_1.rotation);
            }

            positionedPlayerObjects.Add(playerObject.NetworkObjectId);
        }
    }

    public async void StartHost()
    {
        if (!TryGetPlayerName())
            return;

        if (sessionManager == null)
        {
            Debug.LogError("GameKudeatzaile: SessionManager is not assigned.", this);
            return;
        }

        waitingForLocalPlayerName = true;
        hasiera_Canvas.SetActive(false);

        try
        {
            await sessionManager.StartSessionAsHost();
            TrySetLocalPlayerName();
            
        }
        catch (Exception exception)
        {
            waitingForLocalPlayerName = false;
            hasiera_Canvas.SetActive(true);
            Debug.LogException(exception, this);
        }
    }

    public async void StartClient()
    {
        if (!TryGetPlayerName())
            return;

        if (sessionManager == null || joinCode == null)
        {
            Debug.LogError("GameKudeatzaile: SessionManager or join-code input is not assigned.", this);
            return;
        }

        waitingForLocalPlayerName = true;
        hasiera_Canvas.SetActive(false);

        try
        {
            string code = joinCode.text.Trim();
            Debug.Log($"Joining session with code: {code}");
            await sessionManager.StartSessionAsClient(code);
            TrySetLocalPlayerName();
           
        }
        catch (Exception exception)
        {
            waitingForLocalPlayerName = false;
            hasiera_Canvas.SetActive(true);
            Debug.LogException(exception, this);
        }
    }

    private bool TryGetPlayerName()
    {
        if (playerNameInput == null)
        {
            Debug.LogError("GameKudeatzaile: Player-name input is not assigned.", this);
            return false;
        }

        pendingPlayerName = playerNameInput.text.Trim();
        if (string.IsNullOrEmpty(pendingPlayerName))
        {
            Debug.LogWarning("Enter a player name before hosting or joining a session.", this);
            playerNameInput.ActivateInputField();

            izena_Beharra_Abisu.SetActive(true);

            return false;
        }

        return true;
    }


    private void TrySetLocalPlayerName()
    {
        if (!waitingForLocalPlayerName)
            return;

        NetworkManager manager = NetworkManager.Singleton;
        if (manager == null || !manager.IsClient || manager.LocalClient == null)
            return;

        NetworkObject playerObject = manager.LocalClient.PlayerObject;
        if (playerObject == null)
            return;

        PlayerIzena playerIzena = playerObject.GetComponent<PlayerIzena>();
        if (playerIzena == null)
        {
            Debug.LogError("The local Player object is missing the PlayerIzena component.", playerObject);
            waitingForLocalPlayerName = false;
            return;
        }

        playerIzena.SetMyName(pendingPlayerName);
        waitingForLocalPlayerName = false;
    }

    private void OnEnable()
    {
        if (playerNameInput != null)
            playerNameInput.onValueChanged.AddListener(OnPlayerNameInputChanged);
    }

    // Propagates the local edit to the server; other clients only receive it via the NetworkVariable label.
    private void OnPlayerNameInputChanged(string newName)
    {
        pendingPlayerName = newName;

        NetworkManager manager = NetworkManager.Singleton;
        if (manager == null || !manager.IsClient || manager.LocalClient == null)
            return;

        NetworkObject playerObject = manager.LocalClient.PlayerObject;
        if (playerObject != null && playerObject.TryGetComponent(out PlayerIzena playerIzena))
            playerIzena.SetMyName(newName);
    }

    private void OnDisable()
    {
        if (playerNameInput != null)
            playerNameInput.onValueChanged.RemoveListener(OnPlayerNameInputChanged);
    }
}