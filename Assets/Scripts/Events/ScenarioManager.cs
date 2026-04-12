using UnityEngine;
using System.Collections;

public class ScenarioManager : MonoBehaviour
{
    public GreenhouseManager gm;

    [Header("Senaryo Durumlari")]
    public bool heatWaveActive;
    public bool fanFailureActive;
    public bool powerOutageActive;

    // === SICAKLIK DALGASI ===
    public void TriggerHeatWave(float duration = 300f, float heatTemp = 40f)
    {
        if (!heatWaveActive)
            StartCoroutine(HeatWaveRoutine(duration, heatTemp));
    }

    IEnumerator HeatWaveRoutine(float duration, float heatTemp)
    {
        heatWaveActive = true;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            gm.outdoorState.outsideTemp = heatTemp;
            elapsed += Time.fixedDeltaTime * gm.simClock.timeScale;
            yield return new WaitForFixedUpdate();
        }

        heatWaveActive = false;
    }

    // === FAN ARIZASI ===
    public void TriggerFanFailure(float duration = 600f)
    {
        if (!fanFailureActive)
            StartCoroutine(FanFailureRoutine(duration));
    }

    IEnumerator FanFailureRoutine(float duration)
    {
        fanFailureActive = true;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            gm.fanActive = false;
            elapsed += Time.fixedDeltaTime * gm.simClock.timeScale;
            yield return new WaitForFixedUpdate();
        }

        fanFailureActive = false;
    }

    // === ELEKTRIK KESINTISI ===
    public void TriggerPowerOutage(float duration = 600f)
    {
        if (!powerOutageActive)
            StartCoroutine(PowerOutageRoutine(duration));
    }

    IEnumerator PowerOutageRoutine(float duration)
    {
        powerOutageActive = true;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            gm.fanActive = false;
            gm.heaterActive = false;
            gm.irrigationActive = false;
            gm.growLightActive = false;
            gm.misterActive = false;
            elapsed += Time.fixedDeltaTime * gm.simClock.timeScale;
            yield return new WaitForFixedUpdate();
        }

        powerOutageActive = false;
    }

    // === SENSOR ARIZASI ===
    public void TriggerSensorFailure(float duration = 300f)
    {
        StartCoroutine(SensorFailureRoutine(duration));
    }

    IEnumerator SensorFailureRoutine(float duration)
    {
        float elapsed = 0f;
        float fakeTemp = 15f;

        while (elapsed < duration)
        {
            gm.airState.temperature = fakeTemp;
            elapsed += Time.fixedDeltaTime * gm.simClock.timeScale;
            yield return new WaitForFixedUpdate();
        }
    }
}
