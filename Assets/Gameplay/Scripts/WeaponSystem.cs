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
    [SerializeField] private float shootRange = 100f;
    [SerializeField] private LayerMask shootLayer;
    [SerializeField] private AudioClip shotSound;
    [SerializeField] private float recoilAmount = 0.05f;
    [SerializeField] private float recoilDuration = 0.1f;
    [SerializeField] private bool enableMuzzleFlash = true;
    [SerializeField] private bool enableShotSound = true;
    [SerializeField] private bool enableCameraRecoil = true;
    [SerializeField] private bool enableBulletTracer = true;

    [Header("Modelos visuales — índice 0=pistola, 1=escopeta, 2=rifle, 3=sniper")]
    [SerializeField] private GameObject[] weaponPrefabs;

    // Stats del arma base — configurable en el Inspector
    [Header("Arma base (pistola)")]
    [SerializeField]
    private WeaponStats baseWeapon = new WeaponStats
    {
        weaponName = "Pistola",
        damage = 10,
        fireRate = 0.1f,
        ammo = 100,
        maxAmmo = 100,
        rarity = WeaponRarity.Common
    };

    private Transform[] muzzlePoints;
    private GameObject[] weaponInstances;

    [Networked] public int CurrentWeaponIndex { get; set; }
    [Networked] public int CurrentAmmo { get; set; }
    [Networked] public float LastShotTime { get; set; }

    // Stats actuales — se reemplazan al recoger un arma
    private WeaponStats currentWeapon;
    private PlayerController playerController;
    private Camera playerCamera;
    private AudioSource audioSource;
    private bool initialized = false;

    private void Initialize()
    {
        if (initialized) return;
        initialized = true;

        currentWeapon = DeepCopyWeapon(baseWeapon);

        playerController = GetComponentInParent<PlayerController>();
        if (playerController == null)
            playerController = transform.root.GetComponent<PlayerController>();

        if (HasInputAuthority)
        {
            playerCamera = Camera.main;
            if (playerCamera == null)
                playerCamera = transform.root.GetComponentInChildren<Camera>();
        }

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        if (weaponPrefabs != null && weaponHolder != null && HasInputAuthority)
        {
            weaponInstances = new GameObject[weaponPrefabs.Length];
            muzzlePoints = new Transform[weaponPrefabs.Length];

            for (int i = 0; i < weaponPrefabs.Length; i++)
            {
                if (weaponPrefabs[i] == null) continue;

                GameObject instance = Instantiate(weaponPrefabs[i], weaponHolder);
                instance.transform.localPosition = Vector3.zero;
                instance.transform.localRotation = Quaternion.identity;
                weaponInstances[i] = instance;

                Transform mp = instance.transform.Find("MuzzlePoint");
                muzzlePoints[i] = mp;

                instance.SetActive(i == 0);
            }
        }
    }

    private void Start() => Initialize();

    public override void Spawned()
    {
        Initialize();
        if (HasStateAuthority)
        {
            CurrentAmmo = currentWeapon.maxAmmo;
            Debug.Log($"[WeaponSystem] Spawned con: {currentWeapon.weaponName}");
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasInputAuthority) return;
        if (PauseMenu.IsPaused) return;

        if (playerController == null)
        {
            playerController = transform.root.GetComponent<PlayerController>();
            if (playerController == null) return;
        }

        if (!playerController.IsAlive) return;
        if (!GetInput(out PlayerInput input)) return;

        if (input.Shoot && CanShoot()) Shoot();
        if (input.Reload) Reload();
    }

    private void Shoot()
    {
        if (CurrentAmmo <= 0) return;

        CurrentAmmo--;
        LastShotTime = (float)Runner.SimulationTime;

        if (playerCamera == null)
            playerCamera = Camera.main ?? transform.root.GetComponentInChildren<Camera>();
        if (playerCamera == null) return;

        Vector3 origin = playerCamera.transform.position;
        Vector3 direction = playerCamera.transform.forward;
        Vector3 endPoint = origin + direction * shootRange;

        RaycastHit[] hits = Physics.RaycastAll(origin, direction, shootRange);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.transform.root == transform.root) continue;

            PlayerController target = hit.collider.transform.root.GetComponent<PlayerController>();
            if (target == null)
                target = hit.collider.GetComponentInParent<PlayerController>();

            if (target != null)
            {
                target.RPC_TakeDamage(currentWeapon.damage, Runner.LocalPlayer);
                endPoint = hit.point;
                break;
            }

            endPoint = hit.point;
            break;
        }

        if (!Runner.IsResimulation)
            OnShot(origin, endPoint);
    }

    private bool CanShoot() => (float)Runner.SimulationTime - LastShotTime >= currentWeapon.fireRate;

    // FIX: acepta el índice del modelo Y las stats del pickup
    public void PickupWeapon(int index, WeaponStats stats)
    {
        if (!HasInputAuthority) return;
        if (index < 0 || (weaponPrefabs != null && index >= weaponPrefabs.Length)) return;

        // Usar las stats del pickup tal cual — cada arma del suelo tiene las suyas
        currentWeapon = DeepCopyWeapon(stats);
        CurrentWeaponIndex = index;
        CurrentAmmo = stats.maxAmmo;

        // Cambiar modelo visual
        if (weaponInstances != null)
        {
            for (int i = 0; i < weaponInstances.Length; i++)
            {
                if (weaponInstances[i] != null)
                    weaponInstances[i].SetActive(i == index);
            }
        }

        Debug.Log($"[WeaponSystem] Equipado: {currentWeapon.weaponName} | Daño: {currentWeapon.damage} | Cadencia: {currentWeapon.fireRate}");
    }

    private void Reload()
    {
        CurrentAmmo = currentWeapon.maxAmmo;
        Debug.Log($"[WeaponSystem] Recargando {currentWeapon.weaponName}...");
    }

    private void OnShot(Vector3 rayOrigin, Vector3 rayEnd)
    {
        if (enableShotSound && audioSource != null && shotSound != null)
            audioSource.PlayOneShot(shotSound);

        if (enableMuzzleFlash)
        {
            Transform muzzle = null;
            if (muzzlePoints != null && CurrentWeaponIndex < muzzlePoints.Length)
                muzzle = muzzlePoints[CurrentWeaponIndex];
            if (muzzle == null)
                muzzle = weaponHolder != null ? weaponHolder : transform;

            Quaternion flashRotation = playerCamera != null
                ? Quaternion.LookRotation(playerCamera.transform.forward, playerCamera.transform.up)
                : muzzle.rotation;

            MuzzleFlashEffect.CreateMuzzleFlash(muzzle.position, flashRotation, 0.1f, muzzle);
        }

        if (enableBulletTracer && HasInputAuthority)
            ShowBulletTracer(rayOrigin, rayEnd);

        if (enableCameraRecoil && playerCamera != null)
            StartCoroutine(ApplyRecoilCoroutine());
    }

    private void ShowBulletTracer(Vector3 from, Vector3 to)
    {
        GameObject tracer = new GameObject("BulletTracer");
        LineRenderer lr = tracer.AddComponent<LineRenderer>();
        lr.startWidth = 0.02f;
        lr.endWidth = 0.004f;
        lr.positionCount = 2;
        lr.SetPosition(0, from);
        lr.SetPosition(1, to);
        lr.useWorldSpace = true;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows = false;

        Shader shader = Shader.Find("Unlit/Color") ?? Shader.Find("Legacy Shaders/Particles/Additive");
        if (shader != null)
        {
            Material mat = new Material(shader);
            mat.color = new Color(1f, 0.95f, 0.6f, 0.8f);
            lr.material = mat;
        }
        Destroy(tracer, 0.04f);
    }

    private System.Collections.IEnumerator ApplyRecoilCoroutine()
    {
        Vector3 originalPos = playerCamera.transform.localPosition;
        Vector3 recoilDir = new Vector3(Random.Range(-recoilAmount, recoilAmount), Random.Range(-recoilAmount, recoilAmount), -recoilAmount * 0.5f);
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
}