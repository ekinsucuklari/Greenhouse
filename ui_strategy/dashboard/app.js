const API_BASE_KEY = "greenhouse_api_base";
const FALLBACK_ACTUATOR_CATALOG = [
  {
    key: "fan_active",
    label: "Fan",
    power_watts: 350,
    effect_summary: "Ventilation rate 200f, humidity loss factor 0.1",
    source_note: "Fan.cs + EnvironmentPhysics.cs",
  },
  {
    key: "heater_active",
    label: "Heater",
    power_watts: 3000,
    effect_summary: "Heater power 3000f added to heat balance",
    source_note: "Heater.cs + EnvironmentPhysics.cs",
  },
  {
    key: "irrigation_active",
    label: "Irrigation",
    power_watts: 0,
    effect_summary: "Soil irrigation rate 2f (%/s)",
    source_note: "SoilModel.cs",
  },
  {
    key: "mister_active",
    label: "Mister",
    power_watts: 0,
    effect_summary: "Humidity gain 5f when active",
    source_note: "EnvironmentPhysics.cs",
  },
  {
    key: "grow_light_active",
    label: "Grow Light",
    power_watts: 0,
    effect_summary: "Adds 25000 lux to air.lightLux",
    source_note: "EnvironmentPhysics.cs",
  },
];
const REGION_PRESETS = {
  Antalya_TR: {
    region_name: "Antalya_TR",
    timezone: "Europe/Istanbul",
    temp_night_min: 14.0,
    temp_day_max: 33.0,
    humidity_day_min: 40.0,
    humidity_night_max: 78.0,
    max_solar_radiation: 900.0,
    cloud_cover: 0.08,
    seasonal_temp_shift: 4.5,
  },
  Mersin_TR: {
    region_name: "Mersin_TR",
    timezone: "Europe/Istanbul",
    temp_night_min: 13.0,
    temp_day_max: 32.0,
    humidity_day_min: 45.0,
    humidity_night_max: 80.0,
    max_solar_radiation: 880.0,
    cloud_cover: 0.1,
    seasonal_temp_shift: 4.0,
  },
  Izmir_TR: {
    region_name: "Izmir_TR",
    timezone: "Europe/Istanbul",
    temp_night_min: 11.0,
    temp_day_max: 31.0,
    humidity_day_min: 42.0,
    humidity_night_max: 74.0,
    max_solar_radiation: 860.0,
    cloud_cover: 0.12,
    seasonal_temp_shift: 4.0,
  },
  Konya_TR: {
    region_name: "Konya_TR",
    timezone: "Europe/Istanbul",
    temp_night_min: 7.0,
    temp_day_max: 29.0,
    humidity_day_min: 35.0,
    humidity_night_max: 68.0,
    max_solar_radiation: 870.0,
    cloud_cover: 0.09,
    seasonal_temp_shift: 5.0,
  },
};
const LIVE_METRIC_DEFS = [
  { key: "air_temp", label: "Air Temp", unit: "C", digits: 1 },
  { key: "air_humidity", label: "Air Humidity", unit: "%", digits: 1 },
  { key: "co2", label: "CO2", unit: "ppm", digits: 0 },
  { key: "light_lux", label: "Light", unit: "lux", digits: 0 },
  { key: "soil_moisture", label: "Soil Moisture", unit: "%", digits: 1 },
  { key: "soil_ec", label: "Soil EC", unit: "mS/cm", digits: 2 },
  { key: "soil_ph", label: "Soil pH", unit: "", digits: 2 },
  { key: "plant_growth_stage", label: "Plant Growth Stage", unit: "", digits: 2 },
  { key: "plant_health", label: "Plant Health", unit: "", digits: 2 },
  { key: "plant_accumulated_gdd", label: "Plant Accumulated GDD", unit: "", digits: 1 },
  { key: "outdoor_temp", label: "Outdoor Temp", unit: "C", digits: 1 },
  { key: "outdoor_humidity", label: "Outdoor Humidity", unit: "%", digits: 1 },
  { key: "outdoor_solar_radiation", label: "Outdoor Solar", unit: "W/m2", digits: 1 },
  { key: "outdoor_wind_speed", label: "Outdoor Wind", unit: "m/s", digits: 1 },
  { key: "sim_time", label: "Sim Time", unit: "s", digits: 1 },
  { key: "sim_day_count", label: "Sim Day Count", unit: "", digits: 0 },
  { key: "sim_hour_of_day", label: "Sim Hour", unit: "h", digits: 2 },
  { key: "sim_delta_time", label: "Sim Delta", unit: "s", digits: 3 },
];

const apiBaseInput = document.getElementById("apiBase");
const saveApiBaseBtn = document.getElementById("saveApiBaseBtn");
const apiStatus = document.getElementById("apiStatus");
const profileStatus = document.getElementById("profileStatus");
const actuatorStatus = document.getElementById("actuatorStatus");
const regionNameEl = document.getElementById("regionName");
const regionTimezoneEl = document.getElementById("regionTimezone");
const saveRegionBtn = document.getElementById("saveRegionBtn");
const regionStatus = document.getElementById("regionStatus");
const realClockEl = document.getElementById("realClock");
const simClockEl = document.getElementById("simClock");
const lastTelemetryAtEl = document.getElementById("lastTelemetryAt");

const liveMetricsGridEl = document.getElementById("liveMetricsGrid");
const actuatorControlsEl = document.getElementById("actuatorControls");
const cropProfileForm = document.getElementById("cropProfileForm");

let actuatorCatalog = [...FALLBACK_ACTUATOR_CATALOG];

function getApiBase() {
  return localStorage.getItem(API_BASE_KEY) || apiBaseInput.value.trim();
}

function setApiBase(base) {
  localStorage.setItem(API_BASE_KEY, base);
}

function getMntKey(activeKey) {
  return activeKey.replace("_active", "_mnt");
}

function setActuatorRadio(name, isOn) {
  const selector = `input[name="${name}"][value="${isOn ? "on" : "off"}"]`;
  const target = document.querySelector(selector);
  if (target) target.checked = true;
}

function setMntButton(name, isMnt) {
  const button = document.querySelector(`.mnt-btn[data-mnt-key="${name}"]`);
  if (!button) return;
  button.classList.toggle("active", isMnt);
}

function getActuatorRadio(name) {
  const selected = document.querySelector(`input[name="${name}"]:checked`);
  return selected ? selected.value === "on" : false;
}

function getMntState(name) {
  const button = document.querySelector(`.mnt-btn[data-mnt-key="${name}"]`);
  return button ? button.classList.contains("active") : false;
}

function applyMaintenanceRules() {
  actuatorCatalog.forEach((item) => {
    const activeKey = item.key;
    const mntKey = getMntKey(activeKey);
    const isMnt = getMntState(mntKey);
    const switchEl = document
      .querySelector(`input[name="${activeKey}"]`)
      ?.closest(".toggle-switch");

    if (!switchEl) return;
    switchEl.classList.toggle("disabled", isMnt);
    if (isMnt) setActuatorRadio(activeKey, false);
  });
}

function setActuatorsToForm(state) {
  actuatorCatalog.forEach((item) => {
    const activeKey = item.key;
    const mntKey = getMntKey(activeKey);
    setActuatorRadio(activeKey, !!state[activeKey]);
    setMntButton(mntKey, !!state[mntKey]);
  });
  applyMaintenanceRules();
}

function getActuatorPayload() {
  applyMaintenanceRules();
  const payload = {};
  actuatorCatalog.forEach((item) => {
    const activeKey = item.key;
    const mntKey = getMntKey(activeKey);
    const isMnt = getMntState(mntKey);
    payload[activeKey] = isMnt ? false : getActuatorRadio(activeKey);
    payload[mntKey] = isMnt;
  });
  return payload;
}

function getJson(url, options = {}) {
  return fetch(url, options).then((res) => {
    if (!res.ok) throw new Error(`HTTP ${res.status}`);
    return res.json();
  });
}

function formatSimClock(simSeconds) {
  const day = Math.floor(simSeconds / 86400);
  const rem = simSeconds % 86400;
  const hour = Math.floor(rem / 3600);
  const min = Math.floor((rem % 3600) / 60);
  const sec = Math.floor(rem % 60);
  return `Day ${day} ${String(hour).padStart(2, "0")}:${String(min).padStart(2, "0")}:${String(sec).padStart(2, "0")}`;
}

function setTimeContext(ctx) {
  realClockEl.textContent = `Real: ${ctx.local_iso} (${ctx.timezone})`;
  simClockEl.textContent = `Sim: ${formatSimClock(ctx.sim_time_seconds || 0)}`;
}

function setLastTelemetryText(ts) {
  if (!ts) {
    lastTelemetryAtEl.textContent = "Last telemetry: --";
    return;
  }
  const d = new Date(ts * 1000);
  lastTelemetryAtEl.textContent = `Last telemetry: ${d.toLocaleString()}`;
}

function renderLiveMetricCards() {
  liveMetricsGridEl.innerHTML = LIVE_METRIC_DEFS.map((m) => {
    return `<div class="card"><span>${m.label}</span><strong id="metric_${m.key}">--</strong></div>`;
  }).join("");
}

function setMetricValue(metricDef, value) {
  const el = document.getElementById(`metric_${metricDef.key}`);
  if (!el) return;
  if (value === null || value === undefined || Number.isNaN(Number(value))) {
    el.textContent = "--";
    return;
  }
  const numeric = Number(value);
  const formatted = metricDef.digits === 0 ? numeric.toFixed(0) : numeric.toFixed(metricDef.digits);
  el.textContent = metricDef.unit ? `${formatted} ${metricDef.unit}` : formatted;
}

function setAllMetricValues(data) {
  LIVE_METRIC_DEFS.forEach((m) => setMetricValue(m, data[m.key]));
}

function renderActuatorControls() {
  actuatorControlsEl.innerHTML = actuatorCatalog
    .map((item) => {
      const mntKey = getMntKey(item.key);
      const powerText = item.power_watts > 0 ? `${item.power_watts.toFixed(0)} W` : "N/A";
      const offId = `${item.key}_off`;
      const onId = `${item.key}_on`;
      return `
        <div class="switch-row">
          <div class="label-col">
            <span>${item.label}</span>
            <span class="info-chip">i
              <span class="tooltip">
                <strong>${item.label}</strong><br />
                Power: ${powerText}<br />
                Effect: ${item.effect_summary}<br />
                Source: ${item.source_note}
              </span>
            </span>
          </div>
          <div class="control-row">
            <div class="toggle-switch">
              <input class="actuator-radio off-radio" type="radio" id="${offId}" name="${item.key}" value="off" checked />
              <input class="actuator-radio on-radio" type="radio" id="${onId}" name="${item.key}" value="on" />
              <label class="toggle-hit off-hit" for="${offId}" aria-label="${item.label} off"></label>
              <label class="toggle-hit on-hit" for="${onId}" aria-label="${item.label} on"></label>
              <div class="toggle-track"><span class="toggle-knob"></span></div>
            </div>
            <button type="button" class="mnt-btn" data-mnt-key="${mntKey}">MNT</button>
          </div>
        </div>
      `;
    })
    .join("");
}

const chartCtx = document.getElementById("historyChart");
const historyChart = new Chart(chartCtx, {
  type: "line",
  data: {
    labels: [],
    datasets: [
      { label: "Temp (C)", data: [], borderWidth: 2 },
      { label: "Humidity (%)", data: [], borderWidth: 2 },
      { label: "Soil Moisture (%)", data: [], borderWidth: 2 },
    ],
  },
  options: {
    animation: false,
    responsive: true,
    scales: {
      y: { beginAtZero: false },
    },
  },
});

async function refreshHealth() {
  try {
    const base = getApiBase();
    await getJson(`${base}/health`);
    apiStatus.textContent = "API: Connected";
  } catch (_) {
    apiStatus.textContent = "API: Not reachable";
  }
}

async function loadActuatorCatalog() {
  try {
    const base = getApiBase();
    actuatorCatalog = await getJson(`${base}/actuators/catalog`);
  } catch (_) {
    actuatorCatalog = [...FALLBACK_ACTUATOR_CATALOG];
  }
  renderActuatorControls();
}

async function refreshLatest() {
  try {
    const base = getApiBase();
    const data = await getJson(`${base}/telemetry/latest`);
    setAllMetricValues(data);
    setLastTelemetryText(data.timestamp);
  } catch (_) {
    try {
      const base = getApiBase();
      const data = await getJson(`${base}/telemetry/latest`);
      setAllMetricValues(data);
      setLastTelemetryText(data.timestamp);
    } catch (_) {
      setAllMetricValues({});
      setLastTelemetryText(null);
    }
  }
}

async function refreshActuators() {
  try {
    const base = getApiBase();
    const state = await getJson(`${base}/actuators`);
    setActuatorsToForm(state);
    actuatorStatus.textContent = "Actuator state synced";
  } catch (_) {
    actuatorStatus.textContent = "Actuator state sync failed";
  }
}

async function saveActuators() {
  try {
    const base = getApiBase();
    const payload = getActuatorPayload();
    await getJson(`${base}/actuators`, {
      method: "PUT",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(payload),
    });
    actuatorStatus.textContent = "Actuator commands saved";
  } catch (_) {
    actuatorStatus.textContent = "Actuator command save failed";
  }
}

async function refreshHistory() {
  try {
    const base = getApiBase();
    const points = await getJson(`${base}/telemetry/history?minutes=60`);

    historyChart.data.labels = points.map((p) => {
      const d = new Date(p.timestamp * 1000);
      return `${String(d.getHours()).padStart(2, "0")}:${String(d.getMinutes()).padStart(2, "0")}:${String(d.getSeconds()).padStart(2, "0")}`;
    });
    historyChart.data.datasets[0].data = points.map((p) => p.air_temp);
    historyChart.data.datasets[1].data = points.map((p) => p.air_humidity);
    historyChart.data.datasets[2].data = points.map((p) => p.soil_moisture);
    historyChart.update();
  } catch (_) {
    // Ignore temporary failures.
  }
}

async function loadCropProfile() {
  try {
    const base = getApiBase();
    const profile = await getJson(`${base}/crop-profile`);
    Object.keys(profile).forEach((key) => {
      const input = cropProfileForm.elements.namedItem(key);
      if (input) input.value = profile[key];
    });
    profileStatus.textContent = "Profile loaded";
  } catch (_) {
    profileStatus.textContent = "Profile load failed";
  }
}

async function refreshTimeContext() {
  try {
    const base = getApiBase();
    const ctx = await getJson(`${base}/time/context`);
    setTimeContext(ctx);
  } catch (_) {
    realClockEl.textContent = "Real: unavailable";
    simClockEl.textContent = "Sim: unavailable";
  }
}

async function loadRegionConfig() {
  try {
    const base = getApiBase();
    const cfg = await getJson(`${base}/region/config`);
    regionNameEl.value = cfg.region_name;
    regionTimezoneEl.value = cfg.timezone;
    regionStatus.textContent = "Region loaded";
  } catch (_) {
    regionStatus.textContent = "Region load failed";
  }
}

async function saveRegionConfig() {
  try {
    const base = getApiBase();
    const selected = REGION_PRESETS[regionNameEl.value];
    const payload = {
      ...(selected || REGION_PRESETS.Antalya_TR),
      timezone: regionTimezoneEl.value.trim() || "Europe/Istanbul",
    };
    await getJson(`${base}/region/config`, {
      method: "PUT",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(payload),
    });
    regionStatus.textContent = "Region applied";
    await refreshTimeContext();
    await refreshLatest();
  } catch (_) {
    regionStatus.textContent = "Region apply failed";
  }
}

async function saveCropProfile(event) {
  event.preventDefault();
  const formData = new FormData(cropProfileForm);
  const payload = Object.fromEntries(formData.entries());

  for (const [k, v] of Object.entries(payload)) {
    if (k !== "crop_name") payload[k] = Number(v);
  }

  try {
    const base = getApiBase();
    await getJson(`${base}/crop-profile`, {
      method: "PUT",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(payload),
    });
    profileStatus.textContent = "Profile saved";
  } catch (_) {
    profileStatus.textContent = "Profile save failed";
  }
}

saveApiBaseBtn.addEventListener("click", async () => {
  const nextBase = apiBaseInput.value.trim();
  setApiBase(nextBase);
  await refreshHealth();
  await loadRegionConfig();
  await refreshTimeContext();
  await loadActuatorCatalog();
  await loadCropProfile();
  await refreshActuators();
});
saveRegionBtn.addEventListener("click", saveRegionConfig);
regionNameEl.addEventListener("change", () => {
  const preset = REGION_PRESETS[regionNameEl.value];
  if (preset) regionTimezoneEl.value = preset.timezone;
});

cropProfileForm.addEventListener("submit", saveCropProfile);
actuatorControlsEl.addEventListener("change", (event) => {
  if (event.target.classList.contains("actuator-radio")) {
    saveActuators();
  }
});
actuatorControlsEl.addEventListener("click", (event) => {
  const mntBtn = event.target.closest(".mnt-btn");
  if (!mntBtn) return;
  mntBtn.classList.toggle("active");
  applyMaintenanceRules();
  saveActuators();
});

async function init() {
  const saved = localStorage.getItem(API_BASE_KEY);
  if (saved) apiBaseInput.value = saved;

  await refreshHealth();
  renderLiveMetricCards();
  await loadRegionConfig();
  await refreshTimeContext();
  await loadActuatorCatalog();
  await loadCropProfile();
  await refreshLatest();
  await refreshHistory();
  await refreshActuators();

  setInterval(refreshTimeContext, 1000);
  setInterval(refreshHealth, 5000);
  setInterval(refreshLatest, 1000);
  setInterval(refreshHistory, 5000);
  setInterval(refreshActuators, 3000);
}

init();
