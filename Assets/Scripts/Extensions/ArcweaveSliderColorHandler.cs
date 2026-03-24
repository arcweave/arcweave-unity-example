using UnityEngine;
using UnityEngine.UI;

public class ArcweaveSliderColorHandler : ArcweaveAttributeHandler
{
    [Header("Slider Settings")]
    public Slider slider;

    protected override void ApplyAttributeValue(string value)
    {
        if (slider == null)
        {
            Debug.LogError("Slider not assigned!");
            return;
        }

        if (ColorUtility.TryParseHtmlString(value, out Color color))
        {
            slider.fillRect.GetComponent<Image>().color = color;
            Debug.Log($"Slider color set to: {value}");
        }
        else
        {
            Debug.LogWarning($"Invalid color value: {value}");
        }
    }
} 