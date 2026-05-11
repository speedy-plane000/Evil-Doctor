using UnityEngine;

[RequireComponent(typeof(Collider))]
public class CheckpointZone : MonoBehaviour
{
    void Reset()
    {
        Collider zoneCollider = GetComponent<Collider>();
        zoneCollider.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        PlayerRespawn respawn = other.GetComponent<PlayerRespawn>() ?? other.GetComponentInParent<PlayerRespawn>();
        if (respawn == null)
            return;

        respawn.SetCheckpoint(transform);
    }
}