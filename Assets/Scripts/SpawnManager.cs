using UnityEngine;
using Fusion;

/// <summary>
/// Gestiona el spawn de jugadores cuando se conectan a la sala.
/// Se ejecuta en el servidor (MasterClient).
/// </summary>
public class SpawnManager : MonoBehaviour
{
    [SerializeField] private NetworkObject playerPrefab;
    [SerializeField] private Transform[] spawnPoints;

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
            Debug.LogError("[SpawnManager] playerPrefab no está asignado");
            return;
        }

        runner.Spawn(playerPrefab, spawnPosition, Quaternion.identity, player);

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
