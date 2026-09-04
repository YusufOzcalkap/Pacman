using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CMP.Scripts.Hud
{
    /// <summary>
    /// Ekranın sol üstünde kalan canları küçük Pacman ikonlarıyla gösterir.
    ///
    /// İkon sprite'ı Pacman'in kendi görselinden alınır: ayrı bir asset gerekmez ve
    /// karakterin görünümü değişirse gösterge kendiliğinden ona uyar.
    /// İkonlar bir kez yaratılır; can azaldıkça yok edilmez, kapatılır — böylece
    /// can yeniden kazanma gibi bir mekanik eklenirse tekrar açmak yeterli olur.
    /// </summary>
    public class LivesDisplay
    {
        private const float IconSize = 64f;
        private const float IconSpacing = 12f;
        private static readonly Vector2 Margin = new(24f, -24f);

        private readonly List<Image> _icons = new();

        public LivesDisplay(Sprite iconSprite, int maxLives)
        {
            var root = new GameObject("Hud");
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            for (var i = 0; i < maxLives; i++)
            {
                var iconObject = new GameObject($"Life_{i}");
                iconObject.transform.SetParent(root.transform, false);

                var image = iconObject.AddComponent<Image>();
                image.sprite = iconSprite;
                image.raycastTarget = false;

                var rectTransform = image.rectTransform;
                rectTransform.anchorMin = rectTransform.anchorMax = new Vector2(0f, 1f);
                rectTransform.pivot = new Vector2(0f, 1f);
                rectTransform.sizeDelta = new Vector2(IconSize, IconSize);
                rectTransform.anchoredPosition = Margin + new Vector2(i * (IconSize + IconSpacing), 0f);

                _icons.Add(image);
            }
        }

        public void SetLives(int lives)
        {
            for (var i = 0; i < _icons.Count; i++)
            {
                _icons[i].enabled = i < lives;
            }
        }
    }
}
