using UnityEngine;

public class SimulationClock : MonoBehaviour
{
    // Inspector'dan ayarlanabilir değerler
    [Header("Zaman Ayarları")]
    public float timeScale = 1f;        // 1x, 60x, 3600x
    public bool isPaused = false;

    [SerializeField] private float simTime;
    [SerializeField] private float deltaTime;
    [SerializeField] private float hourOfDay;
    [SerializeField] private int dayCount;

    public float SimTime => simTime;
    public float DeltaTime => deltaTime;
    public float HourOfDay => hourOfDay;
    public int DayCount => dayCount;

    // Singleton — diğer scriptler SimulationClock.Instance ile erişir
    public static SimulationClock Instance { get; private set; }

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    void FixedUpdate()
    {
        if (isPaused) return;

        deltaTime = Time.fixedDeltaTime * timeScale;
        simTime += deltaTime;

        hourOfDay = (simTime % 86400f) / 3600f;
        dayCount = Mathf.FloorToInt(simTime / 86400f);
    }

    // UI butonları için metodlar
    public void SetTimeScale(float scale) => timeScale = scale;
    public void Pause() => isPaused = true;
    public void Resume() => isPaused = false;
}