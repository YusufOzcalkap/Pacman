using UnityEngine;

namespace CMP.Scripts.AiStates.ChaseTargets
{
    /// <summary>
    /// Pinky: Pacman'in gittiği yönde birkaç hücre ilerisini hedefler.
    /// Peşinden gelmek yerine önünü kesmeye çalıştığı için Blinky ile birlikte
    /// oyuncuyu iki taraftan sıkıştırır.
    /// </summary>
    public class PinkyChaseTarget : IChaseTarget
    {
        public Vector2Int GetTargetCell(GhostBlackboard blackboard)
        {
            var pacman = blackboard.Pacman;
            return pacman.CurrentCell + pacman.CurrentDirection.ToVector2Int() * GameSettings.PinkyAmbushDistance;
        }
    }
}
