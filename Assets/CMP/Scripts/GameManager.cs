using System.Collections.Generic;
using CMP.Scripts.Debugging;
using CMP.Scripts.Helper;
using CMP.Scripts.Hud;
using CMP.Scripts.Pellets;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

namespace CMP.Scripts
{
    public enum GameMode
    {
        Scatter,
        Chase,

        /// <summary>Level başı "READY!" duraklaması; kimse hareket etmez, girdi okunmaz.</summary>
        Ready,

        /// <summary>Can kaybedildi; fail animasyonu oynarken oyun bekliyor, sonra tur yeniden kurulacak.</summary>
        LifeLost,
        GameOver,
        LevelComplete,
    }

    public class GameManager : MonoBehaviour
    {
        /// <summary>
        /// Yapay zekanın kullandığı rastgelelik kaynağı.
        /// Tek bir örnek üzerinden dağıtılıyor ki seed tüm oyunu deterministik yapsın.
        /// (İsmi bilerek "Random" değil: UnityEngine.Random'ı gölgelemesin.)
        /// </summary>
        public GameRandom AiRandom { get; private set; }

        [ShowInInspector, ReadOnly, BoxGroup("Runtime")]
        private GameMode _gameMode = GameMode.Scatter;

        public GameMode CurrentMode => _gameMode;

        /// <summary>Mevcut dalganın kalan süresi; hata ayıklama katmanı gösterir.</summary>
        public float WaveTimeLeft => _waveTimer;

        [ShowInInspector, ReadOnly, BoxGroup("Runtime")]
        public int Score { get; private set; }

        [ShowInInspector, ReadOnly, BoxGroup("Runtime")]
        public int Lives { get; private set; } = GameSettings.StartingLives;

        [ShowInInspector, ReadOnly, BoxGroup("Runtime")]
        public int RemainingPellets => _pelletManager?.RemainingCount ?? 0;

        [ShowInInspector, ReadOnly, BoxGroup("Runtime")]
        public int LevelNumber => _levelIndex + 1;

        [ShowInInspector, ReadOnly, BoxGroup("Runtime")]
        private readonly List<Ghost> _ghosts = new();

        private PelletManager _pelletManager;
        private GameHud _hud;
        private GameplayDebugView _debugView;
        private int _ghostEatenStreak;
        private int _waveIndex;
        private float _waveTimer;

        private LevelSet _levelSet;
        private LevelRules _rules;
        private GameObject _levelRoot;
        private int _levelIndex;

        private GridData _gridData;
        private Pacman _pacman;
        private InputManager _inputManager;

        private void Start()
        {
            InitializeTweenEngine();
            AiRandom = new GameRandom(GameSettings.RandomSeed);

            _levelSet = AssetDatabase.Instance.LevelSet;
            _inputManager = Instantiate(AssetDatabase.Instance.InputManagerPrefab);
            _debugView = gameObject.AddComponent<GameplayDebugView>();
            CreateHud();

            LoadLevel(0);
        }

        /// <summary>
        /// Bir level'ı sıfırdan kurar. Level'a ait her şey tek bir kök nesnenin altında
        /// yaşadığı için önceki level'ı temizlemek o kökü yok etmekten ibarettir.
        /// Skor level'lar arasında taşınır; canlar her level başında tazelenir.
        /// </summary>
        private void LoadLevel(int levelIndex)
        {
            Lives = GameSettings.StartingLives;
            _hud.SetLives(Lives);

            if (_levelRoot != null)
            {
                Destroy(_levelRoot);
            }

            _levelRoot = new GameObject("Level");
            _levelIndex = levelIndex;
            _ghosts.Clear();
            _ghostEatenStreak = 0;

            var definition = _levelSet.GetLevel(levelIndex);
            _gridData = definition.Grid;
            _rules = new LevelRules(definition);

            LevelValidator.LogIssues(_gridData);

            CreateBackground(_gridData);
            AdjustCamera(_gridData);
            CreatePacman(_gridData);
            _pelletManager = new PelletManager(_gridData, _pacman.CurrentCell,
                AssetDatabase.Instance.MapVisualSettings, _levelRoot.transform);
            CreateGhosts(_gridData);

            _debugView.Initialize(this, _gridData, _pacman, _ghosts);

            _hud.SetLevel(LevelNumber);
            _hud.SetScore(Score);
            BeginReadyPhase();
        }

        /// <summary>
        /// "READY!" duraklaması: level kurulu ama kimse oynamıyor; süre dolunca
        /// dalga tablosu baştan başlar ve oyun açılır.
        /// </summary>
        private void BeginReadyPhase()
        {
            _gameMode = GameMode.Ready;
            _hud.ShowBanner("READY!");

            DOVirtual.DelayedCall(GameSettings.ReadyDuration, () =>
            {
                _hud.HideBanner();
                StartWave(0);
            }).SetLink(gameObject);
        }

        /// <summary>
        /// Oyunun tek tick sahibi burasıdır: girdiyi okur ve karakterleri sırayla ilerletir.
        /// Karakterlerin kendi Update'i olmadığı için oyun bittiğinde her şey tek yerden durur.
        /// </summary>
        private void Update()
        {
            if (_gameMode == GameMode.Ready)
            {
                // READY sırasında hareket yok ama girdi yutulmaz: oyuncu yönünü
                // önceden seçer, Pacman o yöne dönerek "aldım" der ve oyun açılır
                // açılmaz seçilen yönde yola çıkar.
                _pacman.SetRequestedDirection(_inputManager.ConsumeInput());
                return;
            }

            if (_gameMode is GameMode.LifeLost or GameMode.GameOver or GameMode.LevelComplete)
            {
                return;
            }

            _pacman.SetRequestedDirection(_inputManager.ConsumeInput());
            _pacman.Tick();
            ConsumePelletUnderPacman();

            if (_gameMode == GameMode.LevelComplete)
            {
                return;
            }

            foreach (var ghost in _ghosts)
            {
                ghost.Tick();
            }

            if (TryResolveGhostContact())
            {
                return;
            }

            UpdateGameMode();
        }

        /// <summary>
        /// Pacman'in bulunduğu hücredeki yemi toplar. Hücrede yem yoksa hiçbir şey olmaz,
        /// bu yüzden her karede çağrılabiliyor.
        /// </summary>
        private void ConsumePelletUnderPacman()
        {
            if (!_pelletManager.TryConsume(_pacman.CurrentCell, out var gainedScore, out var isPowerPellet))
            {
                return;
            }

            AddScore(gainedScore);

            if (isPowerPellet)
            {
                // Her yeni güçlendirme yemi zinciri sıfırlar: puan katlaması
                // yalnızca aynı korku süresi içinde geçerli.
                _ghostEatenStreak = 0;

                foreach (var ghost in _ghosts)
                {
                    ghost.EnterFrightened();
                }
            }

            if (_pelletManager.RemainingCount == 0)
            {
                CompleteLevel();
            }
        }

        private void CompleteLevel()
        {
            _gameMode = GameMode.LevelComplete;
            StopAllCharacters();
            _hud.ShowBanner($"LEVEL {LevelNumber} COMPLETE!");

            DOVirtual.DelayedCall(GameSettings.LevelCompleteDelay, () => LoadLevel(_levelIndex + 1))
                .SetLink(gameObject);
        }

        /// <summary>
        /// Pacman ile hayaletlerin teması. Sonuç hayaletin durumuna bağlı:
        /// korkmuşsa yenir, normalse oyun biter, yenilmiş gözler ise zararsızdır.
        ///
        /// Mesafe her karede ölçülüyor: karakterler hücreler arasında yumuşak
        /// ilerlediği için yalnızca hücre merkezlerine bakılsaydı yan yana geçerken
        /// birbirlerinin içinden geçip kaçabilirlerdi.
        /// </summary>
        /// <returns>Oyun bittiyse true.</returns>
        private bool TryResolveGhostContact()
        {
            var pacmanPosition = _pacman.transform.position;

            foreach (var ghost in _ghosts)
            {
                if (ghost.IsEaten)
                {
                    continue;
                }

                if (Vector3.Distance(pacmanPosition, ghost.transform.position) >= GameSettings.CatchDistance)
                {
                    continue;
                }

                if (ghost.IsFrightened)
                {
                    EatGhost(ghost);
                    continue;
                }

                LoseLife();
                return true;
            }

            return false;
        }

        private void EatGhost(Ghost ghost)
        {
            var scoreIndex = Mathf.Min(_ghostEatenStreak, GameSettings.GhostEatenScores.Length - 1);
            AddScore(GameSettings.GhostEatenScores[scoreIndex]);
            _ghostEatenStreak++;

            ghost.EnterEaten();
        }

        /// <summary>
        /// Oyunu bitirir: herkesin hareketi kesilir ve Pacman yakalanma animasyonunu oynatır.
        /// Karakterlerin kendi Update'i olmadığı için durdurmak tek yerden yapılabiliyor.
        /// </summary>
        /// <summary>
        /// Can kaybı akışı: herkes durur, fail animasyonu oynar. Can kaldıysa kısa bir
        /// beklemenin ardından tur yeniden kurulur; kalmadıysa oyun biter.
        /// </summary>
        /// <summary>
        /// Can ikonunun sprite'ı Pacman'in kendi prefabından alınır; ayrı asset gerekmez.
        /// HUD level'lar arasında yaşadığı için sahnedeki örneğe değil prefaba bakar.
        /// </summary>
        private void CreateHud()
        {
            var pacmanSprite = AssetDatabase.Instance.PacmanPrefab.Animator
                .GetComponent<SpriteRenderer>().sprite;
            _hud = new GameHud(pacmanSprite, GameSettings.StartingLives, RestartGame);
            _hud.SetLives(Lives);
        }

        /// <summary>Oyun sonu panelindeki "Tekrar Oyna": sahneyi sıfırdan yükler.</summary>
        private void RestartGame()
        {
            DOTween.KillAll();
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
        }

        /// <summary>Skorun tek giriş noktası; HUD'un güncel kalmasını garanti eder.</summary>
        private void AddScore(int amount)
        {
            Score += amount;
            _hud.SetScore(Score);
        }

        private void LoseLife()
        {
            Lives--;
            _hud.SetLives(Lives);
            StopAllCharacters();
            _pacman.PlayFailAnimation();

            if (Lives <= 0)
            {
                _gameMode = GameMode.GameOver;

                // Panel, fail animasyonu bittikten sonra açılır; oyuncu önce ne
                // olduğunu görsün.
                DOVirtual.DelayedCall(GameSettings.LifeLostResetDelay,
                    () => _hud.ShowGameOverPanel(Score)).SetLink(gameObject);
                return;
            }

            _gameMode = GameMode.LifeLost;
            DOVirtual.DelayedCall(GameSettings.LifeLostResetDelay, ResetRound).SetLink(gameObject);
        }

        /// <summary>
        /// Turu yeniden kurar: Pacman başlangıç hücresine, hayaletler evlerine döner,
        /// dalga tablosu baştan başlar. Skor ve toplanmış yemler korunur — oyuncunun
        /// ilerlemesi cana değil, levele bağlıdır.
        /// </summary>
        private void ResetRound()
        {
            _pacman.ResetToStart();

            foreach (var ghost in _ghosts)
            {
                ghost.ResetToHouse();
            }

            _ghostEatenStreak = 0;
            BeginReadyPhase();
        }

        private void StopAllCharacters()
        {
            _pacman.Stop();
            foreach (var ghost in _ghosts)
            {
                ghost.Stop();
            }
        }

        /// <summary>
        /// Oyunun modu iki kaynaktan belirlenir:
        /// dalga tablosu tabanı kurar, görüş ise sıradaki takip dalgasını öne alır.
        /// Sonuç her karede bütün hayaletlere uygulanır; bu sayede sonradan oyuna
        /// katılan ya da korkusu geçen hayaletler de kendiliğinden senkronize olur.
        /// </summary>
        private void UpdateGameMode()
        {
            _waveTimer -= Time.deltaTime;
            if (_waveTimer <= 0f)
            {
                StartWave(_waveIndex + 1);
            }

            if (AnyGhostSeesPacman())
            {
                BringForwardChaseWave();
            }

            foreach (var ghost in _ghosts)
            {
                ghost.ApplyGameMode(_gameMode);
            }
        }

        private void StartWave(int index)
        {
            _waveIndex = Mathf.Min(index, GameSettings.ModeWaves.Length - 1);
            var wave = GameSettings.ModeWaves[_waveIndex];

            _waveTimer = wave.Duration;
            SetMode(wave.Mode);
        }

        /// <summary>
        /// Bir hayalet Pacman'i gördüğünde tabloda sıradaki takip dalgasına atlanır.
        /// Böylece görüş tetikleyicisi dalga sistemini bozmadan öne çekiyor.
        /// </summary>
        private void BringForwardChaseWave()
        {
            if (_gameMode == GameMode.Chase)
            {
                return;
            }

            for (var index = _waveIndex; index < GameSettings.ModeWaves.Length; index++)
            {
                if (GameSettings.ModeWaves[index].Mode != GameMode.Chase)
                {
                    continue;
                }

                StartWave(index);
                return;
            }
        }

        private void SetMode(GameMode mode)
        {
            if (_gameMode == mode)
            {
                return;
            }

            _gameMode = mode;

            // Mod değişiminde hayaletler geri döner. Orijinal oyundaki bu davranış
            // hem oyuncuya değişimi anlatır hem de köşeye sıkışmayı önler.
            foreach (var ghost in _ghosts)
            {
                ghost.ReverseDirection();
            }
        }

        private bool AnyGhostSeesPacman()
        {
            foreach (var ghost in _ghosts)
            {
                if (ghost.HasLineOfSightTo(_pacman.CurrentCell))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Pacman'i GridData'da işaretlenmiş başlangıç hücresinde oluşturur.
        /// Haritada birden fazla Pacman hücresi olması anlamsız olduğu için ilkini kullanıyoruz;
        /// bu durum level editöründeki doğrulama adımında ayrıca yakalanacak.
        /// </summary>
        private void CreatePacman(GridData gridData)
        {
            var startCell = gridData.GetCoordsOfCellType(CellType.Pacman)[0];
            _pacman = Instantiate(AssetDatabase.Instance.PacmanPrefab, _levelRoot.transform);
            _pacman.Initialize(gridData, startCell, _rules.PacmanStepDuration);
        }

        /// <summary>
        /// Yapay zeka karakterlerini GridData'daki AiSpawnZone hücrelerinde oluşturur.
        /// Her birine kendi katılma gecikmesi verilir, böylece oyuna sırayla dahil olurlar.
        /// </summary>
        private void CreateGhosts(GridData gridData)
        {
            var spawnCells = gridData.GetCoordsOfCellType(CellType.AiSpawnZone);

            for (var i = 0; i < _rules.GhostCount; i++)
            {
                // Spawn hücresi sayısı karakter sayısından az olabilir; hücreleri sırayla tekrar kullanıyoruz.
                var spawnCell = spawnCells[i % spawnCells.Count];
                var personality = GameSettings.AiPersonalities[i % GameSettings.AiPersonalities.Length];

                var ghost = Instantiate(AssetDatabase.Instance.GhostPrefab, _levelRoot.transform);
                ghost.name = personality.ToString();
                ghost.SetBodyColor(GameSettings.AiColors[i % GameSettings.AiColors.Length]);
                ghost.Initialize(gridData, _pacman, _ghosts, AiRandom, personality, spawnCell,
                    GetJoinDelay(i), _rules);
                _ghosts.Add(ghost);
            }
        }

        /// <summary>
        /// Tablodaki gecikmeler biterse aynı aralıkla devam edilir; böylece level
        /// başına hayalet sayısı tablodan bağımsız artırılabilir.
        /// </summary>
        private static float GetJoinDelay(int ghostIndex)
        {
            var delays = GameSettings.AiJoinDelays;
            if (ghostIndex < delays.Length)
            {
                return delays[ghostIndex];
            }

            return delays[^1] + (ghostIndex - delays.Length + 1) * 3f;
        }

        /// <summary>
        /// Karakterler her 0.25 saniyede bir yeni tween yarattığı için tween havuzunu
        /// önden ayırıyoruz; böylece oyun sırasında yeniden tahsis yapılmıyor.
        /// </summary>
        private static void InitializeTweenEngine()
        {
            DOTween.Init(recycleAllByDefault: true, useSafeMode: true, LogBehaviour.ErrorsOnly);
            DOTween.SetTweensCapacity(tweenersCapacity: 64, sequencesCapacity: 16);
        }

        private void CreateBackground(GridData gridData)
        {
            var targetTexture = MapTextureGenerator.Generate(gridData, AssetDatabase.Instance.MapVisualSettings);
            var textureObject = new GameObject("MapTexture");
            textureObject.transform.SetParent(_levelRoot.transform);
            textureObject.transform.position = new Vector3(-0.5f, -0.5f, 0f);
            var targetSprite = Sprite.Create(targetTexture, new Rect(0f, 0f, targetTexture.width, targetTexture.height),
                Vector2.zero,AssetDatabase.Instance.MapVisualSettings.pixelsPerCell);
            var spriteRenderer = textureObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = targetSprite;
            // Harita en altta, yemler onun üstünde (-1), karakterler en üstte (0).
            spriteRenderer.sortingOrder = -2;
        }

        /// <summary>
        /// Haritayı, ekranın altındaki yön butonlarının üstünde kalan oyun bandına oturtur.
        /// Bant yüksekliği butonların gerçek ekran alanından ölçülür; böylece farklı
        /// çözünürlüklerde de harita butonların altında kalmaz.
        /// </summary>
        private void AdjustCamera(GridData gridData)
        {
            var mainCamera = Camera.main;

            var uiFraction = _inputManager.GetOccupiedScreenHeightRatio();
            var playAreaFraction = 1f - uiFraction;

            var mapWidth = gridData.Width + GameSettings.CameraPadding * 2f;
            var mapHeight = gridData.Height + GameSettings.CameraPadding * 2f;

            // orthographicSize dikey yarı-yüksekliktir. Dikeyde harita tüm ekrana değil
            // sadece oyun bandına sığmalı; yatayda ise tüm ekranı kullanabilir. İki
            // kısıttan büyüğünü seçiyoruz ki editörden gelecek farklı oranlı haritalar
            // da kadraja tam otursun.
            var verticalSize = mapHeight / (2f * playAreaFraction);
            var horizontalSize = mapWidth / (2f * mainCamera.aspect);
            var orthographicSize = Mathf.Max(verticalSize, horizontalSize);
            mainCamera.orthographicSize = orthographicSize;

            // Hücre merkezleri tam sayı koordinatlarda, harita görseli ise (-0.5, -0.5)
            // noktasından başlıyor; bu yüzden haritanın gerçek merkezi yarım hücre geride.
            var mapCenterX = gridData.Width / 2f - 0.5f;
            var mapCenterY = gridData.Height / 2f - 0.5f;

            // Kamerayı aşağı kaydırınca harita yukarı kayar ve oyun bandının tam ortasına gelir.
            mainCamera.transform.position =
                new Vector3(mapCenterX, mapCenterY - orthographicSize * uiFraction, -10f);
        }
    }
}
