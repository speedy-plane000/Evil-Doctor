using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic; 

public class WireTaskLogic : MonoBehaviour
{
    [Header("Связь с 3D миром")]
    public TaskStation myStation;

    [Header("UI Кнопки Проводов")]
    [Tooltip("Перетащи сюда 4 или 5 кнопок, которые будут играть роль проводов")]
    public Image[] wireImages;

    void OnEnable()
    {
        SetupWires();
    }

    void SetupWires()
    {
        if (myStation == null)
        {
            Debug.LogError($"[{gameObject.name}] Поле myStation не заполнено в Инспекторе!");
            return;
        }

        Color target = myStation.targetColor;
        List<Color> colorsToUse = new List<Color>();

        colorsToUse.Add(target);

        while (colorsToUse.Count < wireImages.Length)
        {
            Color randomColor = myStation.possibleColors[UnityEngine.Random.Range(0, myStation.possibleColors.Length)];
            if (!colorsToUse.Contains(randomColor))
            {
                colorsToUse.Add(randomColor);
            }
        }
        for (int i = 0; i < colorsToUse.Count; i++)
        {
            Color temp = colorsToUse[i];
            int randomIndex = UnityEngine.Random.Range(i, colorsToUse.Count);
            colorsToUse[i] = colorsToUse[randomIndex];
            colorsToUse[randomIndex] = temp;
        }

        for (int i = 0; i < wireImages.Length; i++)
        {
            if (wireImages[i] != null)
            {
                wireImages[i].color = colorsToUse[i];
                wireImages[i].gameObject.SetActive(true);
            }
        }
    }

    public void CutWire(int wireIndex)
    {
        if (wireIndex < 0 || wireIndex >= wireImages.Length || wireImages[wireIndex] == null) return;

        Color cutColor = wireImages[wireIndex].color;

        wireImages[wireIndex].gameObject.SetActive(false);

        if (cutColor == myStation.targetColor)
        {
            Invoke("WinDelay", 0.5f);
        }
        else
        {
            Invoke("FailDelay", 0.5f);
        }
    }

    void WinDelay()
    {
        if (myStation != null) myStation.CompleteTask();
    }

    void FailDelay()
    {
        if (myStation != null) myStation.FailTask();
    }
}