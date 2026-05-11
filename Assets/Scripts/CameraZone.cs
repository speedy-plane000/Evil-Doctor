using UnityEngine;

[RequireComponent(typeof(Collider))]
public class CameraZone : MonoBehaviour
{
    public Color zoneColor = Color.red;
    public float emissionIntensity = 4f;

    void Reset()
    {
        Collider zoneCollider = GetComponent<Collider>();
        zoneCollider.isTrigger = true;
    }

    void Awake()
    {
        ApplyVisuals();
    }

    void OnTriggerEnter(Collider other)
    {
        PlayerRespawn respawn = other.GetComponent<PlayerRespawn>() ?? other.GetComponentInParent<PlayerRespawn>();
        if (respawn == null)
            return;

        respawn.RespawnAtCheckpoint();
    }

    void ApplyVisuals()
    {
        Renderer renderer = GetComponent<Renderer>();
        if (renderer == null)
            return;

        Material materialInstance = renderer.material;

        if (materialInstance.HasProperty("_BaseColor"))
            materialInstance.SetColor("_BaseColor", zoneColor);
        if (materialInstance.HasProperty("_Color"))
            materialInstance.SetColor("_Color", zoneColor);

        if (materialInstance.HasProperty("_EmissionColor"))
        {
            materialInstance.EnableKeyword("_EMISSION");
            materialInstance.SetColor("_EmissionColor", zoneColor * emissionIntensity);
        }
    }
}