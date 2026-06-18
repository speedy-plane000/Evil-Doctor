using UnityEngine;

public class SilentZone : MonoBehaviour
{
    public Transform overrideRespawnPoint;

    PlayerRespawn playerRespawn;
    PlayerMovement playerMovement;
    bool playerInside;

    void OnTriggerEnter(Collider other)
    {
        
        PlayerRespawn respawn = PlayerRespawnResolver.ResolveFromCollider(other);
        

        if (respawn == null) return;

        playerMovement = other.GetComponentInParent<PlayerMovement>();
        if (playerMovement == null)
            playerMovement = FindObjectOfType<PlayerMovement>();

        playerRespawn = respawn;
        playerInside = true;

        if (overrideRespawnPoint != null)
            playerRespawn.SetCheckpoint(overrideRespawnPoint);
    }

    void OnTriggerExit(Collider other)
    {
        PlayerRespawn respawn = PlayerRespawnResolver.ResolveFromCollider(other);
        if (respawn == null || respawn != playerRespawn) return;

        playerInside = false;
        playerRespawn = null;
        playerMovement = null;
    }


    void Update()
    {
        if (!playerInside || playerRespawn == null || playerMovement == null) return;

        if (playerMovement.IsMakingNoise)
        {
            playerInside = false;
            playerRespawn.RespawnAtCheckpoint();
            playerMovement = null;
        }
    }
}