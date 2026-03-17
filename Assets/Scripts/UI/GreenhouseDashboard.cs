using UnityEngine;
using TMPro;

/// <summary>
/// Sera simülasyonundaki değerleri (SimulationClock, WeatherSystem, EnvironmentPhysics, SoilModel)
/// tek yerden okuyup Canvas'taki TMP_Text alanlarına yazar. Canvas'a veya altındaki bir GameObject'e ekle,
/// Inspector'dan ilgili Text (TMP) referanslarını sürükleyip bırak.
/// </summary>
public class GreenhouseDashboard : MonoBehaviour
{
    [Header("Zaman (SimulationClock)")]
    [Tooltip("Günün saati değeri — örn. HourOfDayTime")]
    public TMP_Text hourOfDayText;
    [Tooltip("İsteğe bağlı: Gün numarası")]
    public TMP_Text dayCountText;

    [Header("Dış ortam (WeatherSystem)")]
    public TMP_Text outsideTemperatureText;
    public TMP_Text outsideHumidityText;

    [Header("Sera içi (EnvironmentPhysics)")]
    public TMP_Text insideTemperatureText;
    public TMP_Text insideHumidityText;

    [Header("Toprak (SoilModel) — isteğe bağlı")]
    public TMP_Text soilMoistureText;
    public TMP_Text ecText;
    public TMP_Text phText;

    [Header("Güneş (WeatherSystem) — isteğe bağlı")]
    public TMP_Text solarRadiationText;

    void Update()
    {
        if (SimulationClock.Instance != null)
        {
            if (hourOfDayText != null)
            {
                float hour = SimulationClock.Instance.HourOfDay;
                int h = Mathf.FloorToInt(hour);
                int m = Mathf.FloorToInt((hour - h) * 60f);
                hourOfDayText.text = $"{h:D2}:{m:D2}";
            }
            if (dayCountText != null)
                dayCountText.text = SimulationClock.Instance.DayCount.ToString();
        }

        if (WeatherSystem.Instance != null)
        {
            if (outsideTemperatureText != null)
                outsideTemperatureText.text = $"{WeatherSystem.Instance.OutsideTemp:F1} °C";
            if (outsideHumidityText != null)
                outsideHumidityText.text = $"{WeatherSystem.Instance.OutsideHumidity:F0} %";
            if (solarRadiationText != null)
                solarRadiationText.text = $"{WeatherSystem.Instance.SolarRadiation:F0} W/m²";
        }

        if (EnvironmentPhysics.Instance != null)
        {
            if (insideTemperatureText != null)
                insideTemperatureText.text = $"{GreenhouseManager.Instance.airState.temperature:F1} °C";
            if (insideHumidityText != null)
                insideHumidityText.text = $"{GreenhouseManager.Instance.airState.humidity:F0} %";
        }

        if (GreenhouseManager.Instance != null)
        {
            if (soilMoistureText != null)
                soilMoistureText.text = $"{GreenhouseManager.Instance.soilState.moisture:F1} %";
            if (ecText != null)
                ecText.text = $"{GreenhouseManager.Instance.soilState.ec:F2} mS/cm";
            if (phText != null)
                phText.text = GreenhouseManager.Instance.soilState.ph.ToString("F1");
        }
    }
}
