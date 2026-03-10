using UnityEngine;

public class SoilModel : MonoBehaviour
{
    [Header("Toprak Fiziksel Özellikleri")]
    public float soilVolume = 1000f;         // litre (toplam toprak hacmi)
    public float drainageRate = 0.01f;       // litre/s (drenaj hýzý)

    [Header("Aktüatör Durumlarý — Kiþi 2 bunlarý set edecek")]
    public bool irrigationActive = false;
    public float irrigationRate = 0.5f;      // litre/s

    [Header("Toprak Durumu")]
    [SerializeField] private float soilMoisture = 60f;   // % (0-100)
    [SerializeField] private float ec = 2.0f;            // mS/cm
    [SerializeField] private float ph = 6.5f;            // pH

    public float SoilMoisture => soilMoisture;
    public float EC => ec;
    public float PH => ph;

    public static SoilModel Instance { get; private set; }

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    void FixedUpdate()
    {
        if (SimulationClock.Instance == null) return;

        float dt = SimulationClock.Instance.DeltaTime;
        float temp = EnvironmentPhysics.Instance != null
            ? EnvironmentPhysics.Instance.InsideTemp
            : 22f;

        UpdateSoilMoisture(dt, temp);
        UpdateEC(dt);
    }

    void UpdateSoilMoisture(float dt, float temp)
    {
        float change = 0f;

        // Sulama suyu ekler
        if (irrigationActive)
            change += (irrigationRate / soilVolume) * 100f * dt;

        // Drenaj — toprak %80'in üzerindeyse fazla su akar
        if (soilMoisture > 80f)
            change -= drainageRate * (soilMoisture - 80f) * dt;

        // Bitki su çekimi — PlantGrowthModel tarafýndan dýþarýdan set edilir
        change -= plantUptakeRate * dt;

        // Sýcaklýkla orantýlý buharlaþma
        float evaporation = Mathf.Max(0f, temp - 10f) * 0.001f * dt;
        change -= evaporation;

        soilMoisture += change;
        soilMoisture = Mathf.Clamp(soilMoisture, 0f, 100f);
    }

    void UpdateEC(float dt)
    {
        // Sulama seyreltir, buharlaþma yoðunlaþtýrýr
        if (irrigationActive)
            ec -= 0.001f * dt;

        ec += 0.0005f * dt;  // Doðal birikim
        ec = Mathf.Clamp(ec, 0f, 10f);
    }

    // PlantGrowthModel bu deðeri set edecek
    [HideInInspector] public float plantUptakeRate = 0.01f;

    // Kiþi 2 için manuel sulama
    public void SetIrrigation(bool state) => irrigationActive = state;
}