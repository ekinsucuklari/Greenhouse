using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;

public class RadioSwitch: MonoBehaviour
{
    public Image background;
    public RectTransform switchHandle;
    public TextMeshProUGUI label;
    //public TextMeshProUGUI salesText;
    public Color onColor = new Color(0.2f, 0.8f, 0.2f);   // Yeþil
    public Color offColor = new Color(0.7f, 0.7f, 0.7f);  // Gri
    public float handleOnPos = 33f;
    public float handleOffPos = -33f;
    public UnityEvent<bool> onValueChanged;
    public bool interactable = true;
    public Color disabledColor = new Color(0.7f, 0.7f, 0.7f, 1f);

    [Header("Baþlangýçta Satýþa Açýk mý?")]
    public bool defaultValue = true;

    private bool isInitialized = false;
    private bool _isOn;
    public bool isOn
    {
        get => _isOn;
        set
        {
            _isOn = value;
            UpdateVisual();
            if (isInitialized) onValueChanged?.Invoke(_isOn);
        }
    }

    private void Awake()
    {
        isInitialized = false;
        isOn = defaultValue;
        isInitialized = true;
    }



    public void Toggle()
    {
        if (!interactable) return;
        isOn = !isOn;
    }

    public void SetValue(bool value)
    {
        isOn = value;
    }

    public void UpdateVisual()
    {
        Color active = isOn ? onColor : offColor;
        Color bgColor = interactable ? active : disabledColor;
        background.color = bgColor;
        if (label != null)
        {
            label.text = isOn ? "Açýk" : "Kapalý";
            label.color = bgColor;
        }
        switchHandle.anchoredPosition = isOn ? new Vector2(handleOnPos, 0) : new Vector2(handleOffPos, 0);
    }
}