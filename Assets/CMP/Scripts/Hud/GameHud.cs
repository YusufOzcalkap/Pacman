using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CMP.Scripts.Hud
{
    /// <summary>
    /// Oyun içi arayüz: can ikonları, skor/level yazıları, level arası duyurular
    /// ve oyun sonu paneli. Tamamı koddan kurulur; sahnede ya da prefabta elle
    /// bağlanacak bir referans yoktur.
    ///
    /// Tek "kazandın" anı level bitişidir (oyun sonsuz döngülü), o yüzden kazanma
    /// paneli otomatik geçen bir duyurudur; kaybetme paneli ise oyunu durdurup
    /// yeniden başlatma butonu sunar.
    /// </summary>
    public class GameHud
    {
        private const float BannerPunchDuration = 0.35f;

        private readonly LivesDisplay _livesDisplay;
        private readonly TextMeshProUGUI _scoreText;
        private readonly TextMeshProUGUI _levelText;
        private readonly TextMeshProUGUI _bannerText;

        private readonly GameObject _gameOverPanel;
        private readonly TextMeshProUGUI _gameOverScoreText;

        public GameHud(Sprite lifeIconSprite, int maxLives, Action onRestart)
        {
            var root = new GameObject("Hud");
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            root.AddComponent<GraphicRaycaster>();

            _livesDisplay = new LivesDisplay(root.transform, lifeIconSprite, maxLives);

            _scoreText = CreateText(root.transform, "ScoreText", new Vector2(1f, 1f),
                new Vector2(-24f, -24f), 44f, TextAlignmentOptions.TopRight);
            _levelText = CreateText(root.transform, "LevelText", new Vector2(1f, 1f),
                new Vector2(-24f, -76f), 30f, TextAlignmentOptions.TopRight);

            _bannerText = CreateText(root.transform, "Banner", new Vector2(0.5f, 0.5f),
                new Vector2(0f, 140f), 64f, TextAlignmentOptions.Center);
            _bannerText.color = new Color(1f, 0.85f, 0.2f);
            _bannerText.gameObject.SetActive(false);

            _gameOverPanel = CreateGameOverPanel(root.transform, onRestart, out _gameOverScoreText);
        }

        public void SetLives(int lives)
        {
            _livesDisplay.SetLives(lives);
        }

        public void SetScore(int score)
        {
            _scoreText.text = $"SCORE  {score}";
        }

        public void SetLevel(int levelNumber)
        {
            _levelText.text = $"LEVEL  {levelNumber}";
        }

        /// <summary>Ortada kısa duyuru: "READY!" ya da "LEVEL X TAMAMLANDI!".</summary>
        public void ShowBanner(string message)
        {
            _bannerText.text = message;
            _bannerText.gameObject.SetActive(true);

            _bannerText.transform.DOKill();
            _bannerText.transform.localScale = Vector3.zero;
            _bannerText.transform
                .DOScale(1f, BannerPunchDuration)
                .SetEase(Ease.OutBack)
                .SetLink(_bannerText.gameObject);
        }

        public void HideBanner()
        {
            _bannerText.gameObject.SetActive(false);
        }

        public void ShowGameOverPanel(int finalScore)
        {
            HideBanner();
            _gameOverScoreText.text = $"SCORE  {finalScore}";
            _gameOverPanel.SetActive(true);
        }

        // --- Kurulum -----------------------------------------------------------------

        private static GameObject CreateGameOverPanel(Transform parent, Action onRestart,
            out TextMeshProUGUI scoreText)
        {
            var panel = new GameObject("GameOverPanel");
            panel.transform.SetParent(parent, false);

            // Yarı saydam karartma; raycast hedefi olduğu için alttaki yön butonlarını da kilitler.
            var background = panel.AddComponent<Image>();
            background.color = new Color(0f, 0f, 0f, 0.8f);
            var backgroundRect = background.rectTransform;
            backgroundRect.anchorMin = Vector2.zero;
            backgroundRect.anchorMax = Vector2.one;
            backgroundRect.sizeDelta = Vector2.zero;

            var title = CreateText(panel.transform, "Title", new Vector2(0.5f, 0.5f),
                new Vector2(0f, 160f), 76f, TextAlignmentOptions.Center);
            title.text = "GAME OVER";
            title.color = new Color(1f, 0.3f, 0.3f);

            scoreText = CreateText(panel.transform, "Score", new Vector2(0.5f, 0.5f),
                new Vector2(0f, 40f), 48f, TextAlignmentOptions.Center);

            CreateRestartButton(panel.transform, onRestart);

            panel.SetActive(false);
            return panel;
        }

        private static void CreateRestartButton(Transform parent, Action onRestart)
        {
            var buttonObject = new GameObject("RestartButton");
            buttonObject.transform.SetParent(parent, false);

            var image = buttonObject.AddComponent<Image>();
            image.color = new Color(1f, 0.85f, 0.2f);
            var rectTransform = image.rectTransform;
            rectTransform.anchorMin = rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = new Vector2(0f, -120f);
            rectTransform.sizeDelta = new Vector2(400f, 100f);

            var button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            button.onClick.AddListener(() => onRestart());

            var label = CreateText(buttonObject.transform, "Label", new Vector2(0.5f, 0.5f),
                Vector2.zero, 40f, TextAlignmentOptions.Center);
            label.text = "PLAY AGAIN";
            label.color = new Color(0.12f, 0.12f, 0.12f);
        }

        private static TextMeshProUGUI CreateText(Transform parent, string name, Vector2 anchor,
            Vector2 anchoredPosition, float fontSize, TextAlignmentOptions alignment)
        {
            var textObject = new GameObject(name);
            textObject.transform.SetParent(parent, false);

            // Font atanmıyor: TMP, ayarlarındaki varsayılan fontu kullanır.
            var text = textObject.AddComponent<TextMeshProUGUI>();
            text.fontSize = fontSize;
            text.fontStyle = FontStyles.Bold;
            text.alignment = alignment;
            text.raycastTarget = false;

            var rectTransform = text.rectTransform;
            rectTransform.anchorMin = rectTransform.anchorMax = anchor;
            rectTransform.pivot = anchor;
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.sizeDelta = new Vector2(700f, 90f);

            return text;
        }
    }
}
