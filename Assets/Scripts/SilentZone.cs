using UnityEngine;

[RequireComponent(typeof(Collider))]
public class SilentZone : MonoBehaviour
{
    [Header("Настройки зоны")]
    [Tooltip("Точка респавна — если не задана, берётся последний checkpoint игрока")]
    public Transform overrideRespawnPoint;

    PlayerRespawn playerRespawn;
    bool playerInside;
    bool skipStayChecksUntilExit;

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
        if (!TryTrackPlayer(other))
            return;

        skipStayChecksUntilExit = false;
        RespawnIfCtrlNotHeld();
    }

    void OnTriggerStay(Collider other)
    {
        if (skipStayChecksUntilExit)
            return;

        if (!playerInside && !TryTrackPlayer(other))
            return;

        RespawnIfCtrlNotHeld();
    }

    void OnTriggerExit(Collider other)
    {
        PlayerRespawn respawn = PlayerRespawnResolver.ResolveFromCollider(other);
        if (respawn == null || respawn != playerRespawn) return;

        playerInside = false;
        playerRespawn = null;
        skipStayChecksUntilExit = false;
    }

    void RespawnIfCtrlNotHeld()
    {
        if (!playerInside || playerRespawn == null) return;

        bool isCrouching = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);

        if (!isCrouching)
        {
            playerInside = false;
            playerRespawn.RespawnAtCheckpoint();
            playerRespawn = null;
            skipStayChecksUntilExit = true;
        }
    }

    void EnsureTriggerCollider()
    {
        Collider zoneCollider = GetComponent<Collider>();
        if (zoneCollider != null)
            zoneCollider.isTrigger = true;
    }

    bool TryTrackPlayer(Collider other)
    {
        PlayerRespawn respawn = PlayerRespawnResolver.ResolveFromCollider(other);
        if (respawn == null)
            return false;

        playerRespawn = respawn;
        playerInside = true;

        if (overrideRespawnPoint != null)
            playerRespawn.SetCheckpoint(overrideRespawnPoint);

        return true;
    }
}