using UnityEngine;

[RequireComponent(typeof(Collider))]
public class CheckpointZone : MonoBehaviour
{
    void Reset()
    {
        EnsureTriggerCollider();
    }

    void Awake()
    {
        EnsureTriggerCollider();
    }

    void OnTriggerEnter(Collider other)
    {
        PlayerRespawn respawn = PlayerRespawnResolver.ResolveFromCollider(other);
        if (respawn == null)
            return;

        respawn.SetCheckpoint(transform);
    }

    void EnsureTriggerCollider()
    {
        Collider zoneCollider = GetComponent<Collider>();
        if (zoneCollider != null)
            zoneCollider.isTrigger = true;
    }
}