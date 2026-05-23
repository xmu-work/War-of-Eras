using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace WarOfEras.Battle.UI
{
    public sealed class BattleHUDMockup : MonoBehaviour
    {
        private const string BattleSceneName = "Battle";
        private const string CanvasName = "Battle HUD Canvas";

        private static Sprite whiteSprite;

        private readonly string[] laneNames = { "上路", "中路", "下路" };
        private readonly string[] ageNames = { "蛮荒部落", "机械工坊", "电力时代", "核能纪元", "星海文明" };
        private readonly List<Button> laneButtons = new List<Button>();

        private Font uiFont;
        private Text coinText;
        private Text eraText;
        private Text eraValueText;
        private Text laneText;
        private Text statusText;
        private Image playerHealthFill;
        private Image enemyHealthFill;
        private Image eraFill;
        private Button quakeButton;
        private Button shieldButton;
        private Button upgradeAttackButton;
        private Button upgradeDefenseButton;

        private int coins = 175;
        private int selectedLane = 1;
        private int ageIndex;
        private int eraValue = 120;
        private int eraThreshold = 500;
        private float playerHealth = 1f;
        private float enemyHealth = 1f;
        private float coinBuffer;
        private float quakeCooldown;
        private float shieldCooldown;
        private float incomePerSecond = 8f;
        private string status = "选择路线后点击出兵按钮，先用这个界面测试核心流程。";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void CreateForBattleScene()
        {
            if (SceneManager.GetActiveScene().name != BattleSceneName)
            {
                return;
            }

            if (FindFirstObjectByType<WarOfEras.Battle.Core.BattleGameController>() != null)
            {
                return;
            }

            if (FindFirstObjectByType<BattleHUDMockup>() != null)
            {
                return;
            }

            new GameObject("Battle HUD Mockup").AddComponent<BattleHUDMockup>();
        }

        private void Awake()
        {
            if (FindObjectsByType<BattleHUDMockup>(FindObjectsSortMode.None).Length > 1)
            {
                Destroy(gameObject);
                return;
            }

            uiFont = CreateUiFont();
            EnsureEventSystem();
            BuildHud();
            RefreshHud();
        }

        private void Update()
        {
            coinBuffer += Time.deltaTime * incomePerSecond;
            if (coinBuffer >= 1f)
            {
                var gained = Mathf.FloorToInt(coinBuffer);
                coinBuffer -= gained;
                coins += gained;
            }

            if (quakeCooldown > 0f)
            {
                quakeCooldown = Mathf.Max(0f, quakeCooldown - Time.deltaTime);
            }

            if (shieldCooldown > 0f)
            {
                shieldCooldown = Mathf.Max(0f, shieldCooldown - Time.deltaTime);
            }

            RefreshHud();
        }

        private void BuildHud()
        {
            var canvasObject = new GameObject(CanvasName, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);

            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;

            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            var root = CreateRect("Safe Area", canvasObject.transform);
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = new Vector2(24f, 22f);
            root.offsetMax = new Vector2(-24f, -22f);

            BuildTopBar(root);
            BuildBattleHint(root);
            BuildBottomCommandBar(root);
        }

        private void BuildTopBar(RectTransform root)
        {
            var topBar = CreatePanel("Top Bar", root, new Color(0.055f, 0.071f, 0.086f, 0.9f));
            topBar.anchorMin = new Vector2(0f, 1f);
            topBar.anchorMax = new Vector2(1f, 1f);
            topBar.pivot = new Vector2(0.5f, 1f);
            topBar.anchoredPosition = Vector2.zero;
            topBar.sizeDelta = new Vector2(0f, 112f);

            var topLayout = topBar.gameObject.AddComponent<HorizontalLayoutGroup>();
            topLayout.padding = new RectOffset(14, 14, 12, 12);
            topLayout.spacing = 12f;
            topLayout.childControlHeight = true;
            topLayout.childControlWidth = true;
            topLayout.childForceExpandHeight = true;
            topLayout.childForceExpandWidth = true;

            var playerPanel = CreateInfoPanel("Player Base", topBar, 1f);
            CreateText(playerPanel, "Player Base Label", "我方基地", 24, FontStyle.Bold, TextAnchor.UpperLeft);
            playerHealthFill = CreateProgressBar(playerPanel, "Player Health Bar", new Color(0.14f, 0.17f, 0.18f, 1f), new Color(0.18f, 0.71f, 0.48f, 1f));

            var centerPanel = CreateInfoPanel("Resources", topBar, 1.35f);
            var resourceRow = CreateRect("Resource Row", centerPanel);
            resourceRow.anchorMin = new Vector2(0f, 0.5f);
            resourceRow.anchorMax = new Vector2(1f, 1f);
            resourceRow.offsetMin = new Vector2(0f, 0f);
            resourceRow.offsetMax = Vector2.zero;

            var resourceLayout = resourceRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            resourceLayout.spacing = 10f;
            resourceLayout.childControlWidth = true;
            resourceLayout.childForceExpandWidth = true;
            resourceLayout.childControlHeight = true;
            resourceLayout.childForceExpandHeight = true;

            coinText = CreateChip(resourceRow, "Coins", "金币 175");
            eraText = CreateChip(resourceRow, "Era", "时代 蛮荒部落");

            var eraRow = CreateRect("Era Progress Row", centerPanel);
            eraRow.anchorMin = new Vector2(0f, 0f);
            eraRow.anchorMax = new Vector2(1f, 0.42f);
            eraRow.offsetMin = Vector2.zero;
            eraRow.offsetMax = Vector2.zero;

            eraValueText = CreateText(eraRow, "Era Value", "时代值 120 / 500", 18, FontStyle.Normal, TextAnchor.MiddleLeft);
            eraValueText.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            eraValueText.rectTransform.anchorMax = new Vector2(0.34f, 0.5f);
            eraValueText.rectTransform.sizeDelta = new Vector2(0f, 34f);

            var eraBar = CreateRect("Era Bar", eraRow);
            eraBar.anchorMin = new Vector2(0.36f, 0.5f);
            eraBar.anchorMax = new Vector2(1f, 0.5f);
            eraBar.sizeDelta = new Vector2(0f, 22f);
            eraFill = CreateProgressBar(eraBar, "Era Progress Bar", new Color(0.17f, 0.19f, 0.2f, 1f), new Color(0.93f, 0.68f, 0.28f, 1f));

            var enemyPanel = CreateInfoPanel("Enemy Base", topBar, 1f);
            CreateText(enemyPanel, "Enemy Base Label", "敌方基地", 24, FontStyle.Bold, TextAnchor.UpperRight);
            enemyHealthFill = CreateProgressBar(enemyPanel, "Enemy Health Bar", new Color(0.14f, 0.17f, 0.18f, 1f), new Color(0.84f, 0.24f, 0.23f, 1f));
        }

        private void BuildBattleHint(RectTransform root)
        {
            var hint = CreatePanel("Battle Hint", root, new Color(0.08f, 0.1f, 0.11f, 0.76f));
            hint.anchorMin = new Vector2(0.5f, 1f);
            hint.anchorMax = new Vector2(0.5f, 1f);
            hint.pivot = new Vector2(0.5f, 1f);
            hint.anchoredPosition = new Vector2(0f, -126f);
            hint.sizeDelta = new Vector2(720f, 42f);

            laneText = CreateText(hint, "Lane Status", string.Empty, 20, FontStyle.Bold, TextAnchor.MiddleCenter);
            laneText.rectTransform.anchorMin = Vector2.zero;
            laneText.rectTransform.anchorMax = Vector2.one;
            laneText.rectTransform.offsetMin = new Vector2(14f, 0f);
            laneText.rectTransform.offsetMax = new Vector2(-14f, 0f);
        }

        private void BuildBottomCommandBar(RectTransform root)
        {
            var bottomBar = CreatePanel("Command Bar", root, new Color(0.047f, 0.058f, 0.069f, 0.93f));
            bottomBar.anchorMin = new Vector2(0f, 0f);
            bottomBar.anchorMax = new Vector2(1f, 0f);
            bottomBar.pivot = new Vector2(0.5f, 0f);
            bottomBar.anchoredPosition = Vector2.zero;
            bottomBar.sizeDelta = new Vector2(0f, 194f);

            var layout = bottomBar.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(14, 14, 14, 14);
            layout.spacing = 12f;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = true;
            layout.childForceExpandWidth = true;

            BuildLanePanel(bottomBar);
            BuildUnitPanel(bottomBar);
            BuildSkillPanel(bottomBar);
        }

        private void BuildLanePanel(RectTransform bottomBar)
        {
            var panel = CreateCommandPanel("Lane Panel", bottomBar, 0.72f);
            CreateSectionLabel(panel, "路线");

            var laneRow = CreateRect("Lane Buttons", panel);
            laneRow.anchorMin = new Vector2(0f, 0.24f);
            laneRow.anchorMax = new Vector2(1f, 0.72f);
            laneRow.offsetMin = Vector2.zero;
            laneRow.offsetMax = Vector2.zero;

            var laneLayout = laneRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            laneLayout.spacing = 8f;
            laneLayout.childControlWidth = true;
            laneLayout.childForceExpandWidth = true;
            laneLayout.childControlHeight = true;
            laneLayout.childForceExpandHeight = true;

            for (var i = 0; i < laneNames.Length; i++)
            {
                var laneIndex = i;
                var button = CreateButton(laneRow, laneNames[i], laneNames[i], () => SelectLane(laneIndex), new Color(0.12f, 0.17f, 0.2f, 1f));
                laneButtons.Add(button);
            }

            statusText = CreateText(panel, "Status", status, 16, FontStyle.Normal, TextAnchor.LowerLeft);
            statusText.rectTransform.anchorMin = new Vector2(0f, 0f);
            statusText.rectTransform.anchorMax = new Vector2(1f, 0.22f);
            statusText.rectTransform.offsetMin = new Vector2(4f, 0f);
            statusText.rectTransform.offsetMax = new Vector2(-4f, 0f);
        }

        private void BuildUnitPanel(RectTransform bottomBar)
        {
            var panel = CreateCommandPanel("Unit Panel", bottomBar, 1.5f);
            CreateSectionLabel(panel, "出兵与建造");

            var buttonGrid = CreateRect("Unit Buttons", panel);
            buttonGrid.anchorMin = new Vector2(0f, 0f);
            buttonGrid.anchorMax = new Vector2(1f, 0.72f);
            buttonGrid.offsetMin = Vector2.zero;
            buttonGrid.offsetMax = Vector2.zero;

            var grid = buttonGrid.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(164f, 54f);
            grid.spacing = new Vector2(10f, 10f);
            grid.childAlignment = TextAnchor.MiddleCenter;
            grid.constraint = GridLayoutGroup.Constraint.FixedRowCount;
            grid.constraintCount = 2;

            CreateButton(buttonGrid, "Stone Warrior", "石棒战士\n15 金币", () => SpendCoins(15, "石棒战士已派往" + laneNames[selectedLane], 24), new Color(0.2f, 0.16f, 0.11f, 1f));
            CreateButton(buttonGrid, "Stone Hunter", "投石猎手\n25 金币", () => SpendCoins(25, "投石猎手已派往" + laneNames[selectedLane], 34), new Color(0.18f, 0.2f, 0.14f, 1f));
            CreateButton(buttonGrid, "Bone Champion", "巨骨勇士\n100 金币", () => SpendCoins(100, "巨骨勇士正在压向" + laneNames[selectedLane], 95), new Color(0.24f, 0.18f, 0.15f, 1f));
            CreateButton(buttonGrid, "Bone Tower", "骨石塔\n100 金币", () => SpendCoins(100, "已在基地炮位建造骨石塔", 60), new Color(0.12f, 0.18f, 0.22f, 1f));
            CreateButton(buttonGrid, "Gain Era", "测试时代值\n+100", GainEraValue, new Color(0.2f, 0.18f, 0.1f, 1f));
        }

        private void BuildSkillPanel(RectTransform bottomBar)
        {
            var panel = CreateCommandPanel("Skill Panel", bottomBar, 1f);
            CreateSectionLabel(panel, "技能与进化");

            var skillRow = CreateRect("Skill Buttons", panel);
            skillRow.anchorMin = new Vector2(0f, 0.43f);
            skillRow.anchorMax = new Vector2(1f, 0.72f);
            skillRow.offsetMin = Vector2.zero;
            skillRow.offsetMax = Vector2.zero;

            var skillLayout = skillRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            skillLayout.spacing = 10f;
            skillLayout.childControlWidth = true;
            skillLayout.childForceExpandWidth = true;
            skillLayout.childControlHeight = true;
            skillLayout.childForceExpandHeight = true;

            quakeButton = CreateButton(skillRow, "Earthquake", "地震", UseEarthquake, new Color(0.28f, 0.17f, 0.11f, 1f));
            shieldButton = CreateButton(skillRow, "Shield", "护盾", UseShield, new Color(0.1f, 0.2f, 0.24f, 1f));

            var upgradeRow = CreateRect("Upgrade Buttons", panel);
            upgradeRow.anchorMin = new Vector2(0f, 0f);
            upgradeRow.anchorMax = new Vector2(1f, 0.34f);
            upgradeRow.offsetMin = Vector2.zero;
            upgradeRow.offsetMax = Vector2.zero;

            var upgradeLayout = upgradeRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            upgradeLayout.spacing = 10f;
            upgradeLayout.childControlWidth = true;
            upgradeLayout.childForceExpandWidth = true;
            upgradeLayout.childControlHeight = true;
            upgradeLayout.childForceExpandHeight = true;

            upgradeAttackButton = CreateButton(upgradeRow, "Attack Upgrade", "进攻进化", () => UpgradeAge("进攻倾向"), new Color(0.27f, 0.12f, 0.1f, 1f));
            upgradeDefenseButton = CreateButton(upgradeRow, "Defense Upgrade", "防守进化", () => UpgradeAge("防守倾向"), new Color(0.1f, 0.17f, 0.24f, 1f));
        }

        private RectTransform CreateInfoPanel(string name, RectTransform parent, float flexibleWidth)
        {
            var panel = CreatePanel(name, parent, new Color(0.09f, 0.11f, 0.12f, 0.94f));
            var layout = panel.gameObject.AddComponent<LayoutElement>();
            layout.flexibleWidth = flexibleWidth;
            layout.minWidth = 300f;
            return panel;
        }

        private RectTransform CreateCommandPanel(string name, RectTransform parent, float flexibleWidth)
        {
            var panel = CreatePanel(name, parent, new Color(0.075f, 0.089f, 0.1f, 1f));
            var layout = panel.gameObject.AddComponent<LayoutElement>();
            layout.flexibleWidth = flexibleWidth;
            layout.minWidth = 260f;
            return panel;
        }

        private void CreateSectionLabel(RectTransform parent, string label)
        {
            var text = CreateText(parent, label + " Label", label, 18, FontStyle.Bold, TextAnchor.UpperLeft);
            text.rectTransform.anchorMin = new Vector2(0f, 0.76f);
            text.rectTransform.anchorMax = new Vector2(1f, 1f);
            text.rectTransform.offsetMin = new Vector2(4f, 0f);
            text.rectTransform.offsetMax = new Vector2(-4f, 0f);
        }

        private Text CreateChip(RectTransform parent, string name, string value)
        {
            var chip = CreatePanel(name, parent, new Color(0.13f, 0.15f, 0.16f, 1f));
            chip.gameObject.AddComponent<LayoutElement>().minWidth = 180f;
            var text = CreateText(chip, name + " Text", value, 20, FontStyle.Bold, TextAnchor.MiddleCenter);
            text.rectTransform.anchorMin = Vector2.zero;
            text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.offsetMin = new Vector2(8f, 0f);
            text.rectTransform.offsetMax = new Vector2(-8f, 0f);
            return text;
        }

        private Image CreateProgressBar(RectTransform parent, string name, Color backgroundColor, Color fillColor)
        {
            var bar = CreatePanel(name, parent, backgroundColor);
            bar.anchorMin = new Vector2(0f, 0f);
            bar.anchorMax = new Vector2(1f, 0f);
            bar.pivot = new Vector2(0.5f, 0f);
            bar.anchoredPosition = new Vector2(0f, 4f);
            bar.sizeDelta = new Vector2(0f, 24f);

            var fillObject = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fillObject.transform.SetParent(bar, false);

            var fillRect = fillObject.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = new Vector2(3f, 3f);
            fillRect.offsetMax = new Vector2(-3f, -3f);

            var fill = fillObject.GetComponent<Image>();
            fill.sprite = WhiteSprite;
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = 0;
            fill.color = fillColor;
            return fill;
        }

        private Button CreateButton(RectTransform parent, string name, string label, UnityEngine.Events.UnityAction onClick, Color normalColor)
        {
            var buttonRect = CreatePanel(name, parent, normalColor);
            var button = buttonRect.gameObject.AddComponent<Button>();
            button.targetGraphic = buttonRect.GetComponent<Image>();
            button.onClick.AddListener(onClick);

            var colors = button.colors;
            colors.normalColor = normalColor;
            colors.highlightedColor = Color.Lerp(normalColor, Color.white, 0.14f);
            colors.pressedColor = Color.Lerp(normalColor, Color.black, 0.22f);
            colors.selectedColor = Color.Lerp(normalColor, Color.white, 0.1f);
            colors.disabledColor = new Color(0.18f, 0.19f, 0.2f, 0.55f);
            button.colors = colors;

            var text = CreateText(buttonRect, "Label", label, 17, FontStyle.Bold, TextAnchor.MiddleCenter);
            text.rectTransform.anchorMin = Vector2.zero;
            text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.offsetMin = new Vector2(8f, 4f);
            text.rectTransform.offsetMax = new Vector2(-8f, -4f);
            return button;
        }

        private RectTransform CreatePanel(string name, Transform parent, Color color)
        {
            var rect = CreateRect(name, parent);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = WhiteSprite;
            image.color = color;
            return rect;
        }

        private RectTransform CreateRect(string name, Transform parent)
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
            text.color = new Color(0.93f, 0.95f, 0.94f, 1f);
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
            return text;
        }

        private void SelectLane(int laneIndex)
        {
            selectedLane = laneIndex;
            status = "当前出兵路线切换为" + laneNames[selectedLane] + "。";
            RefreshHud();
        }

        private void SpendCoins(int cost, string successMessage, int eraGain)
        {
            if (coins < cost)
            {
                status = "金币不足，还差 " + (cost - coins) + "。";
                RefreshHud();
                return;
            }

            coins -= cost;
            eraValue = Mathf.Min(eraThreshold, eraValue + eraGain);
            status = successMessage + "。";
            RefreshHud();
        }

        private void GainEraValue()
        {
            eraValue = Mathf.Min(eraThreshold, eraValue + 100);
            status = eraValue >= eraThreshold ? "时代值已满，可以选择进化方向。" : "时代值提升，继续积累后可进化。";
            RefreshHud();
        }

        private void UseEarthquake()
        {
            if (quakeCooldown > 0f)
            {
                return;
            }

            quakeCooldown = 35f;
            enemyHealth = Mathf.Max(0f, enemyHealth - 0.14f);
            eraValue = Mathf.Min(eraThreshold, eraValue + 40);
            status = "地震释放，敌方基地演示扣血。";
            RefreshHud();
        }

        private void UseShield()
        {
            if (shieldCooldown > 0f)
            {
                return;
            }

            shieldCooldown = 50f;
            playerHealth = Mathf.Min(1f, playerHealth + 0.12f);
            status = "护盾屏障启动，己方基地演示回血。";
            RefreshHud();
        }

        private void UpgradeAge(string pathName)
        {
            if (ageIndex >= ageNames.Length - 1)
            {
                status = "已经到达最高时代。";
                RefreshHud();
                return;
            }

            if (eraValue < eraThreshold)
            {
                status = "时代值不足，还需要 " + (eraThreshold - eraValue) + "。";
                RefreshHud();
                return;
            }

            ageIndex++;
            eraValue = 0;
            eraThreshold = Mathf.RoundToInt(eraThreshold * 2.4f);
            incomePerSecond += 2f;
            coins += 75;
            status = "选择" + pathName + "，进入" + ageNames[ageIndex] + "。";
            RefreshHud();
        }

        private void RefreshHud()
        {
            if (coinText == null)
            {
                return;
            }

            coinText.text = "金币 " + coins + " (+" + incomePerSecond.ToString("0") + "/s)";
            eraText.text = "时代 " + ageNames[ageIndex];
            eraValueText.text = "时代值 " + eraValue + " / " + eraThreshold;
            laneText.text = "当前路线：" + laneNames[selectedLane] + "    目标：推进战线并摧毁敌方基地";
            statusText.text = status;

            playerHealthFill.fillAmount = playerHealth;
            enemyHealthFill.fillAmount = enemyHealth;
            eraFill.fillAmount = Mathf.Clamp01((float)eraValue / eraThreshold);

            for (var i = 0; i < laneButtons.Count; i++)
            {
                SetButtonColor(laneButtons[i], i == selectedLane ? new Color(0.82f, 0.56f, 0.24f, 1f) : new Color(0.12f, 0.17f, 0.2f, 1f));
            }

            SetCooldownButton(quakeButton, quakeCooldown, "地震");
            SetCooldownButton(shieldButton, shieldCooldown, "护盾");

            var canUpgrade = eraValue >= eraThreshold && ageIndex < ageNames.Length - 1;
            upgradeAttackButton.interactable = canUpgrade;
            upgradeDefenseButton.interactable = canUpgrade;
        }

        private void SetCooldownButton(Button button, float cooldown, string readyLabel)
        {
            if (button == null)
            {
                return;
            }

            button.interactable = cooldown <= 0f;
            var label = button.GetComponentInChildren<Text>();
            if (label != null)
            {
                label.text = cooldown > 0f ? readyLabel + "\n" + Mathf.CeilToInt(cooldown) + "s" : readyLabel;
            }
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
            colors.highlightedColor = Color.Lerp(normalColor, Color.white, 0.14f);
            colors.pressedColor = Color.Lerp(normalColor, Color.black, 0.22f);
            colors.selectedColor = Color.Lerp(normalColor, Color.white, 0.1f);
            button.colors = colors;
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
    }
}
