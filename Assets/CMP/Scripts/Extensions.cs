using System.Collections.Generic;
using UnityEngine;

namespace CMP.Scripts
{
    public static class Extensions
    {
        public static bool GetIsMovable(this CellType cellType)
        {
            return cellType is CellType.Pacman or CellType.Empty or CellType.JoinGameCell or CellType.PowerPellet;
        }

        public static Vector2Int ToVector2Int(this Direction cellType)
        {
            switch (cellType)
            {
                case Direction.None:
                    return Vector2Int.zero;
                case Direction.Left:
                    return Vector2Int.left;
                case Direction.Right:
                    return Vector2Int.right;
                case Direction.Up:
                    return Vector2Int.up;
                case Direction.Down:
                    return Vector2Int.down;
            }

            Debug.Assert(false);
            return Vector2Int.zero;
        }

        public static Direction Reverse(this Direction direction)
        {
            return direction switch
            {
                Direction.Left => Direction.Right,
                Direction.Right => Direction.Left,
                Direction.Up => Direction.Down,
                Direction.Down => Direction.Up,
                _ => Direction.None
            };
        }
        
        public static Direction ToDirection(this Vector2Int vector)
        {
            if (vector == Vector2Int.left)
            {
                return Direction.Left;
            }
            if (vector == Vector2Int.right)
            {
                return Direction.Right;
            }
            if (vector == Vector2Int.up)
            {
                return Direction.Up;
            }
            if (vector == Vector2Int.down)
            {
                return Direction.Down;
            }

            return Direction.None;
        }

        /// <summary>
        /// Hücre koordinatını dünya konumuna çevirir.
        /// Harita texture'ı (-0.5, -0.5) noktasından başladığı için hücre merkezleri
        /// tam sayı koordinatlara denk gelir.
        /// </summary>
        public static Vector3 ToWorldPosition(this Vector2Int cellCoords)
        {
            return new Vector3(cellCoords.x, cellCoords.y, 0f);
        }

        /// <summary>
        /// Varsayılan olarak sağa bakan sprite'lar için yön → dönüş.
        /// Pacman'in ağzı sprite'ta sağa açıldığı için <see cref="ToQuaternion"/> değil
        /// (o yukarı bakan yön butonu sprite'ına göre yazılmış) bu kullanılır.
        /// Pacman yatay eksende simetrik olduğundan sola bakarken 180° dönmesi
        /// yatay aynalama ile aynı sonucu verir.
        /// </summary>
        public static Quaternion ToRightFacingRotation(this Direction dir)
        {
            switch (dir)
            {
                case Direction.Right:
                    return Quaternion.Euler(0, 0, 0);
                case Direction.Up:
                    return Quaternion.Euler(0, 0, 90);
                case Direction.Left:
                    return Quaternion.Euler(0, 0, 180);
                case Direction.Down:
                    return Quaternion.Euler(0, 0, -90);
            }

            return Quaternion.identity;
        }

        public static Quaternion ToQuaternion(this Direction dir)
        {
            switch (dir)
            {
                case Direction.Left:
                    return Quaternion.Euler(0, 0, 90);
                case Direction.Right:
                    return Quaternion.Euler(0, 0, -90);
                case Direction.Up:
                    return Quaternion.Euler(0, 0, 0);
                case Direction.Down:
                    return Quaternion.Euler(0, 0, 180);
            }
            
            return Quaternion.identity;
        }
    }
}