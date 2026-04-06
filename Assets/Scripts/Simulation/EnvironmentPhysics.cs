using UnityEngine;

public class EnvironmentPhysics : MonoBehaviour
{
    public static EnvironmentPhysics Instance { get; private set; }

    [Header("Sera Fiziksel Özellikleri")]
    public float floorArea = 100f;
    public float wallConductance = 50f;
    public float thermalCapacity = 50000f;
    public float ventilationRate = 200f;
    public float heaterPower = 3000f;
    public float maxSolarPower = 800f;

    [Header("Dýþ Ortam (Inspector'dan izle)")]
    [SerializeField] private float outsideTemp;
    [SerializeField] private float solarRadiation;
    [SerializeField] private float outsideHumidity;

    [Header("Sera Ýçi (Inspector'dan izle)")]
    [SerializeField] private float insideTemp;
    [SerializeField] private float insideHumidity;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void UpdateOutdoor(OutdoorState outdoor, SimulationClock clock)
    {
        float hour = clock.HourOfDay;

        // Güneþ: sabah 6'da doðar, öðlen 12'de tepe, akþam 18'de batar
        float solarAngle = Mathf.Max(0,
            Mathf.Sin((hour - 6f) / 12f * Mathf.PI));
        outdoor.solarRadiation = maxSolarPower * solarAngle;

        // Dýþ sýcaklýk: gece 12°C, öðlen 32°C
        outdoor.outsideTemp = 22f +
            10f * Mathf.Sin((hour - 6f) / 24f * 2f * Mathf.PI);

        // Dýþ nem: gece %70, gündüz %45
        outdoor.outsideHumidity = 57.5f -
            12.5f * Mathf.Sin((hour - 6f) / 24f * 2f * Mathf.PI);

        // Inspector'da görmek için
        outsideTemp = outdoor.outsideTemp;
        solarRadiation = outdoor.solarRadiation;
        outsideHumidity = outdoor.outsideHumidity;
    }

    public void UpdateAir(AirState air, OutdoorState outdoor,
        GreenhouseManager gm, float dt)
    {
        // Isý kazancý
        float solarHeat = outdoor.solarRadiation * floorArea * 0.6f;
        float heaterHeat = gm.heaterActive ? heaterPower : 0f;

        // Isý kaybý
        float tempDiff = air.temperature - outdoor.outsideTemp;
        float ventLoss = gm.fanActive ? ventilationRate * tempDiff : 0f;
        float wallLoss = wallConductance * tempDiff;

        // Sýcaklýk güncelle
        float dT = (solarHeat + heaterHeat - ventLoss - wallLoss)
            / thermalCapacity;
        air.temperature += dT * dt;
        air.temperature = Mathf.Clamp(air.temperature, -10f, 60f);

        // Nem güncelle
        float humidityGain = gm.misterActive ? 5f : 0f;
        float humidityLoss = gm.fanActive ?
            (air.humidity - outdoor.outsideHumidity) * 0.1f : 0f;
        air.humidity += (humidityGain - humidityLoss) * dt;
        air.humidity = Mathf.Clamp(air.humidity, 10f, 100f);

        // Iþýk
        float naturalLight = outdoor.solarRadiation * 100f;
        float growLight = gm.growLightActive ? 25000f : 0f;
        air.lightLux = naturalLight + growLight;

        // CO2
        air.co2 = gm.fanActive ? 400f :
            Mathf.Lerp(air.co2, 1000f, 0.001f * dt);

        // Inspector'da görmek için
        insideTemp = air.temperature;
        insideHumidity = air.humidity;
    }
}