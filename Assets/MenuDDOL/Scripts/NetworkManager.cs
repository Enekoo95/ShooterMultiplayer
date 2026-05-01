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
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
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

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        var data = new PlayerInput();
        data.Move = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
        data.Jump = Input.GetKey(KeyCode.Space);
        data.Sprint = Input.GetKey(KeyCode.LeftShift);
        data.Shoot = Input.GetMouseButton(0);
        data.Reload = Input.GetKey(KeyCode.R);
        data.CycleWeapon = Input.GetKeyDown(KeyCode.Q);
        input.Set(data);
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (runner.IsServer)
        {
            // Buscar spawnpoints si aún no los tenemos
            if (spawnPoints == null || spawnPoints.Length == 0)
            {
                GameObject[] points = GameObject.FindGameObjectsWithTag("Respawn");
                if (points.Length > 0)
                {
                    Array.Sort(points, (a, b) => string.Compare(a.name, b.name));
                    spawnPoints = new Transform[points.Length];
                    for (int i = 0; i < points.Length; i++)
                        spawnPoints[i] = points[i].transform;
                }
            }

            Vector3 pos = Vector3.zero;
            if (spawnPoints != null && spawnPoints.Length > 0)
            {
                int index = Math.Abs(player.PlayerId) % spawnPoints.Length;
                pos = spawnPoints[index].position;
                Debug.Log($"[NetworkManager] Spawneando jugador {player.PlayerId} en {pos}");
            }
            else
            {
                Debug.LogWarning("[NetworkManager] No hay SpawnPoints con tag 'Respawn'");
            }

            NetworkObject playerObj = runner.Spawn(playerPrefab, pos, Quaternion.identity, player);
            _spawnedPlayers[player] = playerObj;
        }

        // FIX: registrar el nombre del jugador local en GameState
        // Solo el jugador local sabe su propio nombre
        if (player == runner.LocalPlayer)
        {
            // Esperar un frame para que GameState esté listo
            StartCoroutine(RegisterLocalPlayer(player));
        }
    }

    private System.Collections.IEnumerator RegisterLocalPlayer(PlayerRef player)
    {
        // Esperar hasta que GameState esté disponible
        float timeout = 5f;
        while (GameState.Instance == null && timeout > 0f)
        {
            timeout -= Time.deltaTime;
            yield return null;
        }

        if (GameState.Instance == null)
        {
            Debug.LogError("[NetworkManager] GameState no encontrado tras esperar.");
            yield break;
        }

        string name = "Jugador";
        if (PlayerProfileManager.Instance != null && !string.IsNullOrEmpty(PlayerProfileManager.Instance.playerName))
            name = PlayerProfileManager.Instance.playerName;
        else
            name = "Jugador" + player.PlayerId;

        GameState.Instance.RegisterPlayer(player, name);
        Debug.Log($"[NetworkManager] Jugador local registrado como: {name}");
    }

    public void RespawnPlayer(PlayerRef player, Vector3 position)
    {
        if (runner.IsServer && _spawnedPlayers.TryGetValue(player, out NetworkObject obj))
            obj.GetComponent<PlayerController>().Respawn(position);
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
            for (int i = 0; i < points.Length; i++)
                spawnPoints[i] = points[i].transform;
        }
    }

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