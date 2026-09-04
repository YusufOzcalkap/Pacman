using CMP.Scripts;
using UnityEngine;

namespace CMP.Editor
{
    /// <summary>
    /// Hücre tiplerinin editördeki renkleri. Hem inspector'daki hızlı düzenleyici
    /// hem de Level Editor penceresi aynı paleti kullanır; renkler tek yerde durur.
    /// </summary>
    public static class CellTypePalette
    {
        public static Color GetColor(CellType type)
        {
            switch (type)
            {
                case CellType.Empty: return new Color(0.16f, 0.16f, 0.16f);
                case CellType.Wall: return new Color(0.15f, 0.3f, 0.85f);
                case CellType.AiSpawnZone: return new Color(0.8f, 0.15f, 0.15f);
                case CellType.AiGate: return new Color(0.1f, 0.75f, 0.75f);
                case CellType.Pacman: return new Color(0.95f, 0.85f, 0.2f);
                case CellType.JoinGameCell: return new Color(0.25f, 0.7f, 0.35f);
                case CellType.PowerPellet: return new Color(0.95f, 0.6f, 0.2f);
                default: return new Color(0.5f, 0.5f, 0.5f);
            }
        }

        /// <summary>Palet butonlarında kullanılan kısa etiketler.</summary>
        public static string GetShortLabel(CellType type)
        {
            switch (type)
            {
                case CellType.Empty: return "Empty";
                case CellType.Wall: return "Wall";
                case CellType.AiSpawnZone: return "Spawn";
                case CellType.AiGate: return "Gate";
                case CellType.Pacman: return "Pacman";
                case CellType.JoinGameCell: return "Join";
                case CellType.PowerPellet: return "Power";
                default: return type.ToString();
            }
        }
    }
}
