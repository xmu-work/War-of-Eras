using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using WarOfEras.Battle.Core;

namespace WarOfEras.MainMenu
{
    public sealed partial class MainMenuController : MonoBehaviour
    {
        private const string MainMenuSceneName = "MainMenu";
        private const string BattleSceneName = "Battle";
        private const string CanvasName = "Main Menu Canvas";
        private const string MenuBackgroundResource = "MainMenu/Background";
        private const string TitleLogoResource = "MainMenu/TitleLogo";
        private const string FallbackBackgroundResource = "Battle/Maps/PixelFrontline_Barbarian";

        private static readonly Color ButtonNormalTint = new Color(0.76f, 0.9f, 1f, 1f);
        private static readonly Color ButtonHoverTint = new Color(1f, 1f, 1f, 1f);
        private static readonly Color PrimaryButtonTint = new Color(1f, 1f, 1f, 1f);
        private static readonly Color SelectedButtonTint = new Color(0.62f, 1f, 0.78f, 1f);
        private static readonly Color TextCyan = new Color(0.42f, 0.88f, 1f, 1f);
        private static readonly Color TextLavender = new Color(0.9f, 0.78f, 1f, 1f);

        private static readonly string[] EraNames =
        {
            "\u86ee\u8352\u90e8\u843d",
            "\u673a\u68b0\u5de5\u574a",
            "\u7535\u529b\u65f6\u4ee3",
            "\u6838\u80fd\u7eaa\u5143",
            "\u661f\u6d77\u6587\u660e"
        };

        private static readonly Color[] EraColors =
        {
            new Color(1f, 0.42f, 0.16f, 1f),
            new Color(0.98f, 0.66f, 0.28f, 1f),
            new Color(0.35f, 0.82f, 1f, 1f),
            new Color(0.47f, 1f, 0.36f, 1f),
            new Color(0.72f, 0.36f, 1f, 1f)
        };

        private static Sprite whiteSprite;
        private static Sprite menuButtonSprite;
        private static Sprite primaryButtonSprite;
        private static Sprite panelSprite;
        private static Sprite topFadeSprite;
        private static Sprite bottomFadeSprite;
        private static Sprite dotSprite;

        private readonly List<DifficultyButtonBinding> difficultyButtons = new List<DifficultyButtonBinding>();

        private Font uiFont;
        private RectTransform contentRoot;
        private RectTransform utilityRoot;
        private Button startButton;
        private Image titleAura;
        private BattleMapDefinition selectedMap;
        private GameDifficulty selectedDifficulty = GameDifficulty.Normal;
        private bool hasSelectedDifficulty = true;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void CreateForMainMenuScene()
        {
            // 允许空场景直接运行：只要当前场景叫 MainMenu，就自动补一个菜单控制器。
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
            selectedDifficulty = GameSession.Difficulty;
            hasSelectedDifficulty = true;
            EnsureEventSystem();
            BuildMenu();
            ShowHomeScreen();
        }

        private void Update()
        {
            var pulse = 0.5f + Mathf.Sin(Time.time * 2.2f) * 0.5f;
            if (titleAura != null)
            {
                titleAura.color = Color.Lerp(
                    new Color(0.12f, 0.62f, 1f, 0.12f),
                    new Color(0.62f, 0.28f, 1f, 0.24f),
                    pulse);
            }

        }

        private void BuildMenu()
        {
            // 主菜单完全由脚本生成，场景中只需要挂载 MainMenuController。
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
            BuildEraTimeline(root);

            contentRoot = CreateRect("Menu Content", root);
            SetContentBounds(0.32f, 0.1f, 0.68f, 0.58f);

            utilityRoot = CreateRect("Menu Utility Actions", root);
            utilityRoot.anchorMin = Vector2.zero;
            utilityRoot.anchorMax = Vector2.one;
            utilityRoot.offsetMin = Vector2.zero;
            utilityRoot.offsetMax = Vector2.zero;
        }

        private void ShowHomeScreen()
        {
            // 每次切屏都重建内容区，避免不同菜单页面的按钮和状态残留。
            ClearContent();
            difficultyButtons.Clear();
            startButton = null;
            SetContentBounds(0.365f, 0.15f, 0.635f, 0.53f);

            CreateHomeMenuButton("\u5f00\u59cb\u6e38\u620f", StartGame, 0.76f, true);
            CreateHomeMenuButton("\u5730\u56fe\u9009\u62e9\uff1a" + selectedMap.DisplayName, ShowMapSelectScreen, 0.5f, false);
            CreateHomeMenuButton("\u96be\u5ea6\u9009\u62e9\uff1a" + GameSession.DifficultyName, ShowDifficultyScreen, 0.24f, false);
            CreateHomeUtilities();
        }

        private void ShowMapSelectScreen()
        {
            ClearContent();
            SetContentBounds(0.2f, 0.12f, 0.8f, 0.61f);

            CreateScreenTitle("\u9009\u62e9\u5730\u56fe");

            var maps = GameSession.AvailableMaps;
            for (var i = 0; i < maps.Count; i++)
            {
                CreateMapCard(maps[i], i, maps.Count);
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
            difficultyButtons.Clear();
            SetContentBounds(0.2f, 0.12f, 0.8f, 0.61f);

            CreateScreenTitle("\u9009\u62e9\u6e38\u620f\u96be\u5ea6");

            var selectedMapText = CreateText(
                contentRoot,
                "Selected Map",
                "\u5df2\u9009\u5730\u56fe\uff1a" + selectedMap.DisplayName,
                22,
                FontStyle.Bold,
                TextAnchor.MiddleCenter);
            selectedMapText.color = new Color(0.82f, 0.93f, 1f, 1f);
            selectedMapText.rectTransform.anchorMin = new Vector2(0.14f, 0.68f);
            selectedMapText.rectTransform.anchorMax = new Vector2(0.86f, 0.8f);
            selectedMapText.rectTransform.offsetMin = Vector2.zero;
            selectedMapText.rectTransform.offsetMax = Vector2.zero;

            CreateDifficultyButton(GameDifficulty.Easy, "\u7b80\u5355", "\u8d44\u6e90\u66f4\u591a\n\u654c\u4eba\u66f4\u6162", 0.14f);
            CreateDifficultyButton(GameDifficulty.Normal, "\u4e2d\u7b49", "\u6807\u51c6\u63a8\u8fdb\n\u5e73\u8861\u4f53\u9a8c", 0.4f);
            CreateDifficultyButton(GameDifficulty.Hard, "\u56f0\u96be", "\u654c\u519b\u66f4\u5f3a\n\u538b\u529b\u66f4\u9ad8", 0.66f);

            startButton = CreateButton(
                contentRoot,
                "Start Game Button",
                "\u5f00\u59cb\u6e38\u620f",
                StartGame,
                PrimaryButtonTint,
                32,
                true);
            var startRect = startButton.GetComponent<RectTransform>();
            startRect.anchorMin = new Vector2(0.5f, 0.13f);
            startRect.anchorMax = new Vector2(0.5f, 0.13f);
            startRect.sizeDelta = new Vector2(360f, 76f);
            startRect.anchoredPosition = Vector2.zero;

            CreateBackButton(ShowHomeScreen);
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
                SelectDifficulty(GameDifficulty.Normal);
            }

            GameSession.SelectMap(selectedMap);
            GameSession.SelectDifficulty(selectedDifficulty);
            if (startButton != null)
            {
                startButton.interactable = false;
            }

            SceneManager.LoadScene(BattleSceneName);
        }

        private void ShowArchiveScreen()
        {
            ShowInfoScreen("\u5175\u5de5\u56fe\u9274", "\u5175\u79cd\u4e0e\u9632\u5fa1\u8d44\u6599\u5df2\u5f52\u6863\uff0c\u540e\u7eed\u53ef\u6269\u5c55\u4e3a\u8be6\u7ec6\u56fe\u9274\u9875\u3002");
        }

        private void ShowSettingsScreen()
        {
            ShowInfoScreen("\u6e38\u620f\u8bbe\u7f6e", "\u9ed8\u8ba4\u97f3\u6548\u4e0e\u753b\u9762\u914d\u7f6e\u5df2\u542f\u7528");
        }

        private void ShowTutorialScreen()
        {
            ShowInfoScreen("\u73a9\u6cd5\u6559\u7a0b", "\u5efa\u9020\u9632\u7ebf\u3001\u8bad\u7ec3\u5175\u79cd\u3001\u63a8\u8fdb\u7eaa\u5143\uff0c\u6467\u6bc1\u654c\u65b9\u636e\u70b9\u5373\u53ef\u83b7\u80dc\u3002");
        }

        private void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void ShowInfoScreen(string titleValue, string detail)
        {
            ClearContent();
            SetContentBounds(0.25f, 0.16f, 0.75f, 0.56f);

            CreateScreenTitle(titleValue);

            var panel = CreateStyledPanel("Info Panel", contentRoot, new Color(0.04f, 0.07f, 0.11f, 0.9f));
            panel.anchorMin = new Vector2(0.14f, 0.32f);
            panel.anchorMax = new Vector2(0.86f, 0.62f);
            panel.offsetMin = Vector2.zero;
            panel.offsetMax = Vector2.zero;

            var message = CreateText(panel, "Info Detail", detail, 24, FontStyle.Bold, TextAnchor.MiddleCenter);
            message.color = new Color(0.84f, 0.93f, 1f, 1f);
            message.rectTransform.anchorMin = new Vector2(0.08f, 0.18f);
            message.rectTransform.anchorMax = new Vector2(0.92f, 0.82f);
            message.rectTransform.offsetMin = Vector2.zero;
            message.rectTransform.offsetMax = Vector2.zero;

            CreateBackButton(ShowHomeScreen);
        }

        private void CreateHomeMenuButton(string label, UnityAction onClick, float y, bool pulse)
        {
            var button = CreateButton(
                contentRoot,
                label + " Button",
                label,
                onClick,
                pulse ? PrimaryButtonTint : ButtonNormalTint,
                26,
                pulse);
            var rect = button.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, y);
            rect.anchorMax = new Vector2(0.5f, y);
            rect.sizeDelta = pulse ? new Vector2(500f, 74f) : new Vector2(455f, 60f);
            rect.anchoredPosition = Vector2.zero;
        }

        private void CreateHomeUtilities()
        {
            CreateHomeUtilityButton("\u73a9\u6cd5\u6559\u7a0b", ShowTutorialScreen, new Vector2(0.875f, 0.47f), ButtonNormalTint, 230f);
            CreateHomeUtilityButton("\u5175\u5de5\u56fe\u9274", ShowArchiveScreen, new Vector2(0.875f, 0.385f), ButtonNormalTint, 230f);
            CreateHomeUtilityButton("\u8bbe\u7f6e", ShowSettingsScreen, new Vector2(0.875f, 0.3f), ButtonNormalTint, 230f);
            CreateHomeUtilityButton("\u9000\u51fa\u6e38\u620f", QuitGame, new Vector2(0.895f, 0.12f), new Color(1f, 0.72f, 0.66f, 1f), 210f);
        }

        private void CreateHomeUtilityButton(string label, UnityAction onClick, Vector2 anchor, Color tint, float width)
        {
            if (utilityRoot == null)
            {
                return;
            }

            var button = CreateButton(
                utilityRoot,
                label + " Utility Button",
                label,
                onClick,
                tint,
                20,
                false,
                false);
            var rect = button.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.sizeDelta = new Vector2(width, 52f);
            rect.anchoredPosition = Vector2.zero;
        }

        private void CreateScreenTitle(string value)
        {
            var shadow = CreateText(contentRoot, "Screen Title Shadow", value, 36, FontStyle.Bold, TextAnchor.MiddleCenter);
            shadow.color = new Color(0f, 0f, 0f, 0.78f);
            shadow.rectTransform.anchorMin = new Vector2(0f, 0.84f);
            shadow.rectTransform.anchorMax = new Vector2(1f, 1f);
            shadow.rectTransform.offsetMin = new Vector2(3f, -5f);
            shadow.rectTransform.offsetMax = new Vector2(3f, -5f);

            var title = CreateText(contentRoot, "Screen Title", value, 36, FontStyle.Bold, TextAnchor.MiddleCenter);
            title.color = TextCyan;
            title.rectTransform.anchorMin = new Vector2(0f, 0.84f);
            title.rectTransform.anchorMax = new Vector2(1f, 1f);
            title.rectTransform.offsetMin = Vector2.zero;
            title.rectTransform.offsetMax = Vector2.zero;
        }

        private void CreateMapCard(BattleMapDefinition map, int index, int total)
        {
            var columns = Mathf.Min(Mathf.Max(total, 1), 3);
            var rows = Mathf.CeilToInt(total / (float)columns);
            var column = index % columns;
            var row = index / columns;
            var gap = 0.035f;
            var cardWidth = columns == 1 ? 0.64f : (0.78f - gap * (columns - 1)) / columns;
            var cardHeight = rows == 1 ? 0.57f : (0.57f - 0.045f * (rows - 1)) / rows;
            var totalWidth = cardWidth * columns + gap * (columns - 1);
            var xMin = 0.5f - totalWidth * 0.5f + column * (cardWidth + gap);
            var yMax = 0.74f - row * (cardHeight + 0.045f);

            var card = CreateStyledPanel("Map Card " + index, contentRoot, new Color(0.04f, 0.07f, 0.11f, 0.92f));
            card.anchorMin = new Vector2(xMin, yMax - cardHeight);
            card.anchorMax = new Vector2(xMin + cardWidth, yMax);
            card.offsetMin = Vector2.zero;
            card.offsetMax = Vector2.zero;

            var button = card.gameObject.AddComponent<Button>();
            var cardImage = card.GetComponent<Image>();
            button.targetGraphic = cardImage;
            button.onClick.AddListener(() => SelectMap(map));
            AddHoverEffect(button, cardImage, ButtonNormalTint, 1.025f);
            SetButtonColor(button, selectedMap != null && selectedMap.Id == map.Id ? SelectedButtonTint : ButtonNormalTint);

            var thumbnailRect = CreatePanel("Map Thumbnail", card, Color.white);
            thumbnailRect.anchorMin = new Vector2(0.035f, 0.24f);
            thumbnailRect.anchorMax = new Vector2(0.965f, 0.92f);
            thumbnailRect.offsetMin = Vector2.zero;
            thumbnailRect.offsetMax = Vector2.zero;

            var thumbnail = thumbnailRect.GetComponent<Image>();
            thumbnail.sprite = LoadResourceSprite(map.ResourcePath);
            thumbnail.preserveAspect = true;
            thumbnail.color = new Color(0.78f, 0.88f, 1f, 1f);
            thumbnail.raycastTarget = false;

            var frame = CreatePanel("Map Thumbnail Frame", card, new Color(0.16f, 0.78f, 1f, 0.54f));
            frame.anchorMin = new Vector2(0.033f, 0.235f);
            frame.anchorMax = new Vector2(0.967f, 0.925f);
            frame.offsetMin = Vector2.zero;
            frame.offsetMax = Vector2.zero;
            frame.SetAsFirstSibling();
            frame.GetComponent<Image>().raycastTarget = false;

            var label = CreateText(card, "Map Label", map.DisplayName, 25, FontStyle.Bold, TextAnchor.MiddleCenter);
            label.color = TextCyan;
            label.rectTransform.anchorMin = new Vector2(0.04f, 0.1f);
            label.rectTransform.anchorMax = new Vector2(0.96f, 0.22f);
            label.rectTransform.offsetMin = Vector2.zero;
            label.rectTransform.offsetMax = Vector2.zero;

            var description = CreateText(card, "Map Description", map.Description, 16, FontStyle.Normal, TextAnchor.MiddleCenter);
            description.color = new Color(0.78f, 0.88f, 1f, 1f);
            description.rectTransform.anchorMin = new Vector2(0.06f, 0.015f);
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
                ButtonNormalTint,
                23,
                false,
                false);
            var rect = button.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(xMin, 0.34f);
            rect.anchorMax = new Vector2(xMin + 0.2f, 0.6f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            difficultyButtons.Add(new DifficultyButtonBinding(button, difficulty));
        }

        private void CreateBackButton(UnityAction onClick)
        {
            var button = CreateButton(contentRoot, "Back Button", "\u8fd4\u56de", onClick, ButtonNormalTint, 22, false, false);
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
                        ? SelectedButtonTint
                        : ButtonNormalTint);
            }

            if (startButton != null)
            {
                startButton.interactable = hasSelectedDifficulty;
                SetButtonColor(startButton, hasSelectedDifficulty ? PrimaryButtonTint : new Color(0.32f, 0.4f, 0.52f, 0.72f));
            }
        }

        private void ClearContent()
        {
            ClearChildren(contentRoot);
            ClearChildren(utilityRoot);
        }

        private static void ClearChildren(RectTransform root)
        {
            if (root == null)
            {
                return;
            }

            for (var i = root.childCount - 1; i >= 0; i--)
            {
                Destroy(root.GetChild(i).gameObject);
            }
        }

        private void SetContentBounds(float xMin, float yMin, float xMax, float yMax)
        {
            if (contentRoot == null)
            {
                return;
            }

            contentRoot.anchorMin = new Vector2(xMin, yMin);
            contentRoot.anchorMax = new Vector2(xMax, yMax);
            contentRoot.offsetMin = Vector2.zero;
            contentRoot.offsetMax = Vector2.zero;
        }

        private void BuildBackground(RectTransform root)
        {
            var battlefield = CreatePanel("Battlefield Background", root, Color.white);
            battlefield.anchorMin = Vector2.zero;
            battlefield.anchorMax = Vector2.one;
            battlefield.offsetMin = Vector2.zero;
            battlefield.offsetMax = Vector2.zero;

            var battlefieldImage = battlefield.GetComponent<Image>();
            battlefieldImage.sprite = LoadResourceSprite(MenuBackgroundResource, FallbackBackgroundResource);
            battlefieldImage.preserveAspect = false;
            battlefieldImage.color = Color.white;

            var wash = CreatePanel("Battlefield Wash", root, new Color(0.01f, 0.015f, 0.03f, 0.24f));
            wash.anchorMin = Vector2.zero;
            wash.anchorMax = Vector2.one;
            wash.offsetMin = Vector2.zero;
            wash.offsetMax = Vector2.zero;

            var topFade = CreatePanel("Top Smoke", root, Color.white);
            topFade.anchorMin = new Vector2(0f, 0.52f);
            topFade.anchorMax = Vector2.one;
            topFade.offsetMin = Vector2.zero;
            topFade.offsetMax = Vector2.zero;
            var topFadeImage = topFade.GetComponent<Image>();
            topFadeImage.sprite = TopFadeSprite;
            topFadeImage.color = Color.white;

            var bottomFade = CreatePanel("Bottom Smoke", root, Color.white);
            bottomFade.anchorMin = Vector2.zero;
            bottomFade.anchorMax = new Vector2(1f, 0.52f);
            bottomFade.offsetMin = Vector2.zero;
            bottomFade.offsetMax = Vector2.zero;
            var bottomFadeImage = bottomFade.GetComponent<Image>();
            bottomFadeImage.sprite = BottomFadeSprite;
            bottomFadeImage.color = Color.white;
        }

        private void BuildTitle(RectTransform root)
        {
            var titleArea = CreateRect("Title Logo Area", root);
            titleArea.anchorMin = new Vector2(0.14f, 0.57f);
            titleArea.anchorMax = new Vector2(0.86f, 0.95f);
            titleArea.offsetMin = Vector2.zero;
            titleArea.offsetMax = Vector2.zero;

            titleAura = CreatePanel("Title Aura", titleArea, new Color(0.12f, 0.62f, 1f, 0.12f)).GetComponent<Image>();
            titleAura.rectTransform.anchorMin = new Vector2(0.16f, 0.18f);
            titleAura.rectTransform.anchorMax = new Vector2(0.84f, 0.78f);
            titleAura.rectTransform.offsetMin = Vector2.zero;
            titleAura.rectTransform.offsetMax = Vector2.zero;
            titleAura.raycastTarget = false;

            var logoRect = CreatePanel("Title Logo", titleArea, Color.white);
            logoRect.anchorMin = new Vector2(0f, 0f);
            logoRect.anchorMax = Vector2.one;
            logoRect.offsetMin = Vector2.zero;
            logoRect.offsetMax = Vector2.zero;

            var logo = logoRect.GetComponent<Image>();
            logo.sprite = LoadResourceSprite(TitleLogoResource);
            logo.preserveAspect = true;
            logo.raycastTarget = false;

            var bottomRule = CreatePanel("Title Red Slash", titleArea, new Color(1f, 0.18f, 0.1f, 0.7f));
            bottomRule.anchorMin = new Vector2(0.32f, 0.02f);
            bottomRule.anchorMax = new Vector2(0.68f, 0.035f);
            bottomRule.offsetMin = Vector2.zero;
            bottomRule.offsetMax = Vector2.zero;
            bottomRule.GetComponent<Image>().raycastTarget = false;
        }

        private void BuildEraTimeline(RectTransform root)
        {
            var timeline = CreateStyledPanel("Era Timeline", root, new Color(0.02f, 0.045f, 0.085f, 0.78f));
            timeline.anchorMin = new Vector2(0.17f, 0.03f);
            timeline.anchorMax = new Vector2(0.83f, 0.1f);
            timeline.offsetMin = Vector2.zero;
            timeline.offsetMax = Vector2.zero;

            for (var i = 0; i < EraNames.Length; i++)
            {
                var x = 0.09f + i * 0.205f;
                if (i < EraNames.Length - 1)
                {
                    var line = CreatePanel("Era Segment " + i, timeline, new Color(0.38f, 0.55f, 0.75f, 0.48f));
                    line.anchorMin = new Vector2(x + 0.028f, 0.59f);
                    line.anchorMax = new Vector2(x + 0.177f, 0.635f);
                    line.offsetMin = Vector2.zero;
                    line.offsetMax = Vector2.zero;
                    line.GetComponent<Image>().raycastTarget = false;
                }

                var dot = CreatePanel("Era Dot " + i, timeline, EraColors[i]);
                dot.anchorMin = new Vector2(x, 0.62f);
                dot.anchorMax = new Vector2(x, 0.62f);
                dot.sizeDelta = new Vector2(18f, 18f);
                dot.anchoredPosition = Vector2.zero;
                var dotImage = dot.GetComponent<Image>();
                dotImage.sprite = DotSprite;
                dotImage.raycastTarget = false;

                var label = CreateText(timeline, "Era Label " + i, EraNames[i], 17, FontStyle.Bold, TextAnchor.MiddleCenter);
                label.color = new Color(0.86f, 0.92f, 1f, 1f);
                label.resizeTextForBestFit = true;
                label.resizeTextMinSize = 12;
                label.resizeTextMaxSize = 17;
                label.rectTransform.anchorMin = new Vector2(x - 0.07f, 0.12f);
                label.rectTransform.anchorMax = new Vector2(x + 0.07f, 0.44f);
                label.rectTransform.offsetMin = Vector2.zero;
                label.rectTransform.offsetMax = Vector2.zero;
            }
        }

        private Button CreateButton(RectTransform parent, string name, string label, UnityAction onClick, Color normalColor, int fontSize, bool pulse, bool showChevron = true)
        {
            var buttonRect = CreatePanel(name, parent, normalColor);
            var image = buttonRect.GetComponent<Image>();
            image.sprite = pulse ? PrimaryButtonSprite : MenuButtonSprite;
            image.type = Image.Type.Sliced;

            var button = buttonRect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(onClick);
            AddHoverEffect(button, image, normalColor, pulse ? 1.055f : 1.045f);
            SetButtonColor(button, normalColor);

            var topShine = CreatePanel("Top Shine", buttonRect, pulse ? new Color(1f, 1f, 1f, 0.28f) : new Color(0.24f, 0.86f, 1f, 0.16f));
            topShine.anchorMin = new Vector2(0.07f, 0.66f);
            topShine.anchorMax = new Vector2(0.93f, 0.78f);
            topShine.offsetMin = Vector2.zero;
            topShine.offsetMax = Vector2.zero;
            topShine.GetComponent<Image>().raycastTarget = false;

            var leftAccent = CreatePanel("Left Accent", buttonRect, pulse ? new Color(1f, 0.34f, 0.16f, 0.84f) : new Color(0.14f, 0.82f, 1f, 0.7f));
            leftAccent.anchorMin = new Vector2(0.03f, 0.21f);
            leftAccent.anchorMax = new Vector2(0.065f, 0.79f);
            leftAccent.offsetMin = Vector2.zero;
            leftAccent.offsetMax = Vector2.zero;
            leftAccent.GetComponent<Image>().raycastTarget = false;

            var text = CreateText(buttonRect, "Label", label, fontSize, FontStyle.Bold, TextAnchor.MiddleCenter);
            text.color = pulse ? new Color(0.02f, 0.07f, 0.2f, 1f) : new Color(0.9f, 0.96f, 1f, 1f);
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 14;
            text.resizeTextMaxSize = fontSize;
            text.rectTransform.anchorMin = Vector2.zero;
            text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.offsetMin = new Vector2(34f, 5f);
            text.rectTransform.offsetMax = new Vector2(showChevron ? -58f : -34f, -5f);

            if (showChevron)
            {
                var chevron = CreateText(buttonRect, "Chevron", "\u203a", fontSize + 8, FontStyle.Bold, TextAnchor.MiddleCenter);
                chevron.color = pulse ? new Color(0.02f, 0.07f, 0.2f, 1f) : new Color(0.9f, 0.96f, 1f, 1f);
                chevron.rectTransform.anchorMin = new Vector2(0.84f, 0f);
                chevron.rectTransform.anchorMax = new Vector2(0.97f, 1f);
                chevron.rectTransform.offsetMin = Vector2.zero;
                chevron.rectTransform.offsetMax = Vector2.zero;
            }

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

            var hover = button.GetComponent<MenuHoverEffect>();
            if (hover != null)
            {
                hover.SetColors(normalColor, ButtonHoverTint);
            }

            var colors = button.colors;
            colors.normalColor = normalColor;
            colors.highlightedColor = Color.Lerp(normalColor, Color.white, 0.18f);
            colors.pressedColor = Color.Lerp(normalColor, Color.black, 0.26f);
            colors.selectedColor = Color.Lerp(normalColor, Color.white, 0.12f);
            colors.disabledColor = new Color(0.25f, 0.25f, 0.24f, 0.72f);
            colors.colorMultiplier = 1f;
            button.colors = colors;
        }

        private void AddHoverEffect(Button button, Image targetImage, Color normalColor, float hoverScale)
        {
            var hover = button.gameObject.AddComponent<MenuHoverEffect>();
            hover.Initialize(button, targetImage, normalColor, ButtonHoverTint, hoverScale);
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

        private Sprite LoadResourceSprite(string resourcePath, string fallbackResourcePath)
        {
            var texture = Resources.Load<Texture2D>(resourcePath)
                ?? Resources.Load<Texture2D>(fallbackResourcePath);
            if (texture == null)
            {
                return WhiteSprite;
            }

            return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
        }

        private RectTransform CreateStyledPanel(string name, Transform parent, Color color)
        {
            var rect = CreatePanel(name, parent, color);
            var image = rect.GetComponent<Image>();
            image.sprite = PanelSprite;
            image.type = Image.Type.Sliced;
            image.color = new Color(1f, 1f, 1f, color.a);
            return rect;
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
            text.raycastTarget = false;

            var shadow = textObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.68f);
            shadow.effectDistance = size >= 24 ? new Vector2(2f, -2f) : new Vector2(1.25f, -1.25f);
            if (size >= 18)
            {
                var outline = textObject.AddComponent<Outline>();
                outline.effectColor = new Color(0.01f, 0.025f, 0.045f, 0.58f);
                outline.effectDistance = size >= 28 ? new Vector2(1.65f, -1.65f) : new Vector2(1.1f, -1.1f);
            }

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
    }
}
