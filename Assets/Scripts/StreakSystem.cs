using UnityEngine;
using Fusion;

public class StreakSystem : NetworkBehaviour
{
    public bool HasGrenade { get; private set; }
    public bool HasAirStrike { get; private set; }
    public bool HasTurret { get; private set; }

    [SerializeField] private GameObject grenadePrefab;
    [SerializeField] private GameObject airStrikePrefab;
    [SerializeField] private GameObject turretPrefab;

    private int lastCheckedStreak = 0;

    public bool GetHasGrenade() => HasGrenade;
    public bool GetHasAirStrike() => HasAirStrike;
    public bool GetHasTurret() => HasTurret;

    private void Update()
    {
        if (!HasInputAuthority || GameState.Instance == null) return;

        int currentStreak = GameState.Instance.GetKillStreak(Runner.LocalPlayer);

        if (currentStreak != lastCheckedStreak)
        {
            lastCheckedStreak = currentStreak;

            if (currentStreak >= 3 && !HasGrenade)
            {
                HasGrenade = true;
                Debug.Log("[StreakSystem] Granada desbloqueada! Pulsa Z");
            }
            if (currentStreak >= 5 && !HasAirStrike)
            {
                HasAirStrike = true;
                Debug.Log("[StreakSystem] Ataque Aéreo desbloqueado! Pulsa X");
            }
            if (currentStreak >= 10 && !HasTurret)
            {
                HasTurret = true;
                Debug.Log("[StreakSystem] Torreta desbloqueada! Pulsa C");
            }
        }

        if (Input.GetKeyDown(KeyCode.Z) && HasGrenade)
            UseGrenade();

        if (Input.GetKeyDown(KeyCode.X) && HasAirStrike)
        {
            UseAirStrikeDirect();
            HasAirStrike = false;
        }

        if (Input.GetKeyDown(KeyCode.C) && HasTurret)
            UseTurret();
    }

    private void UseGrenade()
    {
        if (grenadePrefab == null) return;
        Vector3 spawnPos = transform.position + transform.forward * 2f + Vector3.up;
        RPC_SpawnGrenade(spawnPos, transform.rotation, Runner.LocalPlayer);
        HasGrenade = false;
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_SpawnGrenade(Vector3 pos, Quaternion rot, PlayerRef ownerRef)
    {
        Runner.Spawn(grenadePrefab, pos, rot, ownerRef);
    }

    private void UseAirStrikeDirect()
    {
        PlayerController[] players = FindObjectsOfType<PlayerController>();
        PlayerController target = null;
        float minDist = float.MaxValue;

        foreach (PlayerController p in players)
        {
            if (p.HasInputAuthority) continue;
            if (!p.IsAlive) continue;
            float dist = Vector3.Distance(transform.position, p.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                target = p;
            }
        }

        if (target == null) return;
        Vector3 targetPos = target.transform.position;
        RPC_SpawnAirStrike(targetPos, Runner.LocalPlayer);
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_SpawnAirStrike(Vector3 targetPos, PlayerRef ownerRef)
    {
        Vector3 spawnPos = new Vector3(targetPos.x, targetPos.y + 30f, targetPos.z);
        Runner.Spawn(airStrikePrefab, spawnPos, Quaternion.identity, ownerRef,
            (runner, obj) =>
            {
                var air = obj.GetComponent<AirStrike>();
                air.TargetPosition = targetPos;
                air.Owner = ownerRef;
                air.IsMoving = true;
            });
    }

    private void UseTurret()
    {
        if (turretPrefab == null) return;
        Vector3 spawnPos = transform.position + transform.forward * 2f;
        RPC_SpawnTurret(spawnPos, transform.rotation, Runner.LocalPlayer);
        HasTurret = false;
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_SpawnTurret(Vector3 pos, Quaternion rot, PlayerRef ownerRef)
    {
        Runner.Spawn(turretPrefab, pos, rot, ownerRef);
    }

    public void OnPlayerDied()
    {
        HasGrenade = false;
        HasAirStrike = false;
        HasTurret = false;
        lastCheckedStreak = 0;
    }
}