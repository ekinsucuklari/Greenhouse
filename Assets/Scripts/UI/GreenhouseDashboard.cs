using UnityEngine;
using TMPro;

/// <summary>
/// Sera simülasyon verilerini GreenhouseManager + SimulationClock uzerinden
/// tek kaynaktan okuyup Canvas'taki TMP_Text alanlarina yazar.
/// Bu yaklasim, UI tarafinda singleton karisikliklarini ve null hatalarini azaltir.
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

    private GreenhouseManager _gm;
    private SimulationClock _clock;
    private float _nextWarningTime;

    void Awake()
    {
        TryResolveReferences();
    }

    void Update()
    {
        TryResolveReferences();
        if (_gm == null)
            return;

        // Zaman
        if (_clock != null)
        {
            if (hourOfDayText != null)
            {
                float hour = _clock.HourOfDay;
                int h = Mathf.FloorToInt(hour);
                int m = Mathf.FloorToInt((hour - h) * 60f);
                hourOfDayText.text = $"{h:D2}:{m:D2}";
            }

            if (dayCountText != null)
                dayCountText.text = _clock.DayCount.ToString();
        }

        // Dis ortam (tek kaynak: GreenhouseManager.outdoorState)
        if (outsideTemperatureText != null)
            outsideTemperatureText.text = $"{_gm.outdoorState.outsideTemp:F1} °C";
        if (outsideHumidityText != null)
            outsideHumidityText.text = $"{_gm.outdoorState.outsideHumidity:F0} %";
        if (solarRadiationText != null)
            solarRadiationText.text = $"{_gm.outdoorState.solarRadiation:F0} W/m²";

        // Sera ici (tek kaynak: GreenhouseManager.airState)
        if (insideTemperatureText != null)
            insideTemperatureText.text = $"{_gm.airState.temperature:F1} °C";
        if (insideHumidityText != null)
            insideHumidityText.text = $"{_gm.airState.humidity:F0} %";

        // Toprak
        if (soilMoistureText != null)
            soilMoistureText.text = $"{_gm.soilState.moisture:F1} %";
        if (ecText != null)
            ecText.text = $"{_gm.soilState.ec:F2} mS/cm";
        if (phText != null)
            phText.text = _gm.soilState.ph.ToString("F1");
    }

    private void TryResolveReferences()
    {
        if (_gm == null)
            _gm = GreenhouseManager.Instance;
        if (_clock == null)
            _clock = SimulationClock.Instance;

        // Singleton set edilmediyse sahneden bulup baglanmayi dene.
        if (_gm == null)
            _gm = FindFirstObjectByType<GreenhouseManager>();
        if (_clock == null)
            _clock = FindFirstObjectByType<SimulationClock>();

        if ((_gm == null || _clock == null) && Time.unscaledTime >= _nextWarningTime)
        {
            _nextWarningTime = Time.unscaledTime + 2f;
            Debug.LogWarning("[GreenhouseDashboard] GreenhouseManager/SimulationClock referansi bulunamadi. Scene setup'i kontrol et.");
        }
    }
}
