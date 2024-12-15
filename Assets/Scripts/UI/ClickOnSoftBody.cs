using UnityEngine;
using UnityEngine.EventSystems;

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
            Time.timeScale = 0.0f;
            _softBodyMenu.SetActive(true);
            _softBodyUIController.InitializeUI(_clothBalloon);
        }
    }

}
