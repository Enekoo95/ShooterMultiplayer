using UnityEngine;
using Fusion;

public class Turret : NetworkBehaviour
{
    [SerializeField] private float rotationSpeed = 90f;
    [SerializeField] private float detectionRadius = 15f;
    [SerializeField] private float fireRate = 1f;
    [SerializeField] private float damage = 20f;
    [SerializeField] private float lifetime = 30f;
    [SerializeField] private float health = 100f;

    private float fireTimer = 0f;
    private float lifetimeTimer = 0f;
    private PlayerController currentTarget = null;

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;

        lifetimeTimer += Runner.DeltaTime;
        if (lifetimeTimer >= lifetime)
        {
            Runner.Despawn(Object);
            return;
        }

        FindTarget();

        if (currentTarget != null)
        {
            RotateToTarget();
            fireTimer += Runner.DeltaTime;
            if (fireTimer >= 1f / fireRate)
            {
                fireTimer = 0f;
                Shoot();
            }
        }
        else
        {
            transform.Rotate(Vector3.up * rotationSpeed * Runner.DeltaTime);
        }
    }

    private void FindTarget()
    {
        currentTarget = null;
        float minDist = float.MaxValue;

        foreach (var player in FindObjectsOfType<PlayerController>())
        {
            if (!player.IsAlive) continue;

            float dist = Vector3.Distance(transform.position, player.transform.position);
            if (dist < detectionRadius && dist < minDist)
            {
                minDist = dist;
                currentTarget = player;
            }
        }
    }

    private void RotateToTarget()
    {
        Vector3 direction = (currentTarget.transform.position - transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0f, direction.z));
        transform.rotation = Quaternion.RotateTowards(transform.rotation, lookRotation, rotationSpeed * Runner.DeltaTime);
    }

    private void Shoot()
    {
        if (currentTarget != null && currentTarget.IsAlive)
            currentTarget.TakeDamage(damage);
    }

    public void TakeDamage(float dmg)
    {
        if (!HasStateAuthority) return;
        health -= dmg;
        if (health <= 0f)
            Runner.Despawn(Object);
    }
}