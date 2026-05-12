using UnityEngine;
using System.Collections.Generic;

public class FloorManager : MonoBehaviour
{
    public List<AN_Button> levers = new List<AN_Button>();
    public AN_DoorScript targetLaserDoor;
    private int activatedCount = 0;

    public void RegisterLeverPress()
    {
        activatedCount++;
        if (activatedCount >= levers.Count)
        {
            if (targetLaserDoor != null) targetLaserDoor.Action();
        }
    }
}