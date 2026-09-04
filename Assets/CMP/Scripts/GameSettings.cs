using System.Collections.Generic;
using UnityEngine;

namespace CMP.Scripts
{
    public static class GameSettings
    {
        public const float AiMovementDuration = 0.25f;
        public const float PacmanMovementDuration = 0.25f;
        public const int AiCharacterCount = 3;
        public static readonly float[] AiJoinDelays = { 3f, 6f, 9f };
        public static float CatchDistance = 1f;
        public static readonly Direction[] DirectionsToCheck =
            { Direction.Left, Direction.Right, Direction.Up, Direction.Down };

        public static float CameraPadding = 1f;

        /// <summary>
        /// Scatter ve Chase dalgalarının sırası. Orijinal oyunun ilk seviye tablosuyla
        /// aynı: kısa dağılma araları, uzun takip periyotları ve sonunda hiç bitmeyen takip.
        ///
        /// Dalgalar taban modu belirler; görüş tetiklendiğinde sıradaki takip dalgası
        /// öne alınır.
        /// </summary>
        public static readonly ModeWave[] ModeWaves =
        {
            new(GameMode.Scatter, 7f),
            new(GameMode.Chase, 20f),
            new(GameMode.Scatter, 7f),
            new(GameMode.Chase, 20f),
            new(GameMode.Scatter, 5f),
            new(GameMode.Chase, 20f),
            new(GameMode.Scatter, 5f),
            new(GameMode.Chase, Mathf.Infinity),
        };

        /// <summary>
        /// Chase'i tetikleyen görüş menzili (hücre cinsinden).
        /// Yapay zeka baktığı yönde bu mesafe içinde ve duvar arkasında olmayan
        /// pacman'i görürse oyundaki bütün yapay zekalar Chase'e geçer.
        /// </summary>
        public const int AiSightDistance = 6;

        /// <summary>
        /// Yapay zeka rastgeleliğinin seed'i. Aynı seed = aynı davranış dizisi,
        /// böylece bir hata her seferinde aynı şekilde tekrarlanabilir.
        /// </summary>
        public const int RandomSeed = 1337;

        /// <summary>
        /// Karakterlerin kişilikleri, oluşturulma sırasına göre.
        /// <see cref="AiColors"/> ile aynı sırada olduğu için renk ve davranış eşleşir.
        /// </summary>
        public static readonly GhostPersonality[] AiPersonalities =
        {
            GhostPersonality.Blinky,
            GhostPersonality.Pinky,
            GhostPersonality.Inky,
            GhostPersonality.Clyde,
        };

        /// <summary>
        /// Gövde renkleri. Renk karakterin verisidir, prefabın değil; bu yüzden tek
        /// prefab kullanıp rengi buradan veriyoruz.
        /// </summary>
        public static readonly Color[] AiColors =
        {
            new(1f, 0.16f, 0.16f), // Blinky - kırmızı
            new(1f, 0.72f, 1f), // Pinky - pembe
            new(0.3f, 0.9f, 1f), // Inky - camgöbeği
            new(1f, 0.72f, 0.32f), // Clyde - turuncu
        };

        /// <summary>Pinky'nin Pacman'in kaç hücre önünü hedeflediği.</summary>
        public const int PinkyAmbushDistance = 4;

        /// <summary>Inky'nin yansıtma hesabında kullandığı, Pacman'in önündeki dönüm noktası.</summary>
        public const int InkyPivotDistance = 2;

        /// <summary>Clyde bu mesafeden yakınsa takibi bırakıp köşesine çekilir.</summary>
        public const int ClydeLeashDistance = 8;

        // --- Yürünebilirlik kümeleri -------------------------------------------------
        // Hangi karakterin nereye girebileceğini state belirler. Kurallar tek yerde
        // toplandığı için "oyuna katıldıktan sonra eve dönememe" gibi gereklilikler
        // ayrı bir bayrak tutmadan, sadece doğru kümeyi seçerek karşılanır.

        /// <summary>Pacman'in girebildiği hücreler.</summary>
        public static readonly List<CellType> PacmanWalkableCells = new()
            { CellType.Empty, CellType.Pacman, CellType.JoinGameCell, CellType.PowerPellet };

        /// <summary>
        /// Evdeki yapay zekanın girebildiği hücreler.
        /// Ev duvarlarla çevrili ve tek çıkışı AiGate olduğu için bu küme
        /// yapay zekayı doğal olarak evin içinde tutar.
        /// </summary>
        public static readonly List<CellType> InHouseWalkableCells = new()
            { CellType.AiSpawnZone, CellType.Empty };

        /// <summary>Oyuna katılırken kullanılan küme; kapıdan geçişe izin veren tek küme budur.</summary>
        public static readonly List<CellType> JoiningGameWalkableCells = new()
        {
            CellType.AiSpawnZone, CellType.Empty, CellType.AiGate,
            CellType.JoinGameCell, CellType.Pacman, CellType.PowerPellet
        };

        /// <summary>
        /// Oyuna katılmış yapay zekanın kümesi.
        /// AiGate ve AiSpawnZone dışarıda bırakıldığı için yapay zeka bir daha eve dönemez.
        /// </summary>
        public static readonly List<CellType> InGameAiWalkableCells = new()
            { CellType.Empty, CellType.Pacman, CellType.JoinGameCell, CellType.PowerPellet };

        // --- Puanlama ---------------------------------------------------------------

        public const int PelletScore = 10;
        public const int PowerPelletScore = 50;

        // --- Korku modu -------------------------------------------------------------

        /// <summary>Güçlendirme yeminin toplam etki süresi.</summary>
        public const float FrightenedDuration = 7f;

        /// <summary>Sürenin bitmesine bu kadar kala hayaletler yanıp sönerek uyarır.</summary>
        public const float FrightenedBlinkDuration = 2f;

        public const float FrightenedBlinkInterval = 0.2f;

        /// <summary>Korkmuş hayalet yavaşlar; bir hücreyi bu sürede kat eder.</summary>
        public const float FrightenedMovementDuration = 0.4f;

        public static readonly Color FrightenedColor = new(0.22f, 0.28f, 1f);

        /// <summary>
        /// Aynı korku süresi içinde arka arkaya yenen hayaletlerin puanları.
        /// Zincir uzadıkça değer katlanır; oyuncuyu tek yemde birden fazla hayalet
        /// yakalamaya teşvik eder.
        /// </summary>
        public static readonly int[] GhostEatenScores = { 200, 400, 800, 1600 };

        /// <summary>Yenen hayaletin gözleri eve dönerken çok hızlıdır.</summary>
        public const float EatenMovementDuration = 0.1f;

        /// <summary>Eve dönen hayaletin yeniden oyuna katılmak için beklediği süre.</summary>
        public const float RespawnJoinDelay = 1.5f;

        // --- Can sistemi ------------------------------------------------------------

        public const int StartingLives = 3;

        /// <summary>
        /// Yakalanma anı ile turun yeniden kurulması arasındaki bekleme.
        /// Fail animasyonunun (~0.9 sn) bitmesine ve oyuncunun ne olduğunu
        /// görmesine yetecek kadar.
        /// </summary>
        public const float LifeLostResetDelay = 1.6f;

        /// <summary>Level tamamlandıktan sonra sıradaki levele geçmeden önceki bekleme.</summary>
        public const float LevelCompleteDelay = 2f;
    }
}
