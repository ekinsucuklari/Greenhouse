using UnityEngine;

public class WeatherSystem : MonoBehaviour
{
    [Header("Dış Sıcaklık (°C)")]
    public float nightTempMin = 10f;     // Gece en düşük
    public float dayTempMax = 32f;       // Öğlen en yüksek

    [Header("Güneş")]
    public float maxSolarRadiation = 800f;  // Watt/m² (açık havada max)
    [Range(0f, 1f)]
    public float cloudCover = 0f;           // 0 = açık, 1 = tam bulutlu

    public static WeatherSystem Instance { get; private set; }

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    // Diğer scriptlerin okuyacağı değerler
    public float OutsideTemp { get; private set; }
    public float SolarRadiation { get; private set; }
    public float OutsideHumidity { get; private set; }

    [SerializeField] private float outsideTemp;
    [SerializeField] private float solarRadiation;
    [SerializeField] private float outsideHumidity;

    void FixedUpdate()
    {
        if (SimulationClock.Instance == null) return;

        float hour = SimulationClock.Instance.HourOfDay;

        // Dış sıcaklık — sinüs dalgası (gece soğuk, öğlen sıcak)
        // Tepe noktası saat 14:00, en düşük saat 04:00
        float tempCycle = Mathf.Sin((hour - 4f) / 24f * 2f * Mathf.PI);
        float tempRange = (dayTempMax - nightTempMin) / 2f;
        float tempMid = (dayTempMax + nightTempMin) / 2f;
        outsideTemp = tempMid + tempRange * tempCycle;
        OutsideTemp = outsideTemp;

        // Güneş radyasyonu — sabah 6'da doğar, akşam 20'de batar
        float sunAngle = Mathf.InverseLerp(6f, 20f, hour);   // 0→1→0
        float sunCurve = Mathf.Sin(sunAngle * Mathf.PI);     // tepesi öğlen 13'te
        solarRadiation = maxSolarRadiation * sunCurve * (1f - cloudCover);
        solarRadiation = Mathf.Max(0f, solarRadiation);
        SolarRadiation = solarRadiation;

        // Dış nem — geceleri yüksek, öğlen düşük (basit model)
        outsideHumidity = Mathf.Lerp(80f, 40f, sunCurve);
        OutsideHumidity = outsideHumidity;
    }

    // Senaryo sistemi için — ani bulut olayı
    public void SetCloudCover(float amount)
    {
        cloudCover = Mathf.Clamp01(amount);
    }
}