using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class AlertSystem : MonoBehaviour
{
    [Header("Data Source")]
    public GreenhouseManager gm;
    public CropProfile crop;  // Esikleri buradan okur (RuleBasedController.crop ile ayni olabilir)

    [Header("UI References (optional)")]
    public GameObject alertPanel;
    public TMP_Text alertText;

    [Header("Critical Thresholds (fallback if crop is null)")]
    public float criticalTempHigh = 35f;
    public float criticalTempLow = 10f;
    public float criticalHumidityHigh = 90f;
    public float criticalSoilLow = 25f;
    public float criticalHealthLow = 0.3f;

    [Header("Settings")]
    public float refreshIntervalSeconds = 1f;

    private float _nextRefreshTime;

    void Awake()
    {
        if (gm == null) gm = GreenhouseManager.Instance;
        if (gm == null) gm = FindFirstObjectByType<GreenhouseManager>();
        if (crop == null && gm != null && gm.controller != null)
            crop = gm.controller.crop;
    }

    void Update()
    {
        if (gm == null) return;
        if (Time.time < _nextRefreshTime) return;
        _nextRefreshTime = Time.time + refreshIntervalSeconds;

        var alerts = CheckAlerts();

        if (alertPanel != null)
            alertPanel.SetActive(alerts.Count > 0);

        if (alertText != null)
        {
            if (alerts.Count == 0)
            {
                alertText.text = "No active alerts";
                alertText.color = new Color(0.55f, 0.65f, 0.78f, 1f); // muted
            }
            else
            {
                alertText.text = string.Join("\n", alerts);
                alertText.color = new Color(1f, 0.55f, 0.30f, 1f);    // turuncu
            }
        }
    }

    public List<string> CheckAlerts()
    {
        var msgs = new List<string>();
        if (gm == null) return msgs;

        var air = gm.airState;
        var soil = gm.soilState;
        var plant = gm.plantState;

        float tHigh = crop != null ? crop.tempMax + 7f : criticalTempHigh;
        float tLow  = crop != null ? crop.tempMin - 8f : criticalTempLow;
        float hHigh = crop != null ? crop.humidityMax + 10f : criticalHumidityHigh;
        float soilLow = crop != null ? crop.soilMoistureMin - 15f : criticalSoilLow;

        if (air.temperature > tHigh)
            msgs.Add($"[!] Sicaklik kritik yuksek: {air.temperature:F1} C");
        if (air.temperature < tLow)
            msgs.Add($"[!] Sicaklik kritik dusuk: {air.temperature:F1} C");
        if (air.humidity > hHigh)
            msgs.Add($"[!] Nem kritik yuksek: {air.humidity:F1} %");
        if (soil.moisture < soilLow)
            msgs.Add($"[!] Toprak cok kuru: {soil.moisture:F1} %");
        if (plant != null && plant.health < criticalHealthLow)
            msgs.Add($"[!] Bitki sagligi kritik: {plant.health:F2}");

        // Senaryo aktifse de uyari goster
        if (gm.scenarioManager != null)
        {
            if (gm.scenarioManager.heatWaveActive)    msgs.Add("[Senaryo] Sicaklik dalgasi aktif");
            if (gm.scenarioManager.fanFailureActive)  msgs.Add("[Senaryo] Fan arizali");
            if (gm.scenarioManager.powerOutageActive) msgs.Add("[Senaryo] Elektrik kesintisi");
        }

        return msgs;
    }
}
