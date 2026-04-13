using TMPro;
using UnityEngine;

public class MetricItemView : MonoBehaviour
{
    [Header("UI Refs")]
    public TMP_Text titleText;
    public TMP_Text valueText;

    public void SetTitle(string text)
    {
        if (titleText != null)
            titleText.text = text;
    }

    public void SetValue(string text)
    {
        if (valueText != null)
            valueText.text = text;
    }
}
