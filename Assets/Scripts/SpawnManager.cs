using UnityEngine;
using Fusion;
using System.Collections.Generic;

/// <summary>
/// Gestiona el spawn de jugadores cuando se conectan a la sala.
/// Se ejecuta en el servidor (MasterClient).
/// </summary>
public class SpawnManager : MonoBehaviour
{
    [SerializeField] private NetworkObject playerPrefab;
    [SerializeField] private Transform[] spawnPoints;

    private NetworkRunner runner;

    private void Start()
    {
        runner = FindObjectOfType<NetworkRunner>();
        if (runner != null)
        {
            Debug.Log("[SpawnManager] NetworkRunner encontrado en Start()");
        }
                if (!runner.IsServer)
            return;

        Debug.Log($"[SpawnManager] Jugador {player.PlayerId} se conectó");

        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogError("[SpawnManager] No hay puntos de spawn asignados");
            return;
        }

        int spawnIndex = player.PlayerId % spawnPoints.Length;
        Vector3 spawnPosition = spawnPoints[spawnIndex].position;

        if (playerPrefab == null)
        {
            Debug.LogError("[SpawnManager] playerPrefab ES NULL - NO ESTÁ ASIGNADO EN EL INSPECTOR");
            return;
        }

        Debug.Log($"[SpawnManager] ANTES DE SPAWN - Prefab: {playerPrefab.name}, Posición: {spawnPosition}");
        NetworkObject spawnedObject = runner.Spawn(playerPrefab, spawnPosition, Quaternion.identity, player);
        Debug.Log($"[SpawnManager] SPAWN EJECUTADO - Objeto: {spawnedObject.gameObject.name}");
        Debug.Log("ha spawneado");
        
        if (GameState.Instance != null)
        {
            string playerName = PlayerSessionData.Instance?.PlayerName ?? $"Player{player.PlayerId}";
            GameState.Instance.RegisterPlayer(player, playerName);
        }

        Debug.Log($"[SpawnManager] Jugador {player.PlayerId} spawneado en {spawnPosition}");
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (!runner.IsServer)
            return;

        Debug.Log($"[SpawnManager] Jugador {player.PlayerId} se conectó");

        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogError("[SpawnManager] No hay puntos de spawn asignados");
            return;
        }

        int spawnIndex = player.PlayerId % spawnPoints.Length;
        Vector3 spawnPosition = spawnPoints[spawnIndex].position;

        if (playerPrefab == null)
        {
            Debug.LogError("[SpawnManager] playerPrefab ES NULL - NO ESTÁ ASIGNADO EN EL INSPECTOR");
            return;
        }

        Debug.Log($"[SpawnManager] ANTES DE SPAWN - Prefab: {playerPrefab.name}, Posición: {spawnPosition}");
        NetworkObject spawnedObject = runner.Spawn(playerPrefab, spawnPosition, Quaternion.identity, player);
        Debug.Log($"[SpawnManager] SPAWN EJECUTADO - Objeto: {spawnedObject.gameObject.name}");
        Debug.Log("ha spawneado");
        
        if (GameState.Instance != null)
        {
            string playerName = PlayerSessionData.Instance?.PlayerName ?? $"Player{player.PlayerId}";
            GameState.Instance.RegisterPlayer(player, playerName);
        }

        Debug.Log($"[SpawnManager] Jugador {player.PlayerId} spawneado en {spawnPosition}");
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"[SpawnManager] Jugador {player.PlayerId} se desconectó");
        if (runner.IsServer && GameState.Instance != null)
        {
            // opcional: limpiar datos de GameState
        }
    }

    private void OnDrawGizmos()
    {
        if (spawnPoints == null)
            return;

        Gizmos.color = Color.green;
        foreach (Transform spawnPoint in spawnPoints)
        {
            if (spawnPoint != null)
            {
                Gizmos.DrawSphere(spawnPoint.position, 0.5f);
                Gizmos.DrawLine(spawnPoint.position, spawnPoint.position + spawnPoint.forward * 2f);
            }
        }
    }
}
