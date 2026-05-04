using UnityEngine;
using TMPro;

public class EnergyDashboardUI : MonoBehaviour
{
    [Header("Data Source")]
    public EnergyTracker tracker;

    [Header("UI References (optional)")]
    public TMP_Text totalEnergyLabel;   // Toplam Wh
    public TMP_Text currentPowerLabel;  // Anlik W
    public TMP_Text costLabel;          // TL maliyet
    public TMP_Text breakdownLabel;     // Aktuator basina aktif/pasif

    [Header("Settings")]
    public float pricePerKwh = 4.2f;
    public float refreshIntervalSeconds = 0.5f;

    private float _nextRefreshTime;

    void Awake()
    {
        if (tracker == null)
        {
            var gm = GreenhouseManager.Instance;
            if (gm == null) gm = FindFirstObjectByType<GreenhouseManager>();
            if (gm != null) tracker = gm.energyTracker;
        }
    }

    void Update()
    {
        if (tracker == null) return;
        if (Time.time < _nextRefreshTime) return;
        _nextRefreshTime = Time.time + refreshIntervalSeconds;

        if (totalEnergyLabel != null)
        {
            if (tracker.totalEnergyWh >= 1000f)
                totalEnergyLabel.text = $"{tracker.totalEnergyWh / 1000f:F2} kWh";
            else
                totalEnergyLabel.text = $"{tracker.totalEnergyWh:F1} Wh";
        }

        if (currentPowerLabel != null)
            currentPowerLabel.text = $"{tracker.currentPowerW:F0} W";

        if (costLabel != null)
            costLabel.text = $"{tracker.GetCostTL(pricePerKwh):F2} TL";

        if (breakdownLabel != null && tracker.actuators != null)
        {
            string s = "";
            foreach (var act in tracker.actuators)
            {
                if (act == null) continue;
                string state = act.isActive ? "ON " : "off";
                s += $"{act.actuatorName,-18} {state}  {act.totalEnergyWh,7:F1} Wh\n";
            }
            breakdownLabel.text = s;
        }
    }
}
