using UnityEngine;
using Fusion;

public class AirStrike : NetworkBehaviour
{
    [SerializeField] private float fallSpeed = 20f;
    [SerializeField] private float explosionRadius = 10f;
    [SerializeField] private float damage = 80f;

    [Networked] public Vector3 TargetPosition { get; set; }
    [Networked] public PlayerRef Owner { get; set; }
    [Networked] public bool IsMoving { get; set; }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority || !IsMoving) return;

        transform.position = Vector3.MoveTowards(
            transform.position,
            TargetPosition,
            fallSpeed * Runner.DeltaTime
        );

        if (Vector3.Distance(transform.position, TargetPosition) < 1f)
            Explode();
    }

    private void Explode()
    {
        IsMoving = false;
        PlayerController[] allPlayers = FindObjectsOfType<PlayerController>();
        foreach (PlayerController player in allPlayers)
        {
            if (player.Object.InputAuthority == Owner) continue;
            if (!player.IsAlive) continue;
            float dist = Vector3.Distance(transform.position, player.transform.position);
            if (dist <= explosionRadius)
                player.TakeDamage(damage, Owner);
        }
        Runner.Despawn(Object);
    }
}