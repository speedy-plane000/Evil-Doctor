using UnityEngine;

[DisallowMultipleComponent]
public class RemoteLeverSearchParticipant : MonoBehaviour
{
    AN_Button cachedLeverButton;

    public bool TryGetLeverButton(out AN_Button leverButton)
    {
        if (cachedLeverButton == null)
            ResolveLeverButton();

        leverButton = cachedLeverButton;
        return leverButton != null;
    }

    void Awake()
    {
        ResolveLeverButton();
    }

    void OnValidate()
    {
        if (cachedLeverButton == null)
            ResolveLeverButton();
    }

    void ResolveLeverButton()
    {
        cachedLeverButton = GetComponent<AN_Button>();
        if (cachedLeverButton == null)
            cachedLeverButton = GetComponentInParent<AN_Button>();
        if (cachedLeverButton == null)
            cachedLeverButton = GetComponentInChildren<AN_Button>(true);
    }
}