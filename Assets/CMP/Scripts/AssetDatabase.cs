using UnityEngine;
using Object = UnityEngine.Object;

namespace CMP.Scripts
{
    /// <summary>
    /// Prefab ve ScriptableObject erişiminin tek noktası.
    /// Yükleme ilk kullanımda yapılır: tip yüklenirken Resources'a dokunulmaz,
    /// böylece yükleme sırası belirsizliği ve testte gereksiz asset yükleme olmaz.
    /// </summary>
    public class AssetDatabase
    {
        private static AssetDatabase _instance;
        public static AssetDatabase Instance => _instance ??= new AssetDatabase();

        private Pacman _pacmanPrefab;
        private InputManager _inputManagerPrefab;
        private GameObject _directionButtonPrefab;
        private GameObject _pelletPrefab;
        private GameObject _powerPelletPrefab;
        private Ghost _ghostPrefab;
        private GridData _gridData;
        private LevelSet _levelSet;
        private MapVisualSettings _mapVisualSettings;

        public Pacman PacmanPrefab => Load(ref _pacmanPrefab, "Pacman");
        public InputManager InputManagerPrefab => Load(ref _inputManagerPrefab, "InputManager");
        public GameObject DirectionButtonPrefab => Load(ref _directionButtonPrefab, "DirectionButton");
        public GameObject PelletPrefab => Load(ref _pelletPrefab, "Pellet");
        public GameObject PowerPelletPrefab => Load(ref _powerPelletPrefab, "PowerPellet");
        public Ghost GhostPrefab => Load(ref _ghostPrefab, "Ghost");
        public GridData GridData => Load(ref _gridData, "GridData");
        public LevelSet LevelSet => Load(ref _levelSet, "LevelSet");
        public MapVisualSettings MapVisualSettings => Load(ref _mapVisualSettings, "MapVisualSettings");

        private static T Load<T>(ref T cache, string resourcePath) where T : Object
        {
            if (cache == null)
            {
                cache = Resources.Load<T>(resourcePath);
                Debug.Assert(cache != null, $"Resources/{resourcePath} bulunamadı.");
            }

            return cache;
        }
    }
}
