# 🎧 Oyun İçi Ses & Efekt Dosyaları Rehberi (Audio Assets Guide)

Bu klasör, oyundaki tüm ses efektlerinin (SFX), araç motor seslerinin, arayüz (UI) ve ortam seslerinin (Ambience) yerleştirileceği merkezi ses kütüphanesidir.

Desteklenen Formatlar: **.wav** (En kaliteli & gecikmesiz - önerilen), **.ogg** (Döngüsel sesler için ideal) veya **.mp3**

---

## 📁 Güncel Klasör Yapısı ve Dosya Listesi

### 1. 🚶 `Assets/_Project/Audio/Player/`
- `Player_Footstep` : Tek adım sesi (Yürüme/koşmaya göre hız & pitch koddan modüle edilir)
- `Player_Jump` : Zıplama sesi
- `Player_Land` : Yere iniş sesi

### 2. `Assets/_Project/Audio/Cargo/`
- `Cargo_Grab` : Koli tutma karton hışırtısı
- `Cargo_Drop_Light` : Koliyi hafifçe yere koyma
- `Cargo_Drop_Medium` : Koli orta şiddette çarpma/düşme
- `Cargo_Drop_Heavy` : Koli sert çarpma
- `Cargo_Throw` : Koliyi fırlatma rüzgar sesi (Whoosh)
- `Cargo_Fragile_Rattle` : Kırılabilir koli iç cam şıngırtısı
- `Cargo_Fragile_Break` : Kırılabilir koli cam kırılma sesi
- `Cargo_Explosive_Beep_Warning` : Patlayıcı koli geri sayım ikaz bip sesi
- `Cargo_Explosive_Detonation` : Patlayıcı koli patlama sesi

### 3. 🚗 `Assets/_Project/Audio/Vehicle/`
- `Vehicle_Engine_Start` : Motor marş ve çalışma sesi
- `Vehicle_Engine_Idle_Loop` : **Tek ve ana motor sesi** (Gaza ve hıza göre pitch & ses yükselir - Loop)
- `Vehicle_Engine_Stop` : Motor kontak kapatma sesi
- `Vehicle_Reverse_Beep_Loop` : Geri vites ikaz bip sesi (Loop)
- `Vehicle_Brake_Squeak` : Fren balata ve lastik kayma sesi
- `Vehicle_Door_Open` : Şoför kapısı açılma sesi
- `Vehicle_Door_Close` : Şoför kapısı sert kapanma sesi
- `Vehicle_Tailgate_Open` : Kasa arka kapağı açılma sesi
- `Vehicle_Tailgate_Close` : Kasa arka kapağı kapanma sesi
- `Vehicle_Crash_Light` : Hafif tampon vurma / sürtme sesi
- `Vehicle_Crash_Heavy` : Şiddetli metal kaza çarpma sesi

### 4. 🏢 `Assets/_Project/Audio/Commercial/`
- `Fuel_Pumping_Loop` : Benzin dolum sıvı akış sesi (Loop)
- `Fuel_Pump_Finish_Beep` : Pompa dolum bitti ikaz sesi
- `Garage_Repair_Wrench` : Oto tamir cırcır / anahtar sesi
- `Garage_Paint_Spray` : Boya tabancası fıslama sesi
- `Property_Purchase_Cash` : Mülk satın alma yazarkasa / para sesi

### 5. 🖥️ `Assets/_Project/Audio/UI/`
- `UI_Tablet_Open` : Tablet ve Gün Sonu Bilançosu açılış sesi
- `UI_Tablet_Close` : Tablet kapanış sesi
- `UI_Tab_Switch` : Sekme geçiş sesi
- `UI_Button_Click` : Buton klik sesi
- `UI_Notification_Popup` : HUD bildirim açılış sesi
- `UI_Error_Buzzer` : Yetersiz bakiye / hata buzzer'ı
- `UI_Money_Add` : Kasaya para eklenme şıkırtısı
- `UI_Money_Subtract` : Harcama / ceza kesinti sesi
- `UI_Level_Up` : Seviye atlama kutlama müziği

### 6. 🌲 `Assets/_Project/Audio/Ambience/`
- `Ambient_River_Stream_Loop` : **Akarsu / dere / nehir su akış sesi** (3D - Loop)
- `Ambient_Day_Valley_Loop` : Vadi açık hava rüzgar ve hafif kuş sesi (Loop)
- `AI_Traffic_Horn` : AI araç korna çalma sesi (Önünde engel/araç durduğunda)
