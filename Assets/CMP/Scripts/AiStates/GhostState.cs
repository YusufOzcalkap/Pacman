using UnityEngine;

namespace CMP.Scripts.AiStates
{
    /// <summary>
    /// Yapay zeka davranışlarının ortak arayüzü.
    /// Hareket olay güdümlü olduğu için iki ayrı giriş noktası var:
    /// <see cref="Update"/> her karede (zamanlayıcılar için),
    /// <see cref="OnArrivedAtCell"/> ise yalnızca hücre merkezine varıldığında
    /// (yön kararları için) çağrılır.
    /// </summary>
    public abstract class GhostState
    {
        protected GhostState(GhostBlackboard blackboard)
        {
            GhostBlackboard = blackboard;
        }

        protected GhostBlackboard GhostBlackboard;

        /// <summary>Inspector'da ve hata ayıklamada gösterilen state kimliği.</summary>
        public abstract GhostStateType StateType { get; }

        public abstract void OnEnter();
        public abstract void Update();

        /// <summary>
        /// Hayalet bir hücrenin merkezine vardığında çağrılır.
        /// Yön değişiklikleri ve state geçişleri burada yapılır ki karakter
        /// hiçbir zaman iki hücrenin arasında yön değiştirmiş olmasın.
        /// </summary>
        public virtual void OnArrivedAtCell(Vector2Int cell)
        {
        }
    }
}
