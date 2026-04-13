using UnityEngine;
using Fusion;

/// <summary>
/// Controlador del jugador: movimiento, cámara (FPS) y sincronización en red.
/// Cada jugador tiene autoridad sobre su propio PlayerController.
/// </summary>
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
    private bool isGrounded = false;

    private Vector2 moveInput = Vector2.zero;
    private float verticalLookRotation = 0f;

    [Networked] public float CurrentHealth { get; set; } = 100f;
    [Networked] public bool IsAlive { get; set; } = true;

    private GameState gameState;

    private void Start()
    {
        if (!characterController)
            characterController = GetComponent<CharacterController>();

        if (!cameraHolder)
            cameraHolder = GetComponentInChildren<Camera>()?.transform;

        gameState = GameState.Instance;

        if (HasInputAuthority)
        {
            Cursor.lockState = CursorLockMode.Locked;
        }
    }

    private void Update()
    {
        if (!HasInputAuthority || !IsAlive)
            return;

        isGrounded = characterController.isGrounded;

        moveInput.x = Input.GetAxisRaw("Horizontal");
        moveInput.y = Input.GetAxisRaw("Vertical");
        moveInput = Vector2.ClampMagnitude(moveInput, 1f);
    
        HandleCamera();

        if (Input.GetKeyDown(KeyCode.R))
        {
            // Recarga: por ejemplo, no hace nada por ahora
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasInputAuthority || !IsAlive)
            return;

        HandleMovement();
    }


    private void HandleCamera()
    {
        if (cameraHolder == null)
            return;

        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        transform.Rotate(Vector3.up * mouseX);

        verticalLookRotation -= mouseY;
        verticalLookRotation = Mathf.Clamp(verticalLookRotation, -maxLookAngle, maxLookAngle);

        cameraHolder.localRotation = Quaternion.Euler(verticalLookRotation, 0f, 0f);
    }

    private void HandleMovement()
    {
        if (characterController == null)
            return;

        float currentSpeed = Input.GetKey(KeyCode.LeftShift) ? sprintSpeed : moveSpeed;
        Vector3 moveDirection = (transform.forward * moveInput.y + transform.right * moveInput.x).normalized;

        Vector3 horizontalVelocity = moveDirection * currentSpeed;

        if (characterController.isGrounded && currentVelocity.y < 0)
            currentVelocity.y = -2f;

        currentVelocity.y += gravity * Time.deltaTime;

        if (Input.GetKeyDown(KeyCode.Space) && characterController.isGrounded)
            currentVelocity.y = jumpForce;

        Vector3 finalVelocity = new Vector3(horizontalVelocity.x, currentVelocity.y, horizontalVelocity.z);

        characterController.Move(finalVelocity * Time.deltaTime);
    }

    public void TakeDamage(float damage)
    {
        if (!IsAlive)
            return;

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

        Debug.Log($"[PlayerController] {gameObject.name} ha muerto");

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
