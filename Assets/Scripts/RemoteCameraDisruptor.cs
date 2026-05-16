using UnityEngine;

[DisallowMultipleComponent]
public class RemoteCameraDisruptor : MonoBehaviour
{
    const float PlayerLookupInterval = 1f;

    [Header("Interaction")]
    public float interactionDistance = 2.5f;
    public KeyCode pickupKey = KeyCode.E;
    public KeyCode useKey = KeyCode.R;

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

    void Update()
    {
        if (!TryGetPlayer(out PlayerMovement player))
            return;

        if (!isPickedUp && Input.GetKeyDown(pickupKey))
        {
            float distanceToPickup = Vector3.Distance(transform.position, player.transform.position);
            if (distanceToPickup <= interactionDistance)
                PickUpRemote();
        }

        if (isPickedUp && !isUsed && Input.GetKeyDown(useKey))
            UseRemote();
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

    void PickUpRemote()
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

    void UseRemote()
    {
        CameraZone nearestZone = FindNearestCameraZone();
        if (nearestZone == null)
            return;

        CameraZoneForkSequence sequence = nearestZone.GetComponentInParent<CameraZoneForkSequence>();
        if (sequence != null)
            sequence.DisableSequenceByRemote();
        else
            nearestZone.DisableByRemote();

        isUsed = true;
    }

    CameraZone FindNearestCameraZone()
    {
        if (!TryGetPlayer(out PlayerMovement player))
            return null;

        CameraZone[] allZones = FindObjectsByType<CameraZone>(FindObjectsSortMode.None);
        CameraZone nearestZone = null;
        float nearestDistance = float.MaxValue;
        Vector3 playerPosition = player.transform.position;

        for (int i = 0; i < allZones.Length; i++)
        {
            CameraZone zone = allZones[i];
            if (zone == null || zone.IsPermanentlyDisabledByRemote)
                continue;

            float distance = Vector3.Distance(playerPosition, zone.transform.position);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestZone = zone;
            }
        }

        return nearestZone;
    }
}

public static class RemoteCameraDisruptorBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void SetupRemoteDisruptor()
    {
        GameObject remoteObject = GameObject.Find("Remote");
        if (remoteObject == null)
            return;

        if (remoteObject.GetComponent<RemoteCameraDisruptor>() == null)
            remoteObject.AddComponent<RemoteCameraDisruptor>();
    }
}