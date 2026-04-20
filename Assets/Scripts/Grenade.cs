using UnityEngine;
using Fusion;

public class Grenade : NetworkBehaviour
{
    [SerializeField] private float explodeDelay = 3f;
    [SerializeField] private float explosionRadius = 5f;
    [SerializeField] private float explosionDamage = 50f;

    private float timer = 0f;
    private bool hasExploded = false;

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority || hasExploded) return;

        timer += Runner.DeltaTime;

        if (timer >= explodeDelay)
            Explode();
    }

    private void Explode()
    {
        hasExploded = true;

        Collider[] hits = Physics.OverlapSphere(transform.position, explosionRadius);
        foreach (var hit in hits)
        {
            PlayerController player = hit.GetComponent<PlayerController>();
            if (player != null && player.IsAlive)
                player.TakeDamage(explosionDamage);
        }

        Runner.Despawn(Object);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}