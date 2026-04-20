using UnityEngine;
using Fusion;

public class StreakSystem : NetworkBehaviour
{
    [Networked] public bool HasGrenade { get; set; }
    [Networked] public bool HasAirStrike { get; set; }
    [Networked] public bool HasTurret { get; set; }

    [SerializeField] private GameObject grenadePrefab;
    [SerializeField] private GameObject airStrikePrefab;
    [SerializeField] private GameObject turretPrefab;
    [SerializeField] private GameObject airStrikeIndicatorPrefab; // NUEVO

    private int lastCheckedStreak = 0;
    private GameObject activeIndicator = null; // NUEVO
    private bool isSelectingAirStrike = false; // NUEVO

    public override void FixedUpdateNetwork()
    {
        if (!HasInputAuthority || GameState.Instance == null)
            return;

        int currentStreak = GameState.Instance.GetKillStreak(Runner.LocalPlayer);

        if (currentStreak == lastCheckedStreak)
            return;

        lastCheckedStreak = currentStreak;

        if (currentStreak >= 3 && !HasGrenade)
        {
            HasGrenade = true;
            RPC_NotifyReward("Granada desbloqueada! Pulsa Z");
        }
        if (currentStreak >= 5 && !HasAirStrike)
        {
            HasAirStrike = true;
            RPC_NotifyReward("Ataque Aéreo desbloqueado! Pulsa X");
        }
        if (currentStreak >= 10 && !HasTurret)
        {
            HasTurret = true;
            RPC_NotifyReward("Torreta desbloqueada! Pulsa C");
        }
    }

    private void Update()
    {
        if (!HasInputAuthority) return;

        if (Input.GetKeyDown(KeyCode.Z) && HasGrenade)
            UseGrenade();

        if (Input.GetKeyDown(KeyCode.X) && HasAirStrike && !isSelectingAirStrike)
            StartAirStrikeSelection();

        if (Input.GetKeyDown(KeyCode.C) && HasTurret)
            UseTurret();

        if (isSelectingAirStrike)
            UpdateAirStrikeIndicator();
    }

    private void UseGrenade()
    {
        if (grenadePrefab == null) return;
        Vector3 spawnPos = transform.position + transform.forward * 2f + Vector3.up;
        Runner.Spawn(grenadePrefab, spawnPos, transform.rotation, Runner.LocalPlayer);
        HasGrenade = false;
        lastCheckedStreak = 0;
    }

    private void StartAirStrikeSelection()
    {
        isSelectingAirStrike = true;
        if (airStrikeIndicatorPrefab != null)
            activeIndicator = Instantiate(airStrikeIndicatorPrefab);
    }

    private void UpdateAirStrikeIndicator()
    {
        // Mover el indicador donde apunta el cursor
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 200f))
        {
            if (activeIndicator != null)
                activeIndicator.transform.position = hit.point + Vector3.up * 0.1f;

            // Confirmar con clic izquierdo
            if (Input.GetMouseButtonDown(0))
                ConfirmAirStrike(hit.point);
        }

        // Cancelar con clic derecho
        if (Input.GetMouseButtonDown(1))
            CancelAirStrike();
    }

    private void ConfirmAirStrike(Vector3 targetPos)
    {
        if (airStrikePrefab == null) return;

        Vector3 spawnPos = new Vector3(targetPos.x, targetPos.y + 30f, targetPos.z);
        var obj = Runner.Spawn(airStrikePrefab, spawnPos, Quaternion.identity, Runner.LocalPlayer);
        obj.GetComponent<AirStrike>().Init(targetPos);

        HasAirStrike = false;
        lastCheckedStreak = 0;
        CancelAirStrike();
    }

    private void CancelAirStrike()
    {
        isSelectingAirStrike = false;
        if (activeIndicator != null)
        {
            Destroy(activeIndicator);
            activeIndicator = null;
        }
    }

    private void UseTurret()
    {
        if (turretPrefab == null) return;
        Vector3 spawnPos = transform.position + transform.forward * 2f;
        Runner.Spawn(turretPrefab, spawnPos, transform.rotation, Runner.LocalPlayer);
        HasTurret = false;
        lastCheckedStreak = 0;
    }

    public void OnPlayerDied()
    {
        HasGrenade = false;
        HasAirStrike = false;
        HasTurret = false;
        lastCheckedStreak = 0;
        CancelAirStrike();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.InputAuthority)]
    private void RPC_NotifyReward(string message)
    {
        Debug.Log($"[StreakSystem] {message}");
    }
}