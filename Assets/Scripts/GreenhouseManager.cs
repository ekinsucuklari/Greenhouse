using UnityEngine;

public class GreenhouseManager : MonoBehaviour
{
    public static GreenhouseManager Instance { get; private set; }

    // === SHARED STATE (herkes okur/yazar) ===
    [HideInInspector] public AirState airState = new AirState();
    [HideInInspector] public SoilState soilState = new SoilState();
    [HideInInspector] public PlantState plantState = new PlantState();
    [HideInInspector] public OutdoorState outdoorState = new OutdoorState();

    // === REFERANSLAR (Inspector'dan baðlanacak) ===
    [Header("Simülasyon Bileþenleri")]
    public SimulationClock simClock;
    public EnvironmentPhysics envPhysics;
    public SoilModel soilModel;

    // Kiþi 2 ekleyecek
    // public RuleBasedController controller;

    // Kiþi 3 ekleyecek
    // public DashboardManager dashboard;

    // === AKTÜATÖR DURUMLARI (Kiþi 2 yazar, Kiþi 3 okur) ===
    [HideInInspector] public bool fanActive;
    [HideInInspector] public bool heaterActive;
    [HideInInspector] public bool irrigationActive;
    [HideInInspector] public bool misterActive;
    [HideInInspector] public bool growLightActive;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    void FixedUpdate()
    {
        if (simClock == null) return;

        float dt = Time.fixedDeltaTime * simClock.timeScale;

        // 1. Dýþ ortamý güncelle (Kiþi 1)
        if (envPhysics != null)
        {
            envPhysics.UpdateOutdoor(outdoorState, simClock);
            envPhysics.UpdateAir(airState, outdoorState, this, dt);
        }

        // 2. Topraðý güncelle (Kiþi 1)
        if (soilModel != null)
            soilModel.UpdateSoil(soilState, airState, this, dt);

        // 3. Kontrol kararý ver — Kiþi 2 buraya ekleyecek
        // if (controller != null)
        //     controller.Evaluate(airState, soilState, plantState, this);

        // 4. UI güncelle — Kiþi 3 buraya ekleyecek
        // if (dashboard != null)
        //     dashboard.Refresh(airState, soilState, plantState, this);
    }
}