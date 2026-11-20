
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

[RequireComponent(typeof(PlayerInput))]
public class NetworkPlayerMovement : NetworkBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationSpeed = 180f;
    [SerializeField] private float sprintSpeed = 9f;

    // Solo el servidor puede modificar este estado.
    public NetworkVariable<bool> IsFrozen = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private PlayerInput playerInput;

    private bool isHostPlayer;
    private float currentMoveSpeed;

    [Header("Animation")]
    [SerializeField] private float animationDampTime = 0.1f;

    private PlayerModelSelector modelSelector;
    private Animator animator;

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        currentMoveSpeed = moveSpeed;
    }

    public override void OnNetworkSpawn()
    {
        isHostPlayer =
            OwnerClientId == NetworkManager.ServerClientId;

        SetPlayerLayer(isHostPlayer ? "Armiarma" : "Iheslariak");

        playerInput.enabled = IsOwner && !IsFrozen.Value;

        IsFrozen.OnValueChanged += OnFrozenChanged;

        modelSelector = GetComponent<PlayerModelSelector>();
    }

    private void SetPlayerLayer(string layerName)
    {
        int layer = LayerMask.NameToLayer(layerName);

        if (layer == -1)
        {
            Debug.LogError(
                $"The layer '{layerName}' does not exist in the project.",
                this
            );
            return;
        }

        foreach (Transform child in GetComponentsInChildren<Transform>(true))
            child.gameObject.layer = layer;
    }

    public override void OnNetworkDespawn()
    {
        IsFrozen.OnValueChanged -= OnFrozenChanged;
    }

    public void SetInitialSpawnPosition(Vector3 position, Quaternion rotation)
    {
        if (!IsServer)
        {
            Debug.LogError(
                "Only the server can set a player's initial spawn position.",
                this
            );
            return;
        }

        SetInitialSpawnPositionRpc(position, rotation);
    }

    [Rpc(SendTo.Owner)]
    private void SetInitialSpawnPositionRpc(
        Vector3 position,
        Quaternion rotation)
    {
        if (IsOwner)
            transform.SetPositionAndRotation(position, rotation);
    }

    private void OnFrozenChanged(bool previousValue, bool newValue)
    {
        if (IsOwner)
        {
            playerInput.enabled = !newValue;

            if (newValue)
                UpdateAnimations(Vector2.zero, false);
        }
    }

    private void Update()
    {
        if (!IsOwner)
            return;

        if (isHostPlayer &&
            playerInput.actions["HasiJokoa"].WasPressedThisFrame())
        {
            GameManager.Instance.StartGame();
        }

        if (IsFrozen.Value)
        {
            UpdateAnimations(Vector2.zero, false);
            return;
        }

        Vector2 input =
            playerInput.actions["Move"].ReadValue<Vector2>();

        bool isSprinting =
            playerInput.actions["Sprint"].IsPressed();

        currentMoveSpeed = isSprinting ? sprintSpeed : moveSpeed;

        transform.Rotate(
            Vector3.up * input.x * rotationSpeed * Time.deltaTime
        );

        transform.position +=
            transform.forward * input.y *
            currentMoveSpeed * Time.deltaTime;

        UpdateAnimations(input, isSprinting);

        if (playerInput.actions["Interact"].WasPressedThisFrame())
            RequestAddPointRpc();
    }

    private void UpdateAnimations(Vector2 input, bool isSprinting)
    {
        if (animator == null)
        {
            if (modelSelector == null)
                return;

            animator = modelSelector.CurrentAnimator;

            if (animator == null)
                return;
        }

        bool isMoving = Mathf.Abs(input.y) > 0.01f;

        bool isWalking = isMoving && !isSprinting;
        bool isRunning = isMoving && isSprinting;

        animator.SetBool("IsWalking", isWalking);
        animator.SetBool("IsSprinting", isRunning);
        animator.SetBool("IsIdle", !isMoving);
    }

    private void OnCollisionEnter(Collision collision)
    {
        HandleLayerCollision(collision.gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("DeathZone"))
            return;

        if (other.gameObject.layer == LayerMask.NameToLayer("Point_Zone"))
        {
            if (IsOwner)
            {
                if (IsServer)
                    AwardPointZoneScore();
                else
                    RequestPointZoneScoreRpc();
            }

            return;
        }

        HandleLayerCollision(other.gameObject);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    private void RequestPointZoneScoreRpc(
        RpcParams rpcParams = default)
    {
        if (rpcParams.Receive.SenderClientId != OwnerClientId)
            return;

        AwardPointZoneScore();
    }

    private void AwardPointZoneScore()
    {
        if (!IsServer ||
            gameObject.layer != LayerMask.NameToLayer("Iheslariak"))
            return;

        if (!TryGetComponent(out PlayerIzena playerIzena))
        {
            Debug.LogError(
                "Player is missing the PlayerIzena component.",
                this
            );
            return;
        }

        if (ScoreManager.Instance == null)
        {
            Debug.LogError(
                "ScoreManager is not available to record the leaderboard point.",
                this
            );
            return;
        }

        if (string.IsNullOrWhiteSpace(playerIzena.PlayerName))
        {
            Debug.LogWarning(
                "Cannot add a leaderboard point before the player's name is set.",
                this
            );
            return;
        }

        ScoreManager.Instance.AddPlayerPoint(
            OwnerClientId,
            playerIzena.PlayerName
        );
    }

    private void HandleLayerCollision(GameObject other)
    {
        if (!IsOwner)
            return;

        NetworkObject otherNetworkObject =
            other.GetComponentInParent<NetworkObject>();

        if (otherNetworkObject == null)
            return;

        NetworkObjectReference otherReference =
            new NetworkObjectReference(otherNetworkObject);

        if (IsServer)
        {
            ApplyLayerCollision(otherNetworkObject.gameObject);
        }
        else
        {
            ReportLayerCollisionRpc(otherReference);
        }
    }

    [Rpc(SendTo.Server)]
    private void ReportLayerCollisionRpc(
        NetworkObjectReference otherReference,
        RpcParams rpcParams = default)
    {
        if (rpcParams.Receive.SenderClientId != OwnerClientId)
            return;

        if (!otherReference.TryGet(out NetworkObject otherNetworkObject))
            return;

        ApplyLayerCollision(otherNetworkObject.gameObject);
    }

    private void ApplyLayerCollision(GameObject other)
    {
        if (!IsServer)
            return;

        if (!IsFrozen.Value &&
            other.layer == LayerMask.NameToLayer("Armiarma"))
        {
            IsFrozen.Value = true;
        }
        else if (IsFrozen.Value &&
                 other.layer == LayerMask.NameToLayer("Iheslariak"))
        {
            IsFrozen.Value = false;
        }
    }

    [Rpc(SendTo.Server)]
    private void RequestAddPointRpc()
    {
        if (IsFrozen.Value)
            return;

        if (ScoreManager.Instance == null)
            return;

        ScoreManager.Instance.AddScore(1);
    }
}
