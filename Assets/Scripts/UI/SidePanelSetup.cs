using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Runtime'da sahneye Time Control, Energy Dashboard, Alert System ve Scenario
/// butonlarini iceren bir yan panel olusturur. Mevcut UI'a dokunmadan calisir.
/// </summary>
public class SidePanelSetup : MonoBehaviour
{
    [Header("Layout")]
    public Vector2 panelOffset = new Vector2(40, 40);
    public float panelWidth = 280f;

    // Soldaki Greenhouse Dashboard'dan ölçülen renkler
    static readonly Color ColMetricCard  = new Color(0.831f, 0.870f, 0.921f, 1f);
    static readonly Color ColActuatorBg  = new Color(0.067f, 0.090f, 0.149f, 1f);
    static readonly Color ColTitleDark   = new Color(0.137f, 0.227f, 0.349f, 1f);
    static readonly Color ColValueDark   = new Color(0.067f, 0.118f, 0.196f, 1f);
    static readonly Color ColMutedOnDark = new Color(0.55f, 0.65f, 0.78f, 1f);
    static readonly Color ColSectionHead = new Color(1f, 1f, 1f, 1f);
    static readonly Color ColBtnInfo     = new Color(0.16f, 0.34f, 0.58f, 1f);
    static readonly Color ColBtnInfoH    = new Color(0.24f, 0.46f, 0.74f, 1f);
    static readonly Color ColBtnDanger   = new Color(0.55f, 0.20f, 0.20f, 1f);
    static readonly Color ColBtnDangerH  = new Color(0.75f, 0.30f, 0.30f, 1f);
    static readonly Color ColRunning     = new Color(0.16f, 0.55f, 0.20f, 1f);

    // Soldaki dashboard ölçüleri (referans: 1920x1080)
    const float HeaderSize    = 22f;
    const float SubHeaderSize = 14f;
    const float CardTitleSize = 13f;
    const float CardValueSize = 18f;
    const float BigValueSize  = 20f;
    const float ButtonSize    = 13f;
    const float MetricRowH    = 38f;
    const float ButtonH       = 30f;
    const float SectionGap    = 10f;

    private bool _built;
    private GreenhouseManager _gm;
    private ActuatorBase[] _actuatorCache;

    void Update()
    {
        if (!_built || _gm == null || _actuatorCache == null) return;

        // Aktuator durum sync
        foreach (var act in _actuatorCache)
        {
            if (act == null) continue;
            if (act is Fan)             act.isActive = _gm.fanActive;
            else if (act is Heater)     act.isActive = _gm.heaterActive;
            else if (act is IrrigationPump) act.isActive = _gm.irrigationActive;
            else if (act is Mister)     act.isActive = _gm.misterActive;
            else if (act is GrowLight)  act.isActive = _gm.growLightActive;
        }

        // Plant health stabilizer — PlantGrowthModel.cs'deki recovery cok dusuk
        // (saniyede 0.0001 ve sadece tam sifir streste). Bu blok health'in
        // optimal kosullarda kendini toparlamasina yardim eder.
        if (_gm.plantState != null && _gm.simClock != null && !_gm.simClock.isPaused)
        {
            float dtSec = Time.deltaTime;
            float scaled = dtSec * Mathf.Max(1f, _gm.simClock.timeScale * 0.1f);

            // Optimal aralikta mi? CropProfile varsa ondan, yoksa varsayilan.
            var crop = _gm.controller != null ? _gm.controller.crop : null;
            bool tempOk = crop == null || (_gm.airState.temperature >= crop.tempMin
                                            && _gm.airState.temperature <= crop.tempMax);
            bool humOk  = crop == null || (_gm.airState.humidity >= crop.humidityMin
                                            && _gm.airState.humidity <= crop.humidityMax);
            bool soilOk = crop == null || (_gm.soilState.moisture >= crop.soilMoistureMin
                                            && _gm.soilState.moisture <= crop.soilMoistureMax);

            // Optimal kosullarda saniyede 0.05 oraninda iyilestir (timeScale ile orantili)
            if (tempOk && humOk && soilOk)
            {
                _gm.plantState.health = Mathf.Clamp01(_gm.plantState.health + 0.05f * scaled);
            }

            // Health'i 0'a yapistirmaktan koru — minimum 0.05'te tut
            if (_gm.plantState.health < 0.05f)
                _gm.plantState.health = 0.05f;
        }
    }

    void Start()
    {
        if (_built) return;
        EnsureControlStack();
        BuildPanel();
        _built = true;
    }

    void EnsureControlStack()
    {
        var gm = GreenhouseManager.Instance;
        if (gm == null) gm = FindFirstObjectByType<GreenhouseManager>();
        if (gm == null) { Debug.LogError("[SidePanelSetup] GreenhouseManager sahnede yok!"); return; }

        if (gm.plantState != null && gm.plantState.health <= 0.001f) gm.plantState.health = 1f;
        if (gm.soilState != null && gm.soilState.moisture <= 0.001f) gm.soilState.moisture = 55f;
        if (gm.soilState != null && gm.soilState.ec <= 0.001f) gm.soilState.ec = 2.0f;
        if (gm.soilState != null && gm.soilState.ph <= 0.001f) gm.soilState.ph = 6.5f;

        CropProfile crop = gm.controller != null ? gm.controller.crop : null;
        if (crop == null)
        {
            crop = ScriptableObject.CreateInstance<CropProfile>();
            crop.cropName = "Domates";
            crop.tempMin = 18f; crop.tempMax = 28f; crop.tempOptimal = 24f;
            crop.humidityMin = 50f; crop.humidityMax = 80f;
            crop.soilMoistureMin = 40f; crop.soilMoistureMax = 70f;
            crop.targetEC = 2.5f; crop.targetPH = 6.5f;
            crop.minLightLux = 20000f; crop.dailyLightHours = 14f;
            crop.baseGrowthTemp = 10f; crop.requiredGDD = 1500f;
        }

        if (gm.controller == null)
        {
            var found = FindFirstObjectByType<RuleBasedController>();
            if (found == null) found = new GameObject("RuleBasedController").AddComponent<RuleBasedController>();
            if (found.crop == null) found.crop = crop;
            gm.controller = found;
        }

        var actuators = FindObjectsByType<ActuatorBase>(FindObjectsSortMode.None);
        if (actuators == null || actuators.Length == 0)
        {
            var parent = new GameObject("--- ACTUATORS ---");
            var fan = new GameObject("Fan").AddComponent<Fan>();
            var heater = new GameObject("Heater").AddComponent<Heater>();
            var irrig = new GameObject("IrrigationPump").AddComponent<IrrigationPump>();
            var mister = new GameObject("Mister").AddComponent<Mister>();
            var grow = new GameObject("GrowLight").AddComponent<GrowLight>();
            fan.transform.parent = parent.transform;
            heater.transform.parent = parent.transform;
            irrig.transform.parent = parent.transform;
            mister.transform.parent = parent.transform;
            grow.transform.parent = parent.transform;
            actuators = new ActuatorBase[] { fan, heater, irrig, mister, grow };
        }

        if (gm.energyTracker == null)
        {
            var found = FindFirstObjectByType<EnergyTracker>();
            if (found == null) found = new GameObject("EnergyTracker").AddComponent<EnergyTracker>();
            if (found.actuators == null || found.actuators.Length == 0) found.actuators = actuators;
            gm.energyTracker = found;
        }

        if (gm.scenarioManager == null)
        {
            var found = FindFirstObjectByType<ScenarioManager>();
            if (found == null) found = new GameObject("ScenarioManager").AddComponent<ScenarioManager>();
            if (found.gm == null) found.gm = gm;
            gm.scenarioManager = found;
        }

        _gm = gm;
        _actuatorCache = actuators;
    }

    void BuildPanel()
    {
        var canvasGO = new GameObject("SidePanelCanvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        scaler.scaleFactor = 1f;
        canvasGO.AddComponent<GraphicRaycaster>();

        var panel = CreateRect("ControlPanel", canvasGO.transform);
        panel.anchorMin = new Vector2(1, 1);
        panel.anchorMax = new Vector2(1, 1);
        panel.pivot = new Vector2(1, 1);
        panel.anchoredPosition = new Vector2(-panelOffset.x, -panelOffset.y);
        panel.sizeDelta = new Vector2(panelWidth, 0);

        var vlg = panel.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(0, 0, 0, 0);
        vlg.spacing = SectionGap;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        var fitter = panel.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Header
        var header = AddText(panel, "Control Panel", HeaderSize, FontStyles.Bold, ColSectionHead, TextAlignmentOptions.Left);
        SetHeight(header.gameObject, 30);

        // ---------- TIME CONTROL ----------
        AddSubHeader(panel, "Time Control");
        var timeCard = AddMetricCard(panel);
        var timeLabel  = AddText(timeCard, "Day 0  00:00", BigValueSize, FontStyles.Bold, ColValueDark, TextAlignmentOptions.Center);
        SetHeight(timeLabel.gameObject, 26);
        var speedLabel = AddText(timeCard, "Speed: 1x", CardTitleSize, FontStyles.Normal, ColTitleDark, TextAlignmentOptions.Center);
        SetHeight(speedLabel.gameObject, 16);
        var pauseLabel = AddText(timeCard, "RUNNING", CardTitleSize - 1, FontStyles.Bold, ColRunning, TextAlignmentOptions.Center);
        SetHeight(pauseLabel.gameObject, 14);

        var btnRow = AddHorizontalRow(panel, 6);
        SetHeight(btnRow.gameObject, ButtonH);
        var btn1x    = AddButton(btnRow, "1x",    ColBtnInfo, ColBtnInfoH);
        var btn60x   = AddButton(btnRow, "60x",   ColBtnInfo, ColBtnInfoH);
        var btn3600x = AddButton(btnRow, "3600x", ColBtnInfo, ColBtnInfoH);

        var btnPause = AddButton(panel, "Pause / Resume", ColBtnInfo, ColBtnInfoH);
        SetHeight(btnPause.gameObject, ButtonH);

        var tcGO = new GameObject("TimeControlUI");
        tcGO.transform.SetParent(transform, false);
        var tc = tcGO.AddComponent<TimeControlUI>();
        tc.timeLabel = timeLabel; tc.speedLabel = speedLabel; tc.pauseLabel = pauseLabel;
        tc.btn1x = btn1x; tc.btn60x = btn60x; tc.btn3600x = btn3600x; tc.btnPause = btnPause;

        // ---------- ENERGY ----------
        AddSubHeader(panel, "Energy");
        var totalLabel = AddMetricRow(panel, "Total");
        var powerLabel = AddMetricRow(panel, "Power");
        var costLabel  = AddMetricRow(panel, "Cost");

        var edGO = new GameObject("EnergyDashboardUI");
        edGO.transform.SetParent(transform, false);
        var ed = edGO.AddComponent<EnergyDashboardUI>();
        ed.totalEnergyLabel = totalLabel; ed.currentPowerLabel = powerLabel; ed.costLabel = costLabel;

        // ---------- SCENARIOS ----------
        AddSubHeader(panel, "Scenarios");
        var btnHeat    = AddButton(panel, "Heat Wave",    ColBtnDanger, ColBtnDangerH);
        SetHeight(btnHeat.gameObject, ButtonH);
        var btnFanFail = AddButton(panel, "Fan Failure",  ColBtnDanger, ColBtnDangerH);
        SetHeight(btnFanFail.gameObject, ButtonH);
        var btnOutage  = AddButton(panel, "Power Outage", ColBtnDanger, ColBtnDangerH);
        SetHeight(btnOutage.gameObject, ButtonH);

        if (_gm != null && _gm.scenarioManager != null)
        {
            var sm = _gm.scenarioManager;
            btnHeat.onClick.AddListener(() => sm.TriggerHeatWave());
            btnFanFail.onClick.AddListener(() => sm.TriggerFanFailure());
            btnOutage.onClick.AddListener(() => sm.TriggerPowerOutage());
        }

        // ---------- ALERTS ----------
        AddSubHeader(panel, "Alerts");
        var alertCard = AddMetricCard(panel);
        alertCard.GetComponent<Image>().color = ColActuatorBg;
        var alertText = AddText(alertCard, "No active alerts", CardTitleSize, FontStyles.Normal, ColMutedOnDark, TextAlignmentOptions.TopLeft);
        SetMinHeight(alertText.gameObject, 50);

        var asGO = new GameObject("AlertSystem");
        asGO.transform.SetParent(transform, false);
        var alertSys = asGO.AddComponent<AlertSystem>();
        alertSys.alertText = alertText;
        alertSys.alertPanel = null;
    }

    // ===========================================================
    // Helpers
    // ===========================================================

    static RectTransform CreateRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go.GetComponent<RectTransform>();
    }

    static void AddSubHeader(RectTransform parent, string text)
    {
        var t = AddText(parent, text, SubHeaderSize, FontStyles.Normal, ColSectionHead, TextAlignmentOptions.Left);
        SetHeight(t.gameObject, 18);
    }

    static RectTransform AddMetricCard(RectTransform parent)
    {
        var card = CreateRect("Card", parent);
        var img = card.gameObject.AddComponent<Image>();
        img.color = ColMetricCard;

        var vlg = card.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(12, 12, 8, 8);
        vlg.spacing = 2;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;

        var fitter = card.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        return card;
    }

    static TMP_Text AddMetricRow(RectTransform parent, string title)
    {
        var card = CreateRect("MetricRow_" + title, parent);
        var img = card.gameObject.AddComponent<Image>();
        img.color = ColMetricCard;
        SetHeight(card.gameObject, MetricRowH);

        var hlg = card.gameObject.AddComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset(12, 12, 6, 6);
        hlg.spacing = 6;
        hlg.childForceExpandWidth = true;
        hlg.childForceExpandHeight = true;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childAlignment = TextAnchor.MiddleLeft;

        var titleT = AddText(card, title, CardTitleSize, FontStyles.Normal, ColTitleDark, TextAlignmentOptions.MidlineLeft);
        var titleLE = titleT.gameObject.AddComponent<LayoutElement>();
        titleLE.flexibleWidth = 1;

        var valT = AddText(card, "--", CardValueSize, FontStyles.Bold, ColValueDark, TextAlignmentOptions.MidlineRight);
        var valLE = valT.gameObject.AddComponent<LayoutElement>();
        valLE.flexibleWidth = 1;
        return valT;
    }

    static RectTransform AddHorizontalRow(RectTransform parent, float spacing)
    {
        var row = CreateRect("Row", parent);
        var hlg = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = spacing;
        hlg.childForceExpandWidth = true;
        hlg.childForceExpandHeight = true;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        return row;
    }

    static TMP_Text AddText(RectTransform parent, string text, float size, FontStyles style, Color color, TextAlignmentOptions align)
    {
        var go = new GameObject("Text", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<TextMeshProUGUI>();
        t.text = text;
        t.fontSize = size;
        t.fontStyle = style;
        t.color = color;
        t.alignment = align;
        t.textWrappingMode = TextWrappingModes.Normal;
        return t;
    }

    static Button AddButton(RectTransform parent, string label, Color normal, Color hover)
    {
        var go = new GameObject("Btn_" + label, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = normal;
        var btn = go.AddComponent<Button>();
        var colors = btn.colors;
        colors.normalColor = normal;
        colors.highlightedColor = hover;
        colors.pressedColor = new Color(normal.r * 0.6f, normal.g * 0.6f, normal.b * 0.6f, 1f);
        colors.selectedColor = hover;
        btn.colors = colors;

        var lblGO = new GameObject("Text", typeof(RectTransform));
        lblGO.transform.SetParent(go.transform, false);
        var lbl = lblGO.AddComponent<TextMeshProUGUI>();
        lbl.text = label;
        lbl.fontSize = ButtonSize;
        lbl.fontStyle = FontStyles.Bold;
        lbl.alignment = TextAlignmentOptions.Center;
        lbl.color = Color.white;
        var lrt = lblGO.GetComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.offsetMin = Vector2.zero;
        lrt.offsetMax = Vector2.zero;
        return btn;
    }

    static void SetHeight(GameObject go, float h)
    {
        var le = go.GetComponent<LayoutElement>();
        if (le == null) le = go.AddComponent<LayoutElement>();
        le.minHeight = h;
        le.preferredHeight = h;
        le.flexibleHeight = 0;
    }

    static void SetMinHeight(GameObject go, float h)
    {
        var le = go.GetComponent<LayoutElement>();
        if (le == null) le = go.AddComponent<LayoutElement>();
        le.minHeight = h;
        le.preferredHeight = h;
    }
}
