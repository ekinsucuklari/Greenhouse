using System.Collections.Generic;
using UnityEngine;

public enum DashboardMetricKey
{
    AirTemperature,
    AirHumidity,
    AirCo2,
    AirLightLux,
    SoilMoisture,
    SoilEc,
    SoilPh,
    PlantGrowthStage,
    PlantHealth,
    PlantAccumulatedGdd,
    OutdoorTemperature,
    OutdoorHumidity,
    OutdoorSolarRadiation,
    OutdoorWindSpeed,
    SimHourOfDay,
    SimDayCount,
    FanActive,
    HeaterActive,
    IrrigationActive,
    MisterActive,
    GrowLightActive
}

[System.Serializable]
public class DashboardMetricConfig
{
    public DashboardMetricKey key;
    public bool enabled = true;
    public string titleOverride = "";
}

public class DashboardMetricsRuntime : MonoBehaviour
{
    [Header("Data Sources")]
    public GreenhouseManager greenhouseManager;
    public SimulationClock simulationClock;

    [Header("Prefab / Parent")]
    public MetricItemView metricItemPrefab;
    public Transform metricsParent;

    [Header("Refresh")]
    public float refreshIntervalSeconds = 0.25f;

    [Header("Selected Metrics")]
    public List<DashboardMetricConfig> metrics = new List<DashboardMetricConfig>();

    private readonly List<MetricSlot> _slots = new List<MetricSlot>();
    private float _nextRefreshTime;

    [System.Serializable]
    private class MetricSlot
    {
        public DashboardMetricKey key;
        public MetricItemView view;
    }

    private void Awake()
    {
        if (greenhouseManager == null)
            greenhouseManager = GreenhouseManager.Instance;
        if (simulationClock == null)
            simulationClock = SimulationClock.Instance;
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

    [ContextMenu("Rebuild Metrics UI")]
    public void RebuildUI()
    {
        ClearRuntimeChildren();
        _slots.Clear();

        if (metricItemPrefab == null || metricsParent == null)
        {
            Debug.LogWarning("[DashboardMetricsRuntime] metricItemPrefab or metricsParent missing.");
            return;
        }

        for (int i = 0; i < metrics.Count; i++)
        {
            DashboardMetricConfig cfg = metrics[i];
            if (!cfg.enabled)
                continue;

            MetricItemView item = Instantiate(metricItemPrefab, metricsParent);
            item.name = $"Metric_{cfg.key}";
            item.SetTitle(string.IsNullOrWhiteSpace(cfg.titleOverride) ? DefaultTitle(cfg.key) : cfg.titleOverride);
            item.SetValue("--");

            _slots.Add(new MetricSlot
            {
                key = cfg.key,
                view = item
            });
        }
    }

    public void RefreshValues()
    {
        if (_slots.Count == 0)
            return;

        if (greenhouseManager == null)
            greenhouseManager = GreenhouseManager.Instance;
        if (simulationClock == null)
            simulationClock = SimulationClock.Instance;

        for (int i = 0; i < _slots.Count; i++)
        {
            MetricSlot slot = _slots[i];
            if (slot.view == null)
                continue;
            slot.view.SetValue(FormatValue(greenhouseManager, simulationClock, slot.key));
        }
    }

    private void ClearRuntimeChildren()
    {
        if (metricsParent == null)
            return;

        for (int i = metricsParent.childCount - 1; i >= 0; i--)
        {
            Destroy(metricsParent.GetChild(i).gameObject);
        }
    }

    private static string DefaultTitle(DashboardMetricKey key)
    {
        switch (key)
        {
            case DashboardMetricKey.AirTemperature: return "Inside Temperature";
            case DashboardMetricKey.AirHumidity: return "Inside Humidity";
            case DashboardMetricKey.AirCo2: return "CO2";
            case DashboardMetricKey.AirLightLux: return "Light";
            case DashboardMetricKey.SoilMoisture: return "Soil Moisture";
            case DashboardMetricKey.SoilEc: return "Soil EC";
            case DashboardMetricKey.SoilPh: return "Soil pH";
            case DashboardMetricKey.PlantGrowthStage: return "Plant Growth Stage";
            case DashboardMetricKey.PlantHealth: return "Plant Health";
            case DashboardMetricKey.PlantAccumulatedGdd: return "Plant GDD";
            case DashboardMetricKey.OutdoorTemperature: return "Outside Temperature";
            case DashboardMetricKey.OutdoorHumidity: return "Outside Humidity";
            case DashboardMetricKey.OutdoorSolarRadiation: return "Solar Radiation";
            case DashboardMetricKey.OutdoorWindSpeed: return "Wind Speed";
            case DashboardMetricKey.SimHourOfDay: return "Hour Of Day";
            case DashboardMetricKey.SimDayCount: return "Day Count";
            case DashboardMetricKey.FanActive: return "Fan";
            case DashboardMetricKey.HeaterActive: return "Heater";
            case DashboardMetricKey.IrrigationActive: return "Irrigation";
            case DashboardMetricKey.MisterActive: return "Mister";
            case DashboardMetricKey.GrowLightActive: return "Grow Light";
            default: return key.ToString();
        }
    }

    private static string FormatValue(
        GreenhouseManager gm,
        SimulationClock clock,
        DashboardMetricKey key)
    {
        if (gm == null)
            return "--";

        switch (key)
        {
            case DashboardMetricKey.AirTemperature: return $"{gm.airState.temperature:F1} C";
            case DashboardMetricKey.AirHumidity: return $"{gm.airState.humidity:F1} %";
            case DashboardMetricKey.AirCo2: return $"{gm.airState.co2:F0} ppm";
            case DashboardMetricKey.AirLightLux: return $"{gm.airState.lightLux:F0} lux";
            case DashboardMetricKey.SoilMoisture: return $"{gm.soilState.moisture:F1} %";
            case DashboardMetricKey.SoilEc: return $"{gm.soilState.ec:F2} mS/cm";
            case DashboardMetricKey.SoilPh: return $"{gm.soilState.ph:F2}";
            case DashboardMetricKey.PlantGrowthStage: return $"{gm.plantState.growthStage:F2}";
            case DashboardMetricKey.PlantHealth: return $"{gm.plantState.health:F2}";
            case DashboardMetricKey.PlantAccumulatedGdd: return $"{gm.plantState.accumulatedGDD:F1}";
            case DashboardMetricKey.OutdoorTemperature: return $"{gm.outdoorState.outsideTemp:F1} C";
            case DashboardMetricKey.OutdoorHumidity: return $"{gm.outdoorState.outsideHumidity:F1} %";
            case DashboardMetricKey.OutdoorSolarRadiation: return $"{gm.outdoorState.solarRadiation:F0} W/m2";
            case DashboardMetricKey.OutdoorWindSpeed: return $"{gm.outdoorState.windSpeed:F1} m/s";
            case DashboardMetricKey.SimHourOfDay: return clock != null ? $"{clock.HourOfDay:F2}" : "--";
            case DashboardMetricKey.SimDayCount: return clock != null ? clock.DayCount.ToString() : "--";
            case DashboardMetricKey.FanActive: return BoolText(gm.fanActive);
            case DashboardMetricKey.HeaterActive: return BoolText(gm.heaterActive);
            case DashboardMetricKey.IrrigationActive: return BoolText(gm.irrigationActive);
            case DashboardMetricKey.MisterActive: return BoolText(gm.misterActive);
            case DashboardMetricKey.GrowLightActive: return BoolText(gm.growLightActive);
            default: return "--";
        }
    }

    private static string BoolText(bool value)
    {
        return value ? "ON" : "OFF";
    }
}
