using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CameraZone))]
public class CameraZoneBlink : MonoBehaviour
{
    private const float MinDuration = 1f;

    public float visibleDuration = 15f;
    public float hiddenDuration = 15f;
    public bool startVisible = true;

    private CameraZone cameraZone;
    private Coroutine blinkCoroutine;

    void OnEnable()
    {
        cameraZone = GetComponent<CameraZone>();
        blinkCoroutine = StartCoroutine(BlinkLoop());
    }

    void OnDisable()
    {
        if (blinkCoroutine != null)
            StopCoroutine(blinkCoroutine);

        if (cameraZone != null)
            cameraZone.SetZoneActive(true);
    }

    IEnumerator BlinkLoop()
    {
        bool isVisible = startVisible;

        while (true)
        {
            cameraZone.SetZoneActive(isVisible);

            float waitDuration = isVisible ? visibleDuration : hiddenDuration;
            yield return new WaitForSeconds(Mathf.Max(MinDuration, waitDuration));

            isVisible = !isVisible;
        }
    }
}