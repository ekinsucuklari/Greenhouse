using UnityEngine;

public class RuleBasedController : MonoBehaviour
{
    public CropProfile crop;

    [Header("Hysteresis Bantlari")]
    public float tempHysteresis = 2f;
    public float humidityHysteresis = 5f;
    public float soilHysteresis = 5f;

    public void Evaluate(AirState air, SoilState soil,
                         PlantState plant, GreenhouseManager gm)
    {
        if (crop == null) return;

        // === SICAKLIK KONTROLU ===

        // Fan: sicaklik cok yuksekse ac
        if (air.temperature > crop.tempMax)
            gm.fanActive = true;
        if (air.temperature < crop.tempMax - tempHysteresis)
            gm.fanActive = false;

        // Isitici: sicaklik cok dusukse ac
        if (air.temperature < crop.tempMin)
            gm.heaterActive = true;
        if (air.temperature > crop.tempMin + tempHysteresis)
            gm.heaterActive = false;

        // === NEM KONTROLU ===

        // Sisleyici: nem cok dusukse ac
        if (air.humidity < crop.humidityMin)
            gm.misterActive = true;
        if (air.humidity > crop.humidityMin + humidityHysteresis)
            gm.misterActive = false;

        // Nem cok yuksekse fan ac (sicaklik kontrolunu override eder)
        if (air.humidity > crop.humidityMax)
            gm.fanActive = true;

        // === SULAMA KONTROLU ===

        if (soil.moisture < crop.soilMoistureMin)
            gm.irrigationActive = true;
        if (soil.moisture > crop.soilMoistureMin + soilHysteresis)
            gm.irrigationActive = false;

        // === ISIK KONTROLU ===

        if (air.lightLux < crop.minLightLux * 0.75f)
            gm.growLightActive = true;
        if (air.lightLux > crop.minLightLux)
            gm.growLightActive = false;
    }
}
