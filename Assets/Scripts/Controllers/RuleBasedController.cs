using UnityEngine;

public class RuleBasedController : MonoBehaviour
{
    public CropProfile crop;

    [Header("Hysteresis Bantlari")]
    public float tempHysteresis = 2f;
    public float humidityHysteresis = 5f;
    public float soilHysteresis = 5f;
    public float lightHysteresis = 0.08f;

    [Header("Minimum Calisma Sureleri (sim saniye)")]
    public float irrigationMinRunSeconds = 60f;
    public float growLightMinRunSeconds = 600f;

    [Header("Sulama Hedefi")]
    [Tooltip("Sulama bu nem seviyesine ulasana kadar (min sure dolunca) devam eder.")]
    public float irrigationStopMoisture = 55f;

    private float _irrigationRunElapsed;
    private float _growLightRunElapsed;

    public void Evaluate(AirState air, SoilState soil,
                         PlantState plant, GreenhouseManager gm, float dt)
    {
        if (crop == null) return;

        // === SICAKLIK KONTROLU ===

        if (air.temperature > crop.tempMax)
            gm.fanActive = true;
        if (air.temperature < crop.tempMax - tempHysteresis)
            gm.fanActive = false;

        if (air.temperature < crop.tempMin)
            gm.heaterActive = true;
        if (air.temperature > crop.tempMin + tempHysteresis)
            gm.heaterActive = false;

        // === NEM KONTROLU ===

        if (air.humidity < crop.humidityMin)
            gm.misterActive = true;
        if (air.humidity > crop.humidityMin + humidityHysteresis)
            gm.misterActive = false;

        if (air.humidity > crop.humidityMax)
            gm.fanActive = true;

        // === SULAMA KONTROLU ===
        // Toprak kuruyunca ac; minimum sure boyunca ve hedef neme ulasana kadar calis.

        bool wantIrrigation = gm.irrigationActive;

        if (soil.moisture < crop.soilMoistureMin)
            wantIrrigation = true;

        if (gm.irrigationActive)
        {
            _irrigationRunElapsed += dt;

            float stopAt = Mathf.Max(
                crop.soilMoistureMin + soilHysteresis,
                irrigationStopMoisture);

            bool reachedTarget = soil.moisture >= stopAt;
            bool minTimeElapsed = _irrigationRunElapsed >= irrigationMinRunSeconds;

            if (reachedTarget && minTimeElapsed)
                wantIrrigation = false;
            else if (soil.moisture >= crop.soilMoistureMax)
                wantIrrigation = false;
            else
                wantIrrigation = true;
        }
        else
        {
            _irrigationRunElapsed = 0f;
        }

        gm.irrigationActive = wantIrrigation;

        // === ISIK KONTROLU ===
        // air.lightLux grow light'i de icerir; karar icin yalnizca dogal isik kullan.

        float naturalLux = gm.outdoorState.solarRadiation * 100f;
        float lightOnThreshold = crop.minLightLux * (1f - lightHysteresis);
        float lightOffThreshold = crop.minLightLux * (1f + lightHysteresis * 0.5f);

        bool wantGrowLight = gm.growLightActive;

        if (naturalLux < lightOnThreshold)
            wantGrowLight = true;
        else if (naturalLux > lightOffThreshold)
            wantGrowLight = false;

        if (gm.growLightActive)
        {
            _growLightRunElapsed += dt;
            if (!wantGrowLight && _growLightRunElapsed < growLightMinRunSeconds)
                wantGrowLight = true;
        }
        else
        {
            _growLightRunElapsed = 0f;
        }

        gm.growLightActive = wantGrowLight;
    }
}
