using UnityEngine;

namespace CMP.Scripts.AiStates.ChaseTargets
{
    /// <summary>
    /// Blinky: doğrudan Pacman'in bulunduğu hücreyi hedefler.
    /// En basit ve en ısrarcı takipçi; oyuncunun arkasından hiç kopmaz.
    /// </summary>
    public class BlinkyChaseTarget : IChaseTarget
    {
        public Vector2Int GetTargetCell(GhostBlackboard blackboard)
        {
            return blackboard.Pacman.CurrentCell;
        }
    }
}
