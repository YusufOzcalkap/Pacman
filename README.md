# PAC-MAN Case Study — Yusuf Özçalkap

Agave Games engineering case iskeleti üzerine geliştirilmiş PAC-MAN benzeri oyun.
Unity **2022.3.62f2** · URP 2D · DOTween · Odin Inspector

## Çalıştırma

`Assets/Scenes/GameplayScene` sahnesini açıp Play'e basın.

- **Klavye:** ok tuşları ya da WASD
- **Dokunmatik/Fare:** ekrandaki yön butonları
- Her level **READY!** duraklamasıyla başlar; bu sırada yön seçilebilir.

## Case Gereklilikleri

| Gereklilik | Durum |
|---|---|
| Pacman, GridData'daki başlangıç hücresinde doğar | ✅ |
| Google PAC-MAN referanslı hareket (tamponlu yön, köşede otomatik dönüş) | ✅ `Pacman.cs` |
| Yapay zeka karakterleri AiSpawnZone'da doğar | ✅ |
| **InHouse** — evde yukarı-aşağı salınım | ✅ `AiStates/InHouseState.cs` |
| **JoiningGame** — farklı gecikmelerle (3/6/9 sn) kapıdan JoinGameCell'e | ✅ `AiStates/JoiningGameState.cs` |
| Oyuna katılan bir daha eve dönemez | ✅ yürünebilirlik kümesiyle yapısal olarak (aşağıya bakın) |
| **Scatter** — her köşede rastgele yön, geldiği yöne dönmez | ✅ `AiStates/ScatterState.cs` |
| **Chase** — görüş hattı tetikler, hepsi birden geçer, en kısa yol, köşeye kadar yön sabit | ✅ `AiStates/ChaseState.cs` + `Ghost.HasLineOfSightTo` |
| Yakalanınca oyun durur, fail animasyonu oynar | ✅ `GameManager.LoseLife` |

## Eklenen Ekstralar

- **Hayalet kişilikleri** (orijinal oyundaki gibi): Blinky doğrudan takip eder, Pinky önünü keser,
  Inky Blinky'ye göre yansıtma yapar, Clyde yaklaşınca köşesine kaçar. Tek fark hedef hücredir;
  davranışın geri kalanı ortaktır (`AiStates/ChaseTargets/`).
- **Yem + skor sistemi**: normal yemler tüm boş hücrelere otomatik dağıtılır, güçlendirme yemleri
  editörden işaretlenir. Yemler yalnızca Pacman'in ulaşabildiği hücrelere konur.
- **Frightened / Eaten**: güçlendirme yemi hayaletleri yavaşlatıp kaçırtır; yenen hayaletin
  gözleri eve döner ve yeniden katılır. Zincirleme yeme puanı katlanır (200/400/800/1600).
- **Scatter–Chase dalgaları**: orijinal oyunun zamanlama tablosu; görüş hattı sıradaki takip
  dalgasını öne alır. Mod değişiminde hayaletler geri döner.
- **Tünel desteği**: level başına açılabilir (`GridData.WrapEdges`); hareket, yol bulma ve görüş
  tünelden geçer.
- **Can sistemi + level ilerleme**: 3 can (her levelde tazelenir), 20 hazır level, level başına
  hız/korku süresi/hayalet sayısı ayarı (`Resources/LevelSet.asset`).
- **HUD ve paneller**: skor, level, can ikonları, READY! ve LEVEL COMPLETE duyuruları,
  GAME OVER + PLAY AGAIN paneli.
- **Level Editörü** ve **doğrulama sistemi** (aşağıda).

## Debug Görselleştirme (F1)

Oyun sırasında **F1**: her hayaletin görüş ışını (yeşil = Pacman görünüyor, kırmızı = kesildi),
takip yolu ve kişilik hedefi, kavşak hücreleri, canlı state etiketleri ve özet panel
(mod, dalga süresi, skor, kalan yem). Tamamı genel API üzerinden okur; oynanışı hiç etkilemez.
Çizgiler Scene view'da her zaman, Game view'da Gizmos açıkken görünür; metin katmanı her koşulda görünür.

## Level Editörü

**Tools ▸ Level Editor** — soldaki liste `LevelSet`'i sürer (sıra = oyundaki level sırası).
Sürükleyerek boyama, sağ tık silgi, ayna modu, kova, zoom, `1-7` fırça kısayolları,
oyundaki görüntünün birebir önizlemesi ve **her değişiklikte çalışan doğrulama**:
ev sızdırmazlığı, spawn→join bağlantısı, ulaşılamayan hücreler, tünel simetrisi gibi
10 kural hatalı hücreleri harita üzerinde işaretler. Aynı kurallar oyun açılışında da çalışır.

## Mimari Notlar

- **Tek tick sahibi:** karakterlerin kendi `Update`'i yoktur; `GameManager` girdiyi okur,
  herkesi sırayla ilerletir. Oyunu duraklatmak/durdurmak tek noktadan yapılır.
- **`GridMover`:** hücreden hücreye DOTween ile ilerleyen ortak hareket bileşeni.
  "Nasıl hareket edilir" ile "nereye gidilir" ayrıdır; kararlar hücre merkezinde verilir.
- **State pattern + blackboard:** teslim edilen iskelet dolduruldu; state nesneleri geçişte
  yeniden yaratılır, kalıcı bağlam `GhostBlackboard`'dadır. Scatter/Chase/Frightened ortak
  dolaşım mantığını `RoamingState`'ten alır, yalnızca "kavşakta hangi yön" sorusunu cevaplar.
- **Yürünebilirlik kümeleri:** hangi state'in hangi hücrelere girebildiği `GameSettings`'te
  tek yerde tanımlıdır. "Eve dönememe" kuralı bayrakla değil, katıldıktan sonra kullanılan
  kümenin `AiGate`/`AiSpawnZone` içermemesiyle sağlanır.
- **Veri güdümlü:** haritalar `GridData`, level dizisi ve zorluk `LevelSet`, görsel ayarlar
  `MapVisualSettings`. Zorluk eğrisi kod değişmeden asset'ten ayarlanır.
- **Deterministik rastgelelik:** yapay zeka seed'li `System.Random` kullanır
  (`GameSettings.RandomSeed`); aynı seed aynı davranışı üretir, hatalar tekrarlanabilir.

## Bilinçli Kararlar ve Varsayımlar

- **Eaten istisnası:** "oyuna katılan eve dönemez" kuralı Scatter/Chase için geçerlidir;
  yenilen hayaletin gözlerinin eve dönmesi güçlendirme yeminin karşılığı olarak eklenmiştir.
- **Görüş menzili** dokümanda sayı verilmediği için `GameSettings.AiSightDistance = 6` hücredir.
- Yön kararları hücre merkezinde verilir; tek istisna oyuncunun anında ters dönüşüdür
  (orijinal oyundaki gibi).
- Ses ve meyve/bonus bilinçli olarak kapsam dışıdır.

## Üçüncü Parti

DOTween (hareket/animasyon), Odin Inspector (inspector ve editör araçları), TextMeshPro.
