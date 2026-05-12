using UnityEngine;

public static class StealthLevelBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void SetupStealthZones()
    {
        GameObject player = GameObject.Find("player");
        if (player == null)
            return;

        if (player.GetComponent<PlayerRespawn>() == null)
            player.AddComponent<PlayerRespawn>();
    }
}