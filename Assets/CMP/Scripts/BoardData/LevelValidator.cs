using System.Collections.Generic;
using CMP.Scripts.Helper;
using UnityEngine;

namespace CMP.Scripts
{
    public enum ValidationSeverity
    {
        /// <summary>Level bu haliyle düzgün oynanamaz.</summary>
        Error,

        /// <summary>Büyük ihtimalle tasarım hatası ama oyun çalışır.</summary>
        Warning,
    }

    public class ValidationIssue
    {
        public ValidationSeverity Severity;
        public string Message;

        /// <summary>Sorunun geçtiği hücreler; editör bunları harita üzerinde işaretler.</summary>
        public readonly List<Vector2Int> Cells = new();
    }

    /// <summary>
    /// Level doğrulama kuralları. Editörde her değişiklikte, oyunda ise açılışta çalışır;
    /// böylece bozuk bir harita daha Play'e basmadan yakalanır, gözden kaçarsa da
    /// konsola nedeniyle birlikte yazılır.
    ///
    /// Kurallar oyunun gerçek yürünebilirlik kümelerini ve yol bulmasını kullanır:
    /// "ulaşılabilir" burada ne diyorsa oyunda da onu der.
    /// </summary>
    public static class LevelValidator
    {
        public static List<ValidationIssue> Validate(GridData gridData)
        {
            var issues = new List<ValidationIssue>();

            if (gridData.Grid == null || gridData.Grid.Length != gridData.Width * gridData.Height)
            {
                Add(issues, ValidationSeverity.Error,
                    "Grid verisi boyutlarla uyuşmuyor; haritayı yeniden boyutlandır.");
                return issues;
            }

            var pacmanCells = Collect(gridData, CellType.Pacman);
            var spawnCells = Collect(gridData, CellType.AiSpawnZone);
            var joinCells = Collect(gridData, CellType.JoinGameCell);
            var gateCells = Collect(gridData, CellType.AiGate);
            var powerPelletCells = Collect(gridData, CellType.PowerPellet);

            CheckSingle(issues, pacmanCells, "Pacman başlangıç hücresi");
            CheckSingle(issues, joinCells, "JoinGameCell");

            if (spawnCells.Count == 0)
            {
                Add(issues, ValidationSeverity.Error, "Hiç AiSpawnZone yok; hayaletler doğamaz.");
            }
            else if (spawnCells.Count < GameSettings.AiCharacterCount)
            {
                Add(issues, ValidationSeverity.Warning,
                    $"Spawn hücresi ({spawnCells.Count}) karakter sayısından ({GameSettings.AiCharacterCount}) az; " +
                    "hayaletler üst üste doğacak.", spawnCells);
            }

            if (gateCells.Count == 0)
            {
                Add(issues, ValidationSeverity.Error, "Hiç AiGate yok; hayaletler evden çıkamaz.");
            }

            if (powerPelletCells.Count == 0)
            {
                Add(issues, ValidationSeverity.Warning,
                    "Hiç PowerPellet yok; korku modu bu levelde hiç tetiklenmez.");
            }

            CheckJoinPath(issues, gridData, spawnCells, joinCells);

            var pacmanReach = pacmanCells.Count == 1
                ? Pathfinder.GetReachableCells(gridData, pacmanCells[0], GameSettings.PacmanWalkableCells)
                : new HashSet<Vector2Int>();

            var houseRegion = CollectHouseRegion(gridData, spawnCells);

            CheckHouseSealed(issues, pacmanReach, houseRegion);
            CheckUnreachableCells(issues, gridData, pacmanCells, pacmanReach, houseRegion);
            CheckTunnelSymmetry(issues, gridData);

            return issues;
        }

        /// <summary>Sonuçları konsola yazar; oyun açılışında çağrılır.</summary>
        public static void LogIssues(GridData gridData)
        {
            foreach (var issue in Validate(gridData))
            {
                var message = $"Level '{gridData.name}': {issue.Message}";

                if (issue.Severity == ValidationSeverity.Error)
                {
                    Debug.LogError(message, gridData);
                }
                else
                {
                    Debug.LogWarning(message, gridData);
                }
            }
        }

        private static void CheckSingle(List<ValidationIssue> issues, List<Vector2Int> cells, string label)
        {
            if (cells.Count != 1)
            {
                Add(issues, ValidationSeverity.Error,
                    $"Tam olarak 1 {label} olmalı (şu an {cells.Count}).", cells);
            }
        }

        /// <summary>Her spawn hücresinden JoinGameCell'e kapıdan geçen bir yol olmalı.</summary>
        private static void CheckJoinPath(List<ValidationIssue> issues, GridData gridData,
            List<Vector2Int> spawnCells, List<Vector2Int> joinCells)
        {
            if (joinCells.Count != 1)
            {
                return;
            }

            foreach (var spawnCell in spawnCells)
            {
                if (!Pathfinder.TryGetPath(gridData, spawnCell, joinCells[0],
                        GameSettings.JoiningGameWalkableCells, out _))
                {
                    Add(issues, ValidationSeverity.Error,
                        $"{spawnCell} spawn hücresinden JoinGameCell'e yol yok; hayalet evde kilitli kalır.",
                        new List<Vector2Int> { spawnCell });
                }
            }
        }

        /// <summary>
        /// Evin içi: spawn hücrelerinden, kapıdan geçmeden ulaşılabilen bölge.
        /// Ev düzgün kapatılmışsa bu bölge küçük kalır ve Pacman'in alanına değmez.
        /// </summary>
        private static HashSet<Vector2Int> CollectHouseRegion(GridData gridData, List<Vector2Int> spawnCells)
        {
            var region = new HashSet<Vector2Int>();

            foreach (var spawnCell in spawnCells)
            {
                region.UnionWith(Pathfinder.GetReachableCells(gridData, spawnCell,
                    GameSettings.InHouseWalkableCells));
            }

            return region;
        }

        private static void CheckHouseSealed(List<ValidationIssue> issues,
            HashSet<Vector2Int> pacmanReach, HashSet<Vector2Int> houseRegion)
        {
            var leakedCells = new List<Vector2Int>();

            foreach (var cell in houseRegion)
            {
                if (pacmanReach.Contains(cell))
                {
                    leakedCells.Add(cell);
                }
            }

            if (leakedCells.Count > 0)
            {
                Add(issues, ValidationSeverity.Error,
                    "Ev sızdırıyor: evdeki hayalet kapıyı kullanmadan oyun alanına çıkabilir " +
                    "(işaretli hücreler iki bölgenin kesişimi).", leakedCells);
            }
        }

        /// <summary>
        /// Pacman'in ulaşamadığı açık hücreler. Yem yöneticisi bu hücrelere yem koymaz,
        /// yani oyun kilitlenmez; ama ev dışında böyle bir cep neredeyse her zaman
        /// tasarım hatasıdır.
        /// </summary>
        private static void CheckUnreachableCells(List<ValidationIssue> issues, GridData gridData,
            List<Vector2Int> pacmanCells, HashSet<Vector2Int> pacmanReach, HashSet<Vector2Int> houseRegion)
        {
            if (pacmanCells.Count != 1)
            {
                return;
            }

            var unreachableCells = new List<Vector2Int>();

            for (var y = 0; y < gridData.Height; y++)
            {
                for (var x = 0; x < gridData.Width; x++)
                {
                    var cell = new Vector2Int(x, y);
                    var type = gridData.GetCellAt(cell);

                    if (type is not (CellType.Empty or CellType.PowerPellet))
                    {
                        continue;
                    }

                    if (!pacmanReach.Contains(cell) && !houseRegion.Contains(cell))
                    {
                        unreachableCells.Add(cell);
                    }
                }
            }

            if (unreachableCells.Count > 0)
            {
                Add(issues, ValidationSeverity.Warning,
                    $"Pacman'in ulaşamadığı {unreachableCells.Count} açık hücre var; buralara yem konmayacak.",
                    unreachableCells);
            }
        }

        /// <summary>
        /// Tünel açıkken tek taraflı kalan kenar hücreleri: karşısı duvar olduğu için
        /// oradan geçiş olmaz. Bilinçli olabilir ama çoğunlukla yarım kalmış tüneldir.
        /// </summary>
        private static void CheckTunnelSymmetry(List<ValidationIssue> issues, GridData gridData)
        {
            if (!gridData.WrapEdges)
            {
                return;
            }

            var asymmetricCells = new List<Vector2Int>();

            for (var y = 0; y < gridData.Height; y++)
            {
                CheckEdgePair(gridData, new Vector2Int(0, y), new Vector2Int(gridData.Width - 1, y),
                    asymmetricCells);
            }

            for (var x = 0; x < gridData.Width; x++)
            {
                CheckEdgePair(gridData, new Vector2Int(x, 0), new Vector2Int(x, gridData.Height - 1),
                    asymmetricCells);
            }

            if (asymmetricCells.Count > 0)
            {
                Add(issues, ValidationSeverity.Warning,
                    "Tünel (WrapEdges) açık ama işaretli kenar hücrelerinin karşılığı kapalı; " +
                    "bu kenarlardan geçiş olmayacak.", asymmetricCells);
            }
        }

        private static void CheckEdgePair(GridData gridData, Vector2Int first, Vector2Int second,
            List<Vector2Int> asymmetricCells)
        {
            var firstOpen = gridData.IsCellMovable(first, GameSettings.PacmanWalkableCells);
            var secondOpen = gridData.IsCellMovable(second, GameSettings.PacmanWalkableCells);

            if (firstOpen && !secondOpen)
            {
                asymmetricCells.Add(first);
            }
            else if (secondOpen && !firstOpen)
            {
                asymmetricCells.Add(second);
            }
        }

        private static List<Vector2Int> Collect(GridData gridData, CellType type)
        {
            // GridData.GetCoordsOfCellType boş sonuçta assert attığı için burada
            // kullanılmıyor; doğrulayıcının işi tam da o boş durumları raporlamak.
            var cells = new List<Vector2Int>();

            for (var i = 0; i < gridData.Grid.Length; i++)
            {
                if (gridData.Grid[i] != type)
                {
                    continue;
                }

                var y = i / gridData.Width;
                cells.Add(new Vector2Int(i - y * gridData.Width, y));
            }

            return cells;
        }

        private static void Add(List<ValidationIssue> issues, ValidationSeverity severity, string message,
            List<Vector2Int> cells = null)
        {
            var issue = new ValidationIssue { Severity = severity, Message = message };

            if (cells != null)
            {
                issue.Cells.AddRange(cells);
            }

            issues.Add(issue);
        }
    }
}
