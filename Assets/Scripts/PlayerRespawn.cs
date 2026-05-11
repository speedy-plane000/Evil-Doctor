using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerRespawn : MonoBehaviour
{
    private CharacterController controller;
    private PlayerMovement playerMovement;
    private Vector3 checkpointPosition;
    private Quaternion checkpointRotation;
    private bool hasCheckpoint;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        playerMovement = GetComponent<PlayerMovement>();
    }

    void Start()
    {
        if (!hasCheckpoint)
            SetCheckpoint(transform.position, transform.rotation);
    }

    public void SetCheckpoint(Vector3 position, Quaternion rotation)
    {
        checkpointPosition = position;
        checkpointRotation = rotation;
        hasCheckpoint = true;
    }

    public void SetCheckpoint(Transform checkpoint)
    {
        SetCheckpoint(checkpoint.position, checkpoint.rotation);
    }

    public void RespawnAtCheckpoint()
    {
        if (!hasCheckpoint)
            return;

        if (controller != null)
            controller.enabled = false;

        transform.SetPositionAndRotation(checkpointPosition, checkpointRotation);
        playerMovement?.ResetVerticalVelocity();

        if (controller != null)
            controller.enabled = true;
    }
}