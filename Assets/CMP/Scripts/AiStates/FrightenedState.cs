using System.Collections.Generic;
using UnityEngine;

namespace CMP.Scripts.AiStates
{
    /// <summary>
    /// Güçlendirme yemi yendiğinde girilen state.
    /// Hayalet yavaşlar, mavileşir ve yön seçimini tamamen rastgele yapar.
    ///
    /// State'e girerken anında geri döner: oyuncuya "artık kovalayan değil kaçan benim"
    /// mesajını veren en net işaret budur, orijinal oyunda da böyledir.
    ///
    /// Süre dolduğunda Scatter'a geçilir; oyun modu Chase ise GameManager bir sonraki
    /// karede hayaleti kendiliğinden takibe geri alır.
    /// </summary>
    public class FrightenedState : RoamingState
    {
        public override GhostStateType StateType => GhostStateType.Frightened;

        private float _elapsedTime;
        private bool _isBlinking;

        public FrightenedState(GhostBlackboard blackboard) : base(blackboard)
        {
        }

        public override void OnEnter()
        {
            _elapsedTime = 0f;
            _isBlinking = false;

            GhostBlackboard.Mover.StepDuration = GhostBlackboard.Rules.FrightenedStepDuration;
            GhostBlackboard.Ghost.SetFrightenedLook();
            GhostBlackboard.Mover.ReverseStep();

            base.OnEnter();
        }

        public override void Update()
        {
            _elapsedTime += Time.deltaTime;

            var totalDuration = GhostBlackboard.Rules.FrightenedDuration;

            if (!_isBlinking && _elapsedTime >= totalDuration - GameSettings.FrightenedBlinkDuration)
            {
                _isBlinking = true;
                GhostBlackboard.Ghost.StartFrightenedBlink();
            }

            if (_elapsedTime < totalDuration)
            {
                return;
            }

            GhostBlackboard.Mover.StepDuration = GhostBlackboard.Rules.GhostStepDuration;
            GhostBlackboard.Ghost.RestoreNormalLook();
            GhostBlackboard.Ghost.ChangeState(new ScatterState(GhostBlackboard));
        }

        protected override Direction ChooseAtJunction(List<Direction> candidates)
        {
            return GhostBlackboard.Random.Pick(candidates);
        }
    }
}
