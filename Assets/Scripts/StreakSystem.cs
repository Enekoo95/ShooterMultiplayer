using UnityEngine;
using Fusion;

/// <summary>
/// Gestiona el sistema de rachas de eliminaciones.
/// Otorga recompensas especiales por rachas consecutivas.
/// </summary>
public class StreakSystem : NetworkBehaviour
{
    [System.Serializable]
    public class StreakReward
    {
        public int killThreshold;
        public string rewardName;
        public GameObject rewardPrefab;
        public Vector3 spawnOffset = Vector3.up * 2;
    }

    [SerializeField] private StreakReward[] streakRewards = new StreakReward[]
    {
        new StreakReward { killThreshold = 3, rewardName = "Grenade", spawnOffset = Vector3.up },
        new StreakReward { killThreshold = 5, rewardName = "Air Strike", spawnOffset = Vector3.up * 3 },
        new StreakReward { killThreshold = 10, rewardName = "Turret", spawnOffset = Vector3.up * 2 }
    };

    public override void FixedUpdateNetwork()
    {
        if (!HasInputAuthority || GameState.Instance == null)
            return;

        // Verificar rachas para todos los jugadores
        foreach (var player in Runner.ActivePlayers)
        {
            int streak = GameState.Instance.GetKillStreak(player);

            foreach (var reward in streakRewards)
            {
                if (streak == reward.killThreshold)
                {
                    GrantReward(player, reward);
                    break;
                }
            }
        }
    }

    private void GrantReward(PlayerRef player, StreakReward reward)
    {
        Debug.Log($"[StreakSystem] {player.PlayerId} obtiene {reward.rewardName} por {reward.killThreshold} kills!");

        // Spawn reward cerca del jugador
        if (reward.rewardPrefab != null)
        {
            // Buscar posición del jugador
            var playerObjects = FindObjectsOfType<NetworkObject>();
            foreach (var obj in playerObjects)
            {
                if (obj.HasInputAuthority && obj.InputAuthority == player)
                {
                    Vector3 spawnPos = obj.transform.position + reward.spawnOffset;
                    Runner.Spawn(reward.rewardPrefab, spawnPos, Quaternion.identity);
                    break;
                }
            }
        }
    }
}
