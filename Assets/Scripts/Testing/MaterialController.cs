using TMPro;
using UnityEngine;
using UnityEngine.UI; // UI를 사용하기 위해 필요

public class MaterialSliderController : MonoBehaviour
{
    public Material targetMaterial;  // 변경할 Material
    public Slider smoothnessSlider;  // Smoothness 값을 조절하는 Slider
    public Slider metallicSlider;    // Metallic 값을 조절하는 Slider
    public TextMeshProUGUI smoothnessText;      // Smoothness 값 표시
    public TextMeshProUGUI metallicText;        // Metallic 값 표시

    void Start()
    {
        // 초기 값 설정 (슬라이더가 값 변경 시 반영되도록)
        if (smoothnessSlider != null)
        {
            smoothnessSlider.onValueChanged.AddListener(SetSmoothness);
            SetSmoothness(smoothnessSlider.value); // 초기값 반영
        }

        if (metallicSlider != null)
        {
            metallicSlider.onValueChanged.AddListener(SetMetallic);
            SetMetallic(metallicSlider.value); // 초기값 반영
        }
    }

    // Smoothness 값을 변경
    public void SetSmoothness(float value)
    {
        if (targetMaterial != null)
        {
            targetMaterial.SetFloat("_Smoothness", value);
        }
        if (smoothnessText != null)
        {
            smoothnessText.text = "Smoothness: " + value.ToString("F2");
        }
    }

    // Metallic 값을 변경
    public void SetMetallic(float value)
    {
        if (targetMaterial != null)
        {
            targetMaterial.SetFloat("_Metallic", value);
        }
        if (metallicText != null)
        {
            metallicText.text = "Metallic: " + value.ToString("F2");
        }
    }
}
