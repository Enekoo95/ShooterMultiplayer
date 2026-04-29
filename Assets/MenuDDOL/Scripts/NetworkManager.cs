using Fusion;
using Fusion.Sockets;
using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;

public class NetworkManager : MonoBehaviour, INetworkRunnerCallbacks
{
    public static NetworkManager Instance;
    public NetworkRunner runner;

    [SerializeField] private NetworkObject playerPrefab;
    private Transform[] spawnPoints;
    private Dictionary<PlayerRef, NetworkObject> _spawnedPlayers = new Dictionary<PlayerRef, NetworkObject>();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
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
        }

        await runner.StartGame(new StartGameArgs()
        {
            GameMode = mode,
            SessionName = roomName,
            Scene = SceneRef.FromIndex(2),
            SceneManager = runner.GetComponent<NetworkSceneManagerDefault>()
        });
    }

    // ESTO ES LO QUE HACE QUE EL MOVIMIENTO SE SINCRONICE
    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        var data = new NetworkInputData();
        data.MoveInput = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
        data.Jump = Input.GetKey(KeyCode.Space);
        data.Sprint = Input.GetKey(KeyCode.LeftShift);
        input.Set(data);
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (runner.IsServer)
        {
            int index = Math.Abs(player.PlayerId) % (spawnPoints?.Length ?? 1);
            Vector3 pos = (spawnPoints != null) ? spawnPoints[index].position + Vector3.up * 1.5f : Vector3.up * 1.5f;

            NetworkObject playerObj = runner.Spawn(playerPrefab, pos, Quaternion.identity, player);
            _spawnedPlayers.Add(player, playerObj);
        }
    }

    public void RespawnPlayer(PlayerRef player, Vector3 position)
    {
        if (runner.IsServer && _spawnedPlayers.TryGetValue(player, out NetworkObject obj))
        {
            obj.GetComponent<PlayerController>().Respawn(position);
        }
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        if (runner.IsServer && _spawnedPlayers.TryGetValue(player, out NetworkObject obj))
        {
            runner.Despawn(obj);
            _spawnedPlayers.Remove(player);
        }
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

    // Callbacks obligatorios (vacíos pero necesarios para la interfaz)
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