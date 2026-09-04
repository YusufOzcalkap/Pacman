using System.Collections.Generic;

namespace CMP.Scripts.AiStates
{
    /// <summary>
    /// Oyuna katılmış hayaletin serbest dolaşım state'i.
    /// Her köşede rastgele bir yön seçer; geldiği yöne dönmesi <see cref="RoamingState"/>
    /// tarafından zaten engellenmiştir.
    /// </summary>
    public class ScatterState : RoamingState
    {
        public override GhostStateType StateType => GhostStateType.Scatter;

        public ScatterState(GhostBlackboard blackboard) : base(blackboard)
        {
        }

        protected override Direction ChooseAtJunction(List<Direction> candidates)
        {
            return GhostBlackboard.Random.Pick(candidates);
        }
    }
}
