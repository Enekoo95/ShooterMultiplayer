using UnityEngine;
using Fusion;

public class PlayerController : NetworkBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float sprintSpeed = 8f;
    [SerializeField] private float jumpForce = 5f;
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private float maxLookAngle = 89f;

    [SerializeField] private CharacterController characterController;
    [SerializeField] private Transform cameraHolder;

    private Vector3 currentVelocity = Vector3.zero;
    private float gravity = -9.81f;
    private float verticalLookRotation = 0f;

    [Networked] public float CurrentHealth { get; set; }
    [Networked] public bool IsAlive { get; set; }

    private GameState gameState;

    public override void Spawned()
    {
        CurrentHealth = 100f;
        IsAlive = true;

        // Busca explícitamente en hijos
        characterController = GetComponentInChildren<CharacterController>();
        cameraHolder = GetComponentInChildren<Camera>()?.transform.parent;

        gameState = GameState.Instance;

        Debug.Log($"[PlayerController] Spawned - HasInputAuthority: {HasInputAuthority}");
        Debug.Log($"[PlayerController] CharacterController: {characterController}");
        Debug.Log($"[PlayerController] CameraHolder: {cameraHolder}");

        if (HasInputAuthority)
            Cursor.lockState = CursorLockMode.Locked;
    }

    private void Update()
    {
        if (!HasInputAuthority || !IsAlive)
            return;

        HandleCamera();
    }

    public override void FixedUpdateNetwork()
    {
        if (!IsAlive) return;

        if (GetInput(out NetworkInputData input))
        {
            Debug.Log($"[PlayerController] Input recibido: {input.MoveInput}");
            HandleMovement(input);
        }
        else
        {
            Debug.LogWarning($"[PlayerController] GetInput falló. HasInputAuthority: {HasInputAuthority}");
        }
    }

    private void HandleCamera()
    {
        if (cameraHolder == null) return;

        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        transform.Rotate(Vector3.up * mouseX);

        verticalLookRotation -= mouseY;
        verticalLookRotation = Mathf.Clamp(verticalLookRotation, -maxLookAngle, maxLookAngle);
        cameraHolder.localRotation = Quaternion.Euler(verticalLookRotation, 0f, 0f);
    }

    private void HandleMovement(NetworkInputData input)
    {
        if (characterController == null)
        {
            Debug.LogWarning("[PlayerController] CharacterController es null.");
            return;
        }

        float currentSpeed = input.Sprint ? sprintSpeed : moveSpeed;
        Vector2 move = Vector2.ClampMagnitude(input.MoveInput, 1f);
        Vector3 moveDirection = (transform.forward * move.y + transform.right * move.x).normalized;
        Vector3 horizontalVelocity = moveDirection * currentSpeed;

        if (characterController.isGrounded && currentVelocity.y < 0f)
            currentVelocity.y = -2f;

        currentVelocity.y += gravity * Runner.DeltaTime;

        if (input.Jump && characterController.isGrounded)
            currentVelocity.y = jumpForce;

        Vector3 finalVelocity = new Vector3(horizontalVelocity.x, currentVelocity.y, horizontalVelocity.z);
        characterController.Move(finalVelocity * Runner.DeltaTime);
    }

    public void TakeDamage(float damage)
    {
        if (!HasStateAuthority || !IsAlive) return;

        CurrentHealth -= damage;
        if (CurrentHealth <= 0f)
        {
            CurrentHealth = 0f;
            Die();
        }
    }

    private void Die()
    {
        IsAlive = false;
        if (characterController != null)
            characterController.enabled = false;

        if (gameState != null)
            gameState.RecordKill(Runner.LocalPlayer, Runner.LocalPlayer, "Suicide");

        if (HasInputAuthority)
            Cursor.lockState = CursorLockMode.None;
    }

    public void Respawn(Vector3 spawnPosition)
    {
        CurrentHealth = 100f;
        IsAlive = true;

        if (characterController != null)
            characterController.enabled = true;

        transform.position = spawnPosition;

        if (gameState != null)
            gameState.RespawnPlayer(Runner.LocalPlayer);

        if (HasInputAuthority)
            Cursor.lockState = CursorLockMode.Locked;
    }
}   