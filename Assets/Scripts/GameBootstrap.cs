using UnityEngine;

public class GameBootstrap : MonoBehaviour
{
    bool _pauseCreated = false;

    void Start()
    {
        QuestData.Reset();
        QuestData.RevealFloor3();

        if (Object.FindFirstObjectByType<StartMenuController>() == null)
        {
            Debug.Log("GameBootstrap: создаём StartMenuController");
            GameObject menuObject = new GameObject("StartMenuController");
            menuObject.AddComponent<StartMenuController>();
        }
    }

    void Update()
    {
        if (_pauseCreated) return;

        // Создаём PauseMenuController только когда StartMenu уже уничтожен
        if (Object.FindFirstObjectByType<StartMenuController>() != null) return;

        if (Object.FindFirstObjectByType<PauseMenuController>(FindObjectsInactive.Include) == null)
        {
            Debug.Log("GameBootstrap: создаём PauseMenuController");
            GameObject pauseObj = new GameObject("PauseMenuController");
            pauseObj.AddComponent<PauseMenuController>();
        }

        _pauseCreated = true;
    }
}