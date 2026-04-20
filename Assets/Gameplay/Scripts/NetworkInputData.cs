using Fusion;
using UnityEngine;

public struct NetworkInputData : INetworkInput
{
    public Vector2 MoveInput;
    public float MouseX;
    public float MouseY;
    public NetworkBool Jump;
    public NetworkBool Sprint;
}