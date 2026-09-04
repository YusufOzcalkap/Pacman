using UnityEngine;

namespace CMP.Scripts.Helper
{
    /// <summary>
    /// Yemler için koddan daire sprite'ı üretir.
    /// Harita görselinde olduğu gibi burada da hazır asset kullanmıyoruz: yem boyutu
    /// ve rengi MapVisualSettings'ten geldiği için tek ayar dosyasından yönetilebiliyor.
    /// </summary>
    public static class CircleSpriteFactory
    {
        public static Sprite Create(int radiusInPixels, Color color, float pixelsPerUnit)
        {
            var size = radiusInPixels * 2;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            var pixels = new Color[size * size];
            var center = radiusInPixels - 0.5f;
            var radiusSquared = radiusInPixels * radiusInPixels;

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var offsetX = x - center;
                    var offsetY = y - center;
                    var isInside = offsetX * offsetX + offsetY * offsetY <= radiusSquared;
                    pixels[y * size + x] = isInside ? color : Color.clear;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();

            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), pixelsPerUnit);
        }
    }
}
