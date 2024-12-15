using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SoftBodyUIController : MonoBehaviour
{
    ClothBalloon _selectedClothBallon;

    [Header("Selected")]
    [SerializeField]
    private TMP_Text _selectedText;

    [Header("Collisions")]
    [SerializeField]
    private Toggle _selfColToggle;
    [SerializeField]
    private Toggle _interObjToggle;
    [SerializeField]
    private Toggle _maxSpeedToggle;

    [Header("Constraints")]
    [SerializeField]
    private Toggle _useStretchingConstraintToggle;
    [SerializeField]
    private Toggle _useOverpressureConstraintToggle;
    [SerializeField]
    private Toggle _useBendingConstraintToggle;

    [Header("Compliances")]
    [SerializeField]
    private Slider _stretchingComplianceSlider;
    [SerializeField]
    private Slider _pressureSlider;
    [SerializeField]
    private Slider _bendingComplianceSlider;


    void Awake()
    {
        _selfColToggle.onValueChanged.AddListener(UpdateSelfCollision);
        _interObjToggle.onValueChanged.AddListener(UpdateInterObjectCollisions);
        _maxSpeedToggle.onValueChanged.AddListener(UpdateMaxSpeed);

        _useStretchingConstraintToggle.onValueChanged.AddListener(UpdateStretchingConstraint);
        _useOverpressureConstraintToggle.onValueChanged.AddListener(UpdateOverpressureConstraint);
        _useBendingConstraintToggle.onValueChanged.AddListener(UpdateBendingConstraint);

        _stretchingComplianceSlider.onValueChanged.AddListener(UpdateStretchingCompliance);
        _pressureSlider.onValueChanged.AddListener(UpdatePressure);
        _bendingComplianceSlider.onValueChanged.AddListener(UpdateBendingCompliance);
    }

    public void InitializeUI(ClothBalloon clothBalloon)
    {
        _selectedClothBallon = clothBalloon;
        _selectedText.text = clothBalloon.name;
        
        _selfColToggle.isOn = clothBalloon.HandleSelfCollision;
        _interObjToggle.isOn = clothBalloon.HandleInterObjectCollisions;
        _maxSpeedToggle.isOn = clothBalloon.EnforceMaxSpeed;

        _useStretchingConstraintToggle.isOn = clothBalloon.UseStretchingConstraint;
        _useOverpressureConstraintToggle.isOn = clothBalloon.UseOverpressureConstraint;
        _useBendingConstraintToggle.isOn = clothBalloon.UseBendingConstraint;

        _stretchingComplianceSlider.value = clothBalloon.StretchingComplianceScale;
        _pressureSlider.value = clothBalloon.Pressure;
        _bendingComplianceSlider.value = clothBalloon.BendingComplianceScale;
    }

    private void UpdateSelfCollision(bool value)
    {
        _selectedClothBallon.HandleSelfCollision = value;
    }

    private void UpdateInterObjectCollisions(bool value)
    {
        _selectedClothBallon.HandleInterObjectCollisions = value;
    }

    private void UpdateMaxSpeed(bool value)
    {
        _selectedClothBallon.EnforceMaxSpeed = value;
    }

    private void UpdateStretchingConstraint(bool value)
    {
        _selectedClothBallon.UseStretchingConstraint = value;
    }

    private void UpdateOverpressureConstraint(bool value)
    {
        _selectedClothBallon.UseOverpressureConstraint = value;
    }

    private void UpdateBendingConstraint(bool value)
    {
        _selectedClothBallon.UseBendingConstraint = value;
    }

    private void UpdateStretchingCompliance(float value)
    {
        _selectedClothBallon.StretchingComplianceScale = value;
    }

    private void UpdatePressure(float value)
    {
        _selectedClothBallon.Pressure = value;
    }

    private void UpdateBendingCompliance(float value)
    {
        _selectedClothBallon.BendingComplianceScale = value;
    }
}
