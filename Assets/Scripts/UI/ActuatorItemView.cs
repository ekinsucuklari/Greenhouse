using System;
using TMPro;
using UnityEngine;

public class ActuatorItemView : MonoBehaviour
{
    [Header("UI Refs")]
    public TMP_Text actuatorNameText;
    public RadioSwitch radioSwitch;

    private Func<bool> _getter;
    private Action<bool> _setter;
    private bool _isSyncing;

    private void OnEnable()
    {
        if (radioSwitch != null)
        {
            radioSwitch.onValueChanged.RemoveListener(OnSwitchChanged);
            radioSwitch.onValueChanged.AddListener(OnSwitchChanged);
        }
    }

    private void OnDisable()
    {
        if (radioSwitch != null)
            radioSwitch.onValueChanged.RemoveListener(OnSwitchChanged);
    }

    public void Configure(string actuatorName, Func<bool> getter, Action<bool> setter)
    {
        _getter = getter;
        _setter = setter;

        if (actuatorNameText != null)
            actuatorNameText.text = actuatorName;
    }

    public void RefreshFromState()
    {
        if (_getter == null || radioSwitch == null)
            return;

        bool target = _getter.Invoke();
        if (radioSwitch.isOn == target)
            return;

        _isSyncing = true;
        radioSwitch.SetValue(target);
        _isSyncing = false;
    }

    private void OnSwitchChanged(bool value)
    {
        if (_isSyncing || _setter == null)
            return;

        _setter.Invoke(value);
    }
}
