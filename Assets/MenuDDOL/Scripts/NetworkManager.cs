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

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);
    }

    public async Task StartHost(string roomName) => await StartGame(GameMode.Shared, roomName);
    public async Task JoinGame(string roomName) => await StartGame(GameMode.Shared, roomName);

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
            Scene = SceneRef.FromIndex(2), // Tu escena de juego
            SceneManager = runner.GetComponent<NetworkSceneManagerDefault>()
        });
    }

    public void OnSceneLoadDone(NetworkRunner runner)
    {
        // Buscar y ordenar puntos de spawn por nombre
        GameObject[] points = GameObject.FindGameObjectsWithTag("Respawn");
        if (points.Length > 0)
        {
            Array.Sort(points, (a, b) => string.Compare(a.name, b.name));
            spawnPoints = new Transform[points.Length];
            for (int i = 0; i < points.Length; i++) spawnPoints[i] = points[i].transform;
        }

        if (runner.LocalPlayer != PlayerRef.None) SpawnLocalPlayer(runner);
    }

    private void SpawnLocalPlayer(NetworkRunner runner)
    {
        int index = Math.Abs(runner.LocalPlayer.PlayerId) % (spawnPoints?.Length ?? 1);
        Vector3 pos = (spawnPoints != null) ? spawnPoints[index].position + Vector3.up * 0.2f : Vector3.up;
        Quaternion rot = (spawnPoints != null) ? spawnPoints[index].rotation : Quaternion.identity;

        runner.Spawn(playerPrefab, pos, rot, runner.LocalPlayer);
    }

    // Callbacks obligatorios vacíos
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) { }
    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
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