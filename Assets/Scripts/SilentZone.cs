using UnityEngine;

public class SilentZone : MonoBehaviour
{
    [Header("Настройки зоны")]
    [Tooltip("Точка респавна — если не задана, берётся последний checkpoint игрока")]
    public Transform overrideRespawnPoint;

    [Tooltip("Минимальный ввод, при котором считается движение")]
    public float inputThreshold = 0.1f;

    PlayerRespawn playerRespawn;
    bool playerInside;

    void OnTriggerEnter(Collider other)
    {
        PlayerRespawn respawn = PlayerRespawnResolver.ResolveFromCollider(other);
        if (respawn == null) return;

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
    }

    void Update()
    {
        if (!playerInside || playerRespawn == null) return;

        bool isMoving = Mathf.Abs(Input.GetAxis("Horizontal")) > inputThreshold
                     || Mathf.Abs(Input.GetAxis("Vertical")) > inputThreshold;

        bool isCrouching = Input.GetKey(KeyCode.LeftControl);

        if (isMoving && !isCrouching)
        {
            playerInside = false;
            playerRespawn.RespawnAtCheckpoint();
        }
    }
}