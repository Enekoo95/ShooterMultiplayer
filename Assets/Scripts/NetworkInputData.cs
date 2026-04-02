using UnityEngine;
using Fusion;

/// <summary>
/// Estructura de datos para input de red en Fusion.
/// Se usa para sincronizar inputs entre cliente y servidor.
/// </summary>
public struct NetworkInputData : INetworkInput
{
    public Vector2 moveInput;
    public Vector2 lookDelta;
    public bool isSprinting;
    public bool isJumping;
    public bool isFiring;
}
