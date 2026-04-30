using UnityEngine;
using Fusion;

public class PlayerController : NetworkBehaviour
{
    [Networked] public float CurrentHealth { get; set; }
    [Networked] public bool IsAlive { get; set; }
    [Networked] private float RespawnTime { get; set; }
    [Networked] private int RespawnIndex { get; set; }

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
    }

    public override void FixedUpdateNetwork()
    {
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

    public void TakeDamage(float damage)
    {
        if (!HasStateAuthority || !IsAlive) return;
        CurrentHealth -= damage;
        Debug.Log($"[PlayerController] TakeDamage: {damage}, Vida restante: {CurrentHealth}");
        if (CurrentHealth <= 0f)
        {
            CurrentHealth = 0f;
            Die();
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_TakeDamage(float damage) => TakeDamage(damage);

    private void Die()
    {
        IsAlive = false;
        RespawnTime = (float)Runner.SimulationTime + RESPAWN_DELAY;
        Debug.Log($"[PlayerController] Jugador muerto! Respawn en {RESPAWN_DELAY}s");
        if (_gameState != null)
            _gameState.RecordKill(Object.InputAuthority, Object.InputAuthority, "Suicide");
        GetComponent<StreakSystem>()?.OnPlayerDied();
        if (HasInputAuthority) Cursor.lockState = CursorLockMode.None;
    }

    public void Respawn(Vector3 spawnPosition)
    {
        if (!HasStateAuthority) return;
        CurrentHealth = 100f;
        IsAlive = true;
        Debug.Log($"[PlayerController] Respawneando en {spawnPosition}");
        var nt = GetComponent<NetworkTransform>();
        if (nt != null) nt.Teleport(spawnPosition);
        else transform.position = spawnPosition;
        if (_gameState != null) _gameState.RespawnPlayer(Object.InputAuthority);
        if (HasInputAuthority) Cursor.lockState = CursorLockMode.Locked;
        else RPC_OnRespawn();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.InputAuthority)]
    private void RPC_OnRespawn() => Cursor.lockState = CursorLockMode.Locked;

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (HasInputAuthority) Cursor.lockState = CursorLockMode.None;
    }
}