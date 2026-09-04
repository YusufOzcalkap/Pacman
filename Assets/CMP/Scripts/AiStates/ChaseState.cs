using System.Collections.Generic;
using CMP.Scripts.Helper;

namespace CMP.Scripts.AiStates
{
    /// <summary>
    /// Takip state'i. Hayalet her kavşakta, hedefine giden en kısa yolu veren yönü seçer.
    ///
    /// Hedef hücreyi kişilik belirler (bkz. ChaseTargets); davranışın geri kalanı
    /// bütün hayaletlerde aynıdır.
    ///
    /// Kavşakta doğrudan BFS'in ilk adımını almak yerine her aday yönün hedefe olan
    /// uzaklığını ölçüyoruz; böylece "geldiği yöne dönmeme" kuralı en kısa yol
    /// hesabının içinde kalıyor, sonradan bozulmuyor.
    /// </summary>
    public class ChaseState : RoamingState
    {
        public override GhostStateType StateType => GhostStateType.Chase;

        public ChaseState(GhostBlackboard blackboard) : base(blackboard)
        {
        }

        protected override Direction ChooseAtJunction(List<Direction> candidates)
        {
            var currentCell = GhostBlackboard.Mover.CurrentCell;
            var targetCell = GhostBlackboard.GetChaseTargetCell();

            var bestDirection = candidates[0];
            var bestLength = int.MaxValue;

            foreach (var direction in candidates)
            {
                var neighbourCell = GhostBlackboard.GridData.GetNeighbourCell(currentCell, direction);

                var reachable = Pathfinder.TryGetPathLength(
                    GhostBlackboard.GridData,
                    neighbourCell,
                    targetCell,
                    GameSettings.InGameAiWalkableCells,
                    out var length);

                if (!reachable || length >= bestLength)
                {
                    continue;
                }

                bestLength = length;
                bestDirection = direction;
            }

            return bestDirection;
        }
    }
}
