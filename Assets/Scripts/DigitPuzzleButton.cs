using UnityEngine;

[DisallowMultipleComponent]
public class DigitPuzzleButton : MonoBehaviour
{
    const float PlayerLookupInterval = 1f;

    public enum DigitButtonAction
    {
        Recognize,
        Clear
    }

    [Header("References")]
    public DigitDoorPuzzleController puzzleController;
    public DigitButtonAction action = DigitButtonAction.Recognize;

    [Header("Interaction")]
    public float interactionDistance = 3f;
    public KeyCode interactionKey = KeyCode.E;

    PlayerMovement cachedPlayer;
    float nextPlayerLookupTime;

    void Update()
    {
        if (!Input.GetKeyDown(interactionKey)) return;

        if (cachedPlayer == null && Time.time >= nextPlayerLookupTime)
        {
            cachedPlayer = FindObjectOfType<PlayerMovement>();
            nextPlayerLookupTime = Time.time + PlayerLookupInterval;
        }
        if (cachedPlayer == null) return;

        float distance = Vector3.Distance(transform.position, cachedPlayer.transform.position);
        if (distance > interactionDistance) return;
        if (puzzleController == null) return;

        if (action == DigitButtonAction.Recognize)
            puzzleController.RecognizeAndTryOpenDoor();
        else
            puzzleController.ClearPanel();
    }
}