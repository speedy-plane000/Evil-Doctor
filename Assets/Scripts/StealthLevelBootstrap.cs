using UnityEngine;

public static class StealthLevelBootstrap
{
    private const float CheckpointEmissionIntensity = 2f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void SetupStealthZones()
    {
        Transform stairs = GameObject.Find("stairs")?.transform;
        if (stairs == null)
            return;

        GameObject player = GameObject.Find("player");
        if (player == null)
            return;

        if (player.GetComponent<PlayerRespawn>() == null)
            player.AddComponent<PlayerRespawn>();

        CreateCheckpoint(stairs, "Checkpoint_Floor1", new Vector3(-1f, 1.3f, 4.6f));
        CreateCheckpoint(stairs, "Checkpoint_Floor2", new Vector3(-1f, 2.6f, 5.2f));

        CreateCameraZone(stairs, "CameraZone_Floor1", new Vector3(0.9f, 1.31f, 4.7f), 0.75f);
        CreateCameraZone(stairs, "CameraZone_Floor2", new Vector3(0.9f, 2.61f, 5.3f), 0.75f);
    }

    static void CreateCheckpoint(Transform parent, string name, Vector3 localPosition)
    {
        if (parent.Find(name) != null)
            return;

        GameObject checkpoint = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        checkpoint.name = name;
        checkpoint.transform.SetParent(parent, false);
        checkpoint.transform.localPosition = localPosition;
        checkpoint.transform.localRotation = Quaternion.identity;
        checkpoint.transform.localScale = new Vector3(0.35f, 0.02f, 0.35f);

        Collider defaultCollider = checkpoint.GetComponent<Collider>();
        if (defaultCollider != null)
            Object.Destroy(defaultCollider);

        SphereCollider trigger = checkpoint.AddComponent<SphereCollider>();
        trigger.isTrigger = true;
        trigger.radius = 0.7f;

        Renderer renderer = checkpoint.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material mat = renderer.material;
            Color checkpointColor = new Color(0.2f, 1f, 0.2f, 1f);
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", checkpointColor);
            if (mat.HasProperty("_Color"))
                mat.SetColor("_Color", checkpointColor);
            if (mat.HasProperty("_EmissionColor"))
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", checkpointColor * CheckpointEmissionIntensity);
            }
        }

        checkpoint.AddComponent<CheckpointZone>();
    }

    static void CreateCameraZone(Transform parent, string name, Vector3 localPosition, float radius)
    {
        if (parent.Find(name) != null)
            return;

        GameObject cameraZone = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        cameraZone.name = name;
        cameraZone.transform.SetParent(parent, false);
        cameraZone.transform.localPosition = localPosition;
        cameraZone.transform.localRotation = Quaternion.identity;
        cameraZone.transform.localScale = new Vector3(radius, 0.02f, radius);

        Collider defaultCollider = cameraZone.GetComponent<Collider>();
        if (defaultCollider != null)
            Object.Destroy(defaultCollider);

        SphereCollider trigger = cameraZone.AddComponent<SphereCollider>();
        trigger.isTrigger = true;
        trigger.radius = radius;

        cameraZone.AddComponent<CameraZone>();
    }
}