using UnityEngine;
using UnityEngine.EventSystems;

public class ClickOnObject : MonoBehaviour, IPointerDownHandler
{
    

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
            Debug.Log("Hello");
    }

}
