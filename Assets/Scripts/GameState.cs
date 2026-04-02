using UnityEngine;
using Fusion;
using System.Collections.Generic;
using Unity.Collections;

/// <summary>
/// Estado global del juego sincronizado en red.
/// Controlado por MasterClient (autoridad).
/// Gestiona: scoreboard, rachas, fin de partida, kill feed.
/// </summary>
public class GameState : NetworkBehaviour
{
    [System.Serializable]
    public struct PlayerStats : INetworkStruct
    {
        public int Kills;
        public int Deaths;
        public int Score;
        public int KillStreak; // Racha actual
        public float Health;
        public bool IsAlive;
    }

    [Networked] public NetworkDictionary<PlayerRef, PlayerStats> PlayerStatsDictionary => default;

    // Nombres se guardan localmente ya que Fusion no admite FixedString en structs weaved
    private Dictionary<PlayerRef, string> playerNames = new Dictionary<PlayerRef, string>();
    [Networked] public int TimeRemaining { get; set; }
    [Networked] public int WinnerScore { get; set; }
    [Networked] public bool IsGameActive { get; set; }
    [Networked] public int MaxPlayersInRoom { get; set; }

    private float gameTimer = 0f;
    private const float GAME_DURATION = 300f; // 5 minutos
    private const int MAX_SCORE = 1000; // Victoria por puntuación
    private const int KILL_REWARD = 100;

    // Sistema de rachas
    private Dictionary<PlayerRef, int> playerStreaks = new Dictionary<PlayerRef, int>();

    public static GameState Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasInputAuthority)
            return;

        // Solo MasterClient actualiza
        if (Runner.IsShutdown || !IsGameActive)
            return;

        gameTimer += Runner.DeltaTime;
        TimeRemaining = Mathf.Max(0, (int)(GAME_DURATION - gameTimer));

        // Verificar condiciones de fin de juego
        if (TimeRemaining <= 0 || HasWinner())
        {
            EndGame();
        }
    }

    /// <summary>
    /// Registra un jugador cuando se conecta.
    /// </summary>
    public void RegisterPlayer(PlayerRef player, string playerName)
    {
        if (!HasInputAuthority)
            return;

        var stats = new PlayerStats
        {
            Kills = 0,
            Deaths = 0,
            Score = 0,
            KillStreak = 0,
            Health = 100f,
            IsAlive = true
        };

        PlayerStatsDictionary.Set(player, stats);
        playerStreaks[player] = 0;
        playerNames[player] = playerName;

        Debug.Log($"[GameState] Jugador registrado: {playerName}");
    }

    /// <summary>
    /// Registra una eliminación. Solo MasterClient debe llamar esto.
    /// </summary>
    public void RecordKill(PlayerRef killer, PlayerRef victim, string weaponType)
    {
        if (!HasInputAuthority)
        {
            RPC_RequestKill(killer, victim, weaponType);
            return;
        }

        _ProcessKill(killer, victim, weaponType);
    }

    private void _ProcessKill(PlayerRef killer, PlayerRef victim, string weaponType)
    {
        PlayerRef sniffer = killer;

        if (PlayerStatsDictionary.TryGet(sniffer, out var killerStats))
        {
            killerStats.Kills++;
            killerStats.Score += KILL_REWARD;
            killerStats.KillStreak++;

            PlayerStatsDictionary.Set(sniffer, killerStats);
            playerStreaks[sniffer]++;

            Debug.Log($"[GameState] {GetPlayerName(sniffer)} elimina. Racha: {killerStats.KillStreak}");
        }

        if (PlayerStatsDictionary.TryGet(victim, out var victimStats))
        {
            victimStats.Deaths++;
            victimStats.KillStreak = 0;

            PlayerStatsDictionary.Set(victim, victimStats);
            playerStreaks[victim] = 0;
        }
    }

    [Rpc]
    private void RPC_RequestKill(PlayerRef killer, PlayerRef victim, string weaponType)
    {
        if (HasInputAuthority)
        {
            _ProcessKill(killer, victim, weaponType);
        }
    }

    /// <summary>
    /// Registra daño a un jugador.
    /// </summary>
    public void DamagePlayer(PlayerRef player, float damage)
    {
        if (!HasInputAuthority)
            return;

        if (PlayerStatsDictionary.TryGet(player, out var stats))
        {
            stats.Health -= damage;

            if (stats.Health <= 0)
            {
                stats.Health = 0;
                stats.IsAlive = false;
            }

            PlayerStatsDictionary.Set(player, stats);
        }
    }

    /// <summary>
    /// Respawnea un jugador.
    /// </summary>
    public void RespawnPlayer(PlayerRef player)
    {
        if (!HasInputAuthority)
            return;

        if (PlayerStatsDictionary.TryGet(player, out var stats))
        {
            stats.Health = 100f;
            stats.IsAlive = true;
            PlayerStatsDictionary.Set(player, stats);

            Debug.Log($"[GameState] {GetPlayerName(player)} ha reaparecido");
        }
    }

    /// <summary>
    /// Obtiene la racha de un jugador.
    /// </summary>
    public int GetKillStreak(PlayerRef player)
    {
        if (PlayerStatsDictionary.TryGet(player, out var stats))
        {
            return stats.KillStreak;
        }
        return 0;
    }

    public string GetPlayerName(PlayerRef player)
    {
        if (playerNames.TryGetValue(player, out var name))
            return name;
        return "Jugador" + player.PlayerId;
    }

    /// <summary>
    /// Verifica si hay ganador por puntuación.
    /// </summary>
    private bool HasWinner()
    {
        foreach (var entry in PlayerStatsDictionary)
        {
            var stats = entry.Value;
            if (stats.Score >= MAX_SCORE)
            {
                WinnerScore = stats.Score;
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Finaliza el juego.
    /// </summary>
    public void EndGame()
    {
        IsGameActive = false;
        Debug.Log("[GameState] Fin de la partida");

        // Aquí iría la lógica para cargar pantalla de resultados
    }

    /// <summary>
    /// Inicia el juego.
    /// </summary>
    public void StartGame()
    {
        if (!HasInputAuthority)
            return;

        IsGameActive = true;
        gameTimer = 0f;
        TimeRemaining = (int)GAME_DURATION;

        Debug.Log("[GameState] ¡Partida iniciada!");
    }

    /// <summary>
    /// Obtiene estadísticas ordenadas por puntuación.
    /// </summary>
    public List<PlayerStats> GetSortedStats()
    {
        var sorted = new List<PlayerStats>();
        foreach (var entry in PlayerStatsDictionary)
        {
            sorted.Add(entry.Value);
        }
        sorted.Sort((a, b) => b.Score.CompareTo(a.Score));
        return sorted;
    }
}
