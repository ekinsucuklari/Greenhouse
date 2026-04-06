using UnityEngine;

public class GreenhouseManager : MonoBehaviour
{
    public static GreenhouseManager Instance { get; private set; }

    // === SHARED STATE (herkes okur/yazar) ===
    [HideInInspector] public AirState airState = new AirState();
    [HideInInspector] public SoilState soilState = new SoilState();
    [HideInInspector] public PlantState plantState = new PlantState();
    [HideInInspector] public OutdoorState outdoorState = new OutdoorState();

<<<<<<< Updated upstream
    // === REFERANSLAR (Inspector'dan bağlanacak) ===
    [Header("Simülasyon Bileşenleri")]
=======
    // === REFERANSLAR (Inspector'dan ba�lanacak) ===
    [Header("Sim�lasyon Bile�enleri")]
>>>>>>> Stashed changes
    public SimulationClock simClock;
    public EnvironmentPhysics envPhysics;
    public SoilModel soilModel;

<<<<<<< Updated upstream
    [Header("Kontrol Sistemleri")]
    public RuleBasedController controller;

    // Kişi 3 ekleyecek
    // public DashboardManager dashboard;

    // === AKTÜATÖR DURUMLARI (Kişi 2 yazar, Kişi 3 okur) ===
=======
    // Ki�i 2 ekleyecek
    // public RuleBasedController controller;

    // Ki�i 3 ekleyecek
    // public DashboardManager dashboard;

    // === AKT�AT�R DURUMLARI (Ki�i 2 yazar, Ki�i 3 okur) ===
>>>>>>> Stashed changes
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

    void Start()
    {
        if (simClock == null || envPhysics == null) return;
        envPhysics.UpdateOutdoor(outdoorState, simClock);
        airState.temperature = outdoorState.outsideTemp + 0.5f;
        airState.humidity = Mathf.Clamp(outdoorState.outsideHumidity + 4f, 38f, 92f);
    }

    void FixedUpdate()
    {
        if (simClock == null) return;

        float dt = Time.fixedDeltaTime * simClock.timeScale;

<<<<<<< Updated upstream
        // 1. Dış ortamı güncelle (Kişi 1)
=======
        // 1. D�� ortam� g�ncelle (Ki�i 1)
>>>>>>> Stashed changes
        if (envPhysics != null)
        {
            envPhysics.UpdateOutdoor(outdoorState, simClock);
            envPhysics.UpdateAir(airState, outdoorState, this, dt);
        }

<<<<<<< Updated upstream
        // 2. Toprağı güncelle (Kişi 1)
        if (soilModel != null)
            soilModel.UpdateSoil(soilState, airState, this, dt);

        // 3. Kontrol kararı ver (Kişi 2)
        if (controller != null)
            controller.Evaluate(airState, soilState, plantState, this);

        // 4. UI güncelle — Kişi 3 buraya ekleyecek
=======
        // 2. Topra�� g�ncelle (Ki�i 1)
        if (soilModel != null)
            soilModel.UpdateSoil(soilState, airState, this, dt);

        // 3. Kontrol karar� ver � Ki�i 2 buraya ekleyecek
        // if (controller != null)
        //     controller.Evaluate(airState, soilState, plantState, this);

        // 4. UI g�ncelle � Ki�i 3 buraya ekleyecek
>>>>>>> Stashed changes
        // if (dashboard != null)
        //     dashboard.Refresh(airState, soilState, plantState, this);
    }
}
