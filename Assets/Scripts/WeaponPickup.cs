using UnityEngine;
using Fusion;
using TMPro;

public class WeaponPickup : NetworkBehaviour
{
    [SerializeField] private WeaponSystem.WeaponStats weaponStats;
    [SerializeField] private float rotationSpeed = 50f;
    [SerializeField] private float pickupRadius = 2f;
    [SerializeField] private TextMeshPro pickupText; // NUEVO
    [Networked] private bool isPickedUp { get; set; }

    private void Start()
    {
        if (pickupText != null)
            pickupText.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (isPickedUp) return;

        transform.Rotate(Vector3.up * rotationSpeed * Time.deltaTime);

        // Mostrar/ocultar texto según distancia
        PlayerController[] players = FindObjectsOfType<PlayerController>();
        bool playerNearby = false;
        foreach (PlayerController player in players)
        {
            if (!player.HasInputAuthority) continue;
            float dist = Vector3.Distance(transform.position, player.transform.position);
            if (dist <= pickupRadius)
            {
                playerNearby = true;
                break;
            }
        }

        if (pickupText != null)
            pickupText.gameObject.SetActive(playerNearby);

        if (!Input.GetKeyDown(KeyCode.F)) return;

        foreach (PlayerController player in players)
        {
            if (!player.HasInputAuthority) continue;
            float dist = Vector3.Distance(transform.position, player.transform.position);
            if (dist <= pickupRadius)
            {
                WeaponSystem weaponSystem = player.GetComponent<WeaponSystem>();
                if (weaponSystem != null)
                {
                    weaponSystem.PickupWeapon(weaponStats);
                    RPC_PickupWeapon();
                    break;
                }
            }
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_PickupWeapon()
    {
        isPickedUp = true;
        gameObject.SetActive(false);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, pickupRadius);
    }
}