using UnityEngine;
using Fusion;
using System.Collections.Generic;

public class GameState : NetworkBehaviour
{
    [System.Serializable]
    public struct PlayerStats : INetworkStruct
    {
        public int Kills;
        public int Deaths;
        public int Score;
        public int KillStreak;
        public float Health;
        public bool IsAlive;
    }

    [Networked] public NetworkDictionary<PlayerRef, PlayerStats> PlayerStatsDictionary => default;

    private Dictionary<PlayerRef, string> playerNames = new Dictionary<PlayerRef, string>();
    [Networked] public int TimeRemaining { get; set; }
    [Networked] public int WinnerScore { get; set; }
    [Networked] public bool IsGameActive { get; set; }
    [Networked] public int MaxPlayersInRoom { get; set; }

    private float gameTimer = 0f;
    private const float GAME_DURATION = 300f;
    private const int MAX_SCORE = 1000;
    private const int KILL_REWARD = 100;

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
        if (!HasInputAuthority) return;
        if (Runner.IsShutdown || !IsGameActive) return;

        gameTimer += Runner.DeltaTime;
        TimeRemaining = Mathf.Max(0, (int)(GAME_DURATION - gameTimer));

        if (TimeRemaining <= 0 || HasWinner())
            EndGame();
    }

    public void RegisterPlayer(PlayerRef player, string playerName)
    {
        if (!HasInputAuthority) return;

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
        if (PlayerStatsDictionary.TryGet(killer, out var killerStats))
        {
            killerStats.Kills++;
            killerStats.Score += KILL_REWARD;
            killerStats.KillStreak++;
            PlayerStatsDictionary.Set(killer, killerStats);
            playerStreaks[killer] = killerStats.KillStreak;
            Debug.Log($"[GameState] {GetPlayerName(killer)} elimina. Racha: {killerStats.KillStreak}");
        }

        if (PlayerStatsDictionary.TryGet(victim, out var victimStats))
        {
            victimStats.Deaths++;
            victimStats.KillStreak = 0;
            PlayerStatsDictionary.Set(victim, victimStats);
            playerStreaks[victim] = 0;
        }

        // Notificar KillFeed a todos los clientes
        RPC_UpdateKillFeed(GetPlayerName(killer), GetPlayerName(victim), weaponType);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_UpdateKillFeed(string killerName, string victimName, string weaponType)
    {
        KillFeed killFeed = FindObjectOfType<KillFeed>();
        if (killFeed != null)
            killFeed.AddKill(killerName, victimName, weaponType);
    }

    [Rpc]
    private void RPC_RequestKill(PlayerRef killer, PlayerRef victim, string weaponType)
    {
        if (HasInputAuthority)
            _ProcessKill(killer, victim, weaponType);
    }

    public void DamagePlayer(PlayerRef player, float damage)
    {
        if (!HasInputAuthority) return;

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

    public void RespawnPlayer(PlayerRef player)
    {
        if (!HasInputAuthority) return;

        if (PlayerStatsDictionary.TryGet(player, out var stats))
        {
            stats.Health = 100f;
            stats.IsAlive = true;
            PlayerStatsDictionary.Set(player, stats);
            Debug.Log($"[GameState] {GetPlayerName(player)} ha reaparecido");
        }
    }

    public int GetKillStreak(PlayerRef player)
    {
        if (PlayerStatsDictionary.TryGet(player, out var stats))
            return stats.KillStreak;
        return 0;
    }

    public string GetPlayerName(PlayerRef player)
    {
        if (playerNames.TryGetValue(player, out var name))
            return name;
        return "Jugador" + player.PlayerId;
    }

    private bool HasWinner()
    {
        foreach (var entry in PlayerStatsDictionary)
        {
            if (entry.Value.Score >= MAX_SCORE)
            {
                WinnerScore = entry.Value.Score;
                return true;
            }
        }
        return false;
    }

    public void EndGame()
    {
        IsGameActive = false;
        Debug.Log("[GameState] Fin de la partida");
    }

    public void StartGame()
    {
        if (!HasInputAuthority) return;
        IsGameActive = true;
        gameTimer = 0f;
        TimeRemaining = (int)GAME_DURATION;
        Debug.Log("[GameState] ¡Partida iniciada!");
    }

    public List<PlayerStats> GetSortedStats()
    {
        var sorted = new List<PlayerStats>();
        foreach (var entry in PlayerStatsDictionary)
            sorted.Add(entry.Value);
        sorted.Sort((a, b) => b.Score.CompareTo(a.Score));
        return sorted;
    }
}