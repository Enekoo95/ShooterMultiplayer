using UnityEngine;
using Fusion;
using Fusion.Sockets;
using System.Collections.Generic;
using System.Collections;

/// <summary>
/// Gestiona el spawn de jugadores cuando se conectan a la sala.
/// Se ejecuta en el servidor (MasterClient).
/// </summary>
public class SpawnManager : MonoBehaviour, INetworkRunnerCallbacks
{
    [SerializeField] private NetworkObject playerPrefab;
    [SerializeField] private Transform[] spawnPoints;
    private int id;

    private void Start()
    {
        NetworkManager.Instance.runner.AddCallbacks(this);
    }

    private void OnDestroy()
    {
        if (NetworkManager.Instance != null && NetworkManager.Instance.runner != null)
            NetworkManager.Instance.runner.RemoveCallbacks(this);
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (!runner.IsServer) return;

        if (playerPrefab == null)
        {
            Debug.LogError("[SpawnManager] playerPrefab ES NULL - NO ESTÁ ASIGNADO EN EL INSPECTOR");
            return;
        }

        int index = player.PlayerId % spawnPoints.Length;
        Vector3 spawnPosition = spawnPoints[index].position;

        Debug.Log($"[SpawnManager] ANTES DE SPAWN - Prefab: {playerPrefab.name}, Posición: {spawnPosition}");
        NetworkObject spawnedObject = runner.Spawn(playerPrefab, spawnPosition, Quaternion.identity, player);

        Debug.Log($"[SpawnManager] SPAWN EJECUTADO - Objeto: {spawnedObject.gameObject.name}");

        if (GameState.Instance != null)
        {
            GameState.Instance.RegisterPlayer(player, "playerName");
        }

        Debug.Log($"[SpawnManager] Jugador {player.PlayerId} spawneado en {spawnPosition}");
    }

 

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"[SpawnManager] Jugador {player.PlayerId} se desconectó");
        if (runner.IsServer && GameState.Instance != null)
        {
            // opcional: limpiar datos de GameState
        }
    }

    public void OnInput(NetworkRunner runner, NetworkInput input) { }
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
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, System.ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }

    private void OnDrawGizmos()
    {
        if (spawnPoints == null)
            return;

        Gizmos.color = Color.green;
        foreach (Transform spawnPoint in spawnPoints)
        {
            if (spawnPoint != null)
            {
                Gizmos.DrawSphere(spawnPoint.position, 0.5f);
                Gizmos.DrawLine(spawnPoint.position, spawnPoint.position + spawnPoint.forward * 2f);
            }
        }
    }
}
