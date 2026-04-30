using UnityEngine;
using Fusion;

/// <summary>
/// Sistema de armas: arma actual, disparo, pickups.
/// Sincronizado en red. El cliente tiene autoridad sobre su arma.
/// </summary>
public class WeaponSystem : NetworkBehaviour
{
    [System.Serializable]
    public class WeaponStats
    {
        public string weaponName;
        public int damage;
        public float fireRate;
        public int ammo;
        public int maxAmmo;
        public WeaponRarity rarity;
    }

    public enum WeaponRarity
    {
        Common,
        Special,
        Epic
    }

    [SerializeField] private Transform shootOrigin;
    [SerializeField] private Transform weaponHolder;
    [SerializeField] private GameObject weaponPrefab;
    [SerializeField] private Transform weaponMuzzlePoint;
    [SerializeField] private float shootRange = 100f;
    [SerializeField] private LayerMask shootLayer;

    [SerializeField] private AudioClip shotSound;
    [SerializeField] private float recoilAmount = 0.05f;
    [SerializeField] private float recoilDuration = 0.1f;
    [SerializeField] private bool enableMuzzleFlash = true;
    [SerializeField] private bool enableShotSound = true;
    [SerializeField] private bool enableCameraRecoil = true;

    [SerializeField]
    private WeaponStats baseWeapon = new WeaponStats
    {
        weaponName = "Pistola de Pintura",
        damage = 10,
        fireRate = 0.1f,
        ammo = 100,
        maxAmmo = 100,
        rarity = WeaponRarity.Common
    };

    [SerializeField] private WeaponStats[] specialWeapons = new WeaponStats[3];

    [Networked] public int CurrentWeaponIndex { get; set; } = 0;
    [Networked] public int CurrentAmmo { get; set; }
    [Networked] public float LastShotTime { get; set; }

    private WeaponStats currentWeapon;
    private PlayerController playerController;
    private Camera mainCamera;
    private AudioSource audioSource;
    private Vector3 cameraOriginalPos;

    // FIX: Separar la inicialización local en un método propio
    // para poder llamarlo tanto desde Spawned() como desde Start()
    private bool localInitialized = false;

    private void InitializeLocal()
    {
        if (localInitialized) return;
        localInitialized = true;

        // FIX: Inicializar currentWeapon ANTES de que Spawned() lo necesite
        InitializeSpecialWeapons();
        currentWeapon = DeepCopyWeapon(baseWeapon);

        playerController = GetComponentInParent<PlayerController>();
        mainCamera = transform.root.GetComponentInChildren<Camera>();
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        if (mainCamera != null)
            cameraOriginalPos = mainCamera.transform.localPosition;

        if (weaponPrefab != null && weaponHolder != null)
        {
            GameObject weaponInstance = Instantiate(weaponPrefab, weaponHolder);
            weaponInstance.transform.localPosition = Vector3.zero;
            weaponInstance.transform.localRotation = Quaternion.identity;

            if (weaponMuzzlePoint == null)
            {
                Transform foundMuzzle = weaponInstance.transform.Find("MuzzlePoint");
                if (foundMuzzle != null)
                    weaponMuzzlePoint = foundMuzzle;
            }
        }

        if (weaponMuzzlePoint != null)
            shootOrigin = weaponMuzzlePoint;
        else if (shootOrigin == null)
            shootOrigin = GetComponentInChildren<Camera>()?.transform ?? transform;
    }

    // FIX: Start ya no inicializa nada, solo llama a InitializeLocal por si acaso
    private void Start()
    {
        InitializeLocal();
    }

    public override void Spawned()
    {
        // FIX: Garantizamos que currentWeapon existe ANTES de acceder a CurrentAmmo
        InitializeLocal();

        // Ahora es seguro acceder a propiedades networked
        if (HasInputAuthority)
        {
            CurrentAmmo = currentWeapon.ammo;
            Debug.Log($"[WeaponSystem] Spawned con arma: {currentWeapon.weaponName}, Munición: {CurrentAmmo}");
        }
    }

    private void InitializeSpecialWeapons()
    {
        specialWeapons[0] = new WeaponStats
        {
            weaponName = "Escopeta de Pintura",
            damage = 25,
            fireRate = 0.5f,
            ammo = 20,
            maxAmmo = 20,
            rarity = WeaponRarity.Special
        };

        specialWeapons[1] = new WeaponStats
        {
            weaponName = "Rifle de Pintura",
            damage = 30,
            fireRate = 0.2f,
            ammo = 50,
            maxAmmo = 50,
            rarity = WeaponRarity.Special
        };

        specialWeapons[2] = new WeaponStats
        {
            weaponName = "Lanzador de Pintura",
            damage = 40,
            fireRate = 1f,
            ammo = 10,
            maxAmmo = 10,
            rarity = WeaponRarity.Epic
        };
    }

    public override void FixedUpdateNetwork()
    {
        // FIX: Null check en playerController por si Spawned() llegó antes que Start()
        if (!HasInputAuthority || playerController == null || !playerController.IsAlive)
            return;

        if (!GetInput(out PlayerInput input))
            return;

        if (input.CycleWeapon)
            CycleWeapon();

        if (input.Shoot && CanShoot())
            Shoot();

        if (input.Reload)
            Reload();
    }

    private void Shoot()
    {
        if (CurrentAmmo <= 0)
        {
            Debug.LogWarning("[WeaponSystem] Sin munición");
            return;
        }

        CurrentAmmo--;
        LastShotTime = (float)Runner.SimulationTime;

        if (Physics.Raycast(shootOrigin.position, shootOrigin.forward, out RaycastHit hit, shootRange, shootLayer))
        {
            Debug.Log($"[WeaponSystem] Impacto en {hit.collider.gameObject.name}");

            PlayerController targetPlayer = hit.collider.GetComponent<PlayerController>();
            if (targetPlayer != null && targetPlayer != playerController)
            {
                targetPlayer.TakeDamage(currentWeapon.damage);
                if (GameState.Instance != null)
                {
                    GameState.Instance.RecordKill(Runner.LocalPlayer, targetPlayer.Object.InputAuthority, currentWeapon.weaponName);
                }
            }
        }
        else
        {
            Debug.Log("[WeaponSystem] Disparo fallido");
        }

        OnShot();
    }

    private bool CanShoot()
    {
        float timeSinceLastShot = (float)Runner.SimulationTime - LastShotTime;
        return timeSinceLastShot >= currentWeapon.fireRate;
    }

    private void CycleWeapon()
    {
        CurrentWeaponIndex = (CurrentWeaponIndex + 1) % 4;
        EquipWeapon(CurrentWeaponIndex);
    }

    public void EquipWeapon(int weaponIndex)
    {
        if (weaponIndex == 0)
            currentWeapon = DeepCopyWeapon(baseWeapon);
        else if (weaponIndex > 0 && weaponIndex <= specialWeapons.Length)
            currentWeapon = DeepCopyWeapon(specialWeapons[weaponIndex - 1]);

        CurrentWeaponIndex = weaponIndex;
        CurrentAmmo = currentWeapon.ammo;

        Debug.Log($"[WeaponSystem] Arma equipada: {currentWeapon.weaponName} ({currentWeapon.rarity})");
    }

    private void Reload()
    {
        CurrentAmmo = currentWeapon.maxAmmo;
        Debug.Log($"[WeaponSystem] Recargando... Munición: {CurrentAmmo}");
    }

    public void PickupWeapon(WeaponStats weapon)
    {
        if (!HasInputAuthority) return;
        EquipWeapon(CurrentWeaponIndex + 1);
        Debug.Log($"[WeaponSystem] Recogido: {weapon.weaponName}");
    }

    private void OnShot()
    {
        Debug.Log($"[WeaponSystem] {currentWeapon.weaponName} dispara - Munición: {CurrentAmmo}/{currentWeapon.maxAmmo}");
        PlayShotSound();
        PlayMuzzleFlash();
        if (mainCamera != null && enableCameraRecoil)
            ApplyCameraRecoil();
    }

    private void PlayShotSound()
    {
        if (!enableShotSound || audioSource == null || shotSound == null) return;
        audioSource.PlayOneShot(shotSound, 1f);
    }

    private void PlayMuzzleFlash()
    {
        if (!enableMuzzleFlash) return;
        MuzzleFlashEffect.CreateMuzzleFlash(shootOrigin.position, shootOrigin.rotation, 0.1f);
    }

    private void ApplyCameraRecoil()
    {
        Vector3 recoilDirection = new Vector3(
            Random.Range(-recoilAmount, recoilAmount),
            Random.Range(-recoilAmount, recoilAmount),
            -recoilAmount * 0.5f
        );
        StartCoroutine(ApplyRecoilCoroutine(recoilDirection));
    }

    private System.Collections.IEnumerator ApplyRecoilCoroutine(Vector3 recoilDirection)
    {
        Vector3 originalPos = mainCamera.transform.localPosition;
        Vector3 targetPos = originalPos + recoilDirection;
        float elapsedTime = 0f;

        while (elapsedTime < recoilDuration * 0.5f)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / (recoilDuration * 0.5f);
            mainCamera.transform.localPosition = Vector3.Lerp(originalPos, targetPos, t);
            yield return null;
        }

        elapsedTime = 0f;
        while (elapsedTime < recoilDuration * 0.5f)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / (recoilDuration * 0.5f);
            mainCamera.transform.localPosition = Vector3.Lerp(targetPos, originalPos, t);
            yield return null;
        }

        mainCamera.transform.localPosition = originalPos;
    }

    public WeaponStats GetCurrentWeapon() => currentWeapon;
    public int GetCurrentAmmo() => CurrentAmmo;

    private WeaponStats DeepCopyWeapon(WeaponStats original)
    {
        return new WeaponStats
        {
            weaponName = original.weaponName,
            damage = original.damage,
            fireRate = original.fireRate,
            ammo = original.maxAmmo,
            maxAmmo = original.maxAmmo,
            rarity = original.rarity
        };
    }
}