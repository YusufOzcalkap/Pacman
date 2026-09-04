using UnityEngine;

namespace CMP.Scripts.AiStates.ChaseTargets
{
    /// <summary>
    /// Clyde: Pacman'den uzaktayken Blinky gibi doğrudan takip eder, belirli bir
    /// mesafeden yakınlaşınca ürküp kendi köşesine kaçar.
    ///
    /// Bu yüzden köşesine yakın bölgeler oyuncu için görece güvenlidir; ürkekliği
    /// haritaya bir "nefes alma alanı" kazandırır.
    /// </summary>
    public class ClydeChaseTarget : IChaseTarget
    {
        public Vector2Int GetTargetCell(GhostBlackboard blackboard)
        {
            var pacmanCell = blackboard.Pacman.CurrentCell;
            var distanceSquared = (blackboard.Mover.CurrentCell - pacmanCell).sqrMagnitude;
            var leashSquared = GameSettings.ClydeLeashDistance * GameSettings.ClydeLeashDistance;

            return distanceSquared > leashSquared ? pacmanCell : blackboard.ScatterCorner;
        }
    }
}
