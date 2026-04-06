from collections import deque
from datetime import datetime, timezone
from math import pi, sin
from threading import Lock
from time import time
from typing import Deque, Optional
from zoneinfo import ZoneInfo, ZoneInfoNotFoundError

from fastapi import FastAPI, Query
from fastapi.middleware.cors import CORSMiddleware
from pydantic import BaseModel, Field


class TelemetryPoint(BaseModel):
    timestamp: float = Field(default_factory=time, description="Unix timestamp (seconds)")
    sim_time: float = 0.0
    sim_delta_time: float = 0.0
    sim_hour_of_day: float = 0.0
    sim_day_count: int = 0

    air_temp: float = 0.0
    air_humidity: float = 0.0
    soil_moisture: float = 0.0
    soil_ec: float = 0.0
    soil_ph: float = 0.0
    co2: float = 0.0
    light_lux: float = 0.0
    plant_growth_stage: float = 0.0
    plant_health: float = 1.0
    plant_accumulated_gdd: float = 0.0
    outdoor_temp: float = 0.0
    outdoor_humidity: float = 0.0
    outdoor_solar_radiation: float = 0.0
    outdoor_wind_speed: float = 0.0
    outdoor_is_cloudy: bool = False

    fan_active: bool = False
    heater_active: bool = False
    irrigation_active: bool = False
    mister_active: bool = False
    grow_light_active: bool = False


class CropProfile(BaseModel):
    crop_name: str = "Domates"
    temp_min: float = 18.0
    temp_max: float = 28.0
    humidity_min: float = 50.0
    humidity_max: float = 80.0
    soil_moisture_min: float = 40.0
    soil_moisture_max: float = 70.0
    min_light_lux: float = 20000.0
    target_ec: float = 2.5
    target_ph: float = 6.5


class ActuatorState(BaseModel):
    fan_active: bool = False
    heater_active: bool = False
    irrigation_active: bool = False
    mister_active: bool = False
    grow_light_active: bool = False
    fan_mnt: bool = False
    heater_mnt: bool = False
    irrigation_mnt: bool = False
    mister_mnt: bool = False
    grow_light_mnt: bool = False


class ActuatorCatalogItem(BaseModel):
    key: str
    label: str
    power_watts: float
    effect_summary: str
    source_note: str


class RegionConfig(BaseModel):
    region_name: str = "Antalya_TR"
    timezone: str = "Europe/Istanbul"
    temp_night_min: float = 12.0
    temp_day_max: float = 32.0
    humidity_day_min: float = 45.0
    humidity_night_max: float = 75.0
    max_solar_radiation: float = 850.0
    cloud_cover: float = 0.10
    seasonal_temp_shift: float = 4.0


class TimeContext(BaseModel):
    region_name: str
    timezone: str
    unix_timestamp: float
    local_iso: str
    day_of_year: int
    hour_of_day: float
    sim_time_seconds: float


class ClimateSnapshot(BaseModel):
    region_name: str
    timezone: str
    timestamp: float
    day_of_year: int
    hour_of_day: float
    outside_temp: float
    outside_humidity: float
    solar_radiation: float


REGION_PRESETS: dict[str, RegionConfig] = {
    "Antalya_TR": RegionConfig(
        region_name="Antalya_TR",
        timezone="Europe/Istanbul",
        temp_night_min=14.0,
        temp_day_max=33.0,
        humidity_day_min=40.0,
        humidity_night_max=78.0,
        max_solar_radiation=900.0,
        cloud_cover=0.08,
        seasonal_temp_shift=4.5,
    ),
    "Mersin_TR": RegionConfig(
        region_name="Mersin_TR",
        timezone="Europe/Istanbul",
        temp_night_min=13.0,
        temp_day_max=32.0,
        humidity_day_min=45.0,
        humidity_night_max=80.0,
        max_solar_radiation=880.0,
        cloud_cover=0.10,
        seasonal_temp_shift=4.0,
    ),
    "Izmir_TR": RegionConfig(
        region_name="Izmir_TR",
        timezone="Europe/Istanbul",
        temp_night_min=11.0,
        temp_day_max=31.0,
        humidity_day_min=42.0,
        humidity_night_max=74.0,
        max_solar_radiation=860.0,
        cloud_cover=0.12,
        seasonal_temp_shift=4.0,
    ),
    "Konya_TR": RegionConfig(
        region_name="Konya_TR",
        timezone="Europe/Istanbul",
        temp_night_min=7.0,
        temp_day_max=29.0,
        humidity_day_min=35.0,
        humidity_night_max=68.0,
        max_solar_radiation=870.0,
        cloud_cover=0.09,
        seasonal_temp_shift=5.0,
    ),
}


class InMemoryStore:
    def __init__(self, max_points: int = 5000) -> None:
        self._lock = Lock()
        self._history: Deque[TelemetryPoint] = deque(maxlen=max_points)
        self._latest: Optional[TelemetryPoint] = None
        self._crop_profile = CropProfile()
        self._actuator_state = ActuatorState()
        self._region_config = REGION_PRESETS["Antalya_TR"]
        self._actuator_catalog = [
            ActuatorCatalogItem(
                key="fan_active",
                label="Fan",
                power_watts=350.0,
                effect_summary="Ventilation rate 200f, humidity loss factor 0.1",
                source_note="Fan.cs + EnvironmentPhysics.cs",
            ),
            ActuatorCatalogItem(
                key="heater_active",
                label="Heater",
                power_watts=3000.0,
                effect_summary="Heater power 3000f added to heat balance",
                source_note="Heater.cs + EnvironmentPhysics.cs",
            ),
            ActuatorCatalogItem(
                key="irrigation_active",
                label="Irrigation",
                power_watts=0.0,
                effect_summary="Soil irrigation rate 2f (%/s)",
                source_note="SoilModel.cs",
            ),
            ActuatorCatalogItem(
                key="mister_active",
                label="Mister",
                power_watts=0.0,
                effect_summary="Humidity gain 5f when active",
                source_note="EnvironmentPhysics.cs",
            ),
            ActuatorCatalogItem(
                key="grow_light_active",
                label="Grow Light",
                power_watts=0.0,
                effect_summary="Adds 25000 lux to air.lightLux",
                source_note="EnvironmentPhysics.cs",
            ),
        ]

    def ingest(self, point: TelemetryPoint) -> TelemetryPoint:
        with self._lock:
            self._latest = point
            self._history.append(point)
            return point

    def latest(self) -> Optional[TelemetryPoint]:
        with self._lock:
            return self._latest

    def history(self, minutes: int = 60) -> list[TelemetryPoint]:
        cutoff = time() - (minutes * 60)
        with self._lock:
            return [p for p in self._history if p.timestamp >= cutoff]

    def crop_profile(self) -> CropProfile:
        with self._lock:
            return self._crop_profile

    def update_crop_profile(self, profile: CropProfile) -> CropProfile:
        with self._lock:
            self._crop_profile = profile
            return self._crop_profile

    def actuator_state(self) -> ActuatorState:
        with self._lock:
            return self._actuator_state

    def update_actuator_state(self, state: ActuatorState) -> ActuatorState:
        with self._lock:
            self._actuator_state = state
            return self._actuator_state

    def actuator_catalog(self) -> list[ActuatorCatalogItem]:
        with self._lock:
            return self._actuator_catalog

    def region_config(self) -> RegionConfig:
        with self._lock:
            return self._region_config

    def update_region_config(self, config: RegionConfig) -> RegionConfig:
        with self._lock:
            self._region_config = config
            return self._region_config


store = InMemoryStore()
app = FastAPI(title="Greenhouse UI Strategy API", version="0.1.0")

app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)


@app.get("/health")
def health() -> dict[str, str]:
    return {"status": "ok"}


@app.post("/ingest", response_model=TelemetryPoint)
def ingest(point: TelemetryPoint) -> TelemetryPoint:
    return store.ingest(point)


def _safe_zone(tz_name: str) -> ZoneInfo:
    try:
        return ZoneInfo(tz_name)
    except ZoneInfoNotFoundError:
        return ZoneInfo("UTC")


def _compute_time_context(config: RegionConfig) -> TimeContext:
    now_utc = datetime.now(timezone.utc)
    local = now_utc.astimezone(_safe_zone(config.timezone))
    day_of_year = local.timetuple().tm_yday
    hour_of_day = (
        local.hour
        + (local.minute / 60.0)
        + (local.second / 3600.0)
        + (local.microsecond / 3_600_000_000.0)
    )
    sim_time_seconds = ((day_of_year - 1) * 86400.0) + (hour_of_day * 3600.0)
    return TimeContext(
        region_name=config.region_name,
        timezone=config.timezone,
        unix_timestamp=now_utc.timestamp(),
        local_iso=local.isoformat(timespec="seconds"),
        day_of_year=day_of_year,
        hour_of_day=hour_of_day,
        sim_time_seconds=sim_time_seconds,
    )


def _compute_climate(config: RegionConfig, ctx: TimeContext) -> ClimateSnapshot:
    season_factor = sin(((ctx.day_of_year - 80) / 365.0) * 2.0 * pi)
    sunrise = 7.0 - (season_factor * 1.1)
    sunset = 17.0 + (season_factor * 1.1)
    if ctx.hour_of_day < sunrise or ctx.hour_of_day > sunset:
        solar_shape = 0.0
    else:
        solar_phase = (ctx.hour_of_day - sunrise) / max(0.001, (sunset - sunrise))
        solar_shape = max(0.0, sin(solar_phase * pi))

    solar_radiation = config.max_solar_radiation * solar_shape * (1.0 - config.cloud_cover)
    temp_mid = ((config.temp_day_max + config.temp_night_min) / 2.0) + (season_factor * config.seasonal_temp_shift)
    temp_amp = (config.temp_day_max - config.temp_night_min) / 2.0
    outside_temp = temp_mid + (temp_amp * sin(((ctx.hour_of_day - 5.0) / 24.0) * 2.0 * pi))

    humidity_range = config.humidity_night_max - config.humidity_day_min
    outside_humidity = config.humidity_day_min + ((1.0 - solar_shape) * humidity_range)
    outside_humidity = max(10.0, min(100.0, outside_humidity))

    return ClimateSnapshot(
        region_name=config.region_name,
        timezone=config.timezone,
        timestamp=ctx.unix_timestamp,
        day_of_year=ctx.day_of_year,
        hour_of_day=ctx.hour_of_day,
        outside_temp=outside_temp,
        outside_humidity=outside_humidity,
        solar_radiation=max(0.0, solar_radiation),
    )


def _synthetic_latest() -> TelemetryPoint:
    config = store.region_config()
    act = store.actuator_state()
    ctx = _compute_time_context(config)
    climate = _compute_climate(config, ctx)

    base_temp = climate.outside_temp + (2.2 if act.heater_active else 0.0) - (1.8 if act.fan_active else 0.0)
    base_humidity = climate.outside_humidity + (5.0 if act.mister_active else 0.0) - (3.0 if act.fan_active else 0.0)
    soil_moisture = 56.0 + (8.0 if act.irrigation_active else -0.4)

    return TelemetryPoint(
        timestamp=ctx.unix_timestamp,
        sim_time=ctx.sim_time_seconds,
        sim_delta_time=1.0,
        sim_hour_of_day=ctx.hour_of_day,
        sim_day_count=max(0, ctx.day_of_year - 1),
        air_temp=max(-10.0, min(60.0, base_temp)),
        air_humidity=max(10.0, min(100.0, base_humidity)),
        soil_moisture=max(0.0, min(100.0, soil_moisture)),
        soil_ec=2.0 if act.irrigation_active else 2.2,
        soil_ph=6.5,
        co2=400.0 if act.fan_active else 850.0,
        light_lux=(climate.solar_radiation * 100.0) + (25000.0 if act.grow_light_active else 0.0),
        plant_growth_stage=0.25,
        plant_health=0.95,
        plant_accumulated_gdd=180.0,
        outdoor_temp=climate.outside_temp,
        outdoor_humidity=climate.outside_humidity,
        outdoor_solar_radiation=climate.solar_radiation,
        outdoor_wind_speed=0.0,
        outdoor_is_cloudy=config.cloud_cover > 0.5,
        fan_active=act.fan_active,
        heater_active=act.heater_active,
        irrigation_active=act.irrigation_active,
        mister_active=act.mister_active,
        grow_light_active=act.grow_light_active,
    )


@app.get("/telemetry/latest", response_model=TelemetryPoint)
def telemetry_latest() -> TelemetryPoint:
    return store.latest() or _synthetic_latest()


@app.get("/telemetry/history", response_model=list[TelemetryPoint])
def telemetry_history(
    minutes: int = Query(default=60, ge=1, le=24 * 60),
) -> list[TelemetryPoint]:
    points = store.history(minutes=minutes)
    if points:
        return points
    return [_synthetic_latest()]


@app.get("/crop-profile", response_model=CropProfile)
def get_crop_profile() -> CropProfile:
    return store.crop_profile()


@app.put("/crop-profile", response_model=CropProfile)
def put_crop_profile(profile: CropProfile) -> CropProfile:
    return store.update_crop_profile(profile)


@app.get("/actuators", response_model=ActuatorState)
def get_actuators() -> ActuatorState:
    return store.actuator_state()


@app.put("/actuators", response_model=ActuatorState)
def put_actuators(state: ActuatorState) -> ActuatorState:
    return store.update_actuator_state(state)


@app.get("/actuators/catalog", response_model=list[ActuatorCatalogItem])
def get_actuator_catalog() -> list[ActuatorCatalogItem]:
    return store.actuator_catalog()


@app.get("/region/options", response_model=list[RegionConfig])
def get_region_options() -> list[RegionConfig]:
    return [REGION_PRESETS[k] for k in sorted(REGION_PRESETS.keys())]


@app.get("/region/config", response_model=RegionConfig)
def get_region_config() -> RegionConfig:
    return store.region_config()


@app.put("/region/config", response_model=RegionConfig)
def put_region_config(config: RegionConfig) -> RegionConfig:
    if config.region_name in REGION_PRESETS:
        preset = REGION_PRESETS[config.region_name].model_copy()
        preset.cloud_cover = config.cloud_cover
        return store.update_region_config(preset)
    return store.update_region_config(config)


@app.get("/time/context", response_model=TimeContext)
def get_time_context() -> TimeContext:
    return _compute_time_context(store.region_config())


@app.get("/climate/live", response_model=ClimateSnapshot)
def get_climate_live() -> ClimateSnapshot:
    config = store.region_config()
    ctx = _compute_time_context(config)
    return _compute_climate(config, ctx)
