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

    [SerializeField] private Transform shootOrigin; // Posición de disparo (cámara o arma)
    [SerializeField] private Transform weaponHolder; // Contenedor del modelo de arma
    [SerializeField] private GameObject weaponPrefab; // Prefab visual del arma
    [SerializeField] private Transform weaponMuzzlePoint; // Punto de salida del disparo en el arma
    [SerializeField] private float shootRange = 100f;
    [SerializeField] private LayerMask shootLayer;

    // Efectos visuales de disparo
    [SerializeField] private AudioClip shotSound;
    [SerializeField] private float recoilAmount = 0.05f; // Magnitud del retroceso
    [SerializeField] private float recoilDuration = 0.1f; // Duración del retroceso
    [SerializeField] private bool enableMuzzleFlash = true;
    [SerializeField] private bool enableShotSound = true;
    [SerializeField] private bool enableCameraRecoil = true;

    // Armas disponibles
    [SerializeField] private WeaponStats baseWeapon = new WeaponStats
    {
        weaponName = "Pistola de Pintura",
        damage = 10,
        fireRate = 0.1f,
        ammo = 100,
        maxAmmo = 100,
        rarity = WeaponRarity.Common
    };

    [SerializeField] private WeaponStats[] specialWeapons = new WeaponStats[3];

    // Estado actual
    [Networked] public int CurrentWeaponIndex { get; set; } = 0;
    [Networked] public int CurrentAmmo { get; set; }
    [Networked] public float LastShotTime { get; set; }

    private WeaponStats currentWeapon;
    private PlayerController playerController;
    private Camera mainCamera;
    private AudioSource audioSource;
    private Vector3 cameraOriginalPos;

    private void Start()
    {
        playerController = GetComponent<PlayerController>();
        mainCamera = GetComponentInChildren<Camera>();
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        if (mainCamera != null)
        {
            cameraOriginalPos = mainCamera.transform.localPosition;
        }

        // Instanciar modelo del arma si está configurado
        if (weaponPrefab != null && weaponHolder != null)
        {
            GameObject weaponInstance = Instantiate(weaponPrefab, weaponHolder);
            weaponInstance.transform.localPosition = Vector3.zero;
            weaponInstance.transform.localRotation = Quaternion.identity;

            if (weaponMuzzlePoint == null)
            {
                Transform foundMuzzle = weaponInstance.transform.Find("MuzzlePoint");
                if (foundMuzzle != null)
                {
                    weaponMuzzlePoint = foundMuzzle;
                }
            }
        }

        // Preferir el punto de salida del arma cuando exista
        if (weaponMuzzlePoint != null)
        {
            shootOrigin = weaponMuzzlePoint;
        }
        else if (shootOrigin == null)
        {
            shootOrigin = GetComponentInChildren<Camera>()?.transform ?? transform;
        }

        // Inicializar arma base
        currentWeapon = DeepCopyWeapon(baseWeapon);

        // Inicializar armas especiales (ejemplo)
        InitializeSpecialWeapons();
    }

    public override void Spawned()
    {
        // Aquí sí se pueden acceder las propiedades networked
        CurrentAmmo = currentWeapon.ammo;
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
        if (!HasInputAuthority || !playerController.IsAlive)
            return;

        // Cambio de armas
        if (Input.GetKeyDown(KeyCode.E))
        {
            CycleWeapon();
        }

        // Disparar
        if (Input.GetMouseButton(0) && CanShoot())
        {
            Shoot();
        }

        // Recargar
        if (Input.GetKeyDown(KeyCode.R))
        {
            Reload();
        }
    }

    /// <summary>
    /// Realiza un disparo.
    /// </summary>
    private void Shoot()
    {
        if (CurrentAmmo <= 0)
        {
            Debug.LogWarning("[WeaponSystem] Sin munición");
            return;
        }

        // Decrementar munición local
        CurrentAmmo--;
        LastShotTime = (float)Runner.SimulationTime;

        // Raycast para detectar impacto
        if (Physics.Raycast(shootOrigin.position, shootOrigin.forward, out RaycastHit hit, shootRange, shootLayer))
        {
            Debug.Log($"[WeaponSystem] Impacto en {hit.collider.gameObject.name}");

            // Verificar si es un jugador
            PlayerController targetPlayer = hit.collider.GetComponent<PlayerController>();
            if (targetPlayer && targetPlayer != playerController)
            {
                // Aplicar daño directamente y registrar kill local para debugging; en red, el GameState debe validar
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

        // Efecto visual
        OnShot();
    }

    /// <summary>
    /// Verifica si se puede disparar.
    /// </summary>
    private bool CanShoot()
    {
        float timeSinceLastShot = (float)Runner.SimulationTime - LastShotTime;
        return timeSinceLastShot >= currentWeapon.fireRate;
    }

    /// <summary>
    /// Cambiar arma siguiente.
    /// </summary>
    private void CycleWeapon()
    {
        CurrentWeaponIndex = (CurrentWeaponIndex + 1) % 4; // 0 = base, 1-3 = especiales
        EquipWeapon(CurrentWeaponIndex);
    }

    /// <summary>
    /// Equipar un arma específica.
    /// </summary>
    public void EquipWeapon(int weaponIndex)
    {
        if (weaponIndex == 0)
        {
            currentWeapon = DeepCopyWeapon(baseWeapon);
        }
        else if (weaponIndex > 0 && weaponIndex <= specialWeapons.Length)
        {
            currentWeapon = DeepCopyWeapon(specialWeapons[weaponIndex - 1]);
        }

        CurrentWeaponIndex = weaponIndex;
        CurrentAmmo = currentWeapon.ammo;

        Debug.Log($"[WeaponSystem] Arma equipada: {currentWeapon.weaponName} ({currentWeapon.rarity})");
    }

    /// <summary>
    /// Recargar munición.
    /// </summary>
    private void Reload()
    {
        CurrentAmmo = currentWeapon.maxAmmo;
        Debug.Log($"[WeaponSystem] Recargando... Munición: {CurrentAmmo}");
    }

    /// <summary>
    /// Recibir un pickup de arma.
    /// </summary>
    public void PickupWeapon(WeaponStats weapon)
    {
        if (!HasInputAuthority)
            return;

        EquipWeapon(CurrentWeaponIndex + 1); // Simplificado: siguiente arma
        Debug.Log($"[WeaponSystem] Recogido: {weapon.weaponName}");
    }

    /// <summary>
    /// Efectos visuales de disparo (sonido, muzzle flash, etc).
    /// </summary>
    private void OnShot()
    {
        Debug.Log($"[WeaponSystem] {currentWeapon.weaponName} dispara - Munición: {CurrentAmmo}/{currentWeapon.maxAmmo}");
        
        // Reproducir sonido de disparo
        PlayShotSound();
        
        // Mostrar muzzle flash
        PlayMuzzleFlash();
        
        // Aplicar recoil de cámara
        if (mainCamera != null)
        {
            ApplyCameraRecoil();
        }
    }

    /// <summary>
    /// Reproduce el sonido de disparo.
    /// </summary>
    private void PlayShotSound()
    {
        if (!enableShotSound || audioSource == null || shotSound == null)
            return;

        audioSource.PlayOneShot(shotSound, 1f);
    }

    /// <summary>
    /// Muestra un efecto de muzzle flash en la posición de disparo.
    /// </summary>
    private void PlayMuzzleFlash()
    {
        if (!enableMuzzleFlash)
            return;

        // Crear efecto de muzzle flash dinámicamente
        MuzzleFlashEffect.CreateMuzzleFlash(shootOrigin.position, shootOrigin.rotation, 0.1f);
    }

    /// <summary>
    /// Aplica un retroceso suave a la cámara.
    /// </summary>
    private void ApplyCameraRecoil()
    {
        // Calcular retroceso aleatorio en pequeña cantidad
        Vector3 recoilDirection = new Vector3(
            Random.Range(-recoilAmount, recoilAmount),
            Random.Range(-recoilAmount, recoilAmount),
            -recoilAmount * 0.5f // Retroceso hacia atrás más suave
        );
        
        // Guardar posición original y aplicar recoil suavemente
        StartCoroutine(ApplyRecoilCoroutine(recoilDirection));
    }

    /// <summary>
    /// Corrutina para aplicar retroceso suave de cámara.
    /// </summary>
    private System.Collections.IEnumerator ApplyRecoilCoroutine(Vector3 recoilDirection)
    {
        Vector3 originalPos = mainCamera.transform.localPosition;
        Vector3 targetPos = originalPos + recoilDirection;
        float elapsedTime = 0f;

        // Fase 1: Aplicar retroceso (primera mitad del tiempo)
        while (elapsedTime < recoilDuration * 0.5f)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / (recoilDuration * 0.5f);
            mainCamera.transform.localPosition = Vector3.Lerp(originalPos, targetPos, t);
            yield return null;
        }

        // Fase 2: Recuperarse (segunda mitad del tiempo)
        elapsedTime = 0f;
        while (elapsedTime < recoilDuration * 0.5f)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / (recoilDuration * 0.5f);
            mainCamera.transform.localPosition = Vector3.Lerp(targetPos, originalPos, t);
            yield return null;
        }

        // Asegurar que vuelva a posición original
        mainCamera.transform.localPosition = originalPos;
    }

    public WeaponStats GetCurrentWeapon()
    {
        return currentWeapon;
    }

    public int GetCurrentAmmo()
    {
        return CurrentAmmo;
    }

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

