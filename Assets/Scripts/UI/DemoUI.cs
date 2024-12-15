using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DemoUI : MonoBehaviour
{
    [SerializeField]
    private BalloonHouse _balloonHouse;

    [SerializeField]
    private TMP_Text _pressureValueText;
    [SerializeField]
    private Slider _pressureSlider;


    void Awake()
    {
        _pressureValueText.text = _balloonHouse.BalloonList[0].Pressure.ToString("F2");
        _pressureSlider.value = _balloonHouse.BalloonList[0].Pressure;

        _pressureSlider.onValueChanged.AddListener(UpdatePressure);
    }

    private void UpdatePressure(float value)
    {
        _pressureValueText.text = value.ToString("F2");
        foreach (ClothBalloon balloon in _balloonHouse.BalloonList)
        {
            balloon.Pressure = value;
        }
    }
}
