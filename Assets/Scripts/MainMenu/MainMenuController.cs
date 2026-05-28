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
    public sealed class MainMenuController : MonoBehaviour
    {
        private const string MainMenuSceneName = "MainMenu";
        private const string BattleSceneName = "Battle";
        private const string CanvasName = "Main Menu Canvas";
        private const string GameTitle = "\u6218\u7ebf\u8fdb\u5316\uff1a\u7eaa\u5143\u4e4b\u6218";
        private const string MenuBackgroundResource = "Barbarian/Maps/ForestThreeLanes";
        private const string BaseCrestResource = "Barbarian/Base/Base";

        private static Sprite whiteSprite;
        private static Sprite menuButtonSprite;
        private static Sprite primaryButtonSprite;
        private static Sprite panelSprite;
        private static Sprite iconButtonSprite;
        private static Sprite topFadeSprite;
        private static Sprite bottomFadeSprite;

        private readonly List<DifficultyButtonBinding> difficultyButtons = new List<DifficultyButtonBinding>();

        private Font uiFont;
        private RectTransform contentRoot;
        private Button startButton;
        private Button pulseButton;
        private Image pulseButtonImage;
        private Image leftLine;
        private Image centerLine;
        private Image rightLine;
        private Image titleAura;
        private BattleMapDefinition selectedMap;
        private GameDifficulty selectedDifficulty = GameDifficulty.Normal;
        private bool hasSelectedDifficulty = true;

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
            selectedDifficulty = GameSession.Difficulty;
            hasSelectedDifficulty = true;
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
                    new Color(0.94f, 0.78f, 0.58f, 1f),
                    new Color(1f, 0.98f, 0.84f, 1f),
                    pulse);
            }

            if (titleAura != null)
            {
                titleAura.color = Color.Lerp(
                    new Color(0.9f, 0.32f, 0.08f, 0.12f),
                    new Color(1f, 0.68f, 0.22f, 0.24f),
                    pulse);
            }

            if (leftLine != null)
            {
                leftLine.fillAmount = Mathf.Lerp(0.34f, 1f, Mathf.PingPong(Time.time * 0.25f, 1f));
            }

            if (centerLine != null)
            {
                centerLine.fillAmount = Mathf.Lerp(0.48f, 1f, Mathf.PingPong(Time.time * 0.2f + 0.25f, 1f));
            }

            if (rightLine != null)
            {
                rightLine.fillAmount = Mathf.Lerp(0.38f, 1f, Mathf.PingPong(Time.time * 0.18f + 0.5f, 1f));
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
            BuildSideActions(root);

            contentRoot = CreateRect("Menu Content", root);
            SetContentBounds(0.32f, 0.1f, 0.68f, 0.58f);
        }

        private void ShowHomeScreen()
        {
            ClearContent();
            difficultyButtons.Clear();
            startButton = null;
            pulseButton = null;
            pulseButtonImage = null;
            SetContentBounds(0.34f, 0.1f, 0.66f, 0.57f);

            CreateHomeStatus();
            CreateHomeMenuButton("\u5f00\u59cb\u6e38\u620f", StartGame, 0.72f, true);
            CreateHomeMenuButton("\u9009\u62e9\u5730\u56fe", ShowMapSelectScreen, 0.54f, false);
            CreateHomeMenuButton("\u96be\u5ea6\u8bbe\u7f6e", ShowDifficultyScreen, 0.36f, false);
            CreateHomeMenuButton("\u6218\u7ee9\u6863\u6848", ShowArchiveScreen, 0.18f, false);
            CreateHomeMenuButton("\u6e38\u620f\u8bbe\u7f6e", ShowSettingsScreen, 0f, false);
        }

        private void ShowMapSelectScreen()
        {
            ClearContent();
            pulseButton = null;
            pulseButtonImage = null;
            SetContentBounds(0.18f, 0.1f, 0.82f, 0.62f);

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
            SetContentBounds(0.2f, 0.1f, 0.8f, 0.62f);

            CreateScreenTitle("\u9009\u62e9\u6e38\u620f\u96be\u5ea6");

            var selectedMapText = CreateText(
                contentRoot,
                "Selected Map",
                "\u5df2\u9009\u5730\u56fe\uff1a" + selectedMap.DisplayName,
                22,
                FontStyle.Bold,
                TextAnchor.MiddleCenter);
            selectedMapText.color = new Color(0.95f, 0.9f, 0.78f, 1f);
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
                Color.white,
                32,
                true);
            var startRect = startButton.GetComponent<RectTransform>();
            startRect.anchorMin = new Vector2(0.5f, 0.13f);
            startRect.anchorMax = new Vector2(0.5f, 0.13f);
            startRect.sizeDelta = new Vector2(360f, 76f);
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
            ShowInfoScreen("\u6218\u7ee9\u6863\u6848", "\u6682\u65e0\u672c\u5730\u6218\u7ee9");
        }

        private void ShowSettingsScreen()
        {
            ShowInfoScreen("\u6e38\u620f\u8bbe\u7f6e", "\u9ed8\u8ba4\u97f3\u6548\u4e0e\u753b\u9762\u914d\u7f6e\u5df2\u542f\u7528");
        }

        private void ShowInfoScreen(string titleValue, string detail)
        {
            ClearContent();
            pulseButton = null;
            pulseButtonImage = null;
            SetContentBounds(0.25f, 0.16f, 0.75f, 0.56f);

            CreateScreenTitle(titleValue);

            var panel = CreateStyledPanel("Info Panel", contentRoot, new Color(0.07f, 0.075f, 0.075f, 0.9f));
            panel.anchorMin = new Vector2(0.14f, 0.32f);
            panel.anchorMax = new Vector2(0.86f, 0.62f);
            panel.offsetMin = Vector2.zero;
            panel.offsetMax = Vector2.zero;

            var message = CreateText(panel, "Info Detail", detail, 24, FontStyle.Bold, TextAnchor.MiddleCenter);
            message.color = new Color(0.94f, 0.86f, 0.68f, 1f);
            message.rectTransform.anchorMin = new Vector2(0.08f, 0.18f);
            message.rectTransform.anchorMax = new Vector2(0.92f, 0.82f);
            message.rectTransform.offsetMin = Vector2.zero;
            message.rectTransform.offsetMax = Vector2.zero;

            CreateBackButton(ShowHomeScreen);
        }

        private void CreateHomeStatus()
        {
            var status = CreateStyledPanel("Home Status", contentRoot, new Color(0.045f, 0.052f, 0.055f, 0.88f));
            status.anchorMin = new Vector2(0.04f, 0.86f);
            status.anchorMax = new Vector2(0.96f, 1f);
            status.offsetMin = Vector2.zero;
            status.offsetMax = Vector2.zero;

            var statusText = CreateText(
                status,
                "Status Text",
                "\u5f53\u524d\u6218\u573a\uff1a" + selectedMap.DisplayName + "    \u96be\u5ea6\uff1a" + GameSession.DifficultyName,
                20,
                FontStyle.Bold,
                TextAnchor.MiddleCenter);
            statusText.color = new Color(0.9f, 0.84f, 0.68f, 1f);
            statusText.resizeTextForBestFit = true;
            statusText.resizeTextMinSize = 14;
            statusText.resizeTextMaxSize = 20;
            statusText.rectTransform.anchorMin = new Vector2(0.05f, 0.08f);
            statusText.rectTransform.anchorMax = new Vector2(0.95f, 0.92f);
            statusText.rectTransform.offsetMin = Vector2.zero;
            statusText.rectTransform.offsetMax = Vector2.zero;
        }

        private void CreateHomeMenuButton(string label, UnityAction onClick, float y, bool pulse)
        {
            var button = CreateButton(
                contentRoot,
                label + " Button",
                label,
                onClick,
                pulse ? Color.white : new Color(0.88f, 0.9f, 0.84f, 1f),
                30,
                pulse);
            var rect = button.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, y);
            rect.anchorMax = new Vector2(0.5f, y);
            rect.sizeDelta = new Vector2(420f, 70f);
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
            title.color = new Color(0.96f, 0.86f, 0.58f, 1f);
            title.rectTransform.anchorMin = new Vector2(0f, 0.84f);
            title.rectTransform.anchorMax = new Vector2(1f, 1f);
            title.rectTransform.offsetMin = Vector2.zero;
            title.rectTransform.offsetMax = Vector2.zero;
        }

        private void CreateMapCard(BattleMapDefinition map, int index)
        {
            var card = CreateStyledPanel("Map Card " + index, contentRoot, new Color(0.055f, 0.064f, 0.062f, 0.92f));
            card.anchorMin = new Vector2(0.18f, 0.17f);
            card.anchorMax = new Vector2(0.82f, 0.74f);
            card.offsetMin = Vector2.zero;
            card.offsetMax = Vector2.zero;

            var button = card.gameObject.AddComponent<Button>();
            button.targetGraphic = card.GetComponent<Image>();
            button.onClick.AddListener(() => SelectMap(map));
            SetButtonColor(button, new Color(1f, 1f, 1f, 0.92f));

            var thumbnailRect = CreatePanel("Map Thumbnail", card, Color.white);
            thumbnailRect.anchorMin = new Vector2(0.035f, 0.24f);
            thumbnailRect.anchorMax = new Vector2(0.965f, 0.92f);
            thumbnailRect.offsetMin = Vector2.zero;
            thumbnailRect.offsetMax = Vector2.zero;

            var thumbnail = thumbnailRect.GetComponent<Image>();
            thumbnail.sprite = LoadResourceSprite(map.ResourcePath);
            thumbnail.preserveAspect = true;
            thumbnail.color = new Color(0.86f, 0.88f, 0.78f, 1f);
            thumbnail.raycastTarget = false;

            var frame = CreatePanel("Map Thumbnail Frame", card, new Color(0.95f, 0.68f, 0.28f, 0.54f));
            frame.anchorMin = new Vector2(0.033f, 0.235f);
            frame.anchorMax = new Vector2(0.967f, 0.925f);
            frame.offsetMin = Vector2.zero;
            frame.offsetMax = Vector2.zero;
            frame.SetAsFirstSibling();
            frame.GetComponent<Image>().raycastTarget = false;

            var label = CreateText(card, "Map Label", map.DisplayName, 25, FontStyle.Bold, TextAnchor.MiddleCenter);
            label.color = new Color(1f, 0.86f, 0.52f, 1f);
            label.rectTransform.anchorMin = new Vector2(0.04f, 0.1f);
            label.rectTransform.anchorMax = new Vector2(0.96f, 0.22f);
            label.rectTransform.offsetMin = Vector2.zero;
            label.rectTransform.offsetMax = Vector2.zero;

            var description = CreateText(card, "Map Description", map.Description, 16, FontStyle.Normal, TextAnchor.MiddleCenter);
            description.color = new Color(0.78f, 0.84f, 0.74f, 1f);
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
                new Color(0.88f, 0.9f, 0.84f, 1f),
                23,
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
            var button = CreateButton(contentRoot, "Back Button", "\u8fd4\u56de", onClick, new Color(0.88f, 0.9f, 0.84f, 1f), 22, false);
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
                        ? new Color(1f, 0.78f, 0.36f, 1f)
                        : new Color(0.88f, 0.9f, 0.84f, 1f));
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
            battlefieldImage.sprite = LoadResourceSprite(MenuBackgroundResource);
            battlefieldImage.preserveAspect = false;
            battlefieldImage.color = new Color(0.56f, 0.55f, 0.44f, 1f);

            var wash = CreatePanel("Battlefield Wash", root, new Color(0.025f, 0.03f, 0.035f, 0.4f));
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

            var commandSpine = CreateStyledPanel("Command Spine", root, new Color(0.035f, 0.04f, 0.042f, 0.74f));
            commandSpine.anchorMin = new Vector2(0.31f, 0.075f);
            commandSpine.anchorMax = new Vector2(0.69f, 0.62f);
            commandSpine.offsetMin = Vector2.zero;
            commandSpine.offsetMax = Vector2.zero;

            var spineTop = CreatePanel("Command Spine Top Rule", root, new Color(0.9f, 0.58f, 0.24f, 0.76f));
            spineTop.anchorMin = new Vector2(0.335f, 0.605f);
            spineTop.anchorMax = new Vector2(0.665f, 0.61f);
            spineTop.offsetMin = Vector2.zero;
            spineTop.offsetMax = Vector2.zero;

            var spineBottom = CreatePanel("Command Spine Bottom Rule", root, new Color(0.86f, 0.28f, 0.12f, 0.58f));
            spineBottom.anchorMin = new Vector2(0.345f, 0.086f);
            spineBottom.anchorMax = new Vector2(0.655f, 0.091f);
            spineBottom.offsetMin = Vector2.zero;
            spineBottom.offsetMax = Vector2.zero;

            CreateBaseCrest(root);

            leftLine = CreateLine(root, "Upper Frontline", new Vector2(0.08f, 0.66f), new Vector2(0.39f, 0.668f), new Color(0.86f, 0.3f, 0.14f, 0.82f));
            leftLine.rectTransform.localEulerAngles = new Vector3(0f, 0f, -7f);
            centerLine = CreateLine(root, "Middle Frontline", new Vector2(0.22f, 0.47f), new Vector2(0.78f, 0.477f), new Color(0.93f, 0.68f, 0.25f, 0.66f));
            centerLine.rectTransform.localEulerAngles = new Vector3(0f, 0f, 2.5f);
            rightLine = CreateLine(root, "Lower Frontline", new Vector2(0.59f, 0.23f), new Vector2(0.91f, 0.238f), new Color(0.18f, 0.62f, 0.66f, 0.58f));
            rightLine.rectTransform.localEulerAngles = new Vector3(0f, 0f, -8f);
        }

        private void BuildTitle(RectTransform root)
        {
            titleAura = CreatePanel("Title Aura", root, new Color(0.9f, 0.32f, 0.08f, 0.16f)).GetComponent<Image>();
            titleAura.rectTransform.anchorMin = new Vector2(0.22f, 0.705f);
            titleAura.rectTransform.anchorMax = new Vector2(0.78f, 0.89f);
            titleAura.rectTransform.offsetMin = Vector2.zero;
            titleAura.rectTransform.offsetMax = Vector2.zero;

            var shadow = CreateText(root, "Game Title Shadow", GameTitle, 78, FontStyle.Bold, TextAnchor.MiddleCenter);
            shadow.color = new Color(0f, 0f, 0f, 0.88f);
            shadow.resizeTextForBestFit = true;
            shadow.resizeTextMinSize = 36;
            shadow.resizeTextMaxSize = 78;
            shadow.rectTransform.anchorMin = new Vector2(0.08f, 0.715f);
            shadow.rectTransform.anchorMax = new Vector2(0.92f, 0.86f);
            shadow.rectTransform.offsetMin = new Vector2(6f, -8f);
            shadow.rectTransform.offsetMax = new Vector2(6f, -8f);

            var glow = CreateText(root, "Game Title Glow", GameTitle, 82, FontStyle.Bold, TextAnchor.MiddleCenter);
            glow.color = new Color(0.94f, 0.28f, 0.09f, 0.32f);
            glow.resizeTextForBestFit = true;
            glow.resizeTextMinSize = 36;
            glow.resizeTextMaxSize = 82;
            glow.rectTransform.anchorMin = new Vector2(0.08f, 0.715f);
            glow.rectTransform.anchorMax = new Vector2(0.92f, 0.86f);
            glow.rectTransform.offsetMin = Vector2.zero;
            glow.rectTransform.offsetMax = Vector2.zero;

            var title = CreateText(root, "Game Title", GameTitle, 74, FontStyle.Bold, TextAnchor.MiddleCenter);
            title.color = new Color(1f, 0.9f, 0.58f, 1f);
            title.resizeTextForBestFit = true;
            title.resizeTextMinSize = 36;
            title.resizeTextMaxSize = 74;
            title.rectTransform.anchorMin = new Vector2(0.08f, 0.715f);
            title.rectTransform.anchorMax = new Vector2(0.92f, 0.86f);
            title.rectTransform.offsetMin = Vector2.zero;
            title.rectTransform.offsetMax = Vector2.zero;

            var subtitlePlate = CreateStyledPanel("Subtitle Plate", root, new Color(0.04f, 0.05f, 0.052f, 0.78f));
            subtitlePlate.anchorMin = new Vector2(0.36f, 0.66f);
            subtitlePlate.anchorMax = new Vector2(0.64f, 0.705f);
            subtitlePlate.offsetMin = Vector2.zero;
            subtitlePlate.offsetMax = Vector2.zero;

            var subtitle = CreateText(subtitlePlate, "English Title", "FRONTLINE EVOLUTION", 20, FontStyle.Bold, TextAnchor.MiddleCenter);
            subtitle.color = new Color(0.76f, 0.84f, 0.78f, 1f);
            subtitle.rectTransform.anchorMin = new Vector2(0.05f, 0.05f);
            subtitle.rectTransform.anchorMax = new Vector2(0.95f, 0.95f);
            subtitle.rectTransform.offsetMin = Vector2.zero;
            subtitle.rectTransform.offsetMax = Vector2.zero;
        }

        private void BuildSideActions(RectTransform root)
        {
            CreateIconButton(root, "Map Side Action", "\u56fe", "\u5730\u56fe", ShowMapSelectScreen, new Vector2(0.925f, 0.56f));
            CreateIconButton(root, "Difficulty Side Action", "\u96be", "\u96be\u5ea6", ShowDifficultyScreen, new Vector2(0.925f, 0.44f));
            CreateIconButton(root, "Settings Side Action", "\u8bbe", "\u8bbe\u7f6e", ShowSettingsScreen, new Vector2(0.925f, 0.32f));

            var bulletin = CreateStyledPanel("War Bulletin", root, new Color(0.04f, 0.05f, 0.052f, 0.78f));
            bulletin.anchorMin = new Vector2(0.06f, 0.12f);
            bulletin.anchorMax = new Vector2(0.22f, 0.205f);
            bulletin.offsetMin = Vector2.zero;
            bulletin.offsetMax = Vector2.zero;

            var text = CreateText(bulletin, "Bulletin Text", "AGE OF WAR", 18, FontStyle.Bold, TextAnchor.MiddleCenter);
            text.color = new Color(0.94f, 0.72f, 0.4f, 1f);
            text.rectTransform.anchorMin = new Vector2(0.08f, 0.08f);
            text.rectTransform.anchorMax = new Vector2(0.92f, 0.92f);
            text.rectTransform.offsetMin = Vector2.zero;
            text.rectTransform.offsetMax = Vector2.zero;
        }

        private void CreateBaseCrest(RectTransform root)
        {
            var crest = CreateStyledPanel("Base Crest", root, new Color(0.04f, 0.045f, 0.045f, 0.68f));
            crest.anchorMin = new Vector2(0.065f, 0.29f);
            crest.anchorMax = new Vector2(0.225f, 0.61f);
            crest.offsetMin = Vector2.zero;
            crest.offsetMax = Vector2.zero;

            var imageRect = CreatePanel("Base Crest Image", crest, Color.white);
            imageRect.anchorMin = new Vector2(0.05f, 0.14f);
            imageRect.anchorMax = new Vector2(0.95f, 0.92f);
            imageRect.offsetMin = Vector2.zero;
            imageRect.offsetMax = Vector2.zero;

            var image = imageRect.GetComponent<Image>();
            image.sprite = LoadResourceSprite(BaseCrestResource);
            image.preserveAspect = true;
            image.color = new Color(1f, 0.78f, 0.54f, 0.92f);
            image.raycastTarget = false;

            var label = CreateText(crest, "Crest Label", "\u86ee\u8352\u636e\u70b9", 18, FontStyle.Bold, TextAnchor.MiddleCenter);
            label.color = new Color(0.95f, 0.74f, 0.45f, 1f);
            label.rectTransform.anchorMin = new Vector2(0.05f, 0.02f);
            label.rectTransform.anchorMax = new Vector2(0.95f, 0.16f);
            label.rectTransform.offsetMin = Vector2.zero;
            label.rectTransform.offsetMax = Vector2.zero;
        }

        private void CreateIconButton(RectTransform parent, string name, string symbol, string caption, UnityAction onClick, Vector2 anchor)
        {
            var rect = CreatePanel(name, parent, new Color(0.12f, 0.13f, 0.13f, 0.95f));
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.sizeDelta = new Vector2(82f, 82f);
            rect.anchoredPosition = Vector2.zero;

            var image = rect.GetComponent<Image>();
            image.sprite = IconButtonSprite;
            image.type = Image.Type.Sliced;

            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(onClick);
            SetButtonColor(button, new Color(0.9f, 0.92f, 0.86f, 1f));

            var symbolText = CreateText(rect, "Symbol", symbol, 31, FontStyle.Bold, TextAnchor.MiddleCenter);
            symbolText.color = new Color(0.95f, 0.74f, 0.38f, 1f);
            symbolText.rectTransform.anchorMin = new Vector2(0.12f, 0.3f);
            symbolText.rectTransform.anchorMax = new Vector2(0.88f, 0.9f);
            symbolText.rectTransform.offsetMin = Vector2.zero;
            symbolText.rectTransform.offsetMax = Vector2.zero;

            var captionText = CreateText(rect, "Caption", caption, 13, FontStyle.Bold, TextAnchor.MiddleCenter);
            captionText.color = new Color(0.74f, 0.82f, 0.78f, 1f);
            captionText.rectTransform.anchorMin = new Vector2(0.06f, 0.08f);
            captionText.rectTransform.anchorMax = new Vector2(0.94f, 0.34f);
            captionText.rectTransform.offsetMin = Vector2.zero;
            captionText.rectTransform.offsetMax = Vector2.zero;
        }

        private Button CreateButton(RectTransform parent, string name, string label, UnityAction onClick, Color normalColor, int fontSize, bool pulse)
        {
            var buttonRect = CreatePanel(name, parent, normalColor);
            var image = buttonRect.GetComponent<Image>();
            image.sprite = pulse ? PrimaryButtonSprite : MenuButtonSprite;
            image.type = Image.Type.Sliced;

            var button = buttonRect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(onClick);
            SetButtonColor(button, normalColor);

            if (pulse)
            {
                pulseButton = button;
                pulseButtonImage = image;
            }

            var topShine = CreatePanel("Top Shine", buttonRect, new Color(1f, 0.88f, 0.58f, pulse ? 0.22f : 0.12f));
            topShine.anchorMin = new Vector2(0.07f, 0.66f);
            topShine.anchorMax = new Vector2(0.93f, 0.78f);
            topShine.offsetMin = Vector2.zero;
            topShine.offsetMax = Vector2.zero;
            topShine.GetComponent<Image>().raycastTarget = false;

            var leftAccent = CreatePanel("Left Accent", buttonRect, pulse ? new Color(1f, 0.76f, 0.28f, 0.75f) : new Color(0.8f, 0.62f, 0.34f, 0.5f));
            leftAccent.anchorMin = new Vector2(0.03f, 0.21f);
            leftAccent.anchorMax = new Vector2(0.065f, 0.79f);
            leftAccent.offsetMin = Vector2.zero;
            leftAccent.offsetMax = Vector2.zero;
            leftAccent.GetComponent<Image>().raycastTarget = false;

            var text = CreateText(buttonRect, "Label", label, fontSize, FontStyle.Bold, TextAnchor.MiddleCenter);
            text.color = new Color(1f, 0.94f, 0.78f, 1f);
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 14;
            text.resizeTextMaxSize = fontSize;
            text.rectTransform.anchorMin = Vector2.zero;
            text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.offsetMin = new Vector2(28f, 7f);
            text.rectTransform.offsetMax = new Vector2(-28f, -7f);
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
            colors.highlightedColor = Color.Lerp(normalColor, Color.white, 0.18f);
            colors.pressedColor = Color.Lerp(normalColor, Color.black, 0.26f);
            colors.selectedColor = Color.Lerp(normalColor, Color.white, 0.12f);
            colors.disabledColor = new Color(0.25f, 0.25f, 0.24f, 0.72f);
            colors.colorMultiplier = 1f;
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
            image.raycastTarget = false;
            return image;
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

        private static Sprite MenuButtonSprite
        {
            get
            {
                if (menuButtonSprite == null)
                {
                    menuButtonSprite = CreateBeveledSprite(
                        new Color(0.23f, 0.24f, 0.23f, 1f),
                        new Color(0.1f, 0.11f, 0.11f, 1f),
                        new Color(0.045f, 0.05f, 0.052f, 1f),
                        new Color(0.78f, 0.58f, 0.32f, 1f),
                        new Color(1f, 0.82f, 0.45f, 1f));
                }

                return menuButtonSprite;
            }
        }

        private static Sprite PrimaryButtonSprite
        {
            get
            {
                if (primaryButtonSprite == null)
                {
                    primaryButtonSprite = CreateBeveledSprite(
                        new Color(0.82f, 0.32f, 0.12f, 1f),
                        new Color(0.48f, 0.13f, 0.08f, 1f),
                        new Color(0.2f, 0.055f, 0.035f, 1f),
                        new Color(1f, 0.72f, 0.32f, 1f),
                        new Color(1f, 0.92f, 0.56f, 1f));
                }

                return primaryButtonSprite;
            }
        }

        private static Sprite PanelSprite
        {
            get
            {
                if (panelSprite == null)
                {
                    panelSprite = CreateBeveledSprite(
                        new Color(0.12f, 0.13f, 0.13f, 1f),
                        new Color(0.055f, 0.06f, 0.06f, 1f),
                        new Color(0.025f, 0.028f, 0.03f, 1f),
                        new Color(0.62f, 0.44f, 0.25f, 1f),
                        new Color(0.82f, 0.62f, 0.36f, 1f));
                }

                return panelSprite;
            }
        }

        private static Sprite IconButtonSprite
        {
            get
            {
                if (iconButtonSprite == null)
                {
                    iconButtonSprite = CreateBeveledSprite(
                        new Color(0.18f, 0.2f, 0.2f, 1f),
                        new Color(0.08f, 0.09f, 0.09f, 1f),
                        new Color(0.03f, 0.034f, 0.036f, 1f),
                        new Color(0.8f, 0.58f, 0.28f, 1f),
                        new Color(0.9f, 0.7f, 0.38f, 1f));
                }

                return iconButtonSprite;
            }
        }

        private static Sprite TopFadeSprite
        {
            get
            {
                if (topFadeSprite == null)
                {
                    topFadeSprite = CreateVerticalGradientSprite(new Color(0f, 0f, 0f, 0f), new Color(0f, 0f, 0f, 0.88f));
                }

                return topFadeSprite;
            }
        }

        private static Sprite BottomFadeSprite
        {
            get
            {
                if (bottomFadeSprite == null)
                {
                    bottomFadeSprite = CreateVerticalGradientSprite(new Color(0f, 0f, 0f, 0.9f), new Color(0f, 0f, 0f, 0f));
                }

                return bottomFadeSprite;
            }
        }

        private static Sprite CreateBeveledSprite(Color top, Color center, Color bottom, Color border, Color highlight)
        {
            const int width = 96;
            const int height = 40;
            const int borderSize = 5;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;

            for (var y = 0; y < height; y++)
            {
                var v = y / (float)(height - 1);
                var baseColor = v > 0.5f
                    ? Color.Lerp(center, top, (v - 0.5f) * 2f)
                    : Color.Lerp(bottom, center, v * 2f);

                for (var x = 0; x < width; x++)
                {
                    var edge = x < borderSize || x >= width - borderSize || y < borderSize || y >= height - borderSize;
                    var corner = (x < borderSize * 2 || x >= width - borderSize * 2)
                        && (y < borderSize * 2 || y >= height - borderSize * 2);
                    var color = edge ? Color.Lerp(baseColor, border, corner ? 0.9f : 0.58f) : baseColor;

                    if (y > height - borderSize * 2 && !edge)
                    {
                        color = Color.Lerp(color, highlight, 0.18f);
                    }

                    if (y == height - borderSize - 1 || y == borderSize)
                    {
                        color = Color.Lerp(color, highlight, 0.35f);
                    }

                    texture.SetPixel(x, y, color);
                }
            }

            texture.Apply();
            return Sprite.Create(
                texture,
                new Rect(0f, 0f, width, height),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect,
                new Vector4(18f, 14f, 18f, 14f));
        }

        private static Sprite CreateVerticalGradientSprite(Color bottom, Color top)
        {
            const int width = 4;
            const int height = 128;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;

            for (var y = 0; y < height; y++)
            {
                var color = Color.Lerp(bottom, top, y / (float)(height - 1));
                for (var x = 0; x < width; x++)
                {
                    texture.SetPixel(x, y, color);
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 100f);
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
