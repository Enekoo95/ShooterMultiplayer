using UnityEngine;
using Fusion;

public class AirStrike : NetworkBehaviour
{
    [SerializeField] private float fallSpeed = 20f;
    [SerializeField] private float explosionRadius = 6f;
    [SerializeField] private float damage = 80f;

    private Vector3 targetPosition;
    private bool isMoving = false;

    public void Init(Vector3 target)
    {
        targetPosition = target;
        transform.position = new Vector3(target.x, target.y + 30f, target.z);
        isMoving = true;
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority || !isMoving) return;

        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPosition,
            fallSpeed * Runner.DeltaTime
        );

        if (Vector3.Distance(transform.position, targetPosition) < 0.5f)
            Explode();
    }

    private void Explode()
    {
        isMoving = false;

        Collider[] hits = Physics.OverlapSphere(transform.position, explosionRadius);
        foreach (var hit in hits)
        {
            PlayerController player = hit.GetComponent<PlayerController>();
            if (player != null && player.IsAlive)
                player.TakeDamage(damage);
        }

        Runner.Despawn(Object);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}