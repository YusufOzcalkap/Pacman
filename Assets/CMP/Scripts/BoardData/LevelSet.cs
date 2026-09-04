using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace CMP.Scripts
{
    /// <summary>
    /// Bir level'ın tanımı: haritası ve zorluk ayarları.
    /// Hız değerleri çarpandır; 1 = temel hız, 1.2 = %20 daha hızlı.
    /// </summary>
    [Serializable]
    public class LevelDefinition
    {
        [Required, TableColumnWidth(160)]
        public GridData Grid;

        [Range(0.5f, 2f)]
        public float PacmanSpeedMultiplier = 1f;

        [Range(0.5f, 2f)]
        public float GhostSpeedMultiplier = 1f;

        [SuffixLabel("sn", true), MinValue(0f)]
        public float FrightenedDuration = GameSettings.FrightenedDuration;

        [Range(1, 8)]
        public int GhostCount = GameSettings.AiCharacterCount;
    }

    /// <summary>
    /// Oyunun level sıralaması. Level tamamlandığında listedeki bir sonraki harita
    /// yüklenir; liste bittiğinde başa dönülür. Zorluk eğrisi tamamen buradan,
    /// kod değişmeden ayarlanır.
    /// </summary>
    [CreateAssetMenu(menuName = "PacMan/Level Set", fileName = "LevelSet")]
    public class LevelSet : ScriptableObject
    {
        [TableList(AlwaysExpanded = true)]
        public List<LevelDefinition> Levels = new();

        public LevelDefinition GetLevel(int levelIndex)
        {
            return Levels[levelIndex % Levels.Count];
        }
    }
}
