using UnityEngine;
using Fusion;

public class PlayerController : NetworkBehaviour
{
    [Networked] public float CurrentHealth { get; set; }
    [Networked] public bool IsAlive { get; set; }
    [Networked] private float RespawnTime { get; set; }
    [Networked] private int RespawnIndex { get; set; }
    [Networked] private PlayerRef LastAttacker { get; set; }

    private GameState _gameState;
    private const float RESPAWN_DELAY = 3f;

    public override void Spawned()
    {
        if (Object.HasStateAuthority)
        {
            CurrentHealth = 100f;
            IsAlive = true;
            RespawnTime = -1f;
        }

        _gameState = GameState.Instance;

        // FIX: solo el host inicia el juego (una sola vez, no por cada jugador)
        if (Object.HasStateAuthority && Runner.IsServer)
        {
            if (_gameState != null && !_gameState.IsGameActive)
                _gameState.StartGame();
        }

        // FIX: cada jugador envía su PROPIO nombre al host desde su propio cliente
        // HasInputAuthority = true solo en el cliente que controla este objeto
        if (Object.HasInputAuthority)
        {
            string myName = PlayerPrefs.GetString("PlayerName", "Jugador" + Object.InputAuthority.PlayerId);
            Debug.Log($"[PlayerController] Enviando nombre al host: {myName}");
            RPC_SendNameToHost(myName);
        }
    }

    // RPC de cualquier cliente al host para registrar su nombre
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_SendNameToHost(string playerName)
    {
        if (_gameState == null) _gameState = GameState.Instance;
        if (_gameState == null)
        {
            Debug.LogError("[PlayerController] GameState no encontrado al registrar nombre.");
            return;
        }

        _gameState.RegisterPlayer(Object.InputAuthority, playerName);
        Debug.Log($"[PlayerController] Host registró: {playerName} para {Object.InputAuthority}");
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;

        if (!IsAlive && RespawnTime > 0 && Runner.SimulationTime >= RespawnTime)
        {
            RespawnTime = -1f;
            GameObject[] spawnPoints = GameObject.FindGameObjectsWithTag("Respawn");
            Vector3 spawnPos = Vector3.up;
            if (spawnPoints.Length > 0)
            {
                RespawnIndex = (RespawnIndex + 1) % spawnPoints.Length;
                spawnPos = spawnPoints[RespawnIndex].transform.position + Vector3.up;
            }
            Respawn(spawnPos);
        }
    }

    public void TakeDamage(float damage, PlayerRef attacker = default)
    {
        if (!HasStateAuthority || !IsAlive) return;

        CurrentHealth -= damage;
        if (attacker != default)
            LastAttacker = attacker;

        Debug.Log($"[PlayerController] TakeDamage: {damage}, Vida restante: {CurrentHealth}");

        if (CurrentHealth <= 0f)
        {
            CurrentHealth = 0f;
            Die();
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_TakeDamage(float damage, PlayerRef attacker)
    {
        TakeDamage(damage, attacker);
    }

    private void Die()
    {
        IsAlive = false;
        RespawnTime = (float)Runner.SimulationTime + RESPAWN_DELAY;
        Debug.Log($"[PlayerController] Jugador muerto!");

        if (_gameState != null)
        {
            PlayerRef killer = LastAttacker != default ? LastAttacker : Object.InputAuthority;
            _gameState.RecordKill(killer, Object.InputAuthority, "Pistola de Pintura");
        }

        GetComponent<StreakSystem>()?.OnPlayerDied();

        if (HasInputAuthority)
            Cursor.lockState = CursorLockMode.None;
    }

    public void Respawn(Vector3 spawnPosition)
    {
        if (!HasStateAuthority) return;

        CurrentHealth = 100f;
        IsAlive = true;
        LastAttacker = default;
        Debug.Log($"[PlayerController] Respawneando en {spawnPosition}");

        var nt = GetComponent<NetworkTransform>();
        if (nt != null) nt.Teleport(spawnPosition);
        else transform.position = spawnPosition;

        if (_gameState != null)
            _gameState.RespawnPlayer(Object.InputAuthority);

        if (HasInputAuthority)
            Cursor.lockState = CursorLockMode.Locked;
        else
            RPC_OnRespawn();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.InputAuthority)]
    private void RPC_OnRespawn() => Cursor.lockState = CursorLockMode.Locked;

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (HasInputAuthority)
            Cursor.lockState = CursorLockMode.None;
    }
}