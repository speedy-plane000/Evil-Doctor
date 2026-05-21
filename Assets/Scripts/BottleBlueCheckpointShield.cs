using UnityEngine;

[DisallowMultipleComponent]
public class BottleBlueCheckpointShield : MonoBehaviour
{
    const float PlayerLookupInterval = 1f;

    [Header("Interaction")]
    public float interactionDistance = 2.5f;
    public KeyCode pickupKey = KeyCode.E;
    public KeyCode useKey = KeyCode.Q;
    public float protectionDurationSeconds = 10f;

    PlayerMovement cachedPlayer;
    float nextPlayerLookupTime;
    bool isPickedUp;
    bool isUsed;

    Renderer[] pickupRenderers;
    Collider[] pickupColliders;

    void Awake()
    {
        pickupRenderers = GetComponentsInChildren<Renderer>(true);
        pickupColliders = GetComponentsInChildren<Collider>(true);
        cachedPlayer = ResolvePlayer();
    }

    public bool IsPickedUp => isPickedUp;
    public bool IsUsed => isUsed;
    
    void Update()
    {
        if (!TryGetPlayer(out PlayerMovement player))
            return;

        if (!isPickedUp && Input.GetKeyDown(pickupKey))
        {
            float distanceToPickup = Vector3.Distance(transform.position, player.transform.position);
            if (distanceToPickup <= interactionDistance)
                PickUpBottle();
        }

        if (isPickedUp && !isUsed && Input.GetKeyDown(useKey))
            UseBottle(player);
    }

    bool TryGetPlayer(out PlayerMovement player)
    {
        if (cachedPlayer == null && Time.time >= nextPlayerLookupTime)
        {
            cachedPlayer = ResolvePlayer();
            nextPlayerLookupTime = Time.time + PlayerLookupInterval;
        }

        player = cachedPlayer;
        return player != null;
    }

    PlayerMovement ResolvePlayer()
    {
        GameObject namedPlayer = GameObject.Find("player");
        if (namedPlayer != null)
        {
            PlayerMovement namedPlayerMovement = namedPlayer.GetComponent<PlayerMovement>();
            if (namedPlayerMovement != null)
                return namedPlayerMovement;
        }

        return FindObjectOfType<PlayerMovement>();
    }

    void PickUpBottle()
    {
        isPickedUp = true;
        HidePickupModel();
    }

    void HidePickupModel()
    {
        if (pickupRenderers != null)
        {
            for (int i = 0; i < pickupRenderers.Length; i++)
            {
                if (pickupRenderers[i] != null)
                    pickupRenderers[i].enabled = false;
            }
        }

        if (pickupColliders != null)
        {
            for (int i = 0; i < pickupColliders.Length; i++)
            {
                if (pickupColliders[i] != null)
                    pickupColliders[i].enabled = false;
            }
        }
    }

    void UseBottle(PlayerMovement player)
    {
        if (player == null)
            return;

        PlayerRespawn respawn = player.GetComponent<PlayerRespawn>();
        if (respawn == null)
            return;

        respawn.ActivateCameraCheckpointProtection(protectionDurationSeconds);
        isUsed = true;
    }
}

public static class BottleBlueCheckpointShieldBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void SetupBottleBlueCheckpointShield()
    {
        GameObject bottleBlueObject = GameObject.Find("bottle_blue");
        if (bottleBlueObject == null)
            return;

        if (bottleBlueObject.GetComponent<BottleBlueCheckpointShield>() == null)
            bottleBlueObject.AddComponent<BottleBlueCheckpointShield>();
    }
}