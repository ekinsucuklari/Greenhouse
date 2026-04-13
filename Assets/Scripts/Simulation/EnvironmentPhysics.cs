using UnityEngine;

public class EnvironmentPhysics : MonoBehaviour
{
    public static EnvironmentPhysics Instance { get; private set; }

    [Header("Sera Fiziksel Ozellikleri")]
    public float floorArea = 100f;
    public float wallConductance = 50f;
    public float thermalCapacity = 50000f;
    public float ventilationRate = 200f;
    public float heaterPower = 3000f;
    public float maxSolarPower = 800f;

    [Header("Dinamik Davranis Ayarlari")]
    [Tooltip("Sicakligin dengeye yaklasma zaman sabiti (sn).")]
    public float temperatureTimeConstantSec = 1800f;
    [Tooltip("Gunes radyasyonunun denge sicakligina etkisi (C per W/m2).")]
    public float solarTempGainCoeff = 0.012f;
    [Tooltip("Isitici acikken denge sicakligina eklenecek katk? (C).")]
    public float heaterTempBoost = 8f;
    [Tooltip("Fan acikken denge sicakligindan dusulecek katk? (C).")]
    public float fanCoolingBoost = 6f;
    [Tooltip("Nemin dengeye yaklasma zaman sabiti (sn).")]
    public float humidityTimeConstantSec = 900f;
    [Tooltip("Sisleyici acikken denge neme eklenecek katk? (%).")]
    public float misterHumidityBoost = 20f;
    [Tooltip("Fan acikken denge nemden dusulecek katk? (%).")]
    public float fanHumidityReduction = 12f;
    [Tooltip("Ic sicaklik disariya gore arttikca denge nemde azalis (%/C).")]
    public float heatHumidityPenalty = 0.35f;
    [Tooltip("Sayisal kararlilik icin max alt adim suresi (sn).")]
    public float maxIntegrationStep = 1f;

    [Header("Dis Ortam (Inspector'dan izle)")]
    [SerializeField] private float outsideTemp;
    [SerializeField] private float solarRadiation;
    [SerializeField] private float outsideHumidity;

    [Header("Sera Ici (Inspector'dan izle)")]
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

        // Gunes: sabah 6'da dogar, oglen 12'de tepe, aksam 18'de batar
        float solarAngle = Mathf.Max(0,
            Mathf.Sin((hour - 6f) / 12f * Mathf.PI));
        outdoor.solarRadiation = maxSolarPower * solarAngle;

        // Dis sicaklik: gece 12C, oglen 32C
        outdoor.outsideTemp = 22f +
            10f * Mathf.Sin((hour - 6f) / 24f * 2f * Mathf.PI);

        // Dis nem: gece %70, gunduz %45
        outdoor.outsideHumidity = 57.5f -
            12.5f * Mathf.Sin((hour - 6f) / 24f * 2f * Mathf.PI);

        // Inspector'da gormek icin
        outsideTemp = outdoor.outsideTemp;
        solarRadiation = outdoor.solarRadiation;
        outsideHumidity = outdoor.outsideHumidity;
    }

    public void UpdateAir(AirState air, OutdoorState outdoor,
        GreenhouseManager gm, float dt)
    {
        float remaining = Mathf.Max(0f, dt);
        float step = Mathf.Max(0.05f, maxIntegrationStep);

        while (remaining > 0f)
        {
            float subDt = Mathf.Min(step, remaining);
            IntegrateAirStep(air, outdoor, gm, subDt);
            remaining -= subDt;
        }

        // Inspector'da gormek icin
        insideTemp = air.temperature;
        insideHumidity = air.humidity;
    }

    private void IntegrateAirStep(AirState air, OutdoorState outdoor,
        GreenhouseManager gm, float dt)
    {
        float tempTau = Mathf.Max(5f, temperatureTimeConstantSec);
        float humTau = Mathf.Max(5f, humidityTimeConstantSec);

        // Denge sicakligi: dis ortam + gunes + isitici - fan sogutmasi
        float tempEquilibrium = outdoor.outsideTemp
            + (outdoor.solarRadiation * solarTempGainCoeff)
            + (gm.heaterActive ? heaterTempBoost : 0f)
            - (gm.fanActive ? fanCoolingBoost : 0f);

        float tempAlpha = 1f - Mathf.Exp(-dt / tempTau);
        air.temperature = Mathf.Lerp(air.temperature, tempEquilibrium, tempAlpha);
        air.temperature = Mathf.Clamp(air.temperature, -20f, 80f);

        // Denge nem: dis nem + sisleyici - fan kurutmasi - isi etkisi
        float humidityEquilibrium = outdoor.outsideHumidity
            + (gm.misterActive ? misterHumidityBoost : 0f)
            - (gm.fanActive ? fanHumidityReduction : 0f)
            - (Mathf.Max(0f, air.temperature - outdoor.outsideTemp) * heatHumidityPenalty);

        float humAlpha = 1f - Mathf.Exp(-dt / humTau);
        air.humidity = Mathf.Lerp(air.humidity, humidityEquilibrium, humAlpha);
        air.humidity = Mathf.Clamp(air.humidity, 10f, 100f);

        // Isik
        float naturalLight = outdoor.solarRadiation * 100f;
        float growLight = gm.growLightActive ? 25000f : 0f;
        air.lightLux = naturalLight + growLight;

        // CO2
        float co2Target = gm.fanActive ? 420f : 1000f;
        float co2Alpha = 1f - Mathf.Exp(-dt / 600f);
        air.co2 = Mathf.Lerp(air.co2, co2Target, co2Alpha);
    }
}