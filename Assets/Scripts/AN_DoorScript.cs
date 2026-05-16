using UnityEngine;

public class AN_DoorScript : MonoBehaviour
{
    [Header("Для Лазеров")]
    [Tooltip("Если true, ворота станут невидимыми и неосязаемыми (пропадут)")]
    public bool DestroyOnOpen = true;

    [Header("Состояние")]
    public bool isOpened = false;
    public bool Remote = true;

    public void Action()
    {
        if (isOpened) return;

        isOpened = true;

        if (DestroyOnOpen)
        {
            
            MeshRenderer[] renderers = GetComponentsInChildren<MeshRenderer>();
            foreach (MeshRenderer rend in renderers)
            {
                rend.enabled = false;
            }

           
            Collider[] colliders = GetComponentsInChildren<Collider>();
            foreach (Collider col in colliders)
            {
                col.enabled = false;
            }
        }
        else
        {
            
            transform.Rotate(-90, 0, 0);
        }

        Debug.Log(gameObject.name + " открыт и скрыт из видимости!");
    }
}