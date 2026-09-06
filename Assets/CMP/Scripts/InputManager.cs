using System.Collections.Generic;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CMP.Scripts
{
    public enum Direction
    {
        None,
        Left,
        Right,
        Up,
        Down
    }

    public class InputManager : MonoBehaviour
    {
        /// <summary>
        /// Bir yön butonunun yerleşimi. Dört buton da tek prefabtan üretilir;
        /// aralarındaki fark sadece konum, ok yönü ve etiket.
        /// </summary>
        private readonly struct ButtonLayout
        {
            public readonly Direction Direction;
            public readonly string Label;
            public readonly Vector2 AnchoredPosition;

            public ButtonLayout(Direction direction, string label, Vector2 anchoredPosition)
            {
                Direction = direction;
                Label = label;
                AnchoredPosition = anchoredPosition;
            }
        }

        private static readonly ButtonLayout[] Layouts =
        {
            new(Direction.Up, "U", new Vector2(0f, -308f)),
            new(Direction.Left, "L", new Vector2(-300f, -600f)),
            new(Direction.Right, "R", new Vector2(300f, -600f)),
            new(Direction.Down, "D", new Vector2(0f, -900f)),
        };

        [ShowInInspector, ReadOnly, BoxGroup("Runtime")]
        private Direction _currentDirection;

        private readonly List<RectTransform> _buttonRects = new();
        private Canvas _canvas;

        private void Awake()
        {
            _canvas = GetComponent<Canvas>();
            CreateButtons();
        }

        /// <summary>
        /// Klavye girdisi. Butonlarla aynı tamponu beslediği için oyun tarafında
        /// hiçbir fark yaratmaz; ikisi birlikte kullanılabilir.
        ///
        /// GetKeyDown yerine GetKey kullanılıyor: tuş basılı tutulduğunda yön her karede
        /// yeniden talep edilir, böylece duvara dayanmışken beklerken köşe açılınca
        /// Pacman kendiliğinden döner.
        /// </summary>
        private void Update()
        {
            var keyboardDirection = ReadKeyboardDirection();
            if (keyboardDirection != Direction.None)
            {
                _currentDirection = keyboardDirection;
            }
        }

        private static Direction ReadKeyboardDirection()
        {
            if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A))
            {
                return Direction.Left;
            }

            if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D))
            {
                return Direction.Right;
            }

            if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W))
            {
                return Direction.Up;
            }

            if (Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S))
            {
                return Direction.Down;
            }

            return Direction.None;
        }

        private void CreateButtons()
        {
            foreach (var layout in Layouts)
            {
                // worldPositionStays: false — canvas ölçeği bu anda henüz kesinleşmemiş
                // olabileceği için prefabın kendi yerel değerleri korunmalı.
                var button = Instantiate(AssetDatabase.Instance.DirectionButtonPrefab, transform, false);
                button.name = layout.Direction.ToString();

                var rectTransform = (RectTransform)button.transform;
                rectTransform.anchoredPosition = layout.AnchoredPosition;

                // Ok sprite'ı yukarı bakıyor; ToQuaternion da yukarı referanslı olduğu için
                // dört yön tek sprite'tan döndürülerek elde ediliyor.
                rectTransform.localRotation = layout.Direction.ToQuaternion();
                _buttonRects.Add(rectTransform);

                var label = button.GetComponentInChildren<TextMeshProUGUI>();
                label.text = layout.Label;
                // Buton döndüğü için etiketi dünya uzayında sıfırlayıp dik tutuyoruz.
                label.transform.rotation = Quaternion.identity;

                var direction = layout.Direction;
                var buttonComponent = button.GetComponent<Button>();
                buttonComponent.onClick.AddListener(() => _currentDirection = direction);

                // Ok tuşları EventSystem'in buton seçimini gezdirmesin: tıklanan buton
                // "seçili" kalıyor ve sonraki ok tuşları UI navigasyonuna karışıyordu.
                buttonComponent.navigation = new Navigation { mode = Navigation.Mode.None };
            }
        }

        public Direction ConsumeInput()
        {
            var dir = _currentDirection;
            _currentDirection = Direction.None;
            return dir;
        }

        /// <summary>
        /// Butonların ekranın altından ne kadarını kapladığını oran olarak döner.
        /// Kamera oyun alanını bu bandın üstüne oturtmak için kullanır.
        ///
        /// Canvas "Constant Pixel Size" modunda olduğu ve butonlar canvas merkezine
        /// göre konumlandığı için değeri layout geçişini beklemeden hesaplayabiliyoruz.
        /// </summary>
        public float GetOccupiedScreenHeightRatio()
        {
            var topEdge = float.MinValue;

            foreach (var rectTransform in _buttonRects)
            {
                // Buton dönmüş olabileceğinden iki kenarın büyüğünü yarıçap kabul ediyoruz.
                var halfExtent = Mathf.Max(rectTransform.rect.width, rectTransform.rect.height) * 0.5f;
                topEdge = Mathf.Max(topEdge, rectTransform.anchoredPosition.y + halfExtent);
            }

            var topEdgeScreenY = Screen.height * 0.5f + topEdge * _canvas.scaleFactor;
            return Mathf.Clamp01(topEdgeScreenY / Screen.height);
        }
    }
}
