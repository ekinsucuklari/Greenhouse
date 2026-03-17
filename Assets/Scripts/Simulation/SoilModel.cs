using UnityEngine;

public class SoilModel : MonoBehaviour
{
    public static SoilModel Instance { get; private set; }

    [Header("Toprak Fiziksel Özellikleri")]
    public float irrigationRate = 2f;       // %/saniye
    public float drainageRate = 0.05f;      // doðal süzülme
    public float evaporationCoeff = 0.01f;  // sýcaklýða baðlý

    [Header("Toprak Durumu (Inspector'dan izle)")]
    [SerializeField] private float soilMoisture;
    [SerializeField] private float ec;
    [SerializeField] private float ph;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void UpdateSoil(SoilState soil, AirState air,
        GreenhouseManager gm, float dt)
    {
        // Su kaynaklarý ve kayýplarý
        float irrigation = gm.irrigationActive ? irrigationRate : 0f;

        float drainage = soil.moisture > 90f ?
            (soil.moisture - 90f) * 0.1f : 0f;

        float evaporation = evaporationCoeff * air.temperature;

        float plantUptake = gm.plantState.growthStage * 0.3f;

        // Toprak nemini güncelle
        soil.moisture += (irrigation - drainage
            - evaporation - plantUptake) * dt;
        soil.moisture = Mathf.Clamp(soil.moisture, 0f, 100f);

        // EC: sulama seyreltir
        if (gm.irrigationActive)
            soil.ec = Mathf.Lerp(soil.ec, 1.5f, 0.01f * dt);

        // pH sabit baþlangýçta
        soil.ph = Mathf.Clamp(soil.ph, 0f, 14f);

        // Inspector'da görmek için
        soilMoisture = soil.moisture;
        ec = soil.ec;
        ph = soil.ph;
    }
}