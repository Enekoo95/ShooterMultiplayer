using UnityEngine;
using Fusion;
using TMPro;

public class WeaponPickup : NetworkBehaviour
{
    [SerializeField] private WeaponSystem.WeaponStats weaponStats;
    [SerializeField] private float rotationSpeed = 50f;
    [SerializeField] private float pickupRadius = 2f;
    [SerializeField] private TextMeshPro pickupText;

    [Networked] private NetworkBool IsPickedUp { get; set; }

    // Cache del jugador local — evita FindObjectsOfType cada frame
    private PlayerController localPlayer;
    private WeaponSystem localWeaponSystem;
    private bool isSpawned = false;

    public override void Spawned()
    {
        isSpawned = true;

        if (pickupText != null)
            pickupText.gameObject.SetActive(false);

        // Buscar el jugador local una sola vez al spawnear
        foreach (PlayerController player in FindObjectsOfType<PlayerController>())
        {
            if (player.HasInputAuthority)
            {
                localPlayer = player;
                localWeaponSystem = player.GetComponent<WeaponSystem>();
                break;
            }
        }
    }

    private void Update()
    {
        if (!isSpawned || Object == null || !Object.IsValid) return;
        if (IsPickedUp) return;

        // Rotar el objeto
        transform.Rotate(Vector3.up * rotationSpeed * Time.deltaTime);

        // Si no tenemos jugador local todavía, intentar encontrarlo
        if (localPlayer == null)
        {
            foreach (PlayerController player in FindObjectsOfType<PlayerController>())
            {
                if (player.HasInputAuthority)
                {
                    localPlayer = player;
                    localWeaponSystem = player.GetComponent<WeaponSystem>();
                    break;
                }
            }
            return;
        }

        float dist = Vector3.Distance(transform.position, localPlayer.transform.position);
        bool playerNearby = dist <= pickupRadius;

        if (pickupText != null)
            pickupText.gameObject.SetActive(playerNearby);

        if (playerNearby && Input.GetKeyDown(KeyCode.F))
        {
            if (localWeaponSystem != null)
            {
                localWeaponSystem.PickupWeapon(weaponStats);
                RPC_PickupWeapon();
            }
        }
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        isSpawned = false;
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_PickupWeapon()
    {
        IsPickedUp = true;
        RPC_HideForAll();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_HideForAll()
    {
        gameObject.SetActive(false);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, pickupRadius);
    }
}