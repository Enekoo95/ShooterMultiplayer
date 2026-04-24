using Fusion;
using Fusion.Sockets;
using UnityEngine;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;

public class NetworkManager : MonoBehaviour, INetworkRunnerCallbacks
{
    public static NetworkManager Instance;
    public NetworkRunner runner;

    [SerializeField] private NetworkObject playerPrefab;
    private Transform[] spawnPoints;

    private Dictionary<PlayerRef, NetworkObject> spawnedPlayers = new Dictionary<PlayerRef, NetworkObject>();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);
    }

    public async Task StartHost(string roomName) => await StartGame(GameMode.Host, roomName);
    public async Task JoinGame(string roomName) => await StartGame(GameMode.Client, roomName);

    private async Task StartGame(GameMode mode, string roomName)
    {
        if (runner == null)
        {
            GameObject obj = new GameObject("NetworkRunner");
            runner = obj.AddComponent<NetworkRunner>();
            obj.AddComponent<NetworkSceneManagerDefault>();
            runner.AddCallbacks(this);
            DontDestroyOnLoad(obj);
        }

        await runner.StartGame(new StartGameArgs()
        {
            GameMode = mode,
            SessionName = roomName,
            Scene = SceneRef.FromIndex(2),
            SceneManager = runner.GetComponent<NetworkSceneManagerDefault>()
        });
    }

    public void OnSceneLoadDone(NetworkRunner runner)
    {
        GameObject[] points = GameObject.FindGameObjectsWithTag("Respawn");
        if (points.Length > 0)
        {
            Array.Sort(points, (a, b) => string.Compare(a.name, b.name));
            spawnPoints = new Transform[points.Length];
            for (int i = 0; i < points.Length; i++) spawnPoints[i] = points[i].transform;
        }
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (runner.IsServer)
        {
            SpawnPlayer(runner, player);
        }
    }

    private void SpawnPlayer(NetworkRunner runner, PlayerRef player)
    {
        int index = Math.Abs(player.PlayerId) % (spawnPoints?.Length ?? 1);

        // Aumentado ligeramente el offset a 2.0f para asegurar que nace por encima del suelo y cae suavemente.
        Vector3 pos = (spawnPoints != null) ? spawnPoints[index].position + Vector3.up * 2.0f : Vector3.up * 2.0f;
        Quaternion rot = (spawnPoints != null) ? spawnPoints[index].rotation : Quaternion.identity;

        NetworkObject playerObj = runner.Spawn(playerPrefab, pos, rot, player);
        spawnedPlayers[player] = playerObj;
    }

    public void RespawnPlayer(PlayerRef player, Vector3 position)
    {
        if (!runner.IsServer) return;

        if (spawnedPlayers.TryGetValue(player, out NetworkObject obj))
        {
            var controller = obj.GetComponent<PlayerController>();
            if (controller != null)
                controller.Respawn(position);
        }
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        if (runner.IsServer && spawnedPlayers.TryGetValue(player, out NetworkObject obj))
        {
            runner.Despawn(obj); // Ciclo de vida correcto
            spawnedPlayers.Remove(player);
        }
    }

    // Callbacks obligatorios vacíos...
    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
}