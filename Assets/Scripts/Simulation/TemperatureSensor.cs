using UnityEngine;

public class TemperatureSensor : MonoBehaviour
{
    public static TemperatureSensor Instance { get; private set; }

    [Header("Sensör Özellikleri")]
    public float noiseStdDev = 0.3f;        // Gauss gürültüsü standart sapma (°C)
    public float sampleInterval = 2f;       // Kaç saniyede bir ölçüm yapar
    public float latency = 0.5f;            // Gecikme (saniye)

    [Header("Sensör Çıktısı (Inspector'dan izle)")]
    [SerializeField] private float trueValue;
    [SerializeField] private float measuredValue;
    [SerializeField] private float timeSinceLastSample;

    public float MeasuredValue { get; private set; }

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void UpdateSensor(AirState air, float dt)
    {
        trueValue = air.temperature;

        timeSinceLastSample += dt;
        if (timeSinceLastSample < sampleInterval) return;
        timeSinceLastSample = 0f;

        measuredValue = trueValue + GaussianNoise(noiseStdDev);
        MeasuredValue = measuredValue;
    }

    private float GaussianNoise(float stdDev)
    {
        // Box-Muller dönüşümü
        float u1 = Mathf.Max(0.0001f, Random.Range(0f, 1f));
        float u2 = Random.Range(0f, 1f);
        float noise = Mathf.Sqrt(-2f * Mathf.Log(u1))
                    * Mathf.Sin(2f * Mathf.PI * u2);
        return noise * stdDev;
    }
}
