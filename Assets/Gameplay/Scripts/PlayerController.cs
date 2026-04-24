using UnityEngine;
using Fusion;

public class PlayerController : NetworkBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float sprintSpeed = 8f;
    [SerializeField] private float jumpForce = 5f;
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private float maxLookAngle = 89f;

    [Header("References")]
    [SerializeField] private CharacterController characterController;
    [SerializeField] private Transform cameraHolder;

    [Networked] public float CurrentHealth { get; set; }
    [Networked] public bool IsAlive { get; set; }

    private Vector3 _currentVelocity;
    private float _gravity = -9.81f;

    // --- VARIABLES DE ROTACIÓN ---
    private float _yaw;   // Rotación horizontal (cuerpo)
    private float _pitch; // Rotación vertical (cámara)

    private GameState _gameState;

    public override void Spawned()
    {
        if (Object.HasStateAuthority)
        {
            CurrentHealth = 100f;
            IsAlive = true;
        }

        if (characterController == null) characterController = GetComponent<CharacterController>();
        _gameState = GameState.Instance;

        // Desactivamos el CC para los "proxies" (los otros jugadores en tu pantalla)
        bool isProxy = !HasStateAuthority && !HasInputAuthority;
        if (characterController != null) characterController.enabled = !isProxy;

        // Cámara solo para el jugador local
        Camera cam = GetComponentInChildren<Camera>();
        if (cam != null) cam.gameObject.SetActive(HasInputAuthority);

        if (HasInputAuthority) Cursor.lockState = CursorLockMode.Locked;
    }

    // --- 1. LECTURA DEL RATÓN (Fluidez de FPS) ---
    private void Update()
    {
        if ((HasStateAuthority || HasInputAuthority) && IsAlive)
        {
            _yaw += Input.GetAxis("Mouse X") * mouseSensitivity;
            _pitch -= Input.GetAxis("Mouse Y") * mouseSensitivity;
            _pitch = Mathf.Clamp(_pitch, -maxLookAngle, maxLookAngle);
        }
    }

    // --- 2. SIMULACIÓN DE RED (Sincronización a Ticks) ---
    public override void FixedUpdateNetwork()
    {
        if (!IsAlive || characterController == null || !characterController.enabled) return;

        // APLICAMOS LA ROTACIÓN A LA RED (Evita que el NetworkTransform te bloquee)
        if (HasStateAuthority || HasInputAuthority)
        {
            transform.rotation = Quaternion.Euler(0, _yaw, 0);
        }

        // LECTURA DEL TECLADO PARA MOVERSE
        if (GetInput(out NetworkInputData input))
        {
            HandleMovement(input);
        }
    }

    private void HandleMovement(NetworkInputData input)
    {
        float currentSpeed = input.Sprint ? sprintSpeed : moveSpeed;
        Vector2 move = Vector2.ClampMagnitude(input.MoveInput, 1f);

        // El transform.forward ahora es preciso porque acabamos de aplicar el _yaw
        Vector3 moveDirection = (transform.forward * move.y + transform.right * move.x).normalized;
        Vector3 horizontalVelocity = moveDirection * currentSpeed;

        if (characterController.isGrounded && _currentVelocity.y < 0f)
            _currentVelocity.y = -2f;

        _currentVelocity.y += _gravity * Runner.DeltaTime;

        if (input.Jump && characterController.isGrounded)
            _currentVelocity.y = jumpForce;

        Vector3 finalVelocity = new Vector3(horizontalVelocity.x, _currentVelocity.y, horizontalVelocity.z);
        characterController.Move(finalVelocity * Runner.DeltaTime);
    }

    // --- 3. VISUALES DE LA CÁMARA ---
    public override void Render()
    {
        if ((HasStateAuthority || HasInputAuthority) && IsAlive && cameraHolder != null)
        {
            cameraHolder.localRotation = Quaternion.Euler(_pitch, 0, 0);
        }
    }

    // --- SISTEMA DE DAÑO Y MUERTE ---
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

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_TakeDamage(float damage) => TakeDamage(damage);

    private void Die()
    {
        IsAlive = false;
        if (characterController != null) characterController.enabled = false;

        if (_gameState != null)
            _gameState.RecordKill(Object.InputAuthority, Object.InputAuthority, "Suicide");

        if (HasInputAuthority) Cursor.lockState = CursorLockMode.None;
    }

    public void Respawn(Vector3 spawnPosition)
    {
        if (!HasStateAuthority) return;

        CurrentHealth = 100f;
        IsAlive = true;

        // Apagamos el CC antes de teletransportar para evitar bugs de físicas
        if (characterController != null) characterController.enabled = false;

        var nt = GetComponent<NetworkTransform>();
        if (nt != null) nt.Teleport(spawnPosition);
        else transform.position = spawnPosition;

        if (characterController != null) characterController.enabled = true;

        if (_gameState != null) _gameState.RespawnPlayer(Object.InputAuthority);

        if (HasInputAuthority) Cursor.lockState = CursorLockMode.Locked;
        else RPC_OnRespawn();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.InputAuthority)]
    private void RPC_OnRespawn() => Cursor.lockState = CursorLockMode.Locked;

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (HasInputAuthority) Cursor.lockState = CursorLockMode.None;
    }
}