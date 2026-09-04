using CMP.Scripts.Helper;
using UnityEngine;

namespace CMP.Scripts.AiStates
{
    /// <summary>
    /// Hayaletin evden çıkıp oyuna dahil olduğu state.
    /// GridData'da işaretli JoinGameCell'e en kısa yoldan gider; bu, kapıdan (AiGate)
    /// geçmesine izin verilen tek state'tir. Hedefe varınca oyuna katılmış sayılır ve
    /// bir daha kapıyı kullanamaz.
    /// </summary>
    public class JoiningGameState : GhostState
    {
        public override GhostStateType StateType => GhostStateType.JoiningGame;

        private Vector2Int _joinCell;

        public JoiningGameState(GhostBlackboard blackboard) : base(blackboard)
        {
        }

        public override void OnEnter()
        {
            _joinCell = GhostBlackboard.GridData.GetCoordsOfCellType(CellType.JoinGameCell)[0];
            StartNextStep();
        }

        public override void Update()
        {
        }

        public override void OnArrivedAtCell(Vector2Int cell)
        {
            if (cell == _joinCell)
            {
                // Oyuna katılma tamamlandı; buradan sonrası serbest dolaşımın işi.
                GhostBlackboard.Ghost.ChangeState(new ScatterState(GhostBlackboard));
                return;
            }

            StartNextStep();
        }

        private void StartNextStep()
        {
            var reached = Pathfinder.TryGetNextStep(
                GhostBlackboard.GridData,
                GhostBlackboard.Mover.CurrentCell,
                _joinCell,
                GameSettings.JoiningGameWalkableCells,
                out var nextStep);

            if (reached)
            {
                GhostBlackboard.Mover.StartStep(nextStep);
            }
            else
            {
                // Buraya düşmek harita hatasıdır: spawn alanı ile JoinGameCell arasında
                // kapıdan geçen bir yol yok demektir.
                Debug.LogError($"{GhostBlackboard.Ghost.name}: {_joinCell} hücresine giden yol bulunamadı.");
            }
        }
    }
}
