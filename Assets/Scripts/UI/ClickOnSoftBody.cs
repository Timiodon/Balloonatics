using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// For this to work, the Main Camera needs a Physics Raycaster. Then, add this script together with a Mesh Collider to a 
/// Soft Body for which you want to update properties at runtime and set its useUI property to true.
/// You also need a canvas with a child gameobject that has a SoftBodyUIController.
/// </summary>
public class ClickOnSoftBody : MonoBehaviour, IPointerDownHandler
{
    [SerializeField]
    private GameObject _softBodyMenu;
    private SoftBodyUIController _softBodyUIController;
    private ClothBalloon _clothBalloon;

    public void Awake()
    {
        _clothBalloon = GetComponent<ClothBalloon>();    
        _softBodyUIController = _softBodyMenu.GetComponent<SoftBodyUIController>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            _softBodyUIController.InitializeUI(_clothBalloon);
        }
    }

}
