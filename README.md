# 🌿 Sera Digital Twin Projesi

Unity ile geliştirilmekte olan bir sera simülasyonu. Gerçek bir seranın sıcaklık, nem, toprak ve bitki sistemlerini fizik tabanlı matematiksel modellerle simüle eder.

---

## 👥 Ekip Görev Dağılımı

| Kişi | Sorumluluk |
|------|------------|
| Kişi 1 | Simülasyon & Fizik Motoru (bu repo'daki mevcut çalışma) |
| Kişi 2 | Kontrol Sistemleri & Oyun Mantığı |
| Kişi 3 | Unity Sahne, UI & Görselleştirme |

---

## ✅ Tamamlanan Scriptler (Kişi 1)

### `SimulationClock.cs`
Projenin zaman motorudur. Tüm diğer scriptler buradan zaman bilgisi alır.
- Simülasyon hızını kontrol eder: `1x` (gerçek zaman), `60x` (1 dk/sn), `3600x` (1 saat/sn)
- Pause / Resume desteği
- Günün saatini ve kaçıncı günde olduğunu hesaplar
- **Singleton** — her yerden `SimulationClock.Instance` ile erişilir

### `WeatherSystem.cs`
Dış ortam koşullarını simüle eder.
- Gün/gece sıcaklık döngüsü (sinüs dalgası modeli)
- Güneş radyasyonu hesabı — sabah 6'da doğar, akşam 20'de batar
- Bulut örtüsü ayarı (0 = açık hava, 1 = tam bulutlu)
- Dış nem simülasyonu
- **Singleton** — `WeatherSystem.Instance` ile erişilir

### `EnvironmentPhysics.cs`
Sera içindeki hava fizikini simüle eder. Projenin kalbidir.
- Gerçek termodinamik denklemlerle iç sıcaklık hesabı:
  - Güneşten gelen ısı
  - Isıtıcıdan gelen ısı
  - Fan ile dışarı atılan ısı
  - Duvar/cam ısı kaybı
- Nem simülasyonu:
  - Sisleyici nem ekler
  - Fan dış hava nemiyle dengelenir
- Kişi 2'nin aktüatör komutlarını (`heaterActive`, `fanActive`, `misterActive`) bekler
- **Singleton** — `EnvironmentPhysics.Instance` ile erişilir

### `SoilModel.cs`
Toprak fiziksel modelini simüle eder.
- Toprak nemi dinamiği:
  - Sulama pompası su ekler
  - Drenaj: toprak %80'i geçince fazla su akar
  - Bitki kök su çekimi
  - Sıcaklıkla orantılı buharlaşma
- EC (elektriksel iletkenlik) simülasyonu — besin yoğunluğu
- pH takibi
- Kişi 2'nin sulama komutlarını (`irrigationActive`) bekler
- **Singleton** — `SoilModel.Instance` ile erişilir

---

## ⬜ Yapılacaklar (Kişi 1)

- [ ] `PlantGrowthModel.cs` — GDD tabanlı bitki büyüme modeli
- [ ] Sensör scriptleri — gürültülü gerçekçi sensör okumaları
- [ ] `DataLogger.cs` — simülasyon verilerini CSV'ye kayıt

---

## 🏗️ Proje Klasör Yapısı

```
Assets/
├── Scripts/
│   ├── Simulation/         ← Kişi 1
│   │   ├── SimulationClock.cs
│   │   ├── EnvironmentPhysics.cs
│   │   ├── SoilModel.cs
│   │   └── PlantGrowthModel.cs     (yapılacak)
│   ├── Sensors/            ← Kişi 1
│   ├── Data/               ← Kişi 1
│   ├── Events/             ← Kişi 1
│   │   └── WeatherSystem.cs
│   ├── Actuators/          ← Kişi 2
│   ├── Controllers/        ← Kişi 2
│   └── UI/                 ← Kişi 3
```

---

## 🔗 Scriptler Arası İlişki

```
SimulationClock
     │
     ├──► WeatherSystem        (dış sıcaklık, güneş, nem)
     │         │
     └──► EnvironmentPhysics  (iç sıcaklık, iç nem)
               │
          SoilModel           (toprak nemi, EC, pH)
               │
          PlantGrowthModel    (büyüme aşaması, sağlık)
```

---

## ⚙️ Kurulum

1. [Unity Hub](https://unity.com/download) ile **Unity 6 LTS** sürümünü indir
2. Bu repoyu klonla:
   ```
   git clone https://github.com/kullaniciadi/GreenhouseDigitalTwin.git
   ```
3. Unity Hub → **Open** → klonlanan klasörü seç
4. `GreenhouseScene` sahnesini aç
5. Play ▶ tuşuna bas

---

## 🧪 Test Etme

Her scripti Inspector üzerinden test edebilirsiniz:

- `SimulationClock` → `Time Scale` değerini 3600 yap, gün hızla geçer
- `WeatherSystem` → `outsideTemp` ve `solarRadiation` saate göre değişir
- `EnvironmentPhysics` → `heaterActive` kutusunu işaretle, `insideTemp` artar
- `SoilModel` → `irrigationActive` kutusunu işaretle, `soilMoisture` artar
