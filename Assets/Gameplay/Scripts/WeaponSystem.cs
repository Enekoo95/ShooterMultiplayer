using UnityEngine;
using Fusion;

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

    public enum WeaponRarity { Common, Special, Epic }

    [SerializeField] private Transform weaponHolder;
    [SerializeField] private GameObject weaponPrefab;
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

    [Networked] public int CurrentWeaponIndex { get; set; }
    [Networked] public int CurrentAmmo { get; set; }
    [Networked] public float LastShotTime { get; set; }

    private WeaponStats currentWeapon;
    private PlayerController playerController;
    private Camera playerCamera;
    private AudioSource audioSource;
    private bool initialized = false;

    private void Initialize()
    {
        if (initialized) return;
        initialized = true;

        InitializeSpecialWeapons();
        currentWeapon = DeepCopyWeapon(baseWeapon);

        // Buscar PlayerController en el padre
        playerController = GetComponentInParent<PlayerController>();
        if (playerController == null)
            playerController = transform.root.GetComponent<PlayerController>();

        // Buscar cámara solo para el jugador local
        if (HasInputAuthority)
        {
            playerCamera = Camera.main;
            if (playerCamera == null)
                playerCamera = transform.root.GetComponentInChildren<Camera>();
        }

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        // Instanciar modelo del arma
        if (weaponPrefab != null && weaponHolder != null && HasInputAuthority)
        {
            GameObject weaponInstance = Instantiate(weaponPrefab, weaponHolder);
            weaponInstance.transform.localPosition = Vector3.zero;
            weaponInstance.transform.localRotation = Quaternion.identity;
        }
    }

    private void Start() => Initialize();

    public override void Spawned()
    {
        Initialize();
        if (HasStateAuthority)
        {
            CurrentAmmo = currentWeapon.maxAmmo;
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
        if (!HasInputAuthority) return;

        // Reintentar encontrar playerController si es null
        if (playerController == null)
        {
            playerController = transform.root.GetComponent<PlayerController>();
            if (playerController == null) return;
        }

        if (!playerController.IsAlive) return;

        if (!GetInput(out PlayerInput input)) return;

        if (input.CycleWeapon) CycleWeapon();
        if (input.Shoot && CanShoot()) Shoot();
        if (input.Reload) Reload();
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

        // Buscar cámara si no la tenemos
        if (playerCamera == null)
            playerCamera = Camera.main ?? transform.root.GetComponentInChildren<Camera>();

        if (playerCamera == null)
        {
            Debug.LogWarning("[WeaponSystem] No hay cámara disponible");
            return;
        }

        Vector3 origin = playerCamera.transform.position;
        Vector3 direction = playerCamera.transform.forward;

        Debug.Log($"[WeaponSystem] Disparando desde {origin} hacia {direction}");

        // Usar OverlapSphere como fallback si el raycast falla
        if (Physics.Raycast(origin, direction, out RaycastHit hit, shootRange, shootLayer))
        {
            Debug.Log($"[WeaponSystem] Impacto en {hit.collider.gameObject.name}");

            PlayerController target = hit.collider.GetComponentInParent<PlayerController>();
            if (target == null)
                target = hit.collider.GetComponent<PlayerController>();

            if (target != null && target != playerController)
            {
                Debug.Log($"[WeaponSystem] Dañando a {target.name}");
                target.RPC_TakeDamage(currentWeapon.damage);
            }
            else
            {
                Debug.LogWarning("[WeaponSystem] Impacto sin PlayerController válido");
            }
        }
        else
        {
            Debug.LogWarning($"[WeaponSystem] Raycast no impactó nada - Layer: {shootLayer.value}");
        }

        OnShot();
    }

    private bool CanShoot()
    {
        return (float)Runner.SimulationTime - LastShotTime >= currentWeapon.fireRate;
    }

    private void CycleWeapon()
    {
        CurrentWeaponIndex = (CurrentWeaponIndex + 1) % 4;
        EquipWeapon(CurrentWeaponIndex);
    }

    public void EquipWeapon(int index)
    {
        currentWeapon = index == 0
            ? DeepCopyWeapon(baseWeapon)
            : DeepCopyWeapon(specialWeapons[Mathf.Clamp(index - 1, 0, specialWeapons.Length - 1)]);

        CurrentWeaponIndex = index;
        CurrentAmmo = currentWeapon.maxAmmo;
        Debug.Log($"[WeaponSystem] Arma equipada: {currentWeapon.weaponName}");
    }

    private void Reload()
    {
        CurrentAmmo = currentWeapon.maxAmmo;
        Debug.Log($"[WeaponSystem] Recargando... Munición: {CurrentAmmo}");
    }

    private void OnShot()
    {
        if (enableShotSound && audioSource != null && shotSound != null)
            audioSource.PlayOneShot(shotSound);

        if (enableMuzzleFlash)
            MuzzleFlashEffect.CreateMuzzleFlash(playerCamera.transform.position, playerCamera.transform.rotation, 0.1f);

        if (enableCameraRecoil && playerCamera != null)
            StartCoroutine(ApplyRecoilCoroutine());
    }

    private System.Collections.IEnumerator ApplyRecoilCoroutine()
    {
        Vector3 originalPos = playerCamera.transform.localPosition;
        Vector3 recoilDir = new Vector3(
            Random.Range(-recoilAmount, recoilAmount),
            Random.Range(-recoilAmount, recoilAmount),
            -recoilAmount * 0.5f
        );
        Vector3 targetPos = originalPos + recoilDir;
        float elapsed = 0f;

        while (elapsed < recoilDuration * 0.5f)
        {
            elapsed += Time.deltaTime;
            playerCamera.transform.localPosition = Vector3.Lerp(originalPos, targetPos, elapsed / (recoilDuration * 0.5f));
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < recoilDuration * 0.5f)
        {
            elapsed += Time.deltaTime;
            playerCamera.transform.localPosition = Vector3.Lerp(targetPos, originalPos, elapsed / (recoilDuration * 0.5f));
            yield return null;
        }

        playerCamera.transform.localPosition = originalPos;
    }

    public WeaponStats GetCurrentWeapon() => currentWeapon;
    public int GetCurrentAmmo() => CurrentAmmo;

    private WeaponStats DeepCopyWeapon(WeaponStats src) => new WeaponStats
    {
        weaponName = src.weaponName,
        damage = src.damage,
        fireRate = src.fireRate,
        ammo = src.maxAmmo,
        maxAmmo = src.maxAmmo,
        rarity = src.rarity
    };
    public void PickupWeapon(WeaponStats weapon)
    {
        if (!HasInputAuthority) return;
        EquipWeapon(CurrentWeaponIndex + 1);
        Debug.Log($"[WeaponSystem] Recogido: {weapon.weaponName}");
    }
}