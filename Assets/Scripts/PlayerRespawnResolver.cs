using UnityEngine;

public static class PlayerRespawnResolver
{
    public static PlayerRespawn ResolveFromCollider(Collider other)
    {
        PlayerRespawn respawn = other.GetComponent<PlayerRespawn>();
        if (respawn == null)
            respawn = other.GetComponentInParent<PlayerRespawn>();

        if (respawn != null)
            return respawn;

        if (other.attachedRigidbody != null)
        {
            respawn = other.attachedRigidbody.GetComponent<PlayerRespawn>();
            if (respawn == null)
                respawn = other.attachedRigidbody.GetComponentInParent<PlayerRespawn>();
        }

        return respawn;
    }
}