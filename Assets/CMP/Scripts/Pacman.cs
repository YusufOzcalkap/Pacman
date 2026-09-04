using CMP.Scripts.Movement;
using Sirenix.OdinInspector;
using UnityEngine;

namespace CMP.Scripts
{
    public class Pacman : MonoBehaviour
    {
        [BoxGroup("Visuals"), Required] public Animator Animator;
        private const string FailAnimationName = "FailAnimation";
        private const string IdleAnimationName = "IdleAnimation";

        [ShowInInspector, ReadOnly, BoxGroup("Runtime")]
        public Vector2Int CurrentCell => _mover?.CurrentCell ?? Vector2Int.zero;

        [ShowInInspector, ReadOnly, BoxGroup("Runtime")]
        public Direction CurrentDirection { get; private set; }

        [ShowInInspector, ReadOnly, BoxGroup("Runtime")]
        private Direction _requestedDirection;

        private GridData _gridData;
        private GridMover _mover;
        private Vector2Int _startCell;

        /// <summary>
        /// Pacman'i başlangıç hücresine yerleştirir ve hareket bileşenini kurar.
        /// Konum prefab'dan değil GridData'dan geldiği için haritayı editörden
        /// değiştirmek Pacman'in başlangıç noktasını da değiştirir.
        /// </summary>
        public void Initialize(GridData gridData, Vector2Int startCell, float stepDuration)
        {
            _gridData = gridData;
            _startCell = startCell;
            _mover = new GridMover(transform, gridData, stepDuration);
            _mover.Arrived += _ => StartNextStep();
            _mover.Teleport(startCell);
        }

        /// <summary>Can kaybından sonra Pacman'i başlangıç durumuna döndürür.</summary>
        public void ResetToStart()
        {
            _requestedDirection = Direction.None;
            CurrentDirection = Direction.None;
            _mover.Stop();
            _mover.Teleport(_startCell);

            Animator.transform.rotation = Quaternion.identity;
            Animator.Play(IdleAnimationName);
        }

        /// <summary>
        /// Oyuncunun istediği yönü kaydeder. Yön hemen uygulanmaz, ilk uygun hücrede
        /// devreye girer ve o ana kadar saklı kalır. Orijinal PAC-MAN'deki
        /// "köşeye varmadan yönü basabilme" hissi bu tampondan gelir.
        /// </summary>
        public void SetRequestedDirection(Direction direction)
        {
            if (direction != Direction.None)
            {
                _requestedDirection = direction;
            }
        }

        public void Tick()
        {
            // Duvara çarpıp durduysa yeni bir yön gelene kadar her karede tekrar deniyoruz.
            if (!_mover.IsMoving)
            {
                StartNextStep();
            }
        }

        public void Stop()
        {
            CurrentDirection = Direction.None;
            _mover.Stop();
        }

        /// <summary>
        /// Yakalanma animasyonunu oynatır. Görsel, gidiş yönüne göre döndürülmüş
        /// olabileceğinden animasyondan önce dönüşü sıfırlıyoruz.
        /// </summary>
        public void PlayFailAnimation()
        {
            Animator.transform.rotation = Quaternion.identity;
            Animator.Play(FailAnimationName);
        }

        /// <summary>
        /// Bir sonraki hücreye geçiş kararı. Hücre merkezinde verilir:
        /// önce oyuncunun istediği yön, olmuyorsa mevcut yön, o da olmuyorsa duruş.
        /// </summary>
        private void StartNextStep()
        {
            var nextDirection = Direction.None;

            if (CanEnter(_requestedDirection))
            {
                nextDirection = _requestedDirection;
                _requestedDirection = Direction.None;
            }
            else if (CanEnter(CurrentDirection))
            {
                nextDirection = CurrentDirection;
            }

            CurrentDirection = nextDirection;

            if (nextDirection == Direction.None)
            {
                _mover.Stop();
                return;
            }

            Animator.transform.rotation = nextDirection.ToRightFacingRotation();
            _mover.StartStep(nextDirection);
        }

        private bool CanEnter(Direction direction)
        {
            if (direction == Direction.None)
            {
                return false;
            }

            var targetCell = _gridData.GetNeighbourCell(_mover.CurrentCell, direction);
            return _gridData.IsCellMovable(targetCell, GameSettings.PacmanWalkableCells);
        }
    }
}
