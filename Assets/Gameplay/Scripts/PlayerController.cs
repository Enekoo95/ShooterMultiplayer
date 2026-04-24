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

    // Necesario para teleportar limpiamente en red
    private NetworkTransform networkTransform;

    // Sustituto de Start() - Inicializamos basados en contexto de red
    public override void Spawned()
    {
        CurrentHealth = 100f;
        IsAlive = true;

        characterController = GetComponent<CharacterController>();
        if (characterController == null) characterController = GetComponentInChildren<CharacterController>();

        networkTransform = GetComponent<NetworkTransform>();
        cameraHolder = GetComponentInChildren<Camera>()?.transform.parent;

        // LÓGICA DE AUTORIDAD VITAL:
        // isProxy = true significa que no somos ni el Host (StateAuthority) ni el dueño del jugador (InputAuthority).
        // Solo apagamos el CharacterController en los Proxies para que NetworkTransform asuma el control visual
        // y no haya choques de físicas fantasma. El Host SÍ necesita el CC encendido para calcular colisiones.
        bool isProxy = !HasStateAuthority && !HasInputAuthority;

        if (characterController != null)
        {
            characterController.enabled = !isProxy;
        }

        Camera cam = GetComponentInChildren<Camera>();
        if (cam != null)
        {
            // La cámara SOLO se activa para el jugador que controla este avatar
            cam.gameObject.SetActive(HasInputAuthority);
        }

        if (HasInputAuthority)
        {
            Cursor.lockState = CursorLockMode.Locked;
        }
    }

    private void Update()
    {
        if (!HasInputAuthority || !IsAlive)
            return;

        HandleCamera();
    }

    // Sustituto de FixedUpdate() - Toda la simulación ocurre aquí
    public override void FixedUpdateNetwork()
    {
        if (!IsAlive) return;

        // GetInput devuelve true tanto para el Cliente local como para el Host
        if (GetInput(out NetworkInputData input))
        {
            HandleMovement(input);
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
        if (characterController == null || !characterController.enabled)
            return;

        float currentSpeed = input.Sprint ? sprintSpeed : moveSpeed;
        Vector2 move = Vector2.ClampMagnitude(input.MoveInput, 1f);
        Vector3 moveDirection = (transform.forward * move.y + transform.right * move.x).normalized;
        Vector3 horizontalVelocity = moveDirection * currentSpeed;

        if (characterController.isGrounded && currentVelocity.y < 0f)
            currentVelocity.y = -2f; // Mantenerse bien pegado al suelo

        currentVelocity.y += gravity * Runner.DeltaTime;

        if (input.Jump && characterController.isGrounded)
            currentVelocity.y = jumpForce;

        Vector3 finalVelocity = new Vector3(horizontalVelocity.x, currentVelocity.y, horizontalVelocity.z);

        // Esta línea ahora sí funciona en el Host gracias a la corrección en Spawned()
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

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_TakeDamage(float damage)
    {
        TakeDamage(damage);
    }

    private void Die()
    {
        IsAlive = false;

        if (characterController != null)
            characterController.enabled = false;

        if (HasInputAuthority)
            Cursor.lockState = CursorLockMode.None;
    }

    public void Respawn(Vector3 spawnPosition)
    {
        if (!HasStateAuthority) return;

        CurrentHealth = 100f;
        IsAlive = true;

        // REGLA DE UNITY + FUSION: Para teletransportar un CC, hay que usar NetworkTransform o apagar/encender el CC.
        if (characterController != null) characterController.enabled = false;

        if (networkTransform != null)
        {
            networkTransform.Teleport(spawnPosition); // Teletransporte seguro de red
        }
        else
        {
            transform.position = spawnPosition;
        }

        if (characterController != null) characterController.enabled = true;

        if (HasInputAuthority)
            Cursor.lockState = CursorLockMode.Locked;
        else
            RPC_OnRespawn();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.InputAuthority)]
    private void RPC_OnRespawn()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }

    // Sustituto de OnDestroy() - Limpieza segura al despawnear
    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (HasInputAuthority)
        {
            Cursor.lockState = CursorLockMode.None;
        }
    }
}