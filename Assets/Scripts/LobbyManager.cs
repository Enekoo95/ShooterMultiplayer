using UnityEngine;
using Fusion;
using System.Collections.Generic;

/// <summary>
/// Gestor de lobby: listar salas, crear, unirse.
/// Se ejecuta en el menú/lobby antes de entrar al juego.
/// </summary>
public class LobbyManager : MonoBehaviour
{
    public static LobbyManager Instance { get; private set; }

    [SerializeField] private NetworkRunner networkRunner;
    [SerializeField] private int maxPlayersPerRoom = 2;

    private List<RoomInfo> availableRooms = new List<RoomInfo>();
    private string currentSessionName = "";
    private bool isSearchingRooms = false;

    [System.Serializable]
    public class RoomInfo
    {
        public string RoomName;
        public int CurrentPlayers;
        public int MaxPlayers;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        if (!networkRunner)
            networkRunner = FindObjectOfType<NetworkRunner>();

        if (!networkRunner)
        {
            Debug.LogError("[LobbyManager] NetworkRunner no encontrado");
        }
    }

    /// <summary>
    /// Crea una nueva sala.
    /// </summary>
    public async void CreateRoom(string roomName, int maxPlayers = 2)
    {
        if (!networkRunner)
        {
            Debug.LogError("[LobbyManager] NetworkRunner no asignado");
            return;
        }

        Debug.Log($"[LobbyManager] Creando sala: {roomName} (máx {maxPlayers} jugadores)");

        try
        {
            currentSessionName = roomName;

            var args = new StartGameArgs()
            {
                SessionName = roomName,
                PlayerCount = maxPlayers,
            };

            var result = await networkRunner.StartGame(args);

            if (result.Ok)
            {
                Debug.Log($"[LobbyManager] Sala '{roomName}' creada exitosamente");
                OnRoomCreated(roomName);
            }
            else
            {
                Debug.LogError($"[LobbyManager] Error al crear sala: {result.ShutdownReason}");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[LobbyManager] Excepción al crear sala: {ex.Message}");
        }
    }

    /// <summary>
    /// Busca todas las salas disponibles.
    /// </summary>
    public void SearchRooms()
    {
        if (!networkRunner)
        {
            Debug.LogError("[LobbyManager] NetworkRunner no asignado");
            return;
        }

        isSearchingRooms = true;
        availableRooms.Clear();

        Debug.Log("[LobbyManager] Buscando salas disponibles...");

        // En Fusion Shared, obtener salas desde el servidor
        // Este es un placeholder - necesitarás implementar la búsqueda específica
        // Dependiendo de tu backend Photon
        RefreshRoomList();
    }

    /// <summary>
    /// Actualiza la lista de salas.
    /// </summary>
    private void RefreshRoomList()
    {
        // TODO: Implementar lógica para obtener salas de Photon
        // Esto dependerá de tu configuración específica de Photon Fusion

        Debug.Log("[LobbyManager] Lista de salas actualizada");
        OnRoomsRefreshed();
    }

    /// <summary>
    /// Une el jugador a una sala existente.
    /// </summary>
    public async void JoinRoom(string roomName)
    {
        if (!networkRunner)
        {
            Debug.LogError("[LobbyManager] NetworkRunner no asignado");
            return;
        }

        Debug.Log($"[LobbyManager] Intentando unirse a la sala: {roomName}");

        try
        {
            var args = new StartGameArgs();
            args.SessionName = roomName;

            var result = await networkRunner.StartGame(args);

            if (result.Ok)
            {
                Debug.Log($"[LobbyManager] Unido exitosamente a la sala '{roomName}'");
                OnRoomJoined(roomName);
            }
            else
            {
                Debug.LogError($"[LobbyManager] Error al unirse a sala: {result.ShutdownReason}");
                OnJoinFailed(roomName);
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[LobbyManager] Excepción al unirse a sala: {ex.Message}");
            OnJoinFailed(roomName);
        }
    }

    /// <summary>
    /// Quick Join: Une a cualquier sala disponible o crea una nueva.
    /// </summary>
    public async void QuickJoin()
    {
        if (!networkRunner)
        {
            Debug.LogError("[LobbyManager] NetworkRunner no asignado");
            return;
        }

        Debug.Log("[LobbyManager] Quick Join iniciado");

        try
        {
            // Intentar unirse a una sala aleatoriai
            if (availableRooms.Count > 0)
            {
                var randomRoom = availableRooms[Random.Range(0, availableRooms.Count)];
                if (randomRoom.CurrentPlayers < randomRoom.MaxPlayers)
                {
                    JoinRoom(randomRoom.RoomName);
                    return;
                }
            }

            // Si no hay salas disponibles, crear una nueva
            string newRoomName = $"Room_{System.DateTime.Now.Ticks}";
            CreateRoom(newRoomName, maxPlayersPerRoom);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[LobbyManager] Excepción en Quick Join: {ex.Message}");
        }
    }

    /// <summary>
    /// Obtiene la lista de salas disponibles.
    /// </summary>
    public List<RoomInfo> GetAvailableRooms()
    {
        return new List<RoomInfo>(availableRooms);
    }

    /// <summary>
    /// Obtiene información de una sala específica.
    /// </summary>
    public RoomInfo GetRoomInfo(string roomName)
    {
        return availableRooms.Find(r => r.RoomName == roomName);
    }

    /// <summary>
    /// Abandon la sala actual.
    /// </summary>
    public void LeaveRoom()
    {
        if (networkRunner && !networkRunner.IsShutdown)
        {
            networkRunner.Shutdown();
            Debug.Log("[LobbyManager] Sala abandonada");
            OnRoomLeft();
        }
    }

    // ==================== EVENTOS ====================

    public delegate void RoomCreatedDelegate(string roomName);
    public delegate void RoomReadyDelegate(string roomName);
    public delegate void RoomsRefreshedDelegate();
    public delegate void RoomJoinedDelegate(string roomName);
    public delegate void JoinFailedDelegate(string roomName);
    public delegate void RoomLeftDelegate();

    public event RoomCreatedDelegate OnRoomCreatedEvent;
    public event RoomReadyDelegate OnRoomReadyEvent;
    public event RoomsRefreshedDelegate OnRoomsRefreshedEvent;
    public event RoomJoinedDelegate OnRoomJoinedEvent;
    public event JoinFailedDelegate OnJoinFailedEvent;
    public event RoomLeftDelegate OnRoomLeftEvent;

    private void OnRoomCreated(string roomName)
    {
        OnRoomCreatedEvent?.Invoke(roomName);
    }

    private void OnRoomReady(string roomName)
    {
        OnRoomReadyEvent?.Invoke(roomName);
    }

    private void OnRoomsRefreshed()
    {
        OnRoomsRefreshedEvent?.Invoke();
    }

    private void OnRoomJoined(string roomName)
    {
        OnRoomJoinedEvent?.Invoke(roomName);
    }

    private void OnJoinFailed(string roomName)
    {
        OnJoinFailedEvent?.Invoke(roomName);
    }

    private void OnRoomLeft()
    {
        OnRoomLeftEvent?.Invoke();
    }

    // ==================== DEBUG ====================

    public void DebugListRooms()
    {
        Debug.Log($"[LobbyManager] Salas disponibles: {availableRooms.Count}");
        foreach (var room in availableRooms)
        {
            Debug.Log($"  - {room.RoomName}: {room.CurrentPlayers}/{room.MaxPlayers}");
        }
    }
}
