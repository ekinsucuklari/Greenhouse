using UnityEngine;
using System.IO;

public class DataLogger : MonoBehaviour
{
    public static DataLogger Instance { get; private set; }

    [Header("Log Ayarları")]
    public float logInterval = 5f;          // Kaç saniyede bir satır yazar
    public bool loggingEnabled = true;

    [SerializeField] private float timeSinceLastLog;
    [SerializeField] private string logFilePath;
    [SerializeField] private int rowCount;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        // Dosya adı: greenhouse_log_2026-04-09_143022.csv gibi
        string timestamp = System.DateTime.Now.ToString("yyyy-MM-dd_HHmmss");
        logFilePath = Path.Combine(Application.persistentDataPath,
            $"greenhouse_log_{timestamp}.csv");

        // Başlık satırını yaz
        string header = "simTime,dayCount,hourOfDay," +
                        "insideTemp,insideHumidity,lightLux,co2," +
                        "soilMoisture,soilEC,soilPH," +
                        "growthStage,plantHealth,accumulatedGDD," +
                        "outsideTemp,solarRadiation," +
                        "fanActive,heaterActive,irrigationActive," +
                        "misterActive,growLightActive";
        File.WriteAllText(logFilePath, header + "\n");

        Debug.Log($"[DataLogger] Log dosyası: {logFilePath}");
    }

    void Update()
    {
        if (!loggingEnabled) return;
        if (GreenhouseManager.Instance == null) return;
        if (SimulationClock.Instance == null) return;

        timeSinceLastLog += Time.deltaTime * SimulationClock.Instance.timeScale;

        if (timeSinceLastLog < logInterval) return;
        timeSinceLastLog = 0f;

        WriteRow();
    }

    private void WriteRow()
    {
        var gm = GreenhouseManager.Instance;
        var clock = SimulationClock.Instance;

        string line = string.Format(
            "{0:F1},{1},{2:F2}," +
            "{3:F2},{4:F2},{5:F0},{6:F0}," +
            "{7:F2},{8:F2},{9:F2}," +
            "{10:F4},{11:F4},{12:F1}," +
            "{13:F2},{14:F1}," +
            "{15},{16},{17},{18},{19}",
            clock.SimTime, clock.DayCount, clock.HourOfDay,
            gm.airState.temperature, gm.airState.humidity,
            gm.airState.lightLux, gm.airState.co2,
            gm.soilState.moisture, gm.soilState.ec, gm.soilState.ph,
            gm.plantState.growthStage, gm.plantState.health,
            gm.plantState.accumulatedGDD,
            gm.outdoorState.outsideTemp, gm.outdoorState.solarRadiation,
            gm.fanActive ? 1 : 0, gm.heaterActive ? 1 : 0,
            gm.irrigationActive ? 1 : 0, gm.misterActive ? 1 : 0,
            gm.growLightActive ? 1 : 0
        );

        File.AppendAllText(logFilePath, line + "\n");
        rowCount++;
    }

    // Inspector'daki buton veya UI için
    public void EnableLogging() => loggingEnabled = true;
    public void DisableLogging() => loggingEnabled = false;

    public string GetLogPath() => logFilePath;
}
