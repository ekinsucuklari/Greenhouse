using UnityEngine;

public class PlantGrowthModel : MonoBehaviour
{
    public static PlantGrowthModel Instance { get; private set; }

    [Header("Bitki Profili")]
    public float baseGrowthTemp = 10f;      // Bu sıcaklığın altında büyüme olmaz
    public float requiredGDD = 1500f;       // Hasata kadar gereken toplam GDD
    public float optimalTempMin = 18f;      // Optimal sıcaklık alt sınırı
    public float optimalTempMax = 28f;      // Optimal sıcaklık üst sınırı
    public float optimalHumidityMin = 50f;  // Optimal nem alt sınırı
    public float optimalHumidityMax = 80f;  // Optimal nem üst sınırı
    public float optimalSoilMin = 40f;      // Optimal toprak nemi alt sınırı
    public float optimalSoilMax = 70f;      // Optimal toprak nemi üst sınırı
    public float minLightLux = 5000f;       // Büyüme için minimum ışık

    [Header("Bitki Durumu (Inspector'dan izle)")]
    [SerializeField] private float growthStage;
    [SerializeField] private float health;
    [SerializeField] private float accumulatedGDD;
    [SerializeField] private string growthPhaseName;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void UpdatePlant(PlantState plant, AirState air,
        SoilState soil, float dt)
    {
        // --- GDD hesabı (saniye bazında) ---
        // Günlük GDD = max(0, avgTemp - baseTemp)
        // dt saniye cinsinden geldiği için güne çeviriyoruz
        float gddThisTick = Mathf.Max(0f, air.temperature - baseGrowthTemp)
            * (dt / 86400f);
        plant.accumulatedGDD += gddThisTick;
        plant.growthStage = Mathf.Clamp01(
            plant.accumulatedGDD / requiredGDD);

        // --- Stres hesabı ---
        float stressDamage = 0f;

        // Sıcaklık stresi
        if (air.temperature < optimalTempMin)
        {
            float deviation = optimalTempMin - air.temperature;
            stressDamage += deviation * 0.002f * dt;
        }
        else if (air.temperature > optimalTempMax)
        {
            float deviation = air.temperature - optimalTempMax;
            stressDamage += deviation * 0.003f * dt;
        }

        // Nem stresi
        if (air.humidity < optimalHumidityMin)
        {
            float deviation = optimalHumidityMin - air.humidity;
            stressDamage += deviation * 0.001f * dt;
        }
        else if (air.humidity > optimalHumidityMax)
        {
            float deviation = air.humidity - optimalHumidityMax;
            stressDamage += deviation * 0.001f * dt;  // mantar riski
        }

        // Toprak nemi stresi
        if (soil.moisture < optimalSoilMin)
        {
            float deviation = optimalSoilMin - soil.moisture;
            stressDamage += deviation * 0.002f * dt;  // susuzluk
        }
        else if (soil.moisture > optimalSoilMax)
        {
            float deviation = soil.moisture - optimalSoilMax;
            stressDamage += deviation * 0.001f * dt;  // kök çürümesi
        }

        // Işık stresi
        if (air.lightLux < minLightLux)
        {
            float lightRatio = air.lightLux / minLightLux;
            stressDamage += (1f - lightRatio) * 0.001f * dt;
        }

        // Sağlık düşür, yavaşça toparla
        plant.health -= stressDamage;
        if (stressDamage == 0f)
            plant.health += 0.0001f * dt;   // stres yoksa yavaşça iyileş
        plant.health = Mathf.Clamp01(plant.health);

        // Sağlık kötüyse büyüme yavaşlar
        if (plant.health < 0.5f)
        {
            float penalty = (0.5f - plant.health) * 2f; // 0→1 arası
            plant.accumulatedGDD -= gddThisTick * penalty * 0.5f;
            plant.accumulatedGDD = Mathf.Max(0f, plant.accumulatedGDD);
        }

        // Inspector görüntüsü
        growthStage = plant.growthStage;
        health = plant.health;
        accumulatedGDD = plant.accumulatedGDD;
        growthPhaseName = GetPhaseName(plant.growthStage);
    }

    // Büyüme aşaması adı — Kişi 3 görsel değişim için kullanabilir
    public static string GetPhaseName(float stage)
    {
        if (stage < 0.15f) return "Çimlenme";
        if (stage < 0.50f) return "Vejetatif Büyüme";
        if (stage < 0.70f) return "Çiçeklenme";
        if (stage < 1.00f) return "Meyve Olgunlaşma";
        return "Hasat Hazır";
    }

    // Kişi 3 renk değişimi için
    public static Color GetHealthColor(float health)
    {
        if (health > 0.7f) return Color.green;
        if (health > 0.4f) return Color.yellow;
        return new Color(0.55f, 0.27f, 0.07f); // kahverengi
    }
}