using UnityEngine;

public static class StealthLevelBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void SetupStealthZones()
    {
        GameObject player = FindPlayerObject();
        if (player == null)
        {
            Debug.LogWarning("StealthLevelBootstrap: player object was not found, PlayerRespawn was not added.");
            return;
        }

        if (player.GetComponent<PlayerRespawn>() == null)
            player.AddComponent<PlayerRespawn>();
    }
static GameObject FindPlayerObject()
    {
        GameObject namedPlayer = GameObject.Find("player");
        if (namedPlayer != null)
            return namedPlayer;

        PlayerMovement[] players = Object.FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None);
        foreach (PlayerMovement candidate in players)
        {
            if (candidate != null && candidate.isMainPlayer)
                return candidate.gameObject;
        }

        if (players.Length > 0 && players[0] != null)
            return players[0].gameObject;

        CharacterController[] controllers = Object.FindObjectsByType<CharacterController>(FindObjectsSortMode.None);
        foreach (CharacterController controller in controllers)
        {
            if (controller != null)
                return controller.gameObject;
        }

        return null;
    }
}