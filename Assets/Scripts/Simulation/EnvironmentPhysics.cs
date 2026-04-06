using UnityEngine;

public class EnvironmentPhysics : MonoBehaviour
{
    public static EnvironmentPhysics Instance { get; private set; }

    [Header("Sera fizigi")]
    [Tooltip("Zemin alani m2; gunes kazanci ile carpilir.")]
    public float floorArea = 100f;
    [Tooltip("Ortu + govde ile disa net isi iletimi W/K.")]
    public float wallConductance = 550f;
    [Tooltip("Hava + yapi esdeger isi kapasitesi J/K.")]
    public float thermalCapacity = 50000f;
    [Tooltip("Fan acikken ek havalandirma W/K.")]
    public float ventilationRate = 200f;
    [Tooltip("Fan kapaliyken sizinti / minimal hava degisimi W/K.")]
    public float passiveAirExchange = 120f;
    [Tooltip("Gunes -> ic hava net oran (cam yansima, golge, bitki).")]
    [Range(0.02f, 0.2f)]
    public float solarGainFactor = 0.06f;
    public float heaterPower = 3000f;
    [Tooltip("Ogle tepe gunes isinimi W/m2.")]
    public float maxSolarPower = 800f;

    [Header("Dis ortam (Inspector)")]
    [SerializeField] private float outsideTemp;
    [SerializeField] private float solarRadiation;
    [SerializeField] private float outsideHumidity;

    [Header("Sera ici (Inspector)")]
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

        // G�ne�: sabah 6'da do�ar, ��len 12'de tepe, ak�am 18'de batar
        float solarAngle = Mathf.Max(0,
            Mathf.Sin((hour - 6f) / 12f * Mathf.PI));
        outdoor.solarRadiation = maxSolarPower * solarAngle;

        // D�� s�cakl�k: gece 12�C, ��len 32�C
        outdoor.outsideTemp = 22f +
            10f * Mathf.Sin((hour - 6f) / 24f * 2f * Mathf.PI);

        // D�� nem: gece %70, g�nd�z %45
        outdoor.outsideHumidity = 57.5f -
            12.5f * Mathf.Sin((hour - 6f) / 24f * 2f * Mathf.PI);

        // Inspector'da g�rmek i�in
        outsideTemp = outdoor.outsideTemp;
        solarRadiation = outdoor.solarRadiation;
        outsideHumidity = outdoor.outsideHumidity;
    }

    public void UpdateAir(AirState air, OutdoorState outdoor,
        GreenhouseManager gm, float dt)
    {
        float solarHeat = outdoor.solarRadiation * floorArea * solarGainFactor;
        float heaterHeat = gm.heaterActive ? heaterPower : 0f;

        float tempDiff = air.temperature - outdoor.outsideTemp;
        float ventLoss = gm.fanActive
            ? ventilationRate * tempDiff
            : passiveAirExchange * tempDiff;
        float wallLoss = wallConductance * tempDiff;

        float dT = (solarHeat + heaterHeat - ventLoss - wallLoss)
            / thermalCapacity;
        air.temperature += dT * dt;
        air.temperature = Mathf.Clamp(air.temperature, -5f, 48f);

        float humidityGain = gm.misterActive ? 5f : 0f;
        float humidityExchange = gm.fanActive ? 0.1f : 0.025f;
        float humidityLoss =
            (air.humidity - outdoor.outsideHumidity) * humidityExchange;
        air.humidity += (humidityGain - humidityLoss) * dt;
        air.humidity = Mathf.Clamp(air.humidity, 10f, 100f);

        // I��k
        float naturalLight = outdoor.solarRadiation * 100f;
        float growLight = gm.growLightActive ? 25000f : 0f;
        air.lightLux = naturalLight + growLight;

        // CO2
        air.co2 = gm.fanActive ? 400f :
            Mathf.Lerp(air.co2, 1000f, 0.001f * dt);

        // Inspector'da g�rmek i�in
        insideTemp = air.temperature;
        insideHumidity = air.humidity;
    }
}