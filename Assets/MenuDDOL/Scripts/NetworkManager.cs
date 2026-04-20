using Fusion;
using Fusion.Sockets;
using UnityEngine;
using System.Threading.Tasks;
using System.Collections.Generic;

public class NetworkManager : MonoBehaviour, INetworkRunnerCallbacks
{
    public static NetworkManager Instance;
    public NetworkRunner runner;

    [SerializeField] private NetworkObject playerPrefab;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private NetworkRunner GetRunner()
    {
        if (runner == null)
        {
            GameObject obj = new GameObject("NetworkRunner");
            runner = obj.AddComponent<NetworkRunner>();
            obj.AddComponent<NetworkSceneManagerDefault>();
            runner.ProvideInput = true;
            runner.AddCallbacks(this);  // NetworkManager escucha los callbacks
            DontDestroyOnLoad(obj);
        }
        return runner;
    }

    public async Task StartHost(string roomName)
    {
        NetworkRunner r = GetRunner();
        await r.StartGame(new StartGameArgs()
        {
            GameMode = GameMode.Shared, 
            SessionName = roomName,
            Scene = SceneRef.FromIndex(2)
        });
    }

    public async Task JoinGame(string roomName)
    {
        NetworkRunner r = GetRunner();
        await r.StartGame(new StartGameArgs()
        {
            GameMode = GameMode.Shared,  
            SessionName = roomName,
            Scene = SceneRef.FromIndex(2)
        });
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (player == runner.LocalPlayer)
        {
            Vector3 spawnPos = new Vector3(Random.Range(-3f, 3f), 1f, Random.Range(-3f, 3f));

            // En Shared Mode DEBES pasar inputAuthority explícitamente
            NetworkObject spawnedPlayer = runner.Spawn(
                playerPrefab,
                spawnPos,
                Quaternion.identity,
                inputAuthority: player 
            );

            Debug.Log($"[NetworkManager] Jugador spawneado: {spawnedPlayer.name}, InputAuthority: {spawnedPlayer.InputAuthority}");
        }
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
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
}