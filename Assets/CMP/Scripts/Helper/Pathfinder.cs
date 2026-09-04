using System.Collections.Generic;
using UnityEngine;

namespace CMP.Scripts.Helper
{
    /// <summary>
    /// Grid üzerinde en kısa yolu bulan BFS.
    /// Hücreler arası geçişlerin maliyeti eşit olduğu için BFS zaten en kısa yolu
    /// garanti eder; A*'ın sezgisel maliyetine gerek yoktur.
    ///
    /// Hem "eve katılma" (JoiningGame) hem "takip" (Chase) bunu kullanır;
    /// aralarındaki tek fark hedef hücre ve yürünebilir hücre listesidir.
    /// </summary>
    public static class Pathfinder
    {
        /// <summary>
        /// <paramref name="from"/> hücresinden <paramref name="to"/> hücresine giden en kısa yolu döner.
        /// Dönen liste başlangıç hücresini içermez, hedef hücresini içerir.
        /// </summary>
        public static bool TryGetPath(GridData gridData, Vector2Int from, Vector2Int to,
            List<CellType> walkableCells, out List<Vector2Int> path)
        {
            path = null;

            if (from == to)
            {
                path = new List<Vector2Int>();
                return true;
            }

            if (!gridData.IsCellMovable(to, walkableCells))
            {
                return false;
            }

            // Kendini işaret eden başlangıç kaydı, hem "ziyaret edildi" işareti
            // hem de yol geri sarılırken durma noktası olarak görev yapıyor.
            var cameFrom = new Dictionary<Vector2Int, Vector2Int> { { from, from } };
            var frontier = new Queue<Vector2Int>();
            frontier.Enqueue(from);

            while (frontier.Count > 0)
            {
                var current = frontier.Dequeue();
                if (current == to)
                {
                    path = BuildPath(cameFrom, from, to);
                    return true;
                }

                foreach (var direction in GameSettings.DirectionsToCheck)
                {
                    var neighbour = gridData.GetNeighbourCell(current, direction);
                    if (cameFrom.ContainsKey(neighbour) || !gridData.IsCellMovable(neighbour, walkableCells))
                    {
                        continue;
                    }

                    cameFrom.Add(neighbour, current);
                    frontier.Enqueue(neighbour);
                }
            }

            return false;
        }

        /// <summary>Hedefe giden yolun ilk adımının yönünü döner.</summary>
        public static bool TryGetNextStep(GridData gridData, Vector2Int from, Vector2Int to,
            List<CellType> walkableCells, out Direction nextStep)
        {
            nextStep = Direction.None;

            if (!TryGetPath(gridData, from, to, walkableCells, out var path) || path.Count == 0)
            {
                return false;
            }

            nextStep = (path[0] - from).ToDirection();
            return nextStep != Direction.None;
        }

        /// <summary>
        /// Hedefe olan uzaklığı hücre cinsinden döner.
        /// Chase state'i kavşakta aday yönleri bununla karşılaştırır.
        /// </summary>
        public static bool TryGetPathLength(GridData gridData, Vector2Int from, Vector2Int to,
            List<CellType> walkableCells, out int length)
        {
            length = int.MaxValue;

            if (!TryGetPath(gridData, from, to, walkableCells, out var path))
            {
                return false;
            }

            length = path.Count;
            return true;
        }

        /// <summary>
        /// Başlangıç hücresinden yürüyerek ulaşılabilen bütün hücreler.
        /// Yem dağıtımı bunu kullanır: haritada kapalı kalmış bir cebe yem konursa
        /// level asla tamamlanamaz. Aynı tarama level doğrulamasında da kullanılıyor.
        /// </summary>
        public static HashSet<Vector2Int> GetReachableCells(GridData gridData, Vector2Int startCell,
            List<CellType> walkableCells)
        {
            var reachable = new HashSet<Vector2Int>();
            if (!gridData.IsCellMovable(startCell, walkableCells))
            {
                return reachable;
            }

            var frontier = new Queue<Vector2Int>();
            frontier.Enqueue(startCell);
            reachable.Add(startCell);

            while (frontier.Count > 0)
            {
                var current = frontier.Dequeue();
                foreach (var direction in GameSettings.DirectionsToCheck)
                {
                    var neighbour = gridData.GetNeighbourCell(current, direction);
                    if (reachable.Contains(neighbour) || !gridData.IsCellMovable(neighbour, walkableCells))
                    {
                        continue;
                    }

                    reachable.Add(neighbour);
                    frontier.Enqueue(neighbour);
                }
            }

            return reachable;
        }

        /// <summary>
        /// Verilen hücre yürünebilir değilse ona en yakın yürünebilir hücreyi döner.
        /// Kişilik hedefleri (Pacman'in dört hücre önü gibi) duvara ya da harita dışına
        /// düşebildiği için takip hesabı öncesinde hedefi buraya geçiriyoruz.
        /// </summary>
        public static Vector2Int GetClosestWalkableCell(GridData gridData, Vector2Int idealCell,
            List<CellType> walkableCells)
        {
            // Hedef harita dışına taşmış olabilir (Pacman'in dört hücre önü gibi);
            // tünelli haritada bu koordinatın gerçek karşılığı karşı kenardadır.
            idealCell = gridData.WrapCoords(idealCell);

            if (gridData.IsCellMovable(idealCell, walkableCells))
            {
                return idealCell;
            }

            var closestCell = idealCell;
            var closestDistance = int.MaxValue;

            for (var y = 0; y < gridData.Height; y++)
            {
                for (var x = 0; x < gridData.Width; x++)
                {
                    var cell = new Vector2Int(x, y);
                    if (!gridData.IsCellMovable(cell, walkableCells))
                    {
                        continue;
                    }

                    var distance = (cell - idealCell).sqrMagnitude;
                    if (distance >= closestDistance)
                    {
                        continue;
                    }

                    closestDistance = distance;
                    closestCell = cell;
                }
            }

            return closestCell;
        }

        private static List<Vector2Int> BuildPath(IReadOnlyDictionary<Vector2Int, Vector2Int> cameFrom,
            Vector2Int from, Vector2Int to)
        {
            var path = new List<Vector2Int>();
            var current = to;

            while (current != from)
            {
                path.Add(current);
                current = cameFrom[current];
            }

            path.Reverse();
            return path;
        }
    }
}
