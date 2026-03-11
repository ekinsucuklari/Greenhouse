using UnityEngine;

/// <summary>
/// SimulationClock saatine göre sahnedeki Directional Light (güneş) konumunu,
/// şiddetini ve rengini günceller. Güneş 06:00'da doğar, 20:00'da batar;
/// gündüz aydınlanır, gece kararır.
/// </summary>
[RequireComponent(typeof(Light))]
public class DayNightLightController : MonoBehaviour
{
    [Header("Işık referansı")]
    [Tooltip("Boş bırakırsan bu objedeki Light kullanılır")]
    public Light sunLight;

    [Header("Güneş gücü")]
    public float maxIntensity = 1.2f;
    public float minIntensity = 0.05f;   // Gece tamamen sönmesin, hafif ay ışığı

    [Header("Renk (isteğe bağlı)")]
    public Color noonColor = Color.white;
    public Color sunriseSunsetColor = new Color(1f, 0.85f, 0.7f);  // Sıcak turuncu
    public Color nightColor = new Color(0.4f, 0.45f, 0.6f);       // Mavi-gri

    void Awake()
    {
        if (sunLight == null)
            sunLight = GetComponent<Light>();
    }

    void Update()
    {
        if (sunLight == null || SimulationClock.Instance == null) return;

        float hour = SimulationClock.Instance.HourOfDay;

        // WeatherSystem ile aynı mantık: 6–20 arası gündüz, sinüs tepesi öğlen
        float sunProgress = Mathf.InverseLerp(6f, 20f, hour);
        float sunCurve = Mathf.Sin(sunProgress * Mathf.PI);
        sunCurve = Mathf.Max(0f, sunCurve);

        // Güneş açısı: 6 doğu, 12 tepe, 20 batı
        float elevationDeg = 90f * sunCurve;           // 0 = ufuk, 90 = tepe
        float azimuthDeg = (hour - 6f) / 14f * 180f;  // 6→0°, 20→180°
        float elRad = elevationDeg * Mathf.Deg2Rad;
        float azRad = azimuthDeg * Mathf.Deg2Rad;

        // Güneş yönü (Unity Y = yukarı): doğu (+X) → batı (-X)
        float x = Mathf.Cos(elRad) * Mathf.Cos(azRad);
        float y = Mathf.Sin(elRad);
        float z = Mathf.Cos(elRad) * Mathf.Sin(azRad);
        Vector3 sunDirection = new Vector3(x, y, z);
        Vector3 lightDirection = -sunDirection;  // Işık güneşten sahneye doğru

        transform.rotation = Quaternion.LookRotation(lightDirection, Vector3.up);

        // Şiddet: gündüz max, gece min
        sunLight.intensity = Mathf.Lerp(minIntensity, maxIntensity, sunCurve);

        // Renk: öğlen beyaz, doğuş/batış sıcak, gece soğuk
        float colorT = sunCurve;
        float warmT = 1f - Mathf.Abs(sunCurve - 0.5f) * 2f;  // Doğuş/batışta 1
        Color targetColor = Color.Lerp(nightColor, noonColor, colorT);
        targetColor = Color.Lerp(targetColor, sunriseSunsetColor, warmT * 0.5f);
        sunLight.color = targetColor;
    }
}
