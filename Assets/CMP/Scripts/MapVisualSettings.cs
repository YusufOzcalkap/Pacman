using UnityEngine;

namespace CMP.Scripts
{
    [CreateAssetMenu(menuName = "PacMan/Map Visual Settings", fileName = "MapVisualSettings")]
    public class MapVisualSettings : ScriptableObject
    {
        [Header("Resolution")]
        [Min(1)]
        public int pixelsPerCell = 16;

        [Header("Colors")]
        public Color backgroundColor = Color.black;
        public Color wallColor = new Color(0.2f, 0.5f, 1.0f);
        public Color gateColor = Color.white;

        [Header("Wall Style")]
        [Tooltip("Thickness of wall outlines in pixels.")]
        [Range(1, 32)]
        public int wallThickness = 4;

        [Header("AI Gate Style")]
        [Tooltip("Thickness of the AI gate line in pixels.")]
        [Range(1, 32)]
        public int gateThickness = 2;

        [Header("Pellets")]
        public Color pelletColor = new Color(1f, 0.85f, 0.62f);
        [Tooltip("Radius of a normal pellet in pixels.")]
        [Range(1, 32)]
        public int pelletRadius = 2;
        [Tooltip("Radius of a power pellet in pixels.")]
        [Range(1, 32)]
        public int powerPelletRadius = 6;

        [Header("Outer Border")]
        public bool drawOuterBorder = true;
        public Color outerBorderColor = new Color(0.2f, 0.5f, 1.0f);
        [Range(1, 32)]
        public int outerBorderThickness = 4;
    }
}