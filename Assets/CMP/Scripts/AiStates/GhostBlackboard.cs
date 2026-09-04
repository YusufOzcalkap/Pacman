using System.Collections.Generic;
using CMP.Scripts.AiStates.ChaseTargets;
using CMP.Scripts.Helper;
using CMP.Scripts.Movement;
using UnityEngine;

namespace CMP.Scripts.AiStates
{
    /// <summary>
    /// State'lerin ortak bağlamı. State nesneleri geçişlerde yeniden yaratıldığı için
    /// kalıcı veriler burada tutulur; böylece her state aynı hayaleti, aynı haritayı ve
    /// aynı rastgelelik kaynağını görür.
    /// </summary>
    public class GhostBlackboard
    {
        public readonly Ghost Ghost;
        public readonly GridData GridData;
        public readonly GridMover Mover;
        public readonly GameRandom Random;
        public readonly Pacman Pacman;

        /// <summary>Inky'nin Blinky'ye bakabilmesi için oyundaki bütün hayaletler.</summary>
        public readonly IReadOnlyList<Ghost> Ghosts;

        /// <summary>Bu hayaletin kişiliğine düşen harita köşesi.</summary>
        public readonly Vector2Int ScatterCorner;

        /// <summary>Evdeki başlangıç hücresi; yenildiğinde buraya döner.</summary>
        public readonly Vector2Int SpawnCell;

        /// <summary>Aktif level'ın efektif hız ve süre değerleri.</summary>
        public readonly LevelRules Rules;

        private readonly IChaseTarget _chaseTarget;

        public GhostBlackboard(Ghost ghost, GridData gridData, GridMover mover, GameRandom random,
            Pacman pacman, IReadOnlyList<Ghost> ghosts, Vector2Int spawnCell, LevelRules rules)
        {
            Ghost = ghost;
            GridData = gridData;
            Mover = mover;
            Random = random;
            Pacman = pacman;
            Ghosts = ghosts;
            SpawnCell = spawnCell;
            Rules = rules;

            _chaseTarget = ChaseTargetFactory.Create(ghost.Personality);
            ScatterCorner = GetScatterCorner(ghost.Personality, gridData);
        }

        /// <summary>Hayaletin oyuna katılmak için bekleyeceği süre (saniye).</summary>
        public float JoinDelay => Ghost.JoinDelay;

        /// <summary>
        /// Kişiliğin belirlediği hedef hücre. İdeal hedef duvara ya da harita dışına
        /// düşebileceği için (örneğin Pacman'in dört hücre önü) en yakın yürünebilir
        /// hücreye çekilir; böylece yol bulma her zaman ulaşılabilir bir hedefe çalışır.
        /// </summary>
        public Vector2Int GetChaseTargetCell()
        {
            var idealCell = _chaseTarget.GetTargetCell(this);
            return Pathfinder.GetClosestWalkableCell(GridData, idealCell, GameSettings.InGameAiWalkableCells);
        }

        public Ghost FindGhost(GhostPersonality personality)
        {
            foreach (var ghost in Ghosts)
            {
                if (ghost.Personality == personality)
                {
                    return ghost;
                }
            }

            return null;
        }

        /// <summary>Verilen yöndeki komşu hücreye, o state'in kurallarına göre girilebilir mi?</summary>
        public bool CanEnter(Direction direction, List<CellType> walkableCells)
        {
            if (direction == Direction.None)
            {
                return false;
            }

            var targetCell = GridData.GetNeighbourCell(Mover.CurrentCell, direction);
            return GridData.IsCellMovable(targetCell, walkableCells);
        }

        /// <summary>
        /// Kişiliklere haritanın dört köşesi dağıtılır. Orijinal oyundaki yerleşimin aynısı:
        /// Blinky sağ üst, Pinky sol üst, Inky sağ alt, Clyde sol alt.
        /// </summary>
        private static Vector2Int GetScatterCorner(GhostPersonality personality, GridData gridData)
        {
            var right = gridData.Width - 1;
            var top = gridData.Height - 1;

            return personality switch
            {
                GhostPersonality.Blinky => new Vector2Int(right, top),
                GhostPersonality.Pinky => new Vector2Int(0, top),
                GhostPersonality.Inky => new Vector2Int(right, 0),
                _ => new Vector2Int(0, 0),
            };
        }
    }
}
