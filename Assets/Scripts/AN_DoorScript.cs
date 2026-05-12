using UnityEngine;

public class AN_DoorScript : MonoBehaviour
{
    [Header("Для Лазеров")]
    public bool DestroyOnOpen = true; // Если это лазер, он просто исчезнет

    [Header("Состояние")]
    public bool isOpened = false;
    public bool Remote = true; // Управление только через рычаг

    public void Action()
    {
        if (isOpened) return;

        isOpened = true;

        if (DestroyOnOpen)
        {
            // Лазеры выключаются
            gameObject.SetActive(false);
        }
        else
        {
            // Если это рампа (ворота), просто повернем её один раз
            transform.Rotate(-90, 0, 0);
        }

        Debug.Log(gameObject.name + " открыт!");
    }
}