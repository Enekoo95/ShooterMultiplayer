using UnityEngine;
using Fusion;
using TMPro;

public class WeaponPickup : NetworkBehaviour
{
    [Header("Configuración del arma")]
    [SerializeField] private int weaponIndex = 1;

    [SerializeField]
    private WeaponSystem.WeaponStats weaponStats = new WeaponSystem.WeaponStats
    {
        weaponName = "Escopeta",
        damage = 25,
        fireRate = 0.5f,
        ammo = 20,
        maxAmmo = 20,
        rarity = WeaponSystem.WeaponRarity.Special
    };

    [SerializeField] private TextMeshPro pickupText;
    [SerializeField] private float rotationSpeed = 50f;

    private bool localPlayerInRange = false;
    private WeaponSystem localWeaponSystem;

    private void Awake()
    {
        HideText();
    }

    public override void Spawned()
    {
        HideText();
        BuscarJugadorLocal();
    }

    private void BuscarJugadorLocal()
    {
        foreach (PlayerController player in FindObjectsOfType<PlayerController>())
        {
            if (player.HasInputAuthority)
            {
                localWeaponSystem = player.GetComponent<WeaponSystem>();
                break;
            }
        }
    }

    private void Update()
    {
        if (Object == null || !Object.IsValid) return;

        // Rotar siempre
        transform.Rotate(Vector3.up * rotationSpeed * Time.deltaTime);

        if (!localPlayerInRange) return;

        if (Input.GetKeyDown(KeyCode.F))
        {
            if (localWeaponSystem == null)
                BuscarJugadorLocal();

            if (localWeaponSystem != null)
            {
                localPlayerInRange = false;
                HideText();

                localWeaponSystem.PickupWeapon(weaponIndex, weaponStats);
            }
        }
    }

    private void HideText()
    {
        if (pickupText != null)
            pickupText.gameObject.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player == null || !player.HasInputAuthority) return;

        // Asegurarse de tener referencia al entrar
        if (localWeaponSystem == null)
            BuscarJugadorLocal();

        localPlayerInRange = true;
        if (pickupText != null)
            pickupText.gameObject.SetActive(true);
    }

    private void OnTriggerExit(Collider other)
    {
        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player == null || !player.HasInputAuthority) return;

        localPlayerInRange = false;
        HideText();
    }

    private void OnDrawGizmosSelected()
    {
        Collider[] cols = GetComponents<Collider>();
        foreach (Collider col in cols)
        {
            if (col.isTrigger)
            {
                Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
                Gizmos.DrawSphere(transform.position, col is SphereCollider sc ? sc.radius : 2f);
            }
        }
    }
}