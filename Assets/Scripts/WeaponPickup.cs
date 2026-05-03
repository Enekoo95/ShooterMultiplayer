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

    // Cooldown para evitar recoger el arma múltiples veces seguidas
    private float pickupCooldown = 0f;
    private const float PICKUP_COOLDOWN_TIME = 1f;

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

        // Bajar cooldown
        if (pickupCooldown > 0f)
            pickupCooldown -= Time.deltaTime;

        if (!localPlayerInRange) return;

        if (Input.GetKeyDown(KeyCode.F) && pickupCooldown <= 0f)
        {
            if (localWeaponSystem == null)
                BuscarJugadorLocal();

            if (localWeaponSystem != null)
            {
                // Ocultar texto y equipar arma
                localPlayerInRange = false;
                HideText();

                // Cooldown para que no se recoja instantáneamente de nuevo
                pickupCooldown = PICKUP_COOLDOWN_TIME;

                localWeaponSystem.PickupWeapon(weaponIndex, weaponStats);

                StartCoroutine(ReactivarTrigger());
            }
        }
    }

    // Después del cooldown, si el jugador sigue dentro vuelve a mostrar el texto
    private System.Collections.IEnumerator ReactivarTrigger()
    {
        yield return new WaitForSeconds(PICKUP_COOLDOWN_TIME);

        // Comprobar si el jugador sigue dentro del trigger
        if (localWeaponSystem != null)
        {
            PlayerController player = localWeaponSystem.GetComponent<PlayerController>();
            if (player != null)
            {
                Collider[] cols = GetComponents<Collider>();
                foreach (Collider col in cols)
                {
                    if (!col.isTrigger) continue;
                    // Comprobar distancia manualmente ya que no podemos re-disparar OnTriggerEnter
                    float dist = Vector3.Distance(transform.position, player.transform.position);
                    SphereCollider sc = col as SphereCollider;
                    float radius = sc != null ? sc.radius : 2f;
                    if (dist <= radius)
                    {
                        localPlayerInRange = true;
                        if (pickupText != null)
                            pickupText.gameObject.SetActive(true);
                    }
                    break;
                }
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