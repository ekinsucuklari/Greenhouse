using System.Collections;
using System;
using UnityEngine;
using UnityEngine.Networking;

[System.Serializable]
public class TelemetryPayload
{
    public float timestamp;
    public float sim_time;
    public float sim_delta_time;
    public float sim_hour_of_day;
    public int sim_day_count;

    public float air_temp;
    public float air_humidity;
    public float soil_moisture;
    public float soil_ec;
    public float soil_ph;
    public float co2;
    public float light_lux;
    public float plant_growth_stage;
    public float plant_health;
    public float plant_accumulated_gdd;
    public float outdoor_temp;
    public float outdoor_humidity;
    public float outdoor_solar_radiation;
    public float outdoor_wind_speed;
    public bool outdoor_is_cloudy;

    public bool fan_active;
    public bool heater_active;
    public bool irrigation_active;
    public bool mister_active;
    public bool grow_light_active;
}

public class TelemetryPublisher : MonoBehaviour
{
    [Header("API")]
    public string apiBaseUrl = "http://127.0.0.1:8000";
    public float sendIntervalSeconds = 1f;
    public bool verboseLogs = true;

    [Header("References")]
    public GreenhouseManager greenhouseManager;
    public SimulationClock simulationClock;
    
    private bool missingRefsLogged;
    private bool firstSuccessLogged;

    private void Start()
    {
        TryResolveReferences();
        StartCoroutine(PublishLoop());
    }

    private IEnumerator PublishLoop()
    {
        var wait = new WaitForSeconds(sendIntervalSeconds);
        while (true)
        {
            yield return SendTelemetry();
            yield return wait;
        }
    }

    private IEnumerator SendTelemetry()
    {
        TryResolveReferences();
        if (greenhouseManager == null || simulationClock == null)
        {
            if (!missingRefsLogged)
            {
                Debug.LogWarning("[TelemetryPublisher] Missing refs. Assign GreenhouseManager and SimulationClock in Inspector.");
                missingRefsLogged = true;
            }
            yield break;
        }

        TelemetryPayload payload = new TelemetryPayload
        {
            timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            sim_time = simulationClock.SimTime,
            sim_delta_time = simulationClock.DeltaTime,
            sim_hour_of_day = simulationClock.HourOfDay,
            sim_day_count = simulationClock.DayCount,

            air_temp = greenhouseManager.airState.temperature,
            air_humidity = greenhouseManager.airState.humidity,
            soil_moisture = greenhouseManager.soilState.moisture,
            soil_ec = greenhouseManager.soilState.ec,
            soil_ph = greenhouseManager.soilState.ph,
            co2 = greenhouseManager.airState.co2,
            light_lux = greenhouseManager.airState.lightLux,
            plant_growth_stage = greenhouseManager.plantState.growthStage,
            plant_health = greenhouseManager.plantState.health,
            plant_accumulated_gdd = greenhouseManager.plantState.accumulatedGDD,
            outdoor_temp = greenhouseManager.outdoorState.outsideTemp,
            outdoor_humidity = greenhouseManager.outdoorState.outsideHumidity,
            outdoor_solar_radiation = greenhouseManager.outdoorState.solarRadiation,
            outdoor_wind_speed = greenhouseManager.outdoorState.windSpeed,
            outdoor_is_cloudy = greenhouseManager.outdoorState.isCloudy,

            fan_active = greenhouseManager.fanActive,
            heater_active = greenhouseManager.heaterActive,
            irrigation_active = greenhouseManager.irrigationActive,
            mister_active = greenhouseManager.misterActive,
            grow_light_active = greenhouseManager.growLightActive
        };

        string json = JsonUtility.ToJson(payload);
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
        string endpoint = apiBaseUrl.TrimEnd('/') + "/ingest";

        using (UnityWebRequest req = new UnityWebRequest(endpoint, "POST"))
        {
            req.uploadHandler = new UploadHandlerRaw(bodyRaw);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            yield return req.SendWebRequest();
            
            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[TelemetryPublisher] POST failed: {req.error} -> {endpoint}");
                yield break;
            }
            
            if (verboseLogs && !firstSuccessLogged)
            {
                Debug.Log($"[TelemetryPublisher] First telemetry sent to {endpoint}");
                firstSuccessLogged = true;
            }
        }
    }
    
    private void TryResolveReferences()
    {
        if (greenhouseManager == null)
            greenhouseManager = GreenhouseManager.Instance;
        if (simulationClock == null)
            simulationClock = SimulationClock.Instance;
    }
}
