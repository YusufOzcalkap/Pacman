using UnityEngine;

namespace CMP.Scripts.AiStates
{
    /// <summary>
    /// Yapay zekanın başlangıç state'i. Hayalet, kendi katılma süresi dolana kadar
    /// evin içinde yukarı-aşağı gider gelir.
    ///
    /// Ev duvarlarla çevrili ve tek çıkışı AiGate olduğu için, kapıyı dışarıda bırakan
    /// yürünebilirlik kümesi hayaleti ayrıca kontrol etmeye gerek kalmadan içeride tutar.
    /// </summary>
    public class InHouseState : GhostState
    {
        public override GhostStateType StateType => GhostStateType.InHouse;

        private readonly float _joinDelay;

        private Direction _direction;
        private float _elapsedTime;

        public InHouseState(GhostBlackboard blackboard) : this(blackboard, blackboard.JoinDelay)
        {
        }

        /// <param name="joinDelay">
        /// Oyuna katılmadan önce beklenecek süre. Yenilip eve dönen hayalet çok daha
        /// kısa beklediği için ayrı verilebiliyor.
        /// </param>
        public InHouseState(GhostBlackboard blackboard, float joinDelay) : base(blackboard)
        {
            _joinDelay = joinDelay;
        }

        public override void OnEnter()
        {
            _elapsedTime = 0f;
            _direction = Direction.Up;
            StartNextStep();
        }

        public override void Update()
        {
            _elapsedTime += Time.deltaTime;
        }

        public override void OnArrivedAtCell(Vector2Int cell)
        {
            // Geçişi hücre merkezinde yapıyoruz ki hayalet iki hücrenin arasındayken
            // yol bulmaya başlamasın.
            if (_elapsedTime >= _joinDelay)
            {
                GhostBlackboard.Ghost.ChangeState(new JoiningGameState(GhostBlackboard));
                return;
            }

            StartNextStep();
        }

        /// <summary>Önü kapandığında yönü tersine çevirerek dikey salınımı sürdürür.</summary>
        private void StartNextStep()
        {
            if (!GhostBlackboard.CanEnter(_direction, GameSettings.InHouseWalkableCells))
            {
                _direction = _direction.Reverse();
            }

            if (GhostBlackboard.CanEnter(_direction, GameSettings.InHouseWalkableCells))
            {
                GhostBlackboard.Mover.StartStep(_direction);
            }
        }
    }
}
