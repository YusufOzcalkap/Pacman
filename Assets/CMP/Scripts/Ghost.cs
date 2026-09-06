using System.Collections.Generic;
using CMP.Scripts.AiStates;
using CMP.Scripts.Helper;
using CMP.Scripts.Movement;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

namespace CMP.Scripts
{
    public enum GhostStateType
    {
        InHouse,
        JoiningGame,
        Scatter,
        Chase,
        Frightened,
        Eaten,
    }

    public class Ghost : MonoBehaviour
    {
        [BoxGroup("Visuals"), Required] public GameObject LeftEye;
        [BoxGroup("Visuals"), Required] public GameObject RightEye;

        [ShowInInspector, ReadOnly, BoxGroup("Runtime")]
        public GhostPersonality Personality { get; private set; }

        [ShowInInspector, ReadOnly, BoxGroup("Runtime")]
        public Vector2Int CurrentCell => _mover?.CurrentCell ?? Vector2Int.zero;

        [ShowInInspector, ReadOnly, BoxGroup("Runtime")]
        public GhostStateType CurrentStateType { get; private set; } = GhostStateType.InHouse;

        /// <summary>Oyun başladıktan kaç saniye sonra evden çıkacağı.</summary>
        [ShowInInspector, ReadOnly, BoxGroup("Runtime")]
        public float JoinDelay { get; private set; }

        /// <summary>Evden çıkıp oyuna dahil olmuş mu?</summary>
        public bool IsInGame => CurrentStateType is GhostStateType.Scatter or GhostStateType.Chase
            or GhostStateType.Frightened or GhostStateType.Eaten;

        /// <summary>Korkmuş hayalet Pacman'i yakalayamaz, yenebilir.</summary>
        public bool IsFrightened => CurrentStateType == GhostStateType.Frightened;

        /// <summary>Yenilmiş hayalet sadece gözlerden ibarettir; kimseye dokunmaz.</summary>
        public bool IsEaten => CurrentStateType == GhostStateType.Eaten;

        public Direction CurrentDirection => _mover?.CurrentDirection ?? Direction.None;

        /// <summary>Kişiliğin belirlediği takip hedefi; hata ayıklama görselleştirmesi bunu çizer.</summary>
        public Vector2Int ChaseTargetCell => _blackboard.GetChaseTargetCell();

        /// <summary>Gövde rengi; hata ayıklama görselleştirmesi çizimleri bununla eşleştirir.</summary>
        public Color BodyColor { get; private set; } = Color.white;

        private const float EyeLookOffset = 0.05f;
        private const float EyeLookSpeed = 12f;

        private GridMover _mover;
        private GhostBlackboard _blackboard;
        private GhostState _currentState;
        private SpriteRenderer _spriteRenderer;
        private Tween _blinkTween;
        private Vector3 _leftEyeBasePosition;
        private Vector3 _rightEyeBasePosition;

        private SpriteRenderer SpriteRenderer =>
            _spriteRenderer != null ? _spriteRenderer : _spriteRenderer = GetComponent<SpriteRenderer>();

        public void Initialize(GridData gridData, Pacman pacman, IReadOnlyList<Ghost> ghosts, GameRandom random,
            GhostPersonality personality, Vector2Int spawnCell, float joinDelay, LevelRules rules)
        {
            Personality = personality;
            JoinDelay = joinDelay;
            _leftEyeBasePosition = LeftEye.transform.localPosition;
            _rightEyeBasePosition = RightEye.transform.localPosition;

            _mover = new GridMover(transform, gridData, rules.GhostStepDuration);
            _mover.Arrived += cell => _currentState.OnArrivedAtCell(cell);
            _mover.Teleport(spawnCell);

            _blackboard = new GhostBlackboard(this, gridData, _mover, random, pacman, ghosts, spawnCell, rules);
            ChangeState(new InHouseState(_blackboard));
        }

        /// <summary>
        /// Kişilik rengini belirler. Korku modundaki geçici renkler bu değeri bozmaz,
        /// böylece mod bitince asıl renge dönülebilir.
        /// </summary>
        public void SetBodyColor(Color color)
        {
            BodyColor = color;
            SpriteRenderer.color = color;
        }

        public void SetFrightenedLook()
        {
            KillBlink();
            SpriteRenderer.color = GameSettings.FrightenedColor;
        }

        /// <summary>
        /// Sürenin bitmek üzere olduğunu anlatan yanıp sönme.
        ///
        /// SpriteRenderer.DOColor kısayolu DOTween'in modül kaynak dosyalarında tanımlı;
        /// o dosyalar ayrı bir assembly'de olmadığı için buradan erişilemiyor.
        /// Çekirdek DOTween.To aynı işi ek bağımlılık olmadan yapıyor.
        /// </summary>
        public void StartFrightenedBlink()
        {
            KillBlink();
            SpriteRenderer.color = GameSettings.FrightenedColor;

            _blinkTween = DOTween.To(
                    () => SpriteRenderer.color,
                    color => SpriteRenderer.color = color,
                    Color.white,
                    GameSettings.FrightenedBlinkInterval)
                .SetLoops(-1, LoopType.Yoyo)
                .SetLink(gameObject);
        }

        /// <summary>
        /// Gövdeyi gizler, geriye yalnızca gözler kalır.
        /// Gözler ayrı alt nesneler olduğu için tek satırda çözülüyor.
        /// </summary>
        public void SetEatenLook()
        {
            KillBlink();
            SpriteRenderer.enabled = false;
        }

        public void RestoreNormalLook()
        {
            KillBlink();
            SpriteRenderer.enabled = true;
            SpriteRenderer.color = BodyColor;
        }

        private void KillBlink()
        {
            _blinkTween?.Kill();
            _blinkTween = null;
        }

        /// <summary>
        /// Güçlendirme yemi yendiğinde çağrılır. Evdeki hayaletler etkilenmez.
        /// Zaten korkmuş bir hayalet için süre baştan başlar.
        /// </summary>
        public void EnterFrightened()
        {
            // Evdekiler ve eve dönmekte olan gözler etkilenmez.
            if (CurrentStateType is not (GhostStateType.Scatter or GhostStateType.Chase
                or GhostStateType.Frightened))
            {
                return;
            }

            ChangeState(new FrightenedState(_blackboard));
        }

        /// <summary>Korku modundayken yenildi: gövde kaybolur, gözler eve döner.</summary>
        public void EnterEaten()
        {
            ChangeState(new EatenState(_blackboard));
        }

        /// <summary>
        /// Can kaybından sonra hayaleti başlangıç durumuna döndürür: evine ışınlanır,
        /// görünümü ve hızı sıfırlanır, kendi katılma gecikmesiyle yeniden bekler.
        /// Hangi state'te yakalandıysa yakalansın (korkmuş, yenilmiş, takipte) güvenlidir.
        /// </summary>
        public void ResetToHouse()
        {
            _mover.Stop();
            _mover.StepDuration = _blackboard.Rules.GhostStepDuration;
            _mover.Teleport(_blackboard.SpawnCell);
            RestoreNormalLook();

            ChangeState(new InHouseState(_blackboard));
        }

        /// <summary>
        /// State geçişi. State nesneleri geçişte yeniden yaratılır, kalıcı veriler
        /// blackboard'da durur; böylece hiçbir state eski verisiyle uyanmaz.
        /// </summary>
        public void ChangeState(GhostState state)
        {
            _currentState = state;
            CurrentStateType = state.StateType;
            state.OnEnter();
        }

        /// <summary>
        /// Hayaleti takip moduna alır. Zaten takipteyse ya da henüz oyuna katılmadıysa
        /// bir şey yapmaz; bu sayede her karede güvenle çağrılabilir ve sonradan oyuna
        /// katılan hayaletler de kendiliğinden takibe dahil olur.
        /// </summary>
        /// <summary>
        /// Hayaletin state'ini oyunun moduyla eşitler. Her karede güvenle çağrılabilir:
        /// zaten doğru state'teyse hiçbir şey yapmaz. Evdeki, korkmuş ve yenilmiş
        /// hayaletler moddan etkilenmez; süreleri bitip serbest dolaşıma döndüklerinde
        /// buradan senkronize olurlar.
        /// </summary>
        public void ApplyGameMode(GameMode mode)
        {
            if (mode == GameMode.Chase && CurrentStateType == GhostStateType.Scatter)
            {
                ChangeState(new ChaseState(_blackboard));
            }
            else if (mode == GameMode.Scatter && CurrentStateType == GhostStateType.Chase)
            {
                ChangeState(new ScatterState(_blackboard));
            }
        }

        /// <summary>
        /// Hayaleti olduğu yerde geri döndürür. Mod değiştiğinde çağrılır:
        /// oyuncuya "kurallar değişti" mesajını veren en net görsel işaret budur.
        /// </summary>
        public void ReverseDirection()
        {
            if (CurrentStateType is not (GhostStateType.Scatter or GhostStateType.Chase))
            {
                return;
            }

            _mover.ReverseStep();
        }

        /// <summary>
        /// Hayaletin baktığı yönde, duvara takılmadan ve görüş menzili içinde
        /// hedef hücreyi görüp görmediği.
        /// </summary>
        public bool HasLineOfSightTo(Vector2Int targetCell)
        {
            return HasLineOfSightTo(targetCell, out _);
        }

        /// <param name="lastVisibleCell">
        /// Taramanın durduğu son görünür hücre. Hata ayıklama görselleştirmesi
        /// görüş ışınını buraya kadar çizer.
        /// </param>
        public bool HasLineOfSightTo(Vector2Int targetCell, out Vector2Int lastVisibleCell)
        {
            lastVisibleCell = CurrentCell;

            // Korkmuş hayalet Pacman'i görse de takibi başlatmaz; kaçmakla meşgul.
            if (CurrentStateType is not (GhostStateType.Scatter or GhostStateType.Chase) ||
                _mover.CurrentDirection == Direction.None)
            {
                return false;
            }

            var cell = _mover.CurrentCell;

            for (var distance = 0; distance < GameSettings.AiSightDistance; distance++)
            {
                // Tünelli haritada görüş de tünelden geçer; menzil sınırı zaten
                // döngünün kendisinde olduğu için sonsuza sarmaz.
                cell = _blackboard.GridData.GetNeighbourCell(cell, _mover.CurrentDirection);

                if (!_blackboard.GridData.GetInBounds(cell) ||
                    _blackboard.GridData.GetCellAt(cell) == CellType.Wall)
                {
                    return false;
                }

                lastVisibleCell = cell;

                if (cell == targetCell)
                {
                    return true;
                }
            }

            return false;
        }

        public void Tick()
        {
            _currentState.Update();
            UpdateEyeLook();
        }

        /// <summary>
        /// Gözbebekleri gittiği yöne kayar; durunca merkeze döner. Yenilmiş halde
        /// geriye yalnızca gözler kaldığı için orada da yönü bunlar anlatır.
        /// </summary>
        private void UpdateEyeLook()
        {
            var direction = CurrentDirection.ToVector2Int();
            var offset = new Vector3(direction.x, direction.y, 0f) * EyeLookOffset;
            var lerpFactor = Time.deltaTime * EyeLookSpeed;

            LeftEye.transform.localPosition = Vector3.Lerp(
                LeftEye.transform.localPosition, _leftEyeBasePosition + offset, lerpFactor);
            RightEye.transform.localPosition = Vector3.Lerp(
                RightEye.transform.localPosition, _rightEyeBasePosition + offset, lerpFactor);
        }

        public void Stop()
        {
            _mover.Stop();
            KillBlink();
        }
    }
}
