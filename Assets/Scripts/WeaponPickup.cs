using UnityEngine;

/// <summary>
/// Pickup de arma en el mundo.
/// Al tocarlo, el jugador obtiene esa arma.
/// </summary>
public class WeaponPickup : MonoBehaviour
{
    [SerializeField] private WeaponSystem.WeaponStats weaponStats;
    [SerializeField] private float rotationSpeed = 50f;

    private void OnTriggerEnter(Collider other)
    {
        WeaponSystem weaponSystem = other.GetComponent<WeaponSystem>();
        if (weaponSystem != null)
        {
            weaponSystem.PickupWeapon(weaponStats);
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        transform.Rotate(Vector3.up * rotationSpeed * Time.deltaTime);
    }
}
