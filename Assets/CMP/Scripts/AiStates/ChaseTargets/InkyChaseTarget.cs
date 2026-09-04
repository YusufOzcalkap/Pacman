using UnityEngine;

namespace CMP.Scripts.AiStates.ChaseTargets
{
    /// <summary>
    /// Inky: Pacman'in birkaç hücre önündeki noktayı alır ve Blinky'den o noktaya giden
    /// vektörü iki katına çıkarır.
    ///
    /// Hedefi hem Pacman'e hem Blinky'ye bağlı olduğu için davranışı öngörülemez:
    /// Blinky uzaktayken geniş kavis çizer, Blinky yaklaştıkça oyuncunun üstüne kapanır.
    /// </summary>
    public class InkyChaseTarget : IChaseTarget
    {
        public Vector2Int GetTargetCell(GhostBlackboard blackboard)
        {
            var pacman = blackboard.Pacman;
            var pivotCell = pacman.CurrentCell +
                            pacman.CurrentDirection.ToVector2Int() * GameSettings.InkyPivotDistance;

            var blinky = blackboard.FindGhost(GhostPersonality.Blinky);
            if (blinky == null)
            {
                // Blinky oyunda değilse yansıtacak bir referans yok; dönüm noktası hedef olur.
                return pivotCell;
            }

            return pivotCell + (pivotCell - blinky.CurrentCell);
        }
    }
}
