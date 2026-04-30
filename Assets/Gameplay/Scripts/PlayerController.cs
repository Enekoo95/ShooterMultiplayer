using UnityEngine;
using Fusion;

public class PlayerController : NetworkBehaviour
{
    [Networked] public float CurrentHealth { get; set; }
    [Networked] public bool IsAlive { get; set; }

    private GameState _gameState;

    public override void Spawned()
    {
        if (Object.HasStateAuthority)
        {
            CurrentHealth = 100f;
            IsAlive = true;
        }

        _gameState = GameState.Instance;
    }

    public void TakeDamage(float damage)
    {
        if (!HasStateAuthority || !IsAlive) return;

        CurrentHealth -= damage;
        if (CurrentHealth <= 0f)
        {
            CurrentHealth = 0f;
            Die();
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_TakeDamage(float damage) => TakeDamage(damage);

    private void Die()
    {
        IsAlive = false;

        if (_gameState != null)
            _gameState.RecordKill(Object.InputAuthority, Object.InputAuthority, "Suicide");

        if (HasInputAuthority) Cursor.lockState = CursorLockMode.None;
    }

    public void Respawn(Vector3 spawnPosition)
    {
        if (!HasStateAuthority) return;

        CurrentHealth = 100f;
        IsAlive = true;

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