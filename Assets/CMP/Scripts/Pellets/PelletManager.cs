using System.Collections.Generic;
using CMP.Scripts.Helper;
using DG.Tweening;
using UnityEngine;

namespace CMP.Scripts.Pellets
{
    /// <summary>
    /// Haritadaki yemleri oluşturur ve toplanmalarını yönetir.
    ///
    /// Normal yemler bütün Empty hücrelere otomatik dağıtılır; editörde yüzlerce hücreyi
    /// tek tek boyamak gerekmez. Güçlendirme yemleri ise az sayıda olduğu için
    /// <see cref="CellType.PowerPellet"/> ile elle işaretlenir.
    ///
    /// Yemler yalnızca Pacman'in ulaşabildiği hücrelere konur; aksi halde kapalı bir cebe
    /// düşen tek bir yem level'ın asla tamamlanamamasına yol açardı.
    ///
    /// Görsel Pellet / PowerPellet prefablarından gelir. Prefabın sprite alanı boş
    /// bırakılırsa MapVisualSettings'teki renk ve yarıçapla koddan bir daire üretilir;
    /// prefaba sprite atanırsa o kullanılır. Böylece hem hazır çalışır hem de görseli
    /// editörden değiştirmek mümkün olur.
    /// </summary>
    public class PelletManager
    {
        private const float ConsumeAnimationDuration = 0.12f;
        private const float PowerPelletPulseDuration = 0.5f;
        private const float PowerPelletPulseScale = 0.75f;

        private readonly Dictionary<Vector2Int, PelletInfo> _pellets = new();

        public int RemainingCount => _pellets.Count;
        public int TotalCount { get; }

        public PelletManager(GridData gridData, Vector2Int pacmanStartCell, MapVisualSettings settings,
            Transform parent)
        {
            var root = new GameObject("Pellets").transform;
            root.SetParent(parent);

            var pelletSprite = CircleSpriteFactory.Create(settings.pelletRadius, settings.pelletColor,
                settings.pixelsPerCell);
            var powerPelletSprite = CircleSpriteFactory.Create(settings.powerPelletRadius, settings.pelletColor,
                settings.pixelsPerCell);

            var reachableCells = Pathfinder.GetReachableCells(gridData, pacmanStartCell,
                GameSettings.PacmanWalkableCells);

            foreach (var cell in reachableCells)
            {
                var cellType = gridData.GetCellAt(cell);
                if (cellType is not (CellType.Empty or CellType.PowerPellet))
                {
                    continue;
                }

                var isPowerPellet = cellType == CellType.PowerPellet;
                var prefab = isPowerPellet
                    ? AssetDatabase.Instance.PowerPelletPrefab
                    : AssetDatabase.Instance.PelletPrefab;

                Create(cell, prefab, isPowerPellet, isPowerPellet ? powerPelletSprite : pelletSprite, root);
            }

            TotalCount = _pellets.Count;
        }

        /// <summary>
        /// Verilen hücredeki yemi toplar. Yem yoksa false döner, böylece her karede
        /// güvenle çağrılabilir.
        /// </summary>
        public bool TryConsume(Vector2Int cell, out int score, out bool isPowerPellet)
        {
            score = 0;
            isPowerPellet = false;

            if (!_pellets.TryGetValue(cell, out var pellet))
            {
                return false;
            }

            _pellets.Remove(cell);
            score = pellet.IsPowerPellet ? GameSettings.PowerPelletScore : GameSettings.PelletScore;
            isPowerPellet = pellet.IsPowerPellet;

            pellet.Transform.DOKill();
            pellet.Transform
                .DOScale(0f, ConsumeAnimationDuration)
                .SetEase(Ease.InBack)
                .OnComplete(() => Object.Destroy(pellet.Transform.gameObject));

            return true;
        }

        private void Create(Vector2Int cell, GameObject prefab, bool isPowerPellet, Sprite fallbackSprite,
            Transform root)
        {
            var pelletObject = Object.Instantiate(prefab, cell.ToWorldPosition(), Quaternion.identity, root);
            pelletObject.name = $"{prefab.name}_{cell.x}_{cell.y}";

            var spriteRenderer = pelletObject.GetComponent<SpriteRenderer>();
            if (spriteRenderer.sprite == null)
            {
                spriteRenderer.sprite = fallbackSprite;
            }

            if (isPowerPellet)
            {
                // Güçlendirme yemleri nefes alıp verir; oyuncunun gözü hemen buraya gider.
                pelletObject.transform
                    .DOScale(pelletObject.transform.localScale * PowerPelletPulseScale, PowerPelletPulseDuration)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetLink(pelletObject);
            }

            _pellets.Add(cell, new PelletInfo(pelletObject.transform, isPowerPellet));
        }

        private readonly struct PelletInfo
        {
            public readonly Transform Transform;
            public readonly bool IsPowerPellet;

            public PelletInfo(Transform transform, bool isPowerPellet)
            {
                Transform = transform;
                IsPowerPellet = isPowerPellet;
            }
        }
    }
}
