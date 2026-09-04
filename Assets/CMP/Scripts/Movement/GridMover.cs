using System;
using DG.Tweening;
using UnityEngine;

namespace CMP.Scripts.Movement
{
    /// <summary>
    /// Karakterleri hücreden hücreye taşıyan ortak hareket bileşeni.
    /// Pacman ve yapay zeka aynı ızgara hareketini kullandığı için tek yerde toplandı.
    ///
    /// Fizik yoktur; her adım DOTween ile sabit sürede tamamlanır. Adım bitince
    /// <see cref="Arrived"/> tetiklenir ve bir sonraki adıma karar vermek çağıranın işidir.
    /// Böylece "nasıl hareket edilir" ile "nereye gidilir" birbirinden ayrılmış olur.
    ///
    /// MonoBehaviour değil: prefab'lara bileşen eklemeyi gerektirmez ve
    /// sahne açmadan test edilebilir.
    /// </summary>
    public class GridMover
    {
        /// <summary>Karakter yeni bir hücreye vardığında tetiklenir.</summary>
        public event Action<Vector2Int> Arrived;

        public Vector2Int CurrentCell { get; private set; }
        public Direction CurrentDirection { get; private set; }
        public bool IsMoving => _stepTween != null;

        /// <summary>Bir hücre kat etme süresi. Değiştirmek karakteri hızlandırır ya da yavaşlatır.</summary>
        public float StepDuration { get; set; }

        private readonly Transform _transform;
        private readonly GridData _gridData;
        private Tween _stepTween;

        public GridMover(Transform transform, GridData gridData, float stepDuration)
        {
            _transform = transform;
            _gridData = gridData;
            StepDuration = stepDuration;
        }

        /// <summary>Karakteri anında hedef hücreye koyar. Başlangıç yerleşimi için kullanılır.</summary>
        public void Teleport(Vector2Int cell)
        {
            KillStep();
            CurrentCell = cell;
            CurrentDirection = Direction.None;
            _transform.position = cell.ToWorldPosition();
        }

        /// <summary>
        /// Verilen yöndeki komşu hücreye doğru tek bir adım başlatır.
        /// Hedefin yürünebilir olup olmadığını kontrol etmek çağıranın sorumluluğundadır.
        /// </summary>
        public void StartStep(Direction direction)
        {
            if (direction == Direction.None)
            {
                Stop();
                return;
            }

            KillStep();
            CurrentDirection = direction;

            var targetCell = _gridData.GetNeighbourCell(CurrentCell, direction);

            if (targetCell != CurrentCell + direction.ToVector2Int())
            {
                // Tünelden geçiş: karakteri karşı kenarın hemen dışına alıp içeri
                // kaydırıyoruz. Haritayı kat eden bir kayma yerine temiz bir giriş oluyor.
                _transform.position = (targetCell - direction.ToVector2Int()).ToWorldPosition();
            }

            _stepTween = _transform
                .DOMove(targetCell.ToWorldPosition(), StepDuration)
                .SetEase(Ease.Linear)
                .SetLink(_transform.gameObject)
                .OnComplete(() =>
                {
                    // Referansı callback'ten önce bırakıyoruz: Arrived dinleyicisi
                    // hemen yeni bir adım başlatabilir ve o adımın tween'ini
                    // yanlışlıkla öldürmemeliyiz.
                    _stepTween = null;
                    CurrentCell = targetCell;
                    Arrived?.Invoke(targetCell);
                });
        }

        /// <summary>
        /// Devam eden adımı iptal edip karakteri çıktığı hücreye geri döndürür.
        /// Hayaletler korkuya kapıldığında anında geri dönmeleri için kullanılır.
        ///
        /// Yeni tween bulunulan noktadan başladığı ve süresi kalan mesafeyle
        /// orantılı verildiği için hız sabit kalır, karakter zıplamaz.
        /// </summary>
        public void ReverseStep()
        {
            if (_stepTween == null)
            {
                return;
            }

            KillStep();
            CurrentDirection = CurrentDirection.Reverse();

            var targetPosition = CurrentCell.ToWorldPosition();
            var remainingDistance = Vector3.Distance(_transform.position, targetPosition);

            // Tünelden geçerken ters dönüldüyse çıkılan hücre haritanın öbür ucunda kalır;
            // araya bir kayma koymak yerine doğrudan oraya alıyoruz.
            if (remainingDistance > 1.5f)
            {
                _transform.position = targetPosition;
                remainingDistance = 0f;
            }

            var duration = Mathf.Max(StepDuration * remainingDistance, 0.01f);

            _stepTween = _transform
                .DOMove(targetPosition, duration)
                .SetEase(Ease.Linear)
                .SetLink(_transform.gameObject)
                .OnComplete(() =>
                {
                    _stepTween = null;
                    Arrived?.Invoke(CurrentCell);
                });
        }

        /// <summary>Hareketi olduğu yerde keser. Oyun bittiğinde herkes için çağrılır.</summary>
        public void Stop()
        {
            KillStep();
            CurrentDirection = Direction.None;
        }

        private void KillStep()
        {
            if (_stepTween == null)
            {
                return;
            }

            _stepTween.Kill();
            _stepTween = null;
        }
    }
}
