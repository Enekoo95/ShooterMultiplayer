using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Fusion;
using Fusion.Sockets;

public class InputProvider : MonoBehaviour, INetworkRunnerCallbacks
{
    [Header("Input Actions")]
    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private InputActionReference lookAction;
    [SerializeField] private InputActionReference jumpAction;
    [SerializeField] private InputActionReference sprintAction;
    [SerializeField] private InputActionReference crouchAction;

    private NetworkRunner runner;

    // Acumulamos el delta del raton entre ticks para no perder movimiento
    private Vector2 _accumulatedLook;

    private void Awake()
    {
        runner = GetComponent<NetworkRunner>();
        if (runner == null)
            runner = FindFirstObjectByType<NetworkRunner>();

        if (runner != null)
            runner.AddCallbacks(this);
    }

    private void OnEnable()
    {
        moveAction?.action.Enable();
        lookAction?.action.Enable();
        jumpAction?.action.Enable();
        sprintAction?.action.Enable();
        crouchAction?.action.Enable();
    }

    private void OnDisable()
    {
        moveAction?.action.Disable();
        lookAction?.action.Disable();
        jumpAction?.action.Disable();
        sprintAction?.action.Disable();
        crouchAction?.action.Disable();
    }

    // Update acumula el delta del raton cada frame (mas preciso que leerlo en el tick)
    private void Update()
    {
        if (lookAction != null)
            _accumulatedLook += lookAction.action.ReadValue<Vector2>();
    }

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        var data = new PlayerInput
        {
            Move = moveAction != null ? moveAction.action.ReadValue<Vector2>() : Vector2.zero,
            LookDelta = _accumulatedLook,
            Jump = jumpAction != null && jumpAction.action.IsPressed(),
            Sprint = sprintAction != null && sprintAction.action.IsPressed(),
            Crouch = crouchAction != null && crouchAction.action.IsPressed(),
        };

        _accumulatedLook = Vector2.zero; // Resetea despues de enviar
        input.Set(data);
    }

    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) { }
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
}