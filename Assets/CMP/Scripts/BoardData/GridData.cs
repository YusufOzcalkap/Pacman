using System.Collections.Generic;
using UnityEngine;

namespace CMP.Scripts
{
    public enum CellType
    {
        Empty,
        Wall,
        AiSpawnZone,
        AiGate,
        Pacman,
        JoinGameCell,

        /// <summary>
        /// Güçlendirme yemi. Hareket açısından Empty gibi davranır.
        /// Normal yemler bütün Empty hücrelere otomatik dağıtıldığı için editörde
        /// yalnızca bunların yeri işaretlenir.
        /// </summary>
        PowerPellet,
        Invalid,
    }

    [CreateAssetMenu(fileName = "NewGridData", menuName = "PacMan/Grid Data")]
    public class GridData : ScriptableObject
    {
        public int Width;
        public int Height;
        public CellType[] Grid;

        /// <summary>
        /// Tünel desteği: açıksa, açık bırakılan kenarlar birbirine bağlanır
        /// (soldan çıkan sağdan girer). Level başına bilinçli bir tasarım tercihi
        /// olduğu için varsayılan kapalıdır; tünelli bir haritada editörden açılır.
        /// </summary>
        public bool WrapEdges;

        /// <summary>Harita dışına taşan koordinatı karşı kenara taşır.</summary>
        public Vector2Int WrapCoords(Vector2Int coords)
        {
            if (!WrapEdges)
            {
                return coords;
            }

            var x = ((coords.x % Width) + Width) % Width;
            var y = ((coords.y % Height) + Height) % Height;
            return new Vector2Int(x, y);
        }

        /// <summary>
        /// Verilen yöndeki komşu hücre. Hareket, yol bulma ve görüş hesaplarının
        /// tamamı bunu kullanır; tünel kuralı böylece tek yerde tanımlı kalır.
        /// </summary>
        public Vector2Int GetNeighbourCell(Vector2Int coords, Direction direction)
        {
            return WrapCoords(coords + direction.ToVector2Int());
        }

        public List<Vector2Int> GetCoordsOfCellType(CellType targetType)
        {
            var coords = new List<Vector2Int>();
            for (var i = 0; i < Grid.Length; i++)
            {
                if (Grid[i] == targetType)
                {
                    var y = i / Width;
                    var x = i - y * Width;
                    coords.Add(new Vector2Int(x,y));
                }
            }

            Debug.Assert(coords.Count != 0);
            return coords;
        }
        
        public bool GetInBounds(Vector2Int coords)
        {
            return coords.x >= 0 && coords.x < Width && coords.y >= 0 && coords.y < Height;
        }
        
        public CellType GetCellAtOrDefault(Vector2Int coords, CellType cellType)
        {
            return !GetInBounds(coords) ? cellType : GetCellAt(coords.x, coords.y);
        }
        
        public CellType GetCellAt(Vector2Int coords)
        {
            return GetCellAt(coords.x, coords.y);
        }
        
        public CellType GetCellAt(int x, int y)
        {
            return Grid[y * Width + x];
        }
        
        public bool IsCellMovable(Vector2Int cellCoords, List<CellType> availableCells)
        {
            return GetInBounds(cellCoords) && availableCells.Contains(GetCellAt(cellCoords));
        }
    }
}