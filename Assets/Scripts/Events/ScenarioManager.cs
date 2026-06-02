using UnityEngine;
using System.Collections;

public class ScenarioManager : MonoBehaviour
{
    public GreenhouseManager gm;

    [Header("Senaryo Ayarlari")]
    public float heatWaveTemp = 40f;

    [Header("Senaryo Durumlari")]
    public bool heatWaveActive;
    public bool fanFailureActive;
    public bool powerOutageActive;

    private Coroutine _heatWaveCo;
    private Coroutine _fanFailureCo;
    private Coroutine _powerOutageCo;

    public void ToggleHeatWave()
    {
        if (heatWaveActive) StopHeatWave();
        else StartHeatWave();
    }

    public void StartHeatWave()
    {
        if (gm == null) return;
        StopHeatWave();
        _heatWaveCo = StartCoroutine(HeatWaveRoutine());
    }

    public void StopHeatWave()
    {
        if (_heatWaveCo != null)
        {
            StopCoroutine(_heatWaveCo);
            _heatWaveCo = null;
        }
        heatWaveActive = false;
    }

    IEnumerator HeatWaveRoutine()
    {
        heatWaveActive = true;
        while (heatWaveActive)
        {
            gm.outdoorState.outsideTemp = heatWaveTemp;
            yield return new WaitForFixedUpdate();
        }
    }

    public void ToggleFanFailure()
    {
        if (fanFailureActive) StopFanFailure();
        else StartFanFailure();
    }

    public void StartFanFailure()
    {
        if (gm == null) return;
        StopFanFailure();
        _fanFailureCo = StartCoroutine(FanFailureRoutine());
    }

    public void StopFanFailure()
    {
        if (_fanFailureCo != null)
        {
            StopCoroutine(_fanFailureCo);
            _fanFailureCo = null;
        }
        fanFailureActive = false;
    }

    IEnumerator FanFailureRoutine()
    {
        fanFailureActive = true;
        while (fanFailureActive)
        {
            gm.fanActive = false;
            yield return new WaitForFixedUpdate();
        }
    }

    public void TogglePowerOutage()
    {
        if (powerOutageActive) StopPowerOutage();
        else StartPowerOutage();
    }

    public void StartPowerOutage()
    {
        if (gm == null) return;
        StopPowerOutage();
        _powerOutageCo = StartCoroutine(PowerOutageRoutine());
    }

    public void StopPowerOutage()
    {
        if (_powerOutageCo != null)
        {
            StopCoroutine(_powerOutageCo);
            _powerOutageCo = null;
        }
        powerOutageActive = false;
    }

    IEnumerator PowerOutageRoutine()
    {
        powerOutageActive = true;
        while (powerOutageActive)
        {
            gm.fanActive = false;
            gm.heaterActive = false;
            gm.irrigationActive = false;
            gm.growLightActive = false;
            gm.misterActive = false;
            yield return new WaitForFixedUpdate();
        }
    }

    // Geriye uyumluluk
    public void TriggerHeatWave(float duration = 300f, float heatTemp = 40f)
    {
        heatWaveTemp = heatTemp;
        ToggleHeatWave();
    }

    public void TriggerFanFailure(float duration = 600f) => ToggleFanFailure();
    public void TriggerPowerOutage(float duration = 600f) => TogglePowerOutage();

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
