# 🚚 Delivery Simulation Game - Master Documentation & Architecture Guide

Bu dokümantasyon, Unity Fizik Tabanlı Kargo Dağıtım Simülasyonunun tüm temel sistemlerini, mimari yapısını, editor araçlarını ve oynanış mekaniklerini içerir.

---

## 📋 İçindekiler
1. [Oynanış Döngüsü (Core Game Loop)](#-1-oynani%C5%9F-d%C3%B6ng%C3%BCs%C3%BC)
2. [Araç Fiziği & Sürüş Sistemi](#-2-ara%C3%A7-fizi%C4%9Fi--s%C3%BCr%C3%BC%C5%9F-sistemi)
3. [Kargo & Taşıma Fiziği (Physics Carry & Throw)](#-3-kargo--ta%C5%9F%C4%B1ma-fizi%C4%9Fi)
4. [Seviye (Level), XP & Şube Büyütme](#-4-seviye-level-xp--%C5%9Fube-b%C3%BCy%C3%BCtme)
5. [Özel Kargo Türleri (Fragile & Express)](#-5-%C3%B6zel-kargo-t%C3%BCrleri)
6. [Ekonomi & Gün Sonu Sistemi](#-6-ekonomi--g%C3%BCn-sonu-sistemi)
7. [Editor Araçları & Hızlı Kurulum Menüleri](#-7-editor-ara%C3%A7lar%C4%B1--h%C4%B1zl%C4%B1-kurulum)

---

## 🔄 1. Oynanış Döngüsü (Core Game Loop)

1. **Sabah (09:00):** Depoda (`CargoWarehouseGenerator`) oyuncunun seviyesine (`PlayerLevel`) uygun teslimat noktalarına koli partisi üretilir.
2. **Yükleme & Sürüş:** Koliler **`[E]`** ile tutularak veya şarj edilerek fırlatılarak kamyonetin kasasına yüklenir.
3. **Kargo Dağıtımı:** Haritadaki evlerin / işletmelerin kapı önüne gidilerek koliler bırakılır (Gün içinde para hemen kesilmez/verilmez, koliler dünyada fiziksel kalır).
4. **Akşam (18:00 / F8):** Gün sonu (`DaySummaryManager`) çalışır. Paketlerin nerede olduğu taranır:
   - **Doğru Adres:** +Ödül ($) ve +XP
   - **Ekspres (13:00 Öncesi):** +%40 Erken Teslimat Bonusu ($)
   - **Kırılan Kırılabilir Kargo:** -Hasar Cezası ($)
   - **Yanlış / Bırakılmayan Kargo:** -Ceza ($)
   - **Şube Kirası:** -Günlük Kira ($)
   - **Net Kâr & Seviye Atlama:** Kalan bakiye kalıcı kasaya aktarılır, XP ile seviye atlanır.

---

## 🚗 2. Araç Fiziği & Sürüş Sistemi

- **`CarController.cs`:**
  - Gerçekçi Ackermann Diferansiyel Direksiyon Geometrisi (İç tekerlek 40°, dış tekerlek 32°).
  - AWD 35/65 tork dağılımı (Ön tekerlekler çeker, arka tekerlekler iter).
  - Dinamik Dönüş Torku Desteği (Dar sokaklarda rahat ve seri manevra).
  - Boşa Çıkma & Doğal Yavaşlama (`ClearAllForces()` & nötr frenleme).
- **`DrivableVehicle.cs`:**
  - 0.35s binme/inme koruma süresi (Aynı karede arabadan atılmayı engeller).
  - Koltuk noktasına ebeveynleme (`seatPoint`).
- **Kamera Modları (`FPSPlayerController.cs`):**
  - **Araç İçi Serbest Bakış:** Kokpit içindeyken fare ile sağa/sola (±110°) ve aynalara/göstergelere bakabilme.
  - **`[V]` Tuşu Kamera Değişimi:** Birinci Şahıs (Kokpit) ⟷ Üçüncü Şahıs (Dıştan Takip / Chase Camera).

---

## 🖐️ 3. Kargo & Taşıma Fiziği

- **`PhysicsGrabber.cs`:**
  - **Sıfır Titreme (De-jitter):** Koli tutulduğu anda oyuncunun kendi `CharacterController` kapsülü ile çarpışması `Physics.IgnoreCollision` ile kapatılır.
  - **Otomatik Yüz Dönüşü:** Koli tutulduğunda, üstündeki etiket doğrudan **`(-60°, 0°, 0°)`** açıyla oyuncunun göz hizasına çevrilir.
- **Şarjlı Fırlatma Mekaniği (`FPSPlayerController.cs`):**
  - **Tek Tık `[E]` / `[Sol Tık]`:** Olduğu yere nazikçe bırakır.
  - **Basılı Tutma (0.8s):** Canlı güç göstergesi dolar; bırakınca kameranın baktığı yöne doğru fiziksel kuvvetle fırlatır.
- **Kutu Etiketi (`PhysicalCargoPackage.cs`):**
  - Taşmaları önleyen WordWrapping ve AutoSizing.
  - 15-18 karakterden uzun isimler için **`...` (Ellipsis)** kısaltma sistemi.
  - Kutu üzerinde yalnızca **Alıcı Adı** ve **Adres** yazar.

---

## 📊 4. Seviye (Level), XP & Şube Büyütme

- **`PlayerProgressionManager.cs`:**
  - **Player Level:** Seviye 1'den başlar, XP toplandıkça artar.
  - **Warehouse Level (Şube Seviyesi):**
    - *Lvl 1:* 4 Koli / Gün ($50 Kira)
    - *Lvl 2:* 7 Koli / Gün ($120 Kira)
    - *Lvl 3:* 12 Koli / Gün ($280 Kira)
    - *Lvl 4:* 18 Koli / Gün ($550 Kira)
- **`DeliveryPoint.cs`:**
  - Her teslimat noktasında `requiredLevel` bulunur.
  - Depo yalnızca seviyenin yettiği teslimat noktalarına kargo üretir.

---

## 📦 5. Özel Kargo Türleri

| Kargo Tipi | Görsel Şerit | Kazanç Çarpanı | Özel Mekanik |
| :--- | :--- | :--- | :--- |
| **Standard** | Beyaz Etiket | 1.0x ($) / 1.0x XP | Temel teslimat |
| **Fragile (Kırılabilir)** | Turuncu `[FRAGILE]` | 1.5x ($) / 1.4x XP | Sert çarpmada (>6.5 m/s) hasar alır, kırılırsa ceza |
| **Express (Süreli)** | Mavi `[EXPRESS]` | 1.8x ($) / 1.6x XP | 13:00 öncesi teslimde ekstra +%40 bonus prim |

---

## 🛠️ 6. Editor Araçları (`Tools ➔ Delivery Game`)

Unity üst menüsündeki hazır otomasyon araçları:

1. **`Setup Drivable Vehicle on Selected Object`:** Herhangi bir 3D araba modelini seçip tek tıkla `WheelCollider`lar, koltuk, fizik ve sürülebilir araç scriptleriyle donatır.
2. **`Add Delivery Point at Selected Object`:** Seçili binanın/kapının önüne otomatik tetikleyicili `DeliveryPoint` yerleştirir.
3. **`Create Cargo Generator Area`:** Depo koli üretim platformunu sahneye ekler.
4. **`Create Interaction Prompt HUD`:** Ekran ortası nişangah, istem kutusu ve sağ yan kargo detay kartını oluşturur.
5. **`Create Complete Delivery UI Suite`:** Tablet, Gün Sonu Paneli, Bildirimler ve HUD'u Canvas'a kurar.
