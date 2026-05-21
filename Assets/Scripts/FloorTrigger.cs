using UnityEngine;

public class FloorTrigger : MonoBehaviour
{
    public enum Floor { Floor3, Floor2, Floor1 }
    public Floor floor;

    [Tooltip("Только для Floor2 и Floor1")]
    public int leverCount;

    bool triggered;

    void OnTriggerEnter(Collider other)
    {
        if (triggered) return;
        if (PlayerRespawnResolver.ResolveFromCollider(other) == null) return;
        triggered = true;

        switch (floor)
        {
            case Floor.Floor3: QuestData.RevealFloor3(); break;
            case Floor.Floor2: QuestData.RevealFloor2(leverCount); break;
            case Floor.Floor1: QuestData.RevealFloor1(leverCount); break;
        }
    }
}