using System.Collections.Generic;
using UnityEngine;

namespace CMP.Scripts.AiStates
{
    /// <summary>
    /// Oyuna katılmış hayaletlerin ortak dolaşım mantığı.
    ///
    /// Aday yön toplama, geri dönüş yasağı ve çıkmaz sokak kuralı burada tanımlı;
    /// alt state'ler yalnızca "kavşakta hangi yön" sorusunu cevaplar. Scatter, Chase ve
    /// Frightened arasındaki tek gerçek fark budur.
    ///
    /// Mevcut yön ayrı bir alanda tutulmaz, doğrudan GridMover'dan okunur. Aksi halde
    /// hayalet dışarıdan geri döndürüldüğünde (mod değişimi, korku) state'teki kopya
    /// bayatlar ve "geldiğin yöne dönme" kuralı yanlış yöne uygulanırdı.
    /// </summary>
    public abstract class RoamingState : GhostState
    {
        // Karar başına yeniden ayırmamak için tekrar kullanılan tampon.
        private readonly List<Direction> _candidates = new();

        protected RoamingState(GhostBlackboard blackboard) : base(blackboard)
        {
        }

        public override void OnEnter()
        {
            // Hareket halindeyken state değiştiyse mevcut adım bozulmaz: karar bir
            // sonraki hücre merkezinde verilir. Dokümandaki "bir sonraki köşeye kadar
            // yön değiştirmezler" kuralı bunu gerektiriyor.
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
            StartNextStep();
        }

        /// <summary>
        /// Birden fazla seçenek varsa, yani gerçek bir köşe/kavşak noktasındaysak çağrılır.
        /// </summary>
        protected abstract Direction ChooseAtJunction(List<Direction> candidates);

        private void StartNextStep()
        {
            var direction = ChooseDirection();

            if (direction != Direction.None)
            {
                GhostBlackboard.Mover.StartStep(direction);
            }
        }

        private Direction ChooseDirection()
        {
            var reverse = GhostBlackboard.Mover.CurrentDirection.Reverse();

            _candidates.Clear();
            foreach (var direction in GameSettings.DirectionsToCheck)
            {
                if (direction == reverse)
                {
                    continue;
                }

                if (GhostBlackboard.CanEnter(direction, GameSettings.InGameAiWalkableCells))
                {
                    _candidates.Add(direction);
                }
            }

            // Çıkmaz sokak: geri dönmekten başka seçenek yok.
            if (_candidates.Count == 0)
            {
                return GhostBlackboard.CanEnter(reverse, GameSettings.InGameAiWalkableCells)
                    ? reverse
                    : Direction.None;
            }

            // Koridor: yön değiştirme imkanı yok, "köşe" sayılmaz.
            if (_candidates.Count == 1)
            {
                return _candidates[0];
            }

            return ChooseAtJunction(_candidates);
        }
    }
}
