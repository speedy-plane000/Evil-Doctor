using UnityEngine;

public class GameBootstrap : MonoBehaviour
{
    void Start()
    {
        QuestData.Reset();
        QuestData.RevealFloor3();
    }
}