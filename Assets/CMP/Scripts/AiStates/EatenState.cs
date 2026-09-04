using CMP.Scripts.Helper;
using UnityEngine;

namespace CMP.Scripts.AiStates
{
    /// <summary>
    /// Korku modundayken yenen hayaletin state'i. Geriye yalnızca gözler kalır ve
    /// bunlar en kısa yoldan eve döner.
    ///
    /// Bu, "oyuna katıldıktan sonra eve dönemezler" kuralının bilinçli tek istisnasıdır:
    /// kural serbest dolaşım ve takip için geçerlidir, yenilen hayaletin geri dönmesi
    /// güçlendirme yeminin karşılığıdır. Kapıdan geçebilmesi için katılma kümesini
    /// yeniden kullanıyoruz.
    /// </summary>
    public class EatenState : GhostState
    {
        public override GhostStateType StateType => GhostStateType.Eaten;

        public EatenState(GhostBlackboard blackboard) : base(blackboard)
        {
        }

        public override void OnEnter()
        {
            GhostBlackboard.Mover.StepDuration = GhostBlackboard.Rules.EatenStepDuration;
            GhostBlackboard.Ghost.SetEatenLook();

            // Hareket halindeyse mevcut adım bozulmaz; gözler bir an aynı yönde
            // süzülüp ilk hücre merkezinde eve yönelir.
            if (!GhostBlackboard.Mover.IsMoving)
            {
                StartNextStep();
            }
        }

        public override void Update()
        {
        }

        public override void OnArrivedAtCell(Vector2Int cell)
        {
            if (cell == GhostBlackboard.SpawnCell)
            {
                GhostBlackboard.Mover.StepDuration = GhostBlackboard.Rules.GhostStepDuration;
                GhostBlackboard.Ghost.RestoreNormalLook();
                GhostBlackboard.Ghost.ChangeState(
                    new InHouseState(GhostBlackboard, GameSettings.RespawnJoinDelay));
                return;
            }

            StartNextStep();
        }

        private void StartNextStep()
        {
            var found = Pathfinder.TryGetNextStep(
                GhostBlackboard.GridData,
                GhostBlackboard.Mover.CurrentCell,
                GhostBlackboard.SpawnCell,
                GameSettings.JoiningGameWalkableCells,
                out var nextStep);

            if (found)
            {
                GhostBlackboard.Mover.StartStep(nextStep);
            }
            else
            {
                Debug.LogError($"{GhostBlackboard.Ghost.name}: eve dönüş yolu bulunamadı.");
            }
        }
    }
}
