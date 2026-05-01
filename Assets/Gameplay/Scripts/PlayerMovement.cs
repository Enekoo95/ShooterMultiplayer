using UnityEngine;
using Fusion;

[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : NetworkBehaviour
{
    [Header("Movimiento")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float sprintSpeed = 9f;
    [SerializeField] private float crouchSpeed = 2.5f;

    [Header("Salto y Gravedad")]
    [SerializeField] private float jumpHeight = 1.2f;
    [SerializeField] private float gravity = -20f; // Sugerido -20 para mejor sensación

    [Header("Camara FPS")]
    [SerializeField] private Transform cameraHolder;
    [SerializeField] private float sensX = 2f;
    [SerializeField] private float sensY = 2f;
    [SerializeField] private float maxLookAngle = 85f;

    [Header("Crouch")]
    [SerializeField] private float normalHeight = 2f;
    [SerializeField] private float crouchHeight = 1f;
    [SerializeField] private float crouchTransitionSpeed = 8f;

    [Header("Ground Check")]
    [SerializeField] private float groundCheckDistance = 0.3f; // Aumentado para fiabilidad
    [SerializeField] private LayerMask groundMask;

    [Networked] private NetworkBool IsGrounded { get; set; }
    [Networked] private NetworkBool IsCrouching { get; set; }
    [Networked] public float NetworkedYaw { get; set; }
    [Networked] public float NetworkedPitch { get; set; }

    private CharacterController cc;
    private Vector3 horizontalVelocity;
    private Vector3 verticalVelocity;
    private Vector3 cameraInitialLocalPos;

    public override void Spawned()
    {
        cc = GetComponent<CharacterController>();
        cameraInitialLocalPos = cameraHolder.localPosition;

        if (HasInputAuthority)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            if (cameraHolder != null)
                cameraHolder.GetComponentInChildren<Camera>()?.gameObject.SetActive(true);
        }
        else
        {
            if (cameraHolder != null)
                cameraHolder.GetComponentInChildren<Camera>()?.gameObject.SetActive(false);
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!GetInput(out PlayerInput input)) return;

        // Reset local pos para el crouch lerp visual si es necesario
        if (cameraHolder != null)
        {
            cameraHolder.localPosition = cameraInitialLocalPos;
        }

        GroundCheck();
        HandleCrouch(input);
        HandleLook(input);
        HandleMovement(input);
        HandleJump(input);
        ApplyGravity();
        MoveCharacter();
    }

    private void GroundCheck()
    {
        // Calculamos la base del CharacterController de forma precisa
        Vector3 spherePos = transform.position + cc.center + (Vector3.down * (cc.height / 2f));

        // El radio debe ser suficiente para superar el 'Skin Width' del CC
        IsGrounded = Physics.CheckSphere(spherePos, groundCheckDistance, groundMask, QueryTriggerInteraction.Ignore);
    }

    private void HandleLook(PlayerInput input)
    {
        NetworkedYaw += input.LookDelta.x * sensX;
        NetworkedPitch -= input.LookDelta.y * sensY;
        NetworkedPitch = Mathf.Clamp(NetworkedPitch, -maxLookAngle, maxLookAngle);

        transform.rotation = Quaternion.Euler(0f, NetworkedYaw, 0f);

        if (cameraHolder != null)
            cameraHolder.localRotation = Quaternion.Euler(NetworkedPitch, 0f, 0f);
    }

    private void HandleMovement(PlayerInput input)
    {
        float speed = IsCrouching ? crouchSpeed
                    : input.Sprint ? sprintSpeed
                    : walkSpeed;

        Vector3 move = transform.right * input.Move.x + transform.forward * input.Move.y;

        if (move.magnitude > 1f) move.Normalize();

        horizontalVelocity = move * speed;
    }

    private void HandleJump(PlayerInput input)
    {
        // Solo saltar si el input es verdadero Y estamos en el suelo
        if (input.Jump && IsGrounded && !IsCrouching)
        {
            verticalVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);

            // Forzamos el estado de suelo a falso para evitar impulsos dobles
            IsGrounded = false;
        }
    }

    private void ApplyGravity()
    {
        if (IsGrounded && verticalVelocity.y < 0f)
        {
            // Pequeña fuerza negativa para mantener el contacto con el suelo
            verticalVelocity.y = -2f;
        }
        else
        {
            verticalVelocity.y += gravity * Runner.DeltaTime;
        }
    }

    private void MoveCharacter()
    {
        // Combinamos ambas velocidades y aplicamos el movimiento
        cc.Move((horizontalVelocity + verticalVelocity) * Runner.DeltaTime);
    }

    private void HandleCrouch(PlayerInput input)
    {
        IsCrouching = input.Crouch;

        float previousHeight = cc.height;
        float targetHeight = IsCrouching ? crouchHeight : normalHeight;

        float newHeight = Mathf.Lerp(previousHeight, targetHeight, crouchTransitionSpeed * Runner.DeltaTime);
        cc.height = newHeight;

        // Ajustamos el centro para que el personaje no se hunda o flote al agacharse
        float heightDifference = newHeight - previousHeight;
        cc.center += new Vector3(0f, heightDifference / 2f, 0f);
    }

    private void OnDrawGizmosSelected()
    {
        if (cc == null) cc = GetComponent<CharacterController>();

        Gizmos.color = IsGrounded ? Color.green : Color.red;
        Vector3 p = transform.position + cc.center + (Vector3.down * (cc.height / 2f));
        Gizmos.DrawWireSphere(p, groundCheckDistance);
    }
}