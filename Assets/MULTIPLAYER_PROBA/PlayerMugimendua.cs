using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMugimendua : NetworkBehaviour
{
    [SerializeField] private PlayerInput playerInput;
    [SerializeField] private CharacterController characterController;
    [SerializeField, Min(0f)] private float moveSpeed = 5f;
    [SerializeField, Min(0f)] private float gravity = 20f;

    private InputAction movementAction;
    private float verticalSpeed;

    private void Awake()
    {
        if (playerInput == null)
        {
            Debug.LogError("PlayerMugimendua: PlayerInput is not assigned.", this);
            enabled = false;
            return;
        }

        if (characterController == null)
        {
            Debug.LogError("PlayerMugimendua: CharacterController is not assigned.", this);
            enabled = false;
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        characterController.enabled = IsOwner;
        if (IsOwner)
        {
            playerInput.ActivateInput();
            movementAction = playerInput.currentActionMap?.FindAction("Move");
            if (movementAction == null)
                Debug.LogError(
                    $"PlayerMugimendua: Action 'Move' was not found in the active map '{playerInput.currentActionMap?.name ?? "(none)"}'.",
                    this);
        }
        else
        {
            playerInput.DeactivateInput();
            movementAction = null;
        }

        Debug.Log(
            $"PlayerMugimendua spawned: IsOwner={IsOwner}, OwnerClientId={OwnerClientId}, " +
            $"LocalClientId={NetworkManager.LocalClientId}, ActionMap={playerInput.currentActionMap?.name ?? "(none)"}, " +
            $"InputActive={playerInput.inputIsActive}, MoveEnabled={movementAction?.enabled ?? false}.",
            this);
    }

    private void Update()
    {
        if (!IsOwner || movementAction == null || !characterController.enabled)
            return;

        Vector2 input = movementAction.ReadValue<Vector2>();
        Vector3 direction = transform.right * input.x + transform.forward * input.y;
        direction = Vector3.ProjectOnPlane(direction, Vector3.up);
        direction = Vector3.ClampMagnitude(direction, 1f);

        if (characterController.isGrounded && verticalSpeed < 0f)
            verticalSpeed = -2f;
        else
            verticalSpeed -= gravity * Time.deltaTime;

        Vector3 velocity = direction * moveSpeed;
        velocity.y = verticalSpeed;
        characterController.Move(velocity * Time.deltaTime);
    }

    public override void OnNetworkDespawn()
    {
        if (IsOwner && playerInput != null)
            playerInput.DeactivateInput();

        base.OnNetworkDespawn();
    }
}
