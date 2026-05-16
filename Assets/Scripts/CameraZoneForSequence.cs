using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraZoneForkSequence : MonoBehaviour
{
    private const float MinDuration = 0.1f;

    [System.Serializable]
    public class ZoneStep
    {
        public CameraZone zone;
        public float activeDuration = 3f;
    }

    public List<ZoneStep> steps = new List<ZoneStep>();
    public bool startFromFirstStep = true;

    private Coroutine sequenceCoroutine;
    private bool isDisabledByRemote;

    void OnEnable()
    {
        DisableAllZones();

        if (isDisabledByRemote || steps == null || steps.Count == 0)
            return;

        sequenceCoroutine = StartCoroutine(SequenceLoop());
    }

    void OnDisable()
    {
        if (sequenceCoroutine != null)
        {
            StopCoroutine(sequenceCoroutine);
            sequenceCoroutine = null;
        }

        DisableAllZones();
    }

    public void DisableSequenceByRemote()
    {
        isDisabledByRemote = true;

        if (sequenceCoroutine != null)
        {
            StopCoroutine(sequenceCoroutine);
            sequenceCoroutine = null;
        }

        if (steps == null)
            return;

        for (int i = 0; i < steps.Count; i++)
        {
            ZoneStep step = steps[i];
            if (step == null || step.zone == null)
                continue;

            step.zone.DisableByRemote();
        }
    }

    IEnumerator SequenceLoop()
    {
        int index = startFromFirstStep ? 0 : steps.Count - 1;

        while (true)
        {
            ZoneStep step = steps[index];
            SetOnlyStepActive(index);

            float duration = step != null ? Mathf.Max(MinDuration, step.activeDuration) : MinDuration;
            yield return new WaitForSeconds(duration);

            index = (index + 1) % steps.Count;
        }
    }

    void SetOnlyStepActive(int activeIndex)
    {
        for (int i = 0; i < steps.Count; i++)
        {
            ZoneStep step = steps[i];
            if (step == null || step.zone == null)
                continue;

            step.zone.SetZoneActive(i == activeIndex);
        }
    }

    void DisableAllZones()
    {
        if (steps == null)
            return;

        for (int i = 0; i < steps.Count; i++)
        {
            ZoneStep step = steps[i];
            if (step == null || step.zone == null)
                continue;

            step.zone.SetZoneActive(false);
        }
    }
}