using UnityEngine;

[RequireComponent(typeof(Collider))]
public class CameraZone : MonoBehaviour
{
    public Color zoneColor = Color.red;
    public float emissionIntensity = 4f;
    private Collider zoneCollider;
    private Renderer zoneRenderer;
    private bool isPermanentlyDisabledByRemote;

    public bool IsPermanentlyDisabledByRemote => isPermanentlyDisabledByRemote;

    void Reset()
    {
        EnsureTriggerCollider();
    }

    void Awake()
    {
        EnsureTriggerCollider();
        zoneCollider = GetComponent<Collider>();
        zoneRenderer = GetComponent<Renderer>();
        ApplyVisuals();
    }

    void OnTriggerEnter(Collider other)
    {
        if (isPermanentlyDisabledByRemote)
            return;

        PlayerRespawn respawn = PlayerRespawnResolver.ResolveFromCollider(other);
        if (respawn == null)
            return;
        if (respawn.IsCameraCheckpointProtectionActive)
            return;

        respawn.RespawnAtCheckpoint();
    }

    public void SetZoneActive(bool isActive)
    {
        if (zoneCollider == null)
            zoneCollider = GetComponent<Collider>();
        if (zoneRenderer == null)
            zoneRenderer = GetComponent<Renderer>();

        bool effectiveActive = isActive && !isPermanentlyDisabledByRemote;

        if (zoneCollider != null)
            zoneCollider.enabled = effectiveActive;
        if (zoneRenderer != null)
            zoneRenderer.enabled = effectiveActive;
    }

    public void DisableByRemote()
    {
        isPermanentlyDisabledByRemote = true;
        SetZoneActive(false);
    }

    void ApplyVisuals()
    {
        if (zoneRenderer == null)
            zoneRenderer = GetComponent<Renderer>();
        if (zoneRenderer == null)
            return;

        Material materialInstance = zoneRenderer.material;

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

    void EnsureTriggerCollider()
    {
        Collider colliderInEditor = GetComponent<Collider>();
        if (colliderInEditor != null)
            colliderInEditor.isTrigger = true;
    }
}