using UnityEngine;
using Fusion;

public struct PlayerInput : INetworkInput
{
    public Vector2 Move;     
    public Vector2 LookDelta; 
    public NetworkBool Jump;
    public NetworkBool Sprint;
    public NetworkBool Crouch;
}