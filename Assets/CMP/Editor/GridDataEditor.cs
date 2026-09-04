using System;
using CMP.Scripts;
using UnityEditor;
using UnityEngine;

namespace CMP.Editor
{
    [CustomEditor(typeof(GridData))]
    public class GridDataEditor : UnityEditor.Editor
    {
        private GridData _gridData;
        private CellType _selectedBrush = CellType.Wall;
        private const float CELL_SIZE = 50f;

        private void OnEnable()
        {
            _gridData = (GridData)target;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            if (GUILayout.Button("Level Editörünü Aç", GUILayout.Height(28)))
            {
                LevelEditorWindow.Open(_gridData);
            }

            EditorGUILayout.Space(6);
            DrawGridSettings();
            EditorGUILayout.Space(10);
            DrawPalette();
            EditorGUILayout.Space(10);
            DrawGrid();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawGridSettings()
        {
            EditorGUILayout.LabelField("Grid Settings", EditorStyles.boldLabel);

            var oldWidth = _gridData.Width;
            var oldHeight = _gridData.Height;

            // WrapEdges boyut değişimi diyaloğunu tetiklememeli; bu yüzden
            // aşağıdaki BeginChangeCheck bloğunun dışında çiziliyor.
            EditorGUILayout.PropertyField(serializedObject.FindProperty("WrapEdges"));

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.PropertyField(serializedObject.FindProperty("Width"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("Height"));

            if (EditorGUI.EndChangeCheck())
            {
                if (EditorUtility.DisplayDialog("Resize Grid",
                        "Resizing the grid might result in data loss. Are you sure?",
                        "Resize", "Cancel"))
                {
                    serializedObject.ApplyModifiedProperties();
                    ResizeGrid(oldWidth, oldHeight);
                }
                else
                {
                    serializedObject.Update();
                }
            }
        }
        
        private void ResizeGrid(int oldWidth, int oldHeight)
        {
            var newSize = _gridData.Width * _gridData.Height;
            if (newSize <= 0)
            {
                _gridData.Grid = Array.Empty<CellType>();
                return;
            }

            var oldGrid = _gridData.Grid;
            _gridData.Grid = new CellType[newSize];

            if (oldGrid != null && oldWidth > 0)
            {
                for (var y = 0; y < _gridData.Height; y++)
                {
                    for (var x = 0; x < _gridData.Width; x++)
                    {
                        if (x < oldWidth && y < oldHeight)
                        {
                            _gridData.Grid[y * _gridData.Width + x] = oldGrid[y * oldWidth + x];
                        }
                    }
                }
            }

            EditorUtility.SetDirty(_gridData);
        }

        private void DrawPalette()
        {
            EditorGUILayout.LabelField("Palette (Brush Selection)", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            {
                foreach (CellType type in Enum.GetValues(typeof(CellType)))
                {
                    GUI.backgroundColor = _selectedBrush == type ? Color.green : GetColorForType(type);

                    if (GUILayout.Button(type.ToString(), GUILayout.Height(30)))
                    {
                        _selectedBrush = type;
                    }
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawGrid()
        {
            EditorGUILayout.LabelField("Grid Editor", EditorStyles.boldLabel);

            for (var y = _gridData.Height - 1; y >= 0; y--)
            {
                EditorGUILayout.BeginHorizontal();
                for (var x = 0; x < _gridData.Width; x++)
                {
                    var index = y * _gridData.Width + x;
                    var currentType = _gridData.Grid[index];
                    GUI.backgroundColor = GetColorForType(currentType);
                    if (GUILayout.Button(currentType.ToString(), GUILayout.Width(CELL_SIZE), GUILayout.Height(CELL_SIZE)))
                    {
                        Undo.RecordObject(_gridData, "Paint Grid Cell");
                        _gridData.Grid[index] = _selectedBrush;
                        EditorUtility.SetDirty(_gridData);
                    }
                }

                EditorGUILayout.EndHorizontal();
            }
        }

        private static Color GetColorForType(CellType type)
        {
            return CellTypePalette.GetColor(type);
        }
    }
}