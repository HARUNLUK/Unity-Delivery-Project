# 🎵 Background Music (Arka Plan Fon Müziği)

Bu klasöre oyunun fon müziğini (`.wav`, `.mp3`, `.ogg` veya `.flac`) ekleyebilirsiniz:

- `Background_Music` (veya `BGM`, `Cozy_Music`, `Theme_Music`, `Soundtrack`, `Main_Theme`)

Dosyayı buraya eklediğinizde sistem otomatik olarak `AudioManager.backgroundMusic` alanına bağlar ve oyun açıldığında 2D stereo olarak kesintisiz çalmaya başlar.

---

### Canlı Ses Ayarları:
- `AudioManager -> Background Music Volume` (Varsayılan: 0.50)
- `AudioManager -> Music Volume` kanalı (Varsayılan: 0.80)
- `Play Music On Start`: Açılışta otomatik başlatma tiki (Açık/Kapalı)
