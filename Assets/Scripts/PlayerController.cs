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
    [SerializeField] private float groundDrag = 5f;
    [SerializeField] private float airDrag = 2f;
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private float maxLookAngle = 89f;

    [SerializeField] private CharacterController characterController;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Transform cameraHolder;

    // Velocidad y gravedad
    private Vector3 currentVelocity = Vector3.zero;
    private float gravity = -9.81f;
    private bool isGrounded = false;

    // Input
    private Vector2 moveInput = Vector2.zero;
    private float verticalLookRotation = 0f;

    // Red
    [Networked] public float CurrentHealth { get; set; } = 100f;
    [Networked] public bool IsAlive { get; set; } = true;
    [Networked] public NetworkId PlayerId { get; set; }

    private Rigidbody rb;
    private GameState gameState;

    public override void OnInput(NetworkRunner runner, NetworkInput input)
    {
        if (!HasInputAuthority)
            return;

        // Capturar input
        moveInput = Vector2.zero;
        if (Input.GetKey(KeyCode.W)) moveInput.y += 1;
        if (Input.GetKey(KeyCode.S)) moveInput.y -= 1;
        if (Input.GetKey(KeyCode.A)) moveInput.x -= 1;
        if (Input.GetKey(KeyCode.D)) moveInput.x += 1;

        moveInput = Vector2.ClampMagnitude(moveInput, 1f);

        bool isSprinting = Input.GetKey(KeyCode.LeftShift);
        bool isJumping = Input.GetKeyDown(KeyCode.Space);
        bool isFiring = Input.GetMouseButton(0);

        // Raycasting para groundcheck (si tienes rigidbody con raycast)
        isGrounded = Physics.Raycast(transform.position, Vector3.down, 0.1f);

        // Enviar input al servidor
        var data = new NetworkInputData();
        data.moveInput = moveInput;
        data.isSprinting = isSprinting;
        data.isJumping = isJumping && isGrounded;
        data.isFiring = isFiring;
        data.lookDelta = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));

        input.Set(data);
    }

    public override void OnFixedInput()
    {
        if (!HasInputAuthority)
            return;

        // Aquí se procesa el input después de OnInput
    }

    public override void FixedUpdateNetwork()
    {
        if (!IsAlive || !characterController.enabled)
            return;

        if (HasInputAuthority)
        {
            HandleInput();
            HandleCamera();
            HandleMovement();
        }

        // Sincronizar posición para otros clientes
        if (HasInputAuthority && IsValid)
        {
            // Transform se sincroniza automáticamente si el NetworkBehaviour lo requiere
        }
    }

    private void HandleInput()
    {
        moveInput.x = (Input.GetKey(KeyCode.D) ? 1 : 0) - (Input.GetKey(KeyCode.A) ? 1 : 0);
        moveInput.y = (Input.GetKey(KeyCode.W) ? 1 : 0) - (Input.GetKey(KeyCode.S) ? 1 : 0);
        moveInput = Vector2.ClampMagnitude(moveInput, 1f);
    }

    private void HandleCamera()
    {
        if (!playerCamera)
            return;

        // Rotación horizontal (cuerpo)
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        transform.Rotate(Vector3.up * mouseX);

        // Rotación vertical (cámara, limitada)
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;
        verticalLookRotation -= mouseY;
        verticalLookRotation = Mathf.Clamp(verticalLookRotation, -maxLookAngle, maxLookAngle);

        if (cameraHolder)
        {
            cameraHolder.localRotation = Quaternion.Euler(verticalLookRotation, 0, 0);
        }
    }

    private void HandleMovement()
    {
        if (!characterController)
        {
            Debug.LogError("[PlayerController] CharacterController no asignado");
            return;
        }

        // Velocidad horizontal
        float currentSpeed = (Input.GetKey(KeyCode.LeftShift) && isGrounded) ? sprintSpeed : moveSpeed;
        Vector3 moveDirection = (transform.forward * moveInput.y + transform.right * moveInput.x).normalized;
        Vector3 horizontalVelocity = moveDirection * currentSpeed;

        // Aplicar gravedad
        if (isGrounded && currentVelocity.y < 0)
        {
            currentVelocity.y = -2f; // Mantener pegado al suelo
        }
        else
        {
            currentVelocity.y += gravity * Time.deltaTime;
        }

        // Salto
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            currentVelocity.y = jumpForce;
        }

        // Combinar velocidades
        Vector3 finalVelocity = new Vector3(horizontalVelocity.x, currentVelocity.y, horizontalVelocity.z);

        // Aplicar movimiento
        characterController.Move(finalVelocity * Time.deltaTime);
    }

    /// <summary>
    /// Aplicar daño al jugador.
    /// </summary>
    public void TakeDamage(float damage)
    {
        if (!IsAlive)
            return;

        CurrentHealth -= damage;

        if (CurrentHealth <= 0)
        {
            CurrentHealth = 0;
            Die();
        }
    }

    /// <summary>
    /// Muere el jugador.
    /// </summary>
    private void Die()
    {
        IsAlive = false;
        characterController.enabled = false;

        // Notificar al GameState
        if (GameState.Instance)
        {
            GameState.Instance.DamagePlayer(Runner.LocalPlayer, 100f);
        }

        Debug.Log($"[PlayerController] {gameObject.name} ha muerto");
    }

    /// <summary>
    /// Respawnear el jugador.
    /// </summary>
    public void Respawn(Vector3 spawnPosition)
    {
        CurrentHealth = 100f;
        IsAlive = true;
        characterController.enabled = true;
        characterController.Move(spawnPosition - transform.position);
        transform.position = spawnPosition;

        if (GameState.Instance)
        {
            GameState.Instance.RespawnPlayer(Runner.LocalPlayer);
        }

        Debug.Log($"[PlayerController] {gameObject.name} ha reaparecido en {spawnPosition}");
    }

    private void Start()
    {
        if (!characterController)
            characterController = GetComponent<CharacterController>();

        if (!playerCamera)
            playerCamera = GetComponentInChildren<Camera>();

        if (!cameraHolder)
            cameraHolder = playerCamera?.transform;

        gameState = GameState.Instance;
    }

    public override void OnEnable()
    {
        base.OnEnable();
        if (HasInputAuthority)
        {
            Cursor.lockState = CursorLockMode.Locked;
        }
    }

    public override void OnDisable()
    {
        base.OnDisable();
        Cursor.lockState = CursorLockMode.None;
    }
}

/// <summary>
/// Estructura de input de red.
/// </summary>
public struct NetworkInputData : INetworkInput
{
    public Vector2 moveInput;
    public Vector2 lookDelta;
    public bool isSprinting;
    public bool isJumping;
    public bool isFiring;
}
