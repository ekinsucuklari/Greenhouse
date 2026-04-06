# UI Strategy (2D Web Dashboard + Python API)

Bu klasor, Unity UI'yi sade tutup arayuzu web'e tasimak icin hazirlandi.

## Icerik

- `backend/app.py`: FastAPI endpoint'leri
- `backend/run_api.py`: API calistirma scripti
- `backend/requirements.txt`: Python bagimliliklari
- `dashboard/index.html`: 2D dashboard
- `dashboard/app.js`: Canli veri/grafik ve crop profile islemleri
- `dashboard/style.css`: Dashboard stilleri
- `unity_scripts/TelemetryPublisher.cs`: Unity'den API'ye telemetry gonderimi
- `unity_scripts/ActuatorCommandSync.cs`: API'den actuator komutu cekip Unity'ye uygular

## 1) Python API'yi calistir

```bash
cd ui_strategy/backend
python3 -m venv .venv
source .venv/bin/activate
pip install -r requirements.txt
python run_api.py
```

API varsayilan olarak `http://127.0.0.1:8000` adresinde acilir.

## 2) Dashboard'u ac

`ui_strategy/dashboard/index.html` dosyasini tarayicida ac.

Dashboard API'ye su endpointlerle baglanir:

- `GET /health`
- `POST /ingest`
- `GET /telemetry/latest`
- `GET /telemetry/history?minutes=60`
- `GET /crop-profile`
- `PUT /crop-profile`
- `GET /actuators`
- `PUT /actuators`
- `GET /actuators/catalog`
- `GET /region/options`
- `GET /region/config`
- `PUT /region/config`
- `GET /time/context`
- `GET /climate/live`

`/actuators` icin genisletilmis JSON:

```json
{
  "fan_active": false,
  "heater_active": false,
  "irrigation_active": false,
  "mister_active": false,
  "grow_light_active": false,
  "fan_mnt": false,
  "heater_mnt": false,
  "irrigation_mnt": false,
  "mister_mnt": false,
  "grow_light_mnt": false
}
```

`*_mnt = true` oldugunda ilgili actuator bakimda kabul edilir ve Unity tarafinda zorla OFF uygulanir.

`GET /actuators/catalog` endpoint'i dashboard'daki standart actuator satirlarini (OOD/prefab benzeri) ve hover info kutularini besler.
Bu endpoint; Unity scriptlerinden alinan temel parametre ozetlerini doner (or: Fan 350W, Heater 3000W, Mister humidity gain 5f, vb).

`/time/context` endpoint'i dashboard ve Unity icin tek zaman kaynagidir. Sim clock bu endpoint'teki `sim_time_seconds` degeriyle ayni zaman algisinda takip edilir.
`/climate/live` endpoint'i secilen sera bolgesi iklim profilinden (Antalya, Mersin, Izmir, Konya) anlik dis ortam tahmini uretir.

Dashboard'daki "Live Metrics" paneli Unity state yapisiyla birebir hizalidir:
- AirState: `air_temp`, `air_humidity`, `co2`, `light_lux`
- SoilState: `soil_moisture`, `soil_ec`, `soil_ph`
- PlantState: `plant_growth_stage`, `plant_health`, `plant_accumulated_gdd`
- OutdoorState: `outdoor_temp`, `outdoor_humidity`, `outdoor_solar_radiation`, `outdoor_wind_speed`
- SimulationClock: `sim_time`, `sim_delta_time`, `sim_hour_of_day`, `sim_day_count`

## 3) Unity tarafi

1. `unity_scripts/TelemetryPublisher.cs` ve `unity_scripts/ActuatorCommandSync.cs` dosyalarini Unity projesinde `Assets/Scripts/UI/` altina kopyala.
2. Sahnedeki uygun bir GameObject'e `TelemetryPublisher` ekle.
3. Ayni GameObject'e `ActuatorCommandSync` ekle.
4. `TelemetryPublisher` icinde:
   - `apiBaseUrl = http://127.0.0.1:8000`
   - `greenhouseManager` ve `simulationClock` referanslarini ata (veya singleton auto-find'e birak)
5. `ActuatorCommandSync` icinde:
   - `apiBaseUrl = http://127.0.0.1:8000`
   - `greenhouseManager` referansini ata
   - `enableRemoteOverride = true` yap (dashboard toggle komutlarini Unity'ye uygular)
6. Play modda:
   - Unity her saniye telemetry'yi API'ye yollar (`POST /ingest`)
   - Unity her saniye actuator komutlarini API'den ceker (`GET /actuators`)

## Notlar

- Bu surum "faz-1 hizli entegrasyon" icindir.
- Veri su anda memory'de tutuluyor; kalici depolama yok.
- Sonraki adimda PostgreSQL/SQLite eklenebilir.
- `enableRemoteOverride = false` iken middleware controller normal calismaya devam eder, dashboard sadece izleme yapar.
