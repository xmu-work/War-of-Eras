using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using WarOfEras.Battle.Core;

namespace WarOfEras.MainMenu
{
    public sealed class MainMenuController : MonoBehaviour
    {
        private const string MainMenuSceneName = "MainMenu";
        private const string BattleSceneName = "Battle";
        private const string CanvasName = "Main Menu Canvas";
        private const string GameTitle = "\u6218\u7ebf\u8fdb\u5316\uff1a\u7eaa\u5143\u4e4b\u6218";

        private static Sprite whiteSprite;

        private readonly List<DifficultyButtonBinding> difficultyButtons = new List<DifficultyButtonBinding>();

        private Font uiFont;
        private RectTransform contentRoot;
        private Button startButton;
        private Button pulseButton;
        private Image pulseButtonImage;
        private Image leftLine;
        private Image centerLine;
        private Image rightLine;
        private BattleMapDefinition selectedMap;
        private GameDifficulty selectedDifficulty = GameDifficulty.Normal;
        private bool hasSelectedDifficulty;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void CreateForMainMenuScene()
        {
            if (SceneManager.GetActiveScene().name != MainMenuSceneName)
            {
                return;
            }

            if (FindFirstObjectByType<MainMenuController>() != null)
            {
                return;
            }

            new GameObject("Main Menu Controller").AddComponent<MainMenuController>();
        }

        private void Awake()
        {
            if (FindObjectsByType<MainMenuController>(FindObjectsSortMode.None).Length > 1)
            {
                Destroy(gameObject);
                return;
            }

            uiFont = CreateUiFont();
            selectedMap = GameSession.SelectedMap;
            EnsureEventSystem();
            BuildMenu();
            ShowHomeScreen();
        }

        private void Update()
        {
            var pulse = 0.5f + Mathf.Sin(Time.time * 2.2f) * 0.5f;
            if (pulseButtonImage != null && pulseButton != null && pulseButton.interactable)
            {
                pulseButtonImage.color = Color.Lerp(
                    new Color(0.58f, 0.15f, 0.1f, 1f),
                    new Color(0.82f, 0.31f, 0.16f, 1f),
                    pulse);
            }

            if (leftLine != null)
            {
                leftLine.fillAmount = Mathf.Lerp(0.32f, 1f, Mathf.PingPong(Time.time * 0.25f, 1f));
            }

            if (centerLine != null)
            {
                centerLine.fillAmount = Mathf.Lerp(0.42f, 1f, Mathf.PingPong(Time.time * 0.2f + 0.25f, 1f));
            }

            if (rightLine != null)
            {
                rightLine.fillAmount = Mathf.Lerp(0.36f, 1f, Mathf.PingPong(Time.time * 0.18f + 0.5f, 1f));
            }
        }

        private void BuildMenu()
        {
            var canvasObject = new GameObject(CanvasName, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);

            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            var root = CreateRect("Safe Area", canvasObject.transform);
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = Vector2.zero;
            root.offsetMax = Vector2.zero;

            BuildBackground(root);
            BuildTitle(root);

            contentRoot = CreateRect("Menu Content", root);
            contentRoot.anchorMin = new Vector2(0.12f, 0.14f);
            contentRoot.anchorMax = new Vector2(0.88f, 0.58f);
            contentRoot.offsetMin = Vector2.zero;
            contentRoot.offsetMax = Vector2.zero;
        }

        private void ShowHomeScreen()
        {
            ClearContent();
            pulseButton = null;
            pulseButtonImage = null;

            var hint = CreateText(contentRoot, "Home Hint", "\u9009\u62e9\u5730\u56fe\u540e\uff0c\u518d\u9009\u62e9\u96be\u5ea6\u5e76\u5f00\u59cb\u6e38\u620f\u3002", 24, FontStyle.Bold, TextAnchor.MiddleCenter);
            hint.color = new Color(0.85f, 0.91f, 0.82f, 1f);
            hint.rectTransform.anchorMin = new Vector2(0.14f, 0.58f);
            hint.rectTransform.anchorMax = new Vector2(0.86f, 0.74f);
            hint.rectTransform.offsetMin = Vector2.zero;
            hint.rectTransform.offsetMax = Vector2.zero;

            var chooseMapButton = CreateButton(
                contentRoot,
                "Choose Map Button",
                "\u9009\u62e9\u5730\u56fe",
                ShowMapSelectScreen,
                new Color(0.69f, 0.2f, 0.12f, 1f),
                34,
                true);
            var rect = chooseMapButton.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.34f);
            rect.anchorMax = new Vector2(0.5f, 0.34f);
            rect.sizeDelta = new Vector2(390f, 92f);
            rect.anchoredPosition = Vector2.zero;
        }

        private void ShowMapSelectScreen()
        {
            ClearContent();
            pulseButton = null;
            pulseButtonImage = null;

            CreateScreenTitle("\u9009\u62e9\u5730\u56fe");

            var maps = GameSession.AvailableMaps;
            for (var i = 0; i < maps.Count; i++)
            {
                CreateMapCard(maps[i], i);
            }

            CreateBackButton(ShowHomeScreen);
        }

        private void SelectMap(BattleMapDefinition map)
        {
            selectedMap = map;
            GameSession.SelectMap(map);
            hasSelectedDifficulty = false;
            ShowDifficultyScreen();
        }

        private void ShowDifficultyScreen()
        {
            ClearContent();
            pulseButton = null;
            pulseButtonImage = null;
            difficultyButtons.Clear();

            CreateScreenTitle("\u9009\u62e9\u6e38\u620f\u96be\u5ea6");

            var selectedMapText = CreateText(
                contentRoot,
                "Selected Map",
                "\u5df2\u9009\u5730\u56fe\uff1a" + selectedMap.DisplayName,
                22,
                FontStyle.Bold,
                TextAnchor.MiddleCenter);
            selectedMapText.color = new Color(0.85f, 0.91f, 0.82f, 1f);
            selectedMapText.rectTransform.anchorMin = new Vector2(0.14f, 0.68f);
            selectedMapText.rectTransform.anchorMax = new Vector2(0.86f, 0.8f);
            selectedMapText.rectTransform.offsetMin = Vector2.zero;
            selectedMapText.rectTransform.offsetMax = Vector2.zero;

            CreateDifficultyButton(GameDifficulty.Easy, "\u7b80\u5355", "\u8d44\u6e90\u66f4\u591a\n\u654c\u4eba\u66f4\u6162", 0.18f);
            CreateDifficultyButton(GameDifficulty.Normal, "\u4e2d\u7b49", "\u6807\u51c6\u63a8\u8fdb\n\u5e73\u8861\u4f53\u9a8c", 0.4f);
            CreateDifficultyButton(GameDifficulty.Hard, "\u56f0\u96be", "\u654c\u519b\u66f4\u5f3a\n\u538b\u529b\u66f4\u9ad8", 0.62f);

            startButton = CreateButton(
                contentRoot,
                "Start Game Button",
                "\u5f00\u59cb\u6e38\u620f",
                StartGame,
                new Color(0.69f, 0.2f, 0.12f, 1f),
                32,
                true);
            var startRect = startButton.GetComponent<RectTransform>();
            startRect.anchorMin = new Vector2(0.5f, 0.13f);
            startRect.anchorMax = new Vector2(0.5f, 0.13f);
            startRect.sizeDelta = new Vector2(330f, 76f);
            startRect.anchoredPosition = Vector2.zero;

            CreateBackButton(ShowMapSelectScreen);
            RefreshDifficultyButtons();
        }

        private void SelectDifficulty(GameDifficulty difficulty)
        {
            selectedDifficulty = difficulty;
            hasSelectedDifficulty = true;
            GameSession.SelectDifficulty(difficulty);
            RefreshDifficultyButtons();
        }

        private void StartGame()
        {
            if (!hasSelectedDifficulty)
            {
                return;
            }

            GameSession.SelectMap(selectedMap);
            GameSession.SelectDifficulty(selectedDifficulty);
            startButton.interactable = false;
            SceneManager.LoadScene(BattleSceneName);
        }

        private void CreateScreenTitle(string value)
        {
            var title = CreateText(contentRoot, "Screen Title", value, 34, FontStyle.Bold, TextAnchor.MiddleCenter);
            title.color = new Color(0.94f, 0.9f, 0.82f, 1f);
            title.rectTransform.anchorMin = new Vector2(0f, 0.84f);
            title.rectTransform.anchorMax = new Vector2(1f, 1f);
            title.rectTransform.offsetMin = Vector2.zero;
            title.rectTransform.offsetMax = Vector2.zero;
        }

        private void CreateMapCard(BattleMapDefinition map, int index)
        {
            var card = CreatePanel("Map Card " + index, contentRoot, new Color(0.1f, 0.12f, 0.1f, 0.94f));
            card.anchorMin = new Vector2(0.22f, 0.18f);
            card.anchorMax = new Vector2(0.78f, 0.72f);
            card.offsetMin = Vector2.zero;
            card.offsetMax = Vector2.zero;

            var button = card.gameObject.AddComponent<Button>();
            button.targetGraphic = card.GetComponent<Image>();
            button.onClick.AddListener(() => SelectMap(map));
            SetButtonColor(button, new Color(0.1f, 0.12f, 0.1f, 0.94f));

            var thumbnailRect = CreatePanel("Map Thumbnail", card, Color.white);
            thumbnailRect.anchorMin = new Vector2(0.04f, 0.22f);
            thumbnailRect.anchorMax = new Vector2(0.96f, 0.92f);
            thumbnailRect.offsetMin = Vector2.zero;
            thumbnailRect.offsetMax = Vector2.zero;

            var thumbnail = thumbnailRect.GetComponent<Image>();
            thumbnail.sprite = LoadResourceSprite(map.ResourcePath);
            thumbnail.preserveAspect = true;

            var label = CreateText(card, "Map Label", map.DisplayName, 24, FontStyle.Bold, TextAnchor.MiddleCenter);
            label.color = new Color(0.95f, 0.88f, 0.66f, 1f);
            label.rectTransform.anchorMin = new Vector2(0.04f, 0.09f);
            label.rectTransform.anchorMax = new Vector2(0.96f, 0.22f);
            label.rectTransform.offsetMin = Vector2.zero;
            label.rectTransform.offsetMax = Vector2.zero;

            var description = CreateText(card, "Map Description", map.Description, 16, FontStyle.Normal, TextAnchor.MiddleCenter);
            description.color = new Color(0.78f, 0.83f, 0.74f, 1f);
            description.rectTransform.anchorMin = new Vector2(0.06f, 0.01f);
            description.rectTransform.anchorMax = new Vector2(0.94f, 0.1f);
            description.rectTransform.offsetMin = Vector2.zero;
            description.rectTransform.offsetMax = Vector2.zero;
        }

        private void CreateDifficultyButton(GameDifficulty difficulty, string label, string detail, float xMin)
        {
            var button = CreateButton(
                contentRoot,
                label + " Difficulty",
                label + "\n" + detail,
                () => SelectDifficulty(difficulty),
                new Color(0.14f, 0.16f, 0.13f, 1f),
                23,
                false);
            var rect = button.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(xMin, 0.34f);
            rect.anchorMax = new Vector2(xMin + 0.2f, 0.6f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            difficultyButtons.Add(new DifficultyButtonBinding(button, difficulty));
        }

        private void CreateBackButton(UnityEngine.Events.UnityAction onClick)
        {
            var button = CreateButton(contentRoot, "Back Button", "\u8fd4\u56de", onClick, new Color(0.13f, 0.15f, 0.14f, 1f), 22, false);
            var rect = button.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.02f, 0.02f);
            rect.anchorMax = new Vector2(0.16f, 0.14f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private void RefreshDifficultyButtons()
        {
            for (var i = 0; i < difficultyButtons.Count; i++)
            {
                var binding = difficultyButtons[i];
                var selected = hasSelectedDifficulty && binding.Difficulty == selectedDifficulty;
                SetButtonColor(
                    binding.Button,
                    selected
                        ? new Color(0.48f, 0.34f, 0.12f, 1f)
                        : new Color(0.14f, 0.16f, 0.13f, 1f));
            }

            if (startButton != null)
            {
                startButton.interactable = hasSelectedDifficulty;
            }
        }

        private void ClearContent()
        {
            if (contentRoot == null)
            {
                return;
            }

            for (var i = contentRoot.childCount - 1; i >= 0; i--)
            {
                Destroy(contentRoot.GetChild(i).gameObject);
            }
        }

        private void BuildBackground(RectTransform root)
        {
            var background = CreatePanel("Background", root, new Color(0.055f, 0.063f, 0.058f, 1f));
            background.anchorMin = Vector2.zero;
            background.anchorMax = Vector2.one;
            background.offsetMin = Vector2.zero;
            background.offsetMax = Vector2.zero;

            var horizon = CreatePanel("Frontline Band", root, new Color(0.1f, 0.12f, 0.11f, 0.96f));
            horizon.anchorMin = new Vector2(0f, 0.28f);
            horizon.anchorMax = new Vector2(1f, 0.36f);
            horizon.offsetMin = Vector2.zero;
            horizon.offsetMax = Vector2.zero;

            leftLine = CreateLine(root, "Upper Frontline", new Vector2(0.08f, 0.69f), new Vector2(0.46f, 0.705f), new Color(0.77f, 0.22f, 0.13f, 0.95f));
            centerLine = CreateLine(root, "Middle Frontline", new Vector2(0.18f, 0.49f), new Vector2(0.82f, 0.505f), new Color(0.8f, 0.66f, 0.28f, 0.95f));
            rightLine = CreateLine(root, "Lower Frontline", new Vector2(0.54f, 0.2f), new Vector2(0.92f, 0.215f), new Color(0.17f, 0.56f, 0.56f, 0.95f));

            CreateNode(root, "Left Base Node", new Vector2(0.14f, 0.35f), new Color(0.79f, 0.25f, 0.15f, 1f));
            CreateNode(root, "Center Control Node", new Vector2(0.5f, 0.35f), new Color(0.82f, 0.68f, 0.29f, 1f));
            CreateNode(root, "Right Base Node", new Vector2(0.86f, 0.35f), new Color(0.17f, 0.58f, 0.58f, 1f));
        }

        private void BuildTitle(RectTransform root)
        {
            var title = CreateText(root, "Game Title", GameTitle, 72, FontStyle.Bold, TextAnchor.MiddleCenter);
            title.color = new Color(0.94f, 0.9f, 0.82f, 1f);
            title.resizeTextForBestFit = true;
            title.resizeTextMinSize = 36;
            title.resizeTextMaxSize = 72;
            title.rectTransform.anchorMin = new Vector2(0.08f, 0.68f);
            title.rectTransform.anchorMax = new Vector2(0.92f, 0.84f);
            title.rectTransform.offsetMin = Vector2.zero;
            title.rectTransform.offsetMax = Vector2.zero;

            var subtitle = CreateText(root, "English Title", "FRONTLINE EVOLUTION", 22, FontStyle.Bold, TextAnchor.MiddleCenter);
            subtitle.color = new Color(0.57f, 0.72f, 0.7f, 1f);
            subtitle.rectTransform.anchorMin = new Vector2(0.2f, 0.63f);
            subtitle.rectTransform.anchorMax = new Vector2(0.8f, 0.68f);
            subtitle.rectTransform.offsetMin = Vector2.zero;
            subtitle.rectTransform.offsetMax = Vector2.zero;
        }

        private Button CreateButton(RectTransform parent, string name, string label, UnityEngine.Events.UnityAction onClick, Color normalColor, int fontSize, bool pulse)
        {
            var buttonRect = CreatePanel(name, parent, normalColor);
            var image = buttonRect.GetComponent<Image>();

            var button = buttonRect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(onClick);
            SetButtonColor(button, normalColor);

            if (pulse)
            {
                pulseButton = button;
                pulseButtonImage = image;
            }

            var text = CreateText(buttonRect, "Label", label, fontSize, FontStyle.Bold, TextAnchor.MiddleCenter);
            text.color = new Color(1f, 0.94f, 0.84f, 1f);
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 14;
            text.resizeTextMaxSize = fontSize;
            text.rectTransform.anchorMin = Vector2.zero;
            text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.offsetMin = new Vector2(14f, 6f);
            text.rectTransform.offsetMax = new Vector2(-14f, -6f);
            return button;
        }

        private void SetButtonColor(Button button, Color normalColor)
        {
            if (button == null)
            {
                return;
            }

            var image = button.targetGraphic as Image;
            if (image != null)
            {
                image.color = normalColor;
            }

            var colors = button.colors;
            colors.normalColor = normalColor;
            colors.highlightedColor = Color.Lerp(normalColor, Color.white, 0.16f);
            colors.pressedColor = Color.Lerp(normalColor, Color.black, 0.24f);
            colors.selectedColor = Color.Lerp(normalColor, Color.white, 0.12f);
            colors.disabledColor = new Color(0.25f, 0.25f, 0.24f, 0.72f);
            button.colors = colors;
        }

        private Sprite LoadResourceSprite(string resourcePath)
        {
            var texture = Resources.Load<Texture2D>(resourcePath);
            if (texture == null)
            {
                return WhiteSprite;
            }

            return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
        }

        private Image CreateLine(RectTransform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Color color)
        {
            var line = CreatePanel(name, parent, color);
            line.anchorMin = anchorMin;
            line.anchorMax = anchorMax;
            line.offsetMin = Vector2.zero;
            line.offsetMax = Vector2.zero;

            var image = line.GetComponent<Image>();
            image.type = Image.Type.Filled;
            image.fillMethod = Image.FillMethod.Horizontal;
            image.fillAmount = 0.72f;
            return image;
        }

        private void CreateNode(RectTransform parent, string name, Vector2 anchor, Color color)
        {
            var outer = CreatePanel(name, parent, new Color(color.r, color.g, color.b, 0.22f));
            outer.anchorMin = anchor;
            outer.anchorMax = anchor;
            outer.sizeDelta = new Vector2(112f, 112f);
            outer.anchoredPosition = Vector2.zero;

            var inner = CreatePanel(name + " Core", outer, color);
            inner.anchorMin = new Vector2(0.28f, 0.28f);
            inner.anchorMax = new Vector2(0.72f, 0.72f);
            inner.offsetMin = Vector2.zero;
            inner.offsetMax = Vector2.zero;
        }

        private RectTransform CreatePanel(string name, Transform parent, Color color)
        {
            var rect = CreateRect(name, parent);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = WhiteSprite;
            image.color = color;
            return rect;
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            var rectObject = new GameObject(name, typeof(RectTransform));
            rectObject.transform.SetParent(parent, false);
            return rectObject.GetComponent<RectTransform>();
        }

        private Text CreateText(RectTransform parent, string name, string value, int size, FontStyle style, TextAnchor alignment)
        {
            var textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(parent, false);

            var text = textObject.GetComponent<Text>();
            text.font = uiFont;
            text.text = value;
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = alignment;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        private static void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null)
            {
                return;
            }

            var eventSystem = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
            eventSystem.AddComponent<InputSystemUIInputModule>();
#else
            eventSystem.AddComponent<StandaloneInputModule>();
#endif
        }

        private static Font CreateUiFont()
        {
            var font = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "SimHei", "Arial" }, 16);
            if (font != null)
            {
                return font;
            }

            return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        private static Sprite WhiteSprite
        {
            get
            {
                if (whiteSprite != null)
                {
                    return whiteSprite;
                }

                var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                texture.SetPixel(0, 0, Color.white);
                texture.Apply();
                whiteSprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f));
                return whiteSprite;
            }
        }

        private readonly struct DifficultyButtonBinding
        {
            public DifficultyButtonBinding(Button button, GameDifficulty difficulty)
            {
                Button = button;
                Difficulty = difficulty;
            }

            public Button Button { get; }
            public GameDifficulty Difficulty { get; }
        }
    }
}
