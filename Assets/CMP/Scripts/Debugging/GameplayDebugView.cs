using System.Collections.Generic;
using System.Text;
using CMP.Scripts.Helper;
using UnityEngine;

namespace CMP.Scripts.Debugging
{
    /// <summary>
    /// Yapay zekanın ne düşündüğünü görünür kılan hata ayıklama katmanı. F1 ile açılır.
    ///
    /// Oyun kodunu hiçbir şekilde etkilemez: hiçbir state'e kanca takmaz, gerekli
    /// bilgiyi genel API üzerinden kendisi hesaplar. Bu yüzden kapalıyken maliyeti sıfırdır
    /// ve kaldırıldığında oyunda hiçbir şey değişmez.
    ///
    /// Çizgiler <see cref="Debug.DrawLine"/> ile çizilir: Scene view'da her zaman,
    /// Game view'da ise Gizmos açıkken görünür. Metin katmanı her durumda görünür.
    /// </summary>
    public class GameplayDebugView : MonoBehaviour
    {
        private const KeyCode ToggleKey = KeyCode.F1;
        private const float MarkerSize = 0.3f;

        private static readonly Color JunctionColor = new(1f, 1f, 1f, 0.25f);
        private static readonly Color SightBlockedColor = new(1f, 0.3f, 0.3f);
        private static readonly Color SightClearColor = new(0.3f, 1f, 0.3f);

        private GameManager _gameManager;
        private GridData _gridData;
        private Pacman _pacman;
        private IReadOnlyList<Ghost> _ghosts;

        private readonly List<Vector2Int> _junctions = new();
        private readonly StringBuilder _panelText = new();

        private GUIStyle _labelStyle;
        private GUIStyle _panelStyle;
        private bool _isVisible;

        public void Initialize(GameManager gameManager, GridData gridData, Pacman pacman, IReadOnlyList<Ghost> ghosts)
        {
            _gameManager = gameManager;
            _gridData = gridData;
            _pacman = pacman;
            _ghosts = ghosts;
            CacheJunctions();
        }

        /// <summary>
        /// Karar noktaları haritayla birlikte sabit olduğu için bir kez hesaplanır.
        /// Üçten fazla komşusu olan hücre, yapay zekanın gerçekten seçim yaptığı yerdir:
        /// geldiği yön çıkarıldığında geriye birden fazla aday kalır.
        /// </summary>
        private void CacheJunctions()
        {
            _junctions.Clear();

            for (var y = 0; y < _gridData.Height; y++)
            {
                for (var x = 0; x < _gridData.Width; x++)
                {
                    var cell = new Vector2Int(x, y);
                    if (!_gridData.IsCellMovable(cell, GameSettings.InGameAiWalkableCells))
                    {
                        continue;
                    }

                    var neighbourCount = 0;
                    foreach (var direction in GameSettings.DirectionsToCheck)
                    {
                        var neighbour = _gridData.GetNeighbourCell(cell, direction);
                        if (_gridData.IsCellMovable(neighbour, GameSettings.InGameAiWalkableCells))
                        {
                            neighbourCount++;
                        }
                    }

                    if (neighbourCount > 2)
                    {
                        _junctions.Add(cell);
                    }
                }
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(ToggleKey))
            {
                _isVisible = !_isVisible;
            }

            if (!_isVisible)
            {
                return;
            }

            DrawJunctions();

            foreach (var ghost in _ghosts)
            {
                DrawGhost(ghost);
            }
        }

        private void DrawJunctions()
        {
            foreach (var junction in _junctions)
            {
                DrawMarker(junction, JunctionColor, MarkerSize * 0.5f);
            }
        }

        private void DrawGhost(Ghost ghost)
        {
            if (!ghost.IsInGame)
            {
                return;
            }

            DrawSightRay(ghost);

            if (ghost.CurrentStateType == GhostStateType.Chase)
            {
                DrawChasePath(ghost);
            }
        }

        /// <summary>Baktığı yöndeki görüş ışını: yeşil = Pacman görünüyor, kırmızı = kesildi.</summary>
        private void DrawSightRay(Ghost ghost)
        {
            var seesPacman = ghost.HasLineOfSightTo(_pacman.CurrentCell, out var lastVisibleCell);
            var color = seesPacman ? SightClearColor : SightBlockedColor;
            Debug.DrawLine(ghost.transform.position, lastVisibleCell.ToWorldPosition(), color);
        }

        /// <summary>
        /// Takip halindeki hayaletin kendi kişilik hedefine giden en kısa yolu.
        /// Hedef Pacman'in hücresi olmak zorunda değil: Pinky önünü keser,
        /// Inky Blinky'ye göre yansıtır, Clyde yaklaşınca köşesine kaçar.
        /// </summary>
        private void DrawChasePath(Ghost ghost)
        {
            var targetCell = ghost.ChaseTargetCell;

            var found = Pathfinder.TryGetPath(
                _gridData,
                ghost.CurrentCell,
                targetCell,
                GameSettings.InGameAiWalkableCells,
                out var path);

            if (!found)
            {
                return;
            }

            var previous = ghost.CurrentCell.ToWorldPosition();
            foreach (var cell in path)
            {
                var current = cell.ToWorldPosition();
                Debug.DrawLine(previous, current, ghost.BodyColor);
                previous = current;
            }

            DrawMarker(targetCell, ghost.BodyColor, MarkerSize);
        }

        private static void DrawMarker(Vector2Int cell, Color color, float size)
        {
            var center = cell.ToWorldPosition();
            Debug.DrawLine(center + new Vector3(-size, -size), center + new Vector3(size, size), color);
            Debug.DrawLine(center + new Vector3(-size, size), center + new Vector3(size, -size), color);
        }

        private void OnGUI()
        {
            // Kapalıyken ekranda hiçbir iz bırakmaz; varlığı README'de not edilir.
            if (!_isVisible)
            {
                return;
            }

            EnsureStyles();
            DrawStatePanel();
            DrawGhostLabels();
        }

        private void DrawStatePanel()
        {
            _panelText.Clear();
            _panelText.AppendLine(
                $"LEVEL {_gameManager.LevelNumber}   MODE: {_gameManager.CurrentMode} " +
                $"({_gameManager.WaveTimeLeft:0.0}s)   SCORE: {_gameManager.Score}   " +
                $"LIVES: {_gameManager.Lives}   PELLETS LEFT: {_gameManager.RemainingPellets}");
            _panelText.AppendLine($"Pacman   cell {_pacman.CurrentCell}  dir {_pacman.CurrentDirection}");

            foreach (var ghost in _ghosts)
            {
                var target = ghost.CurrentStateType == GhostStateType.Chase
                    ? $"  target {ghost.ChaseTargetCell}"
                    : string.Empty;
                var sight = ghost.HasLineOfSightTo(_pacman.CurrentCell) ? "  SEES PACMAN" : string.Empty;

                _panelText.AppendLine(
                    $"{ghost.Personality,-7} {ghost.CurrentStateType,-12} cell {ghost.CurrentCell} dir {ghost.CurrentDirection}{target}{sight}");
            }

            GUI.Box(new Rect(10f, 10f, 640f, 30f + _ghosts.Count * 26f), _panelText.ToString(), _panelStyle);
        }

        /// <summary>Her hayaletin state'ini kendi üstünde gösterir.</summary>
        private void DrawGhostLabels()
        {
            var mainCamera = Camera.main;

            foreach (var ghost in _ghosts)
            {
                var screenPosition = mainCamera.WorldToScreenPoint(ghost.transform.position + Vector3.up * 0.6f);
                if (screenPosition.z < 0f)
                {
                    continue;
                }

                var previousColor = GUI.color;
                GUI.color = ghost.BodyColor;
                GUI.Label(
                    new Rect(screenPosition.x - 90f, Screen.height - screenPosition.y - 15f, 180f, 30f),
                    ghost.CurrentStateType.ToString(),
                    _labelStyle);
                GUI.color = previousColor;
            }
        }

        private void EnsureStyles()
        {
            if (_labelStyle != null)
            {
                return;
            }

            _labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 22,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };

            _panelStyle = new GUIStyle(GUI.skin.box)
            {
                fontSize = 20,
                alignment = TextAnchor.UpperLeft,
                padding = new RectOffset(12, 12, 10, 10)
            };
        }
    }
}
