using System;
using System.Collections.Generic;
using CMP.Scripts;
using CMP.Scripts.Helper;
using UnityEditor;
using UnityEngine;

namespace CMP.Editor
{
    /// <summary>
    /// Level tasarım penceresi: Tools ▸ Level Editor.
    ///
    /// Sol panel LevelSet asset'ini sürer: listedeki sıra, oyundaki level sırasının
    /// ta kendisidir. Bir level seçilince sağda haritası ve zorluk ayarları açılır.
    ///
    /// Sağ taraf tek yüzeye çizim yapar: sürükleyerek boyama, sağ tıkla silme,
    /// ayna modu, kova, zoom, oyundaki görüntünün birebir önizlemesi ve her
    /// değişiklikte çalışan doğrulama. Kısayollar: 1-7 fırça, tekerlek zoom,
    /// sürükleme başına tek undo adımı.
    /// </summary>
    public class LevelEditorWindow : EditorWindow
    {
        private enum PaintTool
        {
            Brush,
            Fill,
        }

        private const float MinCellSize = 12f;
        private const float MaxCellSize = 48f;
        private const float PreviewPanelWidth = 240f;
        private const float ListPanelWidth = 230f;
        private const string LevelsFolder = "Assets/CMP/Levels";

        private LevelSet _levelSet;
        private int _selectedIndex = -1;
        private Vector2 _listScrollPosition;
        private GUIStyle _rowStyle;

        private GridData _gridData;
        private CellType _brush = CellType.Wall;
        private PaintTool _tool = PaintTool.Brush;
        private bool _mirrorHorizontal;
        private float _cellSize = 28f;
        private Vector2 _scrollPosition;

        private Texture2D _previewTexture;
        private bool _previewDirty = true;
        private int _undoGroup;

        private readonly List<ValidationIssue> _issues = new();
        private readonly Dictionary<Vector2Int, ValidationSeverity> _issueCells = new();
        private Vector2 _issueScrollPosition;

        private int _pendingWidth;
        private int _pendingHeight;

        [MenuItem("Tools/Level Editor")]
        public static void Open()
        {
            var window = GetWindow<LevelEditorWindow>("Level Editor");
            window.minSize = new Vector2(960f, 520f);
        }

        /// <summary>Inspector'daki "Level Editörünü Aç" butonu buraya gelir.</summary>
        public static void Open(GridData gridData)
        {
            Open();
            var window = GetWindow<LevelEditorWindow>();
            window.SelectGrid(gridData);
        }

        private void OnEnable()
        {
            Undo.undoRedoPerformed += OnUndoRedo;

            if (_levelSet == null)
            {
                _levelSet = Resources.Load<LevelSet>("LevelSet");
            }

            if (_selectedIndex < 0 && _levelSet != null && _levelSet.Levels.Count > 0)
            {
                SelectLevel(0);
            }
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= OnUndoRedo;
            DestroyPreviewTexture();
        }

        private void OnUndoRedo()
        {
            _previewDirty = true;

            if (_gridData != null)
            {
                RunValidation();
            }

            Repaint();
        }

        private void OnGUI()
        {
            HandleKeyboard();
            DrawToolbar();

            EditorGUILayout.BeginHorizontal();
            {
                DrawLevelListPanel();
                DrawEditPanel();
            }
            EditorGUILayout.EndHorizontal();
        }

        // --- Seçim -------------------------------------------------------------------

        private void SelectLevel(int index)
        {
            _selectedIndex = index;
            SetGridData(_levelSet.Levels[index].Grid);
            GUI.FocusControl(null);
        }

        private void SelectGrid(GridData gridData)
        {
            if (_levelSet != null)
            {
                var index = _levelSet.Levels.FindIndex(level => level.Grid == gridData);
                if (index >= 0)
                {
                    SelectLevel(index);
                    return;
                }
            }

            // Sette olmayan bir harita da düzenlenebilir; sadece zorluk ayarları görünmez.
            _selectedIndex = -1;
            SetGridData(gridData);
        }

        private void SetGridData(GridData gridData)
        {
            _gridData = gridData;
            _previewDirty = true;

            if (_gridData != null)
            {
                _pendingWidth = _gridData.Width;
                _pendingHeight = _gridData.Height;
                RunValidation();
            }
        }

        // --- Üst çubuk ---------------------------------------------------------------

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            {
                if (GUILayout.Button("Tümünü Kaydet", EditorStyles.toolbarButton, GUILayout.Width(100f)))
                {
                    UnityEditor.AssetDatabase.SaveAssets();
                }

                var selectedSet = (LevelSet)EditorGUILayout.ObjectField(_levelSet, typeof(LevelSet), false,
                    GUILayout.Width(180f));
                if (selectedSet != _levelSet)
                {
                    _levelSet = selectedSet;
                    _selectedIndex = -1;

                    if (_levelSet != null && _levelSet.Levels.Count > 0)
                    {
                        SelectLevel(0);
                    }
                }

                if (_gridData != null &&
                    GUILayout.Button("Şablon ▾", EditorStyles.toolbarButton, GUILayout.Width(70f)))
                {
                    ShowTemplateMenu();
                }

                GUILayout.FlexibleSpace();
            }
            EditorGUILayout.EndHorizontal();
        }

        // --- Sol panel: level listesi ------------------------------------------------

        private void DrawLevelListPanel()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(ListPanelWidth));
            {
                if (_levelSet == null)
                {
                    EditorGUILayout.HelpBox("LevelSet asset'i bulunamadı.", MessageType.Warning);
                    EditorGUILayout.EndVertical();
                    return;
                }

                _rowStyle ??= new GUIStyle(GUI.skin.button)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 11,
                };

                _listScrollPosition = EditorGUILayout.BeginScrollView(_listScrollPosition);

                for (var i = 0; i < _levelSet.Levels.Count; i++)
                {
                    DrawLevelRow(i);
                }

                EditorGUILayout.EndScrollView();

                DrawListButtons();
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawLevelRow(int index)
        {
            var definition = _levelSet.Levels[index];
            var grid = definition.Grid;

            var title = grid != null ? grid.name : "— harita yok —";
            var info = grid != null
                ? $"{grid.Width}x{grid.Height}  ·  {definition.GhostCount} hayalet" +
                  (grid.WrapEdges ? "  ·  tünel" : "")
                : "GridData ata!";

            var previousColor = GUI.backgroundColor;
            GUI.backgroundColor = index == _selectedIndex ? new Color(0.35f, 0.6f, 1f) : previousColor;

            if (GUILayout.Button($"{index + 1}.  {title}\n{info}", _rowStyle, GUILayout.Height(38f)))
            {
                SelectLevel(index);
            }

            GUI.backgroundColor = previousColor;
        }

        private void DrawListButtons()
        {
            EditorGUILayout.BeginHorizontal();
            {
                if (GUILayout.Button("+ Ekle"))
                {
                    AddLevel(duplicateSelected: false);
                }

                using (new EditorGUI.DisabledScope(_selectedIndex < 0))
                {
                    if (GUILayout.Button("Kopyala"))
                    {
                        AddLevel(duplicateSelected: true);
                    }

                    if (GUILayout.Button("Sil"))
                    {
                        RemoveSelectedLevel();
                    }
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            {
                using (new EditorGUI.DisabledScope(_selectedIndex <= 0))
                {
                    if (GUILayout.Button("↑ Yukarı"))
                    {
                        MoveSelectedLevel(-1);
                    }
                }

                using (new EditorGUI.DisabledScope(
                           _selectedIndex < 0 || _selectedIndex >= _levelSet.Levels.Count - 1))
                {
                    if (GUILayout.Button("↓ Aşağı"))
                    {
                        MoveSelectedLevel(1);
                    }
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// Yeni level: harita asset'i Levels klasörüne oluşturulur ve setin sonuna
        /// (seçili varsa hemen altına) eklenir. Kopyalamada zorluk ayarları da taşınır.
        /// </summary>
        private void AddLevel(bool duplicateSelected)
        {
            var sourceDefinition = _selectedIndex >= 0 ? _levelSet.Levels[_selectedIndex] : null;

            var newGrid = duplicateSelected && sourceDefinition?.Grid != null
                ? DuplicateGridAsset(sourceDefinition.Grid)
                : CreateGridAsset();

            if (newGrid == null)
            {
                return;
            }

            var definition = new LevelDefinition { Grid = newGrid };

            if (sourceDefinition != null)
            {
                definition.PacmanSpeedMultiplier = sourceDefinition.PacmanSpeedMultiplier;
                definition.GhostSpeedMultiplier = sourceDefinition.GhostSpeedMultiplier;
                definition.FrightenedDuration = sourceDefinition.FrightenedDuration;
                definition.GhostCount = sourceDefinition.GhostCount;
            }

            Undo.RecordObject(_levelSet, "Add Level");
            var insertIndex = _selectedIndex >= 0 ? _selectedIndex + 1 : _levelSet.Levels.Count;
            _levelSet.Levels.Insert(insertIndex, definition);
            EditorUtility.SetDirty(_levelSet);

            SelectLevel(insertIndex);
        }

        private void RemoveSelectedLevel()
        {
            var grid = _levelSet.Levels[_selectedIndex].Grid;
            var gridName = grid != null ? grid.name : "(boş)";

            if (!EditorUtility.DisplayDialog("Level'ı Sil",
                    $"'{gridName}' listeden kaldırılacak. Harita asset'i silinmez.", "Kaldır", "Vazgeç"))
            {
                return;
            }

            Undo.RecordObject(_levelSet, "Remove Level");
            _levelSet.Levels.RemoveAt(_selectedIndex);
            EditorUtility.SetDirty(_levelSet);

            _selectedIndex = Mathf.Min(_selectedIndex, _levelSet.Levels.Count - 1);
            if (_selectedIndex >= 0)
            {
                SelectLevel(_selectedIndex);
            }
            else
            {
                SetGridData(null);
            }
        }

        private void MoveSelectedLevel(int offset)
        {
            Undo.RecordObject(_levelSet, "Reorder Levels");
            var definition = _levelSet.Levels[_selectedIndex];
            _levelSet.Levels.RemoveAt(_selectedIndex);
            _levelSet.Levels.Insert(_selectedIndex + offset, definition);
            EditorUtility.SetDirty(_levelSet);

            _selectedIndex += offset;
        }

        private static GridData CreateGridAsset()
        {
            var asset = CreateInstance<GridData>();
            asset.Width = 11;
            asset.Height = 11;
            asset.Grid = new CellType[asset.Width * asset.Height];
            ApplyBorderTemplate(asset);

            UnityEditor.AssetDatabase.CreateAsset(asset, GetUniqueLevelPath());
            UnityEditor.AssetDatabase.SaveAssets();
            return asset;
        }

        private static GridData DuplicateGridAsset(GridData source)
        {
            var sourcePath = UnityEditor.AssetDatabase.GetAssetPath(source);
            var newPath = GetUniqueLevelPath();

            if (!UnityEditor.AssetDatabase.CopyAsset(sourcePath, newPath))
            {
                return null;
            }

            return UnityEditor.AssetDatabase.LoadAssetAtPath<GridData>(newPath);
        }

        private static string GetUniqueLevelPath()
        {
            if (!UnityEditor.AssetDatabase.IsValidFolder(LevelsFolder))
            {
                UnityEditor.AssetDatabase.CreateFolder("Assets/CMP", "Levels");
            }

            for (var i = 1; ; i++)
            {
                var path = $"{LevelsFolder}/Level{i:00}.asset";
                if (UnityEditor.AssetDatabase.LoadAssetAtPath<GridData>(path) == null)
                {
                    return path;
                }
            }
        }

        // --- Sağ panel ---------------------------------------------------------------

        private void DrawEditPanel()
        {
            EditorGUILayout.BeginVertical();
            {
                if (_gridData == null)
                {
                    EditorGUILayout.HelpBox("Soldan bir level seç ya da '+ Ekle' ile yeni oluştur.",
                        MessageType.Info);
                    EditorGUILayout.EndVertical();
                    return;
                }

                DrawDefinitionRow();
                DrawSettingsRow();
                DrawBrushPalette();

                EditorGUILayout.BeginHorizontal();
                {
                    DrawGridCanvas();
                    DrawPreviewPanel();
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();
        }

        /// <summary>Seçili level'ın zorluk ayarları; doğrudan LevelSet'e yazar.</summary>
        private void DrawDefinitionRow()
        {
            if (_selectedIndex < 0 || _levelSet == null)
            {
                return;
            }

            var definition = _levelSet.Levels[_selectedIndex];

            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            {
                EditorGUIUtility.labelWidth = 78f;
                EditorGUI.BeginChangeCheck();

                var pacmanSpeed = EditorGUILayout.Slider("Pacman Hız",
                    definition.PacmanSpeedMultiplier, 0.5f, 2f);
                var ghostSpeed = EditorGUILayout.Slider("Hayalet Hız",
                    definition.GhostSpeedMultiplier, 0.5f, 2f);

                EditorGUIUtility.labelWidth = 62f;
                var frightened = EditorGUILayout.FloatField("Korku (sn)",
                    definition.FrightenedDuration, GUILayout.Width(110f));
                var ghostCount = EditorGUILayout.IntSlider("Hayalet",
                    definition.GhostCount, 1, 8, GUILayout.Width(160f));

                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(_levelSet, "Edit Level Definition");
                    definition.PacmanSpeedMultiplier = pacmanSpeed;
                    definition.GhostSpeedMultiplier = ghostSpeed;
                    definition.FrightenedDuration = Mathf.Max(0f, frightened);
                    definition.GhostCount = ghostCount;
                    EditorUtility.SetDirty(_levelSet);
                }

                EditorGUIUtility.labelWidth = 0f;
            }
            EditorGUILayout.EndHorizontal();
        }

        private void ShowTemplateMenu()
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Boş (hepsi Empty)"), false, () => ApplyTemplate(ClearToEmpty));
            menu.AddItem(new GUIContent("Çerçeveli (kenarlar Wall)"), false,
                () => ApplyTemplate(ApplyBorderTemplate));
            menu.ShowAsContext();
        }

        private void ApplyTemplate(Action<GridData> template)
        {
            Undo.RecordObject(_gridData, "Apply Level Template");
            template(_gridData);
            MarkGridChanged();
        }

        private static void ClearToEmpty(GridData gridData)
        {
            for (var i = 0; i < gridData.Grid.Length; i++)
            {
                gridData.Grid[i] = CellType.Empty;
            }
        }

        private static void ApplyBorderTemplate(GridData gridData)
        {
            for (var y = 0; y < gridData.Height; y++)
            {
                for (var x = 0; x < gridData.Width; x++)
                {
                    var isBorder = x == 0 || y == 0 || x == gridData.Width - 1 || y == gridData.Height - 1;
                    gridData.Grid[y * gridData.Width + x] = isBorder ? CellType.Wall : CellType.Empty;
                }
            }
        }

        private void DrawSettingsRow()
        {
            EditorGUILayout.BeginHorizontal();
            {
                _tool = (PaintTool)EditorGUILayout.EnumPopup(_tool, GUILayout.Width(70f));

                _mirrorHorizontal = GUILayout.Toggle(_mirrorHorizontal, "Ayna (yatay)",
                    EditorStyles.miniButton, GUILayout.Width(90f));

                var wrapEdges = GUILayout.Toggle(_gridData.WrapEdges, "Tünel (WrapEdges)",
                    EditorStyles.miniButton, GUILayout.Width(120f));
                if (wrapEdges != _gridData.WrapEdges)
                {
                    Undo.RecordObject(_gridData, "Toggle Wrap Edges");
                    _gridData.WrapEdges = wrapEdges;
                    MarkGridChanged();
                }

                GUILayout.Space(16f);

                GUILayout.Label("Boyut", GUILayout.Width(38f));
                _pendingWidth = EditorGUILayout.IntField(_pendingWidth, GUILayout.Width(36f));
                GUILayout.Label("x", GUILayout.Width(12f));
                _pendingHeight = EditorGUILayout.IntField(_pendingHeight, GUILayout.Width(36f));

                var sizeChanged = _pendingWidth != _gridData.Width || _pendingHeight != _gridData.Height;
                using (new EditorGUI.DisabledScope(!sizeChanged || _pendingWidth < 3 || _pendingHeight < 3))
                {
                    if (GUILayout.Button("Boyutu Uygula", GUILayout.Width(100f)))
                    {
                        ResizeGrid();
                    }
                }

                GUILayout.FlexibleSpace();

                GUILayout.Label("Zoom", GUILayout.Width(40f));
                _cellSize = GUILayout.HorizontalSlider(_cellSize, MinCellSize, MaxCellSize,
                    GUILayout.Width(120f));
            }
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// Mevcut hücreler korunarak yeniden boyutlandırır; kazayla veri kaybını
        /// önlemek için ayrı bir onay butonuna bağlıdır.
        /// </summary>
        private void ResizeGrid()
        {
            Undo.RecordObject(_gridData, "Resize Level");

            var oldGrid = _gridData.Grid;
            var oldWidth = _gridData.Width;
            var oldHeight = _gridData.Height;

            _gridData.Width = _pendingWidth;
            _gridData.Height = _pendingHeight;
            _gridData.Grid = new CellType[_pendingWidth * _pendingHeight];

            for (var y = 0; y < Mathf.Min(oldHeight, _pendingHeight); y++)
            {
                for (var x = 0; x < Mathf.Min(oldWidth, _pendingWidth); x++)
                {
                    _gridData.Grid[y * _pendingWidth + x] = oldGrid[y * oldWidth + x];
                }
            }

            MarkGridChanged();
        }

        private void DrawBrushPalette()
        {
            EditorGUILayout.BeginHorizontal();
            {
                var index = 1;
                foreach (CellType type in Enum.GetValues(typeof(CellType)))
                {
                    if (type == CellType.Invalid)
                    {
                        continue;
                    }

                    var previousColor = GUI.backgroundColor;
                    GUI.backgroundColor = _brush == type ? Color.white : CellTypePalette.GetColor(type);

                    var label = $"{index}. {CellTypePalette.GetShortLabel(type)}";
                    if (GUILayout.Button(label, GUILayout.Height(24f)))
                    {
                        _brush = type;
                    }

                    GUI.backgroundColor = previousColor;
                    index++;
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        private void HandleKeyboard()
        {
            var current = Event.current;

            // Boyut alanına sayı yazılırken rakam tuşları fırça değiştirmemeli.
            if (current.type != EventType.KeyDown || EditorGUIUtility.editingTextField)
            {
                return;
            }

            var index = current.keyCode - KeyCode.Alpha1;
            var paintableTypes = (CellType[])Enum.GetValues(typeof(CellType));

            if (index >= 0 && index < paintableTypes.Length && paintableTypes[index] != CellType.Invalid)
            {
                _brush = paintableTypes[index];
                current.Use();
                Repaint();
            }
        }

        // --- Doğrulama ---------------------------------------------------------------

        /// <summary>
        /// Her değişiklikten sonra çalışır. Hatalı hücreler haritada işaretlenir;
        /// ciddiyeti yüksek olan (Error) işaret her zaman öncelik kazanır.
        /// </summary>
        private void RunValidation()
        {
            _issues.Clear();
            _issueCells.Clear();

            _issues.AddRange(LevelValidator.Validate(_gridData));

            foreach (var issue in _issues)
            {
                foreach (var cell in issue.Cells)
                {
                    if (!_issueCells.TryGetValue(cell, out var existing) ||
                        existing == ValidationSeverity.Warning)
                    {
                        _issueCells[cell] = issue.Severity;
                    }
                }
            }
        }

        // --- Çizim yüzeyi ------------------------------------------------------------

        private void DrawGridCanvas()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            var totalWidth = _gridData.Width * _cellSize;
            var totalHeight = _gridData.Height * _cellSize;
            var canvasRect = GUILayoutUtility.GetRect(totalWidth, totalHeight,
                GUILayout.Width(totalWidth), GUILayout.Height(totalHeight));

            if (Event.current.type == EventType.Repaint)
            {
                DrawCells(canvasRect);
            }

            HandleCanvasInput(canvasRect);

            EditorGUILayout.EndScrollView();
        }

        private void DrawCells(Rect canvasRect)
        {
            EditorGUI.DrawRect(canvasRect, Color.black);

            for (var y = 0; y < _gridData.Height; y++)
            {
                for (var x = 0; x < _gridData.Width; x++)
                {
                    var cellRect = GetCellRect(canvasRect, x, y);
                    EditorGUI.DrawRect(cellRect, CellTypePalette.GetColor(_gridData.GetCellAt(x, y)));

                    if (_issueCells.TryGetValue(new Vector2Int(x, y), out var severity))
                    {
                        var overlayColor = severity == ValidationSeverity.Error
                            ? new Color(1f, 0.1f, 0.1f, 0.45f)
                            : new Color(1f, 0.85f, 0.1f, 0.4f);
                        EditorGUI.DrawRect(cellRect, overlayColor);
                    }
                }
            }
        }

        /// <summary>Oyun koordinatında y yukarı bakar, GUI'de aşağı; burada çevrilir.</summary>
        private Rect GetCellRect(Rect canvasRect, int x, int y)
        {
            return new Rect(
                canvasRect.x + x * _cellSize,
                canvasRect.y + (_gridData.Height - 1 - y) * _cellSize,
                _cellSize - 1f,
                _cellSize - 1f);
        }

        private void HandleCanvasInput(Rect canvasRect)
        {
            var current = Event.current;

            if (current.type == EventType.ScrollWheel && canvasRect.Contains(current.mousePosition))
            {
                _cellSize = Mathf.Clamp(_cellSize - current.delta.y, MinCellSize, MaxCellSize);
                current.Use();
                Repaint();
                return;
            }

            var isPaintEvent = current.type is EventType.MouseDown or EventType.MouseDrag;
            if (!isPaintEvent || current.button > 1 || !canvasRect.Contains(current.mousePosition))
            {
                return;
            }

            var x = (int)((current.mousePosition.x - canvasRect.x) / _cellSize);
            var flippedY = (int)((current.mousePosition.y - canvasRect.y) / _cellSize);
            var cell = new Vector2Int(x, _gridData.Height - 1 - flippedY);

            if (!_gridData.GetInBounds(cell))
            {
                return;
            }

            // Sağ tık her zaman silgidir.
            var paintType = current.button == 1 ? CellType.Empty : _brush;

            if (current.type == EventType.MouseDown)
            {
                // Sürükleme boyunca yapılan tüm değişiklikler tek undo adımı olsun.
                Undo.IncrementCurrentGroup();
                _undoGroup = Undo.GetCurrentGroup();
            }

            Undo.RecordObject(_gridData, "Paint Level");

            if (_tool == PaintTool.Fill && current.type == EventType.MouseDown)
            {
                FloodFill(cell, paintType);
            }
            else
            {
                PaintCell(cell, paintType);
            }

            Undo.CollapseUndoOperations(_undoGroup);
            MarkGridChanged();
            current.Use();
        }

        private void PaintCell(Vector2Int cell, CellType type)
        {
            _gridData.Grid[cell.y * _gridData.Width + cell.x] = type;

            if (_mirrorHorizontal)
            {
                var mirroredX = _gridData.Width - 1 - cell.x;
                _gridData.Grid[cell.y * _gridData.Width + mirroredX] = type;
            }
        }

        /// <summary>Kova aracı: tıklanan hücreyle aynı tipteki bitişik alanı boyar.</summary>
        private void FloodFill(Vector2Int startCell, CellType paintType)
        {
            var targetType = _gridData.GetCellAt(startCell);
            if (targetType == paintType)
            {
                return;
            }

            var frontier = new Queue<Vector2Int>();
            frontier.Enqueue(startCell);
            _gridData.Grid[startCell.y * _gridData.Width + startCell.x] = paintType;

            while (frontier.Count > 0)
            {
                var currentCell = frontier.Dequeue();

                foreach (var direction in GameSettings.DirectionsToCheck)
                {
                    var neighbour = currentCell + direction.ToVector2Int();
                    if (!_gridData.GetInBounds(neighbour) || _gridData.GetCellAt(neighbour) != targetType)
                    {
                        continue;
                    }

                    _gridData.Grid[neighbour.y * _gridData.Width + neighbour.x] = paintType;
                    frontier.Enqueue(neighbour);
                }
            }
        }

        private void MarkGridChanged()
        {
            EditorUtility.SetDirty(_gridData);
            _previewDirty = true;
            RunValidation();
            Repaint();
        }

        // --- Önizleme ----------------------------------------------------------------

        /// <summary>
        /// Oyunun gerçek harita üretecini kullanır: burada görünen, oyunda görünenin
        /// ta kendisidir. Ayrı bir önizleme çizimi yazıp iki görüntünün zamanla
        /// birbirinden kopması riskine girmiyoruz.
        /// </summary>
        private void DrawPreviewPanel()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(PreviewPanelWidth));
            {
                EditorGUILayout.LabelField("Oyun Önizlemesi", EditorStyles.boldLabel);

                if (_previewDirty || _previewTexture == null)
                {
                    RegeneratePreview();
                }

                if (_previewTexture != null)
                {
                    var aspect = (float)_previewTexture.height / _previewTexture.width;
                    var previewRect = GUILayoutUtility.GetRect(PreviewPanelWidth,
                        PreviewPanelWidth * aspect);
                    GUI.DrawTexture(previewRect, _previewTexture, ScaleMode.ScaleToFit);
                }

                DrawValidationPanel();
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawValidationPanel()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Doğrulama", EditorStyles.boldLabel);

            if (_issues.Count == 0)
            {
                EditorGUILayout.HelpBox("Sorun yok — level oynanabilir.", MessageType.Info);
                return;
            }

            _issueScrollPosition = EditorGUILayout.BeginScrollView(_issueScrollPosition);

            foreach (var issue in _issues)
            {
                var messageType = issue.Severity == ValidationSeverity.Error
                    ? MessageType.Error
                    : MessageType.Warning;
                EditorGUILayout.HelpBox(issue.Message, messageType);
            }

            EditorGUILayout.EndScrollView();
        }

        private void RegeneratePreview()
        {
            var settings = Resources.Load<MapVisualSettings>("MapVisualSettings");
            if (settings == null)
            {
                return;
            }

            DestroyPreviewTexture();
            _previewTexture = MapTextureGenerator.Generate(_gridData, settings);
            _previewDirty = false;
        }

        private void DestroyPreviewTexture()
        {
            if (_previewTexture != null)
            {
                DestroyImmediate(_previewTexture);
                _previewTexture = null;
            }
        }
    }
}
