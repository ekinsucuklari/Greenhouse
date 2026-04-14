using System;
using System.Collections.Generic;
using UnityEngine;

public enum DashboardActuatorKey
{
    Fan,
    Heater,
    Irrigation,
    Mister,
    GrowLight
}

[Serializable]
public class DashboardActuatorConfig
{
    public DashboardActuatorKey key;
    public bool enabled = true;
    public string titleOverride = "";
}

public class DashboardActuatorsRuntime : MonoBehaviour
{
    [Header("Data Source")]
    public GreenhouseManager greenhouseManager;

    [Header("Prefab / Parent")]
    public ActuatorItemView actuatorItemPrefab;
    public Transform actuatorsParent;

    [Header("Refresh")]
    public float refreshIntervalSeconds = 0.25f;

    [Header("Selected Actuators")]
    public List<DashboardActuatorConfig> actuators = new List<DashboardActuatorConfig>();

    private readonly List<ActuatorItemView> _items = new List<ActuatorItemView>();
    private float _nextRefreshTime;

    private void Awake()
    {
        if (greenhouseManager == null)
            greenhouseManager = GreenhouseManager.Instance;
    }

    private void Start()
    {
        RebuildUI();
    }

    private void Update()
    {
        if (Time.time < _nextRefreshTime)
            return;

        _nextRefreshTime = Time.time + refreshIntervalSeconds;
        RefreshValues();
    }

    [ContextMenu("Rebuild Actuators UI")]
    public void RebuildUI()
    {
        ClearRuntimeChildren();
        _items.Clear();

        if (actuatorItemPrefab == null || actuatorsParent == null)
        {
            Debug.LogWarning("[DashboardActuatorsRuntime] actuatorItemPrefab or actuatorsParent missing.");
            return;
        }

        for (int i = 0; i < actuators.Count; i++)
        {
            DashboardActuatorConfig cfg = actuators[i];
            if (!cfg.enabled)
                continue;

            ActuatorItemView item = Instantiate(actuatorItemPrefab, actuatorsParent);
            item.name = $"Actuator_{cfg.key}";
            item.Configure(
                string.IsNullOrWhiteSpace(cfg.titleOverride) ? DefaultTitle(cfg.key) : cfg.titleOverride,
                BuildGetter(cfg.key),
                BuildSetter(cfg.key));

            _items.Add(item);
        }
    }

    public void RefreshValues()
    {
        if (_items.Count == 0)
            return;

        if (greenhouseManager == null)
            greenhouseManager = GreenhouseManager.Instance;

        for (int i = 0; i < _items.Count; i++)
        {
            if (_items[i] != null)
                _items[i].RefreshFromState();
        }
    }

    private Func<bool> BuildGetter(DashboardActuatorKey key)
    {
        return () =>
        {
            GreenhouseManager gm = GetManager();
            if (gm == null) return false;

            switch (key)
            {
                case DashboardActuatorKey.Fan: return gm.fanActive;
                case DashboardActuatorKey.Heater: return gm.heaterActive;
                case DashboardActuatorKey.Irrigation: return gm.irrigationActive;
                case DashboardActuatorKey.Mister: return gm.misterActive;
                case DashboardActuatorKey.GrowLight: return gm.growLightActive;
                default: return false;
            }
        };
    }

    private Action<bool> BuildSetter(DashboardActuatorKey key)
    {
        return value =>
        {
            GreenhouseManager gm = GetManager();
            if (gm == null) return;

            switch (key)
            {
                case DashboardActuatorKey.Fan:
                    gm.fanActive = value;
                    break;
                case DashboardActuatorKey.Heater:
                    gm.heaterActive = value;
                    break;
                case DashboardActuatorKey.Irrigation:
                    gm.irrigationActive = value;
                    break;
                case DashboardActuatorKey.Mister:
                    gm.misterActive = value;
                    break;
                case DashboardActuatorKey.GrowLight:
                    gm.growLightActive = value;
                    break;
            }
        };
    }

    private GreenhouseManager GetManager()
    {
        if (greenhouseManager == null)
            greenhouseManager = GreenhouseManager.Instance;
        return greenhouseManager;
    }

    private void ClearRuntimeChildren()
    {
        if (actuatorsParent == null)
            return;

        for (int i = actuatorsParent.childCount - 1; i >= 0; i--)
            Destroy(actuatorsParent.GetChild(i).gameObject);
    }

    private static string DefaultTitle(DashboardActuatorKey key)
    {
        switch (key)
        {
            case DashboardActuatorKey.Fan: return "Fan";
            case DashboardActuatorKey.Heater: return "Heater";
            case DashboardActuatorKey.Irrigation: return "Irrigation";
            case DashboardActuatorKey.Mister: return "Mister";
            case DashboardActuatorKey.GrowLight: return "Grow Light";
            default: return key.ToString();
        }
    }
}
