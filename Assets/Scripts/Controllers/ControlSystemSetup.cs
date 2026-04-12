using UnityEngine;

public class ControlSystemSetup : MonoBehaviour
{
    [Header("Bu scripti sahneye ekle ve Play'e bas")]
    [Header("Otomatik olarak bilesenleri olusturulur")]
    public bool setupComplete = false;

    void Start()
    {
        if (setupComplete) return;

        var gm = FindFirstObjectByType<GreenhouseManager>();
        if (gm == null)
        {
            Debug.LogError("[ControlSystemSetup] GreenhouseManager bulunamadi! Sahnede GreenhouseManager objesi var mi kontrol et.");
            return;
        }

        // --- CropProfile olustur (Domates) ---
        CropProfile tomato = ScriptableObject.CreateInstance<CropProfile>();
        tomato.cropName = "Domates";
        tomato.tempMin = 18f;
        tomato.tempMax = 28f;
        tomato.tempOptimal = 24f;
        tomato.humidityMin = 50f;
        tomato.humidityMax = 80f;
        tomato.soilMoistureMin = 40f;
        tomato.soilMoistureMax = 70f;
        tomato.targetEC = 2.5f;
        tomato.targetPH = 6.5f;
        tomato.minLightLux = 20000f;
        tomato.dailyLightHours = 14f;
        tomato.baseGrowthTemp = 10f;
        tomato.requiredGDD = 1500f;

        // --- RuleBasedController ---
        if (gm.controller == null)
        {
            var controllerObj = new GameObject("RuleBasedController");
            var controller = controllerObj.AddComponent<RuleBasedController>();
            controller.crop = tomato;
            gm.controller = controller;
            Debug.Log("[ControlSystemSetup] RuleBasedController olusturuldu");
        }

        // --- Aktuatorler ---
        var actuatorParent = new GameObject("--- ACTUATORS ---");

        var fanObj = new GameObject("Fan");
        fanObj.transform.parent = actuatorParent.transform;
        var fan = fanObj.AddComponent<Fan>();

        var heaterObj = new GameObject("Heater");
        heaterObj.transform.parent = actuatorParent.transform;
        var heater = heaterObj.AddComponent<Heater>();

        var irrigObj = new GameObject("IrrigationPump");
        irrigObj.transform.parent = actuatorParent.transform;
        var irrig = irrigObj.AddComponent<IrrigationPump>();

        var misterObj = new GameObject("Mister");
        misterObj.transform.parent = actuatorParent.transform;
        var mister = misterObj.AddComponent<Mister>();

        var glObj = new GameObject("GrowLight");
        glObj.transform.parent = actuatorParent.transform;
        var growLight = glObj.AddComponent<GrowLight>();

        Debug.Log("[ControlSystemSetup] 5 aktuator olusturuldu");

        // --- EnergyTracker ---
        var trackerObj = new GameObject("EnergyTracker");
        var tracker = trackerObj.AddComponent<EnergyTracker>();
        tracker.actuators = new ActuatorBase[] { fan, heater, irrig, mister, growLight };
        gm.energyTracker = tracker;
        Debug.Log("[ControlSystemSetup] EnergyTracker olusturuldu");

        // --- ScenarioManager ---
        var scenarioObj = new GameObject("ScenarioManager");
        var scenario = scenarioObj.AddComponent<ScenarioManager>();
        scenario.gm = gm;
        gm.scenarioManager = scenario;
        Debug.Log("[ControlSystemSetup] ScenarioManager olusturuldu");

        setupComplete = true;
        Debug.Log("[ControlSystemSetup] === TUM KISI 2 BILESENLERI HAZIR ===");
    }

    void Update()
    {
        if (!setupComplete) return;

        var gm = GreenhouseManager.Instance;
        if (gm == null) return;

        // Her frame aktuator sync
        SyncActuators(gm);

        // Console'da durum goster (her 60 frame'de bir)
        if (Time.frameCount % 60 == 0)
        {
            PrintStatus(gm);
        }
    }

    void SyncActuators(GreenhouseManager gm)
    {
        var actuators = FindObjectsByType<ActuatorBase>(FindObjectsSortMode.None);
        foreach (var act in actuators)
        {
            if (act is Fan) act.isActive = gm.fanActive;
            else if (act is Heater) act.isActive = gm.heaterActive;
            else if (act is IrrigationPump) act.isActive = gm.irrigationActive;
            else if (act is Mister) act.isActive = gm.misterActive;
            else if (act is GrowLight) act.isActive = gm.growLightActive;
        }
    }

    void PrintStatus(GreenhouseManager gm)
    {
        var air = gm.airState;
        var soil = gm.soilState;
        var tracker = gm.energyTracker;

        string status = $"[SERA DURUM] " +
            $"Sicaklik: {air.temperature:F1}C | " +
            $"Nem: {air.humidity:F1}% | " +
            $"Toprak: {soil.moisture:F1}% | " +
            $"Isik: {air.lightLux:F0} lux | " +
            $"Fan: {(gm.fanActive ? "ON" : "OFF")} | " +
            $"Isitici: {(gm.heaterActive ? "ON" : "OFF")} | " +
            $"Sulama: {(gm.irrigationActive ? "ON" : "OFF")} | " +
            $"Sisleyici: {(gm.misterActive ? "ON" : "OFF")} | " +
            $"GrowLight: {(gm.growLightActive ? "ON" : "OFF")}";

        if (tracker != null)
            status += $" | Enerji: {tracker.totalEnergyWh:F1} Wh ({tracker.GetCostTL():F2} TL)";

        Debug.Log(status);
    }
}
