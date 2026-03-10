using UnityEngine;

public class EnvironmentPhysics : MonoBehaviour
{
    [Header("Sera Fiziksel Özellikleri")]
    public float floorArea = 100f;           // m² (10x10 sera)
    public float wallConductance = 5f;       // Watt/°C (cam/plastik ýsý geçirgenliði)
    public float thermalCapacity = 50000f;   // Joule/°C (sera ne kadar ýsý depolar)
    public float volume = 300f;              // m³ (sera hacmi)

    [Header("Aktüatör Güçleri")]
    public float heaterPower = 3000f;        // Watt
    public float ventilationRate = 0.1f;     // m³/s (fan açýkken hava deðiþimi)
    public float misterRate = 0.05f;         // kg/s (sisleyici nem üretimi)

    [Header("Aktüatör Durumlarý — Kiþi 2 bunlarý set edecek")]
    public bool heaterActive = false;
    public bool fanActive = false;
    public bool misterActive = false;

    [Header("Sera Ýç Durumu")]
    [SerializeField] private float insideTemp = 22f;
    [SerializeField] private float insideHumidity = 60f;

    public static EnvironmentPhysics Instance { get; private set; }

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public float InsideTemp => insideTemp;
    public float InsideHumidity => insideHumidity;

    void FixedUpdate()
    {
        if (SimulationClock.Instance == null) return;
        if (WeatherSystem.Instance == null) return;

        float dt = SimulationClock.Instance.DeltaTime;
        float outsideTemp = WeatherSystem.Instance.OutsideTemp;
        float solarRad = WeatherSystem.Instance.SolarRadiation;
        float outsideHumidity = WeatherSystem.Instance.OutsideHumidity;

        UpdateTemperature(dt, outsideTemp, solarRad);
        UpdateHumidity(dt, outsideHumidity);
    }

    void UpdateTemperature(float dt, float outsideTemp, float solarRad)
    {
        // Isý kazanýmý
        float heatGain = solarRad * floorArea * 0.6f;           // Güneþten gelen ýsý
        if (heaterActive) heatGain += heaterPower;               // Isýtýcýdan gelen ýsý

        // Isý kaybý
        float tempDiff = insideTemp - outsideTemp;
        float heatLoss = wallConductance * floorArea * tempDiff; // Duvardan kayýp
        if (fanActive)
            heatLoss += ventilationRate * 1200f * tempDiff;      // Fan ile kayýp (1200 = havanýn ýsýl kapasitesi)

        // Sýcaklýðý güncelle
        float dT = (heatGain - heatLoss) * dt / thermalCapacity;
        insideTemp += dT;
        insideTemp = Mathf.Clamp(insideTemp, -10f, 60f);         // Fiziksel sýnýrlar
    }

    void UpdateHumidity(float dt, float outsideHumidity)
    {
        float humidityChange = 0f;

        // Sisleyici nem ekler
        if (misterActive) humidityChange += misterRate * dt * 10f;

        // Fan dýþarýdan hava çeker — dýþ neme doðru iter
        if (fanActive)
            humidityChange += (outsideHumidity - insideHumidity) * ventilationRate * dt;

        // Doðal nem kaybý (havalandýrma, yoðuþma)
        humidityChange -= insideHumidity * 0.0001f * dt;

        insideHumidity += humidityChange;
        insideHumidity = Mathf.Clamp(insideHumidity, 0f, 100f);
    }
}