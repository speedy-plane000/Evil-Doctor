using UnityEngine;

public class AN_DoorScript : MonoBehaviour
{
    [Header("Для Лазеров")]
    public bool DestroyOnOpen = true;

    [Header("Состояние")]
    public bool isOpened = false;
    public bool Remote = true;

    [Header("Квест")]
    public bool completesFloorQuest = false;
    public enum QuestFloor { Floor3, Floor2, Floor1 }
    public QuestFloor questFloor;

    public void Action()
    {
        if (isOpened) return;
        isOpened = true;

        if (completesFloorQuest)
        {
            switch (questFloor)
            {
                case QuestFloor.Floor3: QuestData.CompleteFloor3Main(); break;
                case QuestFloor.Floor2: QuestData.CompleteFloor2Main(); break;
                case QuestFloor.Floor1: QuestData.CompleteFloor1Main(); break;
            }
        }

        if (DestroyOnOpen)
        {
            MeshRenderer[] renderers = GetComponentsInChildren<MeshRenderer>();
            foreach (MeshRenderer rend in renderers)
                rend.enabled = false;

            Collider[] colliders = GetComponentsInChildren<Collider>();
            foreach (Collider col in colliders)
                col.enabled = false;
        }
        else
        {
            transform.Rotate(-90, 0, 0);
        }

        Debug.Log(gameObject.name + " открыт и скрыт из видимости!");
    }
}