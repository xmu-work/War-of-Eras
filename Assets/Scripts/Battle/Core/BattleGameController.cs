using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace WarOfEras.Battle.Core
{
    public sealed class BattleGameController : MonoBehaviour
    {
        public const float PlayerBaseX = -7.45f;
        public const float EnemyBaseX = 7.45f;

        private const string BattleSceneName = "Battle";
        private const float MaxBaseHealth = 1000f;
        private const int TowerCost = 100;

        private static readonly float[] LaneY = { 1.48f, -0.34f, -2.46f };
        private static readonly Vector3[][] LaneRoutes =
        {
            new[]
            {
                new Vector3(-7.35f, 1.02f, 0f),
                new Vector3(-5.95f, 1.35f, 0f),
                new Vector3(-4.45f, 1.18f, 0f),
                new Vector3(-2.6f, 1.5f, 0f),
                new Vector3(-1.05f, 1.7f, 0f),
                new Vector3(0.1f, 0.75f, 0f),
                new Vector3(1.35f, 1.22f, 0f),
                new Vector3(3.1f, 1.58f, 0f),
                new Vector3(5.25f, 1.92f, 0f),
                new Vector3(7.35f, 1.6f, 0f)
            },
            new[]
            {
                new Vector3(-7.35f, -0.38f, 0f),
                new Vector3(-5.65f, -0.32f, 0f),
                new Vector3(-3.55f, -0.28f, 0f),
                new Vector3(-1.35f, -0.32f, 0f),
                new Vector3(0f, -0.5f, 0f),
                new Vector3(1.35f, -0.32f, 0f),
                new Vector3(3.65f, -0.28f, 0f),
                new Vector3(5.8f, -0.08f, 0f),
                new Vector3(7.35f, 0.08f, 0f)
            },
            new[]
            {
                new Vector3(-5.45f, -2.72f, 0f),
                new Vector3(-3.95f, -2.42f, 0f),
                new Vector3(-2.0f, -2.5f, 0f),
                new Vector3(-0.28f, -2.78f, 0f),
                new Vector3(0.78f, -2.08f, 0f),
                new Vector3(1.5f, -1.15f, 0f),
                new Vector3(3.25f, -1.38f, 0f),
                new Vector3(4.95f, -2.15f, 0f),
                new Vector3(6.55f, -2.42f, 0f),
                new Vector3(7.35f, -2.08f, 0f)
            }
        };

        private static readonly Vector3[] PlayerTowerPositions =
        {
            new Vector3(-4.25f, 1.16f, 0f),
            new Vector3(-3.35f, -0.08f, 0f),
            new Vector3(-2.2f, -2.24f, 0f)
        };
        private static Sprite whiteSprite;

        private readonly List<BattleUnit> units = new List<BattleUnit>();
        private readonly List<Button> laneButtons = new List<Button>();
        private readonly List<UnitButtonBinding> unitButtons = new List<UnitButtonBinding>();
        private readonly BattleTower[] playerTowers = new BattleTower[3];

        private readonly string[] laneNames =
        {
            "\u4e0a\u8def",
            "\u4e2d\u8def",
            "\u4e0b\u8def"
        };

        private UnitDefinition[] playerUnitDefinitions;
        private UnitDefinition[] enemyUnitDefinitions;
        private Sprite[] towerFrames;
        private Font uiFont;
        private Transform worldRoot;
        private Text coinText;
        private Text playerHealthText;
        private Text enemyHealthText;
        private Text laneText;
        private Text statusText;
        private Button towerButton;
        private Button restartButton;
        private Image playerHealthFill;
        private Image enemyHealthFill;
        private BattleMapDefinition selectedMap;
        private float incomePerSecond;
        private float enemySpawnIntervalScale;

        private float coins;
        private float playerBaseHealth = MaxBaseHealth;
        private float enemyBaseHealth = MaxBaseHealth;
        private float enemySpawnTimer = 2.5f;
        private float elapsedTime;
        private int selectedLane = 1;
        private bool gameOver;
        private string status;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void CreateForBattleScene()
        {
            if (SceneManager.GetActiveScene().name != BattleSceneName)
            {
                return;
            }

            if (FindFirstObjectByType<BattleGameController>() != null)
            {
                return;
            }

            new GameObject("Battle Game Controller").AddComponent<BattleGameController>();
        }

        private void Awake()
        {
            if (FindObjectsByType<BattleGameController>(FindObjectsSortMode.None).Length > 1)
            {
                Destroy(gameObject);
                return;
            }

            uiFont = CreateUiFont();
            ApplyGameSetup();
            BuildDefinitions();
            ConfigureCamera();
            BuildWorld();
            EnsureEventSystem();
            BuildHud();
            RefreshHud();
        }

        private void Update()
        {
            if (!gameOver)
            {
                elapsedTime += Time.deltaTime;
                coins += incomePerSecond * Time.deltaTime;
                UpdateEnemySpawns();
            }

            RefreshHud();
        }

        public IReadOnlyList<BattleUnit> Units => units;

        public float GetLaneY(int laneIndex)
        {
            return LaneY[Mathf.Clamp(laneIndex, 0, LaneY.Length - 1)];
        }

        public Vector3[] GetLaneRoute(int laneIndex)
        {
            return LaneRoutes[Mathf.Clamp(laneIndex, 0, LaneRoutes.Length - 1)];
        }

        public Vector3 GetLaneSpawnPosition(int team, int laneIndex)
        {
            var route = GetLaneRoute(laneIndex);
            return team == 0 ? route[0] : route[route.Length - 1];
        }

        public Vector3 GetPlayerTowerPosition(int laneIndex)
        {
            return PlayerTowerPositions[Mathf.Clamp(laneIndex, 0, PlayerTowerPositions.Length - 1)];
        }

        public BattleUnit FindNearestEnemy(BattleUnit seeker)
        {
            BattleUnit best = null;
            var bestDistance = float.MaxValue;
            var seekerX = seeker.transform.position.x;
            var direction = seeker.Team == 0 ? 1f : -1f;

            for (var i = 0; i < units.Count; i++)
            {
                var candidate = units[i];
                if (candidate == null || !candidate.IsAlive || candidate.Team == seeker.Team || candidate.LaneIndex != seeker.LaneIndex)
                {
                    continue;
                }

                var signedDistance = (candidate.transform.position.x - seekerX) * direction;
                if (signedDistance < -0.25f)
                {
                    continue;
                }

                var distance = Vector2.Distance(candidate.transform.position, seeker.transform.position);
                if (distance < bestDistance)
                {
                    best = candidate;
                    bestDistance = distance;
                }
            }

            return best;
        }

        public BattleUnit FindTowerTarget(int laneIndex, Vector3 towerPosition, float range)
        {
            BattleUnit best = null;
            var bestDistance = float.MaxValue;

            for (var i = 0; i < units.Count; i++)
            {
                var candidate = units[i];
                if (candidate == null || !candidate.IsAlive || candidate.Team != 1 || candidate.LaneIndex != laneIndex)
                {
                    continue;
                }

                var distance = Mathf.Abs(candidate.transform.position.x - towerPosition.x);
                if (distance <= range && distance < bestDistance)
                {
                    best = candidate;
                    bestDistance = distance;
                }
            }

            return best;
        }

        public void DamageBase(int baseTeam, float damage)
        {
            if (gameOver)
            {
                return;
            }

            if (baseTeam == 0)
            {
                playerBaseHealth = Mathf.Max(0f, playerBaseHealth - damage);
                status = "\u654c\u65b9\u6b63\u5728\u51b2\u51fb\u6211\u65b9\u57fa\u5730\uff01";
                if (playerBaseHealth <= 0f)
                {
                    EndGame(false);
                }
            }
            else
            {
                enemyBaseHealth = Mathf.Max(0f, enemyBaseHealth - damage);
                status = "\u86ee\u8352\u90e8\u843d\u6b63\u5728\u538b\u5411\u654c\u65b9\u57fa\u5730\u3002";
                if (enemyBaseHealth <= 0f)
                {
                    EndGame(true);
                }
            }
        }

        public void NotifyUnitKilled(BattleUnit unit, int attackerTeam)
        {
            units.Remove(unit);
            if (attackerTeam == 0 && !gameOver)
            {
                coins += unit.Definition.Reward;
                status = unit.Definition.DisplayName + "\u51fb\u6e83\u4e86\u654c\u4eba\uff0c\u83b7\u5f97 " + unit.Definition.Reward + " \u91d1\u5e01\u3002";
            }
        }

        private void BuildDefinitions()
        {
            playerUnitDefinitions = new[]
            {
                new UnitDefinition(
                    "\u730e\u77db\u624b",
                    "Hunter",
                    20,
                    120f,
                    18f,
                    1.18f,
                    0.74f,
                    0.9f,
                    0.34f,
                    10,
                    LoadFrames("Barbarian/Units/Hunter/move_", 5, 100f),
                    LoadFrames("Barbarian/Units/Hunter/attack_", 5, 100f)),
                new UnitDefinition(
                    "\u63b7\u77f3\u5974",
                    "Thrower",
                    35,
                    90f,
                    24f,
                    0.92f,
                    2.45f,
                    1.12f,
                    0.33f,
                    16,
                    LoadFrames("Barbarian/Units/Thrower/move_", 5, 100f),
                    LoadFrames("Barbarian/Units/Thrower/attack_", 5, 100f)),
                new UnitDefinition(
                    "\u5de8\u9aa8\u52c7\u58eb",
                    "Champion",
                    85,
                    280f,
                    42f,
                    0.62f,
                    0.84f,
                    1.25f,
                    0.42f,
                    32,
                    LoadFrames("Barbarian/Units/Champion/move_", 5, 100f),
                    LoadFrames("Barbarian/Units/Champion/attack_", 5, 100f)),
                new UnitDefinition(
                    "\u56fe\u817e\u8428\u6ee1",
                    "Shaman",
                    65,
                    145f,
                    30f,
                    0.78f,
                    2.9f,
                    1.35f,
                    0.36f,
                    24,
                    LoadFrames("Barbarian/Units/Shaman/move_", 5, 100f),
                    LoadFrames("Barbarian/Units/Shaman/attack_", 5, 100f))
            };

            enemyUnitDefinitions = BuildEnemyDefinitions(playerUnitDefinitions);
            towerFrames = LoadFrames("Barbarian/Towers/BoneTower/attack_", 5, 100f);
        }

        private void ApplyGameSetup()
        {
            selectedMap = GameSession.SelectedMap;
            coins = GameSession.PlayerStartingCoins;
            incomePerSecond = GameSession.IncomePerSecond;
            enemySpawnIntervalScale = GameSession.EnemySpawnIntervalScale;
            enemySpawnTimer = GameSession.InitialEnemySpawnDelay;
            status = "\u5df2\u9009\u62e9" + selectedMap.DisplayName + "\uff0c\u96be\u5ea6\uff1a" + GameSession.DifficultyName + "\u3002";
        }

        private UnitDefinition[] BuildEnemyDefinitions(UnitDefinition[] baseDefinitions)
        {
            var healthScale = GameSession.EnemyHealthScale;
            var damageScale = GameSession.EnemyDamageScale;
            var definitions = new UnitDefinition[baseDefinitions.Length];

            for (var i = 0; i < baseDefinitions.Length; i++)
            {
                var source = baseDefinitions[i];
                definitions[i] = new UnitDefinition(
                    source.DisplayName,
                    source.Key,
                    source.Cost,
                    source.MaxHealth * healthScale,
                    source.Damage * damageScale,
                    source.Speed,
                    source.AttackRange,
                    source.AttackInterval,
                    source.Scale,
                    source.Reward,
                    source.MoveFrames,
                    source.AttackFrames);
            }

            return definitions;
        }

        private void ConfigureCamera()
        {
            var camera = Camera.main;
            if (camera == null)
            {
                var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
                camera = cameraObject.GetComponent<Camera>();
                cameraObject.tag = "MainCamera";
            }

            camera.transform.position = new Vector3(0f, 0f, -10f);
            camera.orthographic = true;
            camera.orthographicSize = 5.25f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.12f, 0.15f, 0.12f, 1f);
        }

        private void BuildWorld()
        {
            worldRoot = new GameObject("Playable Barbarian Battlefield").transform;
            worldRoot.SetParent(transform, false);

            var mapSprite = LoadSprite(selectedMap.ResourcePath, 100f);
            var map = CreateSprite(selectedMap.DisplayName + " Map", mapSprite, Vector3.zero, 0);
            map.transform.localScale = Vector3.one;
        }

        private void CreateBaseArt()
        {
            var baseSprite = LoadSprite("Barbarian/Base/Base", 100f, new Rect(250f, 80f, 1170f, 740f));
            var playerBase = CreateSprite("Player Barbarian Base Art", baseSprite, new Vector3(PlayerBaseX + 1.25f, -0.16f, 0f), 3);
            playerBase.transform.localScale = Vector3.one * 0.16f;
            playerBase.color = new Color(1f, 0.96f, 0.86f, 0.88f);

            var enemyBase = CreateSprite("Enemy Barbarian Base Art", baseSprite, new Vector3(EnemyBaseX - 1.25f, -0.16f, 0f), 3);
            enemyBase.flipX = true;
            enemyBase.transform.localScale = Vector3.one * 0.16f;
            enemyBase.color = new Color(1f, 0.66f, 0.58f, 0.78f);
        }

        private void CreateLaneMarker(int laneIndex)
        {
            var marker = CreateSprite("Lane " + laneIndex + " Combat Guide", WhiteSprite, new Vector3(0f, LaneY[laneIndex], 0f), 5);
            marker.color = laneIndex == selectedLane
                ? new Color(1f, 0.87f, 0.36f, 0.33f)
                : new Color(0.08f, 0.08f, 0.06f, 0.2f);
            marker.transform.localScale = new Vector3(14.8f, 0.08f, 1f);
        }

        private void CreateBaseMarker(string label, Vector3 position, Color color)
        {
            var marker = CreateSprite(label, WhiteSprite, position, 6);
            marker.color = color;
            marker.transform.localScale = new Vector3(0.38f, 2.2f, 1f);
        }

        private SpriteRenderer CreateSprite(string name, Sprite sprite, Vector3 position, int sortingOrder)
        {
            var spriteObject = new GameObject(name, typeof(SpriteRenderer));
            spriteObject.transform.SetParent(worldRoot, false);
            spriteObject.transform.position = position;

            var renderer = spriteObject.GetComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = sortingOrder;
            return renderer;
        }

        private void BuildHud()
        {
            var canvasObject = new GameObject("Battle Gameplay Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);

            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            var root = CreateRect("HUD Safe Area", canvasObject.transform);
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = new Vector2(24f, 18f);
            root.offsetMax = new Vector2(-24f, -18f);

            BuildTopHud(root);
            BuildLanePanel(root);
            BuildCommandPanel(root);
            BuildStatusPanel(root);
        }

        private void BuildTopHud(RectTransform root)
        {
            var top = CreatePanel("Top HUD", root, new Color(0.07f, 0.08f, 0.07f, 0.9f));
            top.anchorMin = new Vector2(0f, 1f);
            top.anchorMax = new Vector2(1f, 1f);
            top.sizeDelta = new Vector2(0f, 92f);
            top.anchoredPosition = new Vector2(0f, -46f);

            var layout = top.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(18, 18, 12, 12);
            layout.spacing = 18f;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = true;
            layout.childForceExpandWidth = true;

            var playerPanel = CreateHudCell("Player Base Cell", top, "\u6211\u65b9\u57fa\u5730");
            playerHealthText = CreateText(playerPanel, "Player Health", string.Empty, 18, FontStyle.Bold, TextAnchor.MiddleLeft);
            playerHealthText.rectTransform.anchorMin = new Vector2(0.04f, 0.48f);
            playerHealthText.rectTransform.anchorMax = new Vector2(0.96f, 0.9f);
            playerHealthText.rectTransform.offsetMin = Vector2.zero;
            playerHealthText.rectTransform.offsetMax = Vector2.zero;
            playerHealthFill = CreateProgressBar(playerPanel, new Color(0.16f, 0.68f, 0.42f, 1f));

            var resourcePanel = CreateHudCell("Resource Cell", top, "\u86ee\u8352\u8d44\u6e90");
            coinText = CreateText(resourcePanel, "Coins", string.Empty, 30, FontStyle.Bold, TextAnchor.MiddleCenter);
            coinText.rectTransform.anchorMin = new Vector2(0.04f, 0.12f);
            coinText.rectTransform.anchorMax = new Vector2(0.96f, 0.82f);
            coinText.rectTransform.offsetMin = Vector2.zero;
            coinText.rectTransform.offsetMax = Vector2.zero;

            var enemyPanel = CreateHudCell("Enemy Base Cell", top, "\u654c\u65b9\u57fa\u5730");
            enemyHealthText = CreateText(enemyPanel, "Enemy Health", string.Empty, 18, FontStyle.Bold, TextAnchor.MiddleRight);
            enemyHealthText.rectTransform.anchorMin = new Vector2(0.04f, 0.48f);
            enemyHealthText.rectTransform.anchorMax = new Vector2(0.96f, 0.9f);
            enemyHealthText.rectTransform.offsetMin = Vector2.zero;
            enemyHealthText.rectTransform.offsetMax = Vector2.zero;
            enemyHealthFill = CreateProgressBar(enemyPanel, new Color(0.82f, 0.23f, 0.17f, 1f));
        }

        private RectTransform CreateHudCell(string name, RectTransform parent, string title)
        {
            var cell = CreatePanel(name, parent, new Color(0.12f, 0.13f, 0.11f, 0.94f));
            var titleText = CreateText(cell, "Title", title, 16, FontStyle.Bold, TextAnchor.UpperCenter);
            titleText.color = new Color(0.95f, 0.88f, 0.66f, 1f);
            titleText.rectTransform.anchorMin = new Vector2(0.04f, 0.66f);
            titleText.rectTransform.anchorMax = new Vector2(0.96f, 0.98f);
            titleText.rectTransform.offsetMin = Vector2.zero;
            titleText.rectTransform.offsetMax = Vector2.zero;
            return cell;
        }

        private Image CreateProgressBar(RectTransform parent, Color fillColor)
        {
            var background = CreatePanel("Health Background", parent, new Color(0.04f, 0.045f, 0.04f, 1f));
            background.anchorMin = new Vector2(0.04f, 0.12f);
            background.anchorMax = new Vector2(0.96f, 0.38f);
            background.offsetMin = Vector2.zero;
            background.offsetMax = Vector2.zero;

            var fill = CreatePanel("Health Fill", background, fillColor).GetComponent<Image>();
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = 0;
            fill.rectTransform.anchorMin = Vector2.zero;
            fill.rectTransform.anchorMax = Vector2.one;
            fill.rectTransform.offsetMin = Vector2.zero;
            fill.rectTransform.offsetMax = Vector2.zero;
            return fill;
        }

        private void BuildLanePanel(RectTransform root)
        {
            var panel = CreatePanel("Lane Panel", root, new Color(0.07f, 0.08f, 0.07f, 0.88f));
            panel.anchorMin = new Vector2(0f, 0.18f);
            panel.anchorMax = new Vector2(0.18f, 0.52f);
            panel.offsetMin = Vector2.zero;
            panel.offsetMax = Vector2.zero;

            laneText = CreateText(panel, "Lane Status", string.Empty, 18, FontStyle.Bold, TextAnchor.UpperCenter);
            laneText.rectTransform.anchorMin = new Vector2(0.08f, 0.72f);
            laneText.rectTransform.anchorMax = new Vector2(0.92f, 0.95f);
            laneText.rectTransform.offsetMin = Vector2.zero;
            laneText.rectTransform.offsetMax = Vector2.zero;

            for (var i = 0; i < laneNames.Length; i++)
            {
                var laneIndex = i;
                var button = CreateButton(panel, "Lane " + i, laneNames[i], () => SelectLane(laneIndex), new Color(0.13f, 0.17f, 0.14f, 1f));
                var rect = button.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.1f, 0.47f - i * 0.22f);
                rect.anchorMax = new Vector2(0.9f, 0.64f - i * 0.22f);
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                laneButtons.Add(button);
            }
        }

        private void BuildCommandPanel(RectTransform root)
        {
            var panel = CreatePanel("Command Panel", root, new Color(0.07f, 0.08f, 0.07f, 0.92f));
            panel.anchorMin = new Vector2(0.2f, 0f);
            panel.anchorMax = new Vector2(1f, 0.19f);
            panel.offsetMin = Vector2.zero;
            panel.offsetMax = Vector2.zero;

            var layout = panel.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(14, 14, 14, 14);
            layout.spacing = 12f;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = true;
            layout.childForceExpandWidth = true;

            for (var i = 0; i < playerUnitDefinitions.Length; i++)
            {
                var definition = playerUnitDefinitions[i];
                var button = CreateButton(panel, definition.Key, definition.DisplayName + "\n" + definition.Cost + " \u91d1\u5e01", () => TrySpawnPlayerUnit(definition), new Color(0.18f, 0.16f, 0.11f, 1f));
                unitButtons.Add(new UnitButtonBinding(button, definition));
            }

            towerButton = CreateButton(panel, "Bone Tower", "\u5efa\u9020\u9aa8\u77f3\u5854\n" + TowerCost + " \u91d1\u5e01", TryBuildTower, new Color(0.12f, 0.18f, 0.2f, 1f));
            restartButton = CreateButton(panel, "Restart", "\u91cd\u7f6e\u6218\u6597", RestartBattle, new Color(0.16f, 0.13f, 0.19f, 1f));
        }

        private void BuildStatusPanel(RectTransform root)
        {
            var panel = CreatePanel("Status Panel", root, new Color(0.06f, 0.07f, 0.06f, 0.82f));
            panel.anchorMin = new Vector2(0.2f, 0.2f);
            panel.anchorMax = new Vector2(0.66f, 0.27f);
            panel.offsetMin = Vector2.zero;
            panel.offsetMax = Vector2.zero;

            statusText = CreateText(panel, "Status", string.Empty, 17, FontStyle.Bold, TextAnchor.MiddleLeft);
            statusText.rectTransform.anchorMin = Vector2.zero;
            statusText.rectTransform.anchorMax = Vector2.one;
            statusText.rectTransform.offsetMin = new Vector2(14f, 0f);
            statusText.rectTransform.offsetMax = new Vector2(-14f, 0f);
        }

        private void SelectLane(int laneIndex)
        {
            selectedLane = Mathf.Clamp(laneIndex, 0, laneNames.Length - 1);
            status = "\u5df2\u5207\u6362\u5230" + laneNames[selectedLane] + "\u3002";
        }

        private void TrySpawnPlayerUnit(UnitDefinition definition)
        {
            if (gameOver || coins < definition.Cost)
            {
                return;
            }

            coins -= definition.Cost;
            SpawnUnit(definition, 0, selectedLane);
            status = definition.DisplayName + "\u5df2\u6d3e\u5f80" + laneNames[selectedLane] + "\u3002";
        }

        private void TryBuildTower()
        {
            if (gameOver || coins < TowerCost || playerTowers[selectedLane] != null)
            {
                return;
            }

            coins -= TowerCost;
            var towerObject = new GameObject("\u9aa8\u77f3\u5854 - " + laneNames[selectedLane]);
            towerObject.transform.SetParent(worldRoot, false);
            towerObject.transform.position = GetPlayerTowerPosition(selectedLane);

            var tower = towerObject.AddComponent<BattleTower>();
            tower.Configure(this, selectedLane, towerFrames);
            playerTowers[selectedLane] = tower;
            status = "\u5df2\u5728" + laneNames[selectedLane] + "\u5efa\u9020\u9aa8\u77f3\u5854\u3002";
        }

        private void SpawnUnit(UnitDefinition definition, int team, int laneIndex)
        {
            if (gameOver)
            {
                return;
            }

            var unitObject = new GameObject((team == 0 ? "Player " : "Enemy ") + definition.Key);
            unitObject.transform.SetParent(worldRoot, false);

            var unit = unitObject.AddComponent<BattleUnit>();
            unit.Configure(this, definition, team, laneIndex, GetLaneSpawnPosition(team, laneIndex));
            units.Add(unit);
        }

        private void UpdateEnemySpawns()
        {
            enemySpawnTimer -= Time.deltaTime;
            if (enemySpawnTimer > 0f)
            {
                return;
            }

            var lane = Random.Range(0, LaneY.Length);
            var definition = ChooseEnemyDefinition();
            SpawnUnit(definition, 1, lane);
            status = "\u654c\u65b9" + definition.DisplayName + "\u51fa\u73b0\u5728" + laneNames[lane] + "\u3002";

            var pressure = Mathf.Clamp01(elapsedTime / 150f);
            enemySpawnTimer = Mathf.Lerp(4.7f, 2.35f, pressure) * enemySpawnIntervalScale;
        }

        private UnitDefinition ChooseEnemyDefinition()
        {
            var roll = Random.value;
            if (elapsedTime > 75f && roll > 0.76f)
            {
                return enemyUnitDefinitions[2];
            }

            if (elapsedTime > 45f && roll > 0.58f)
            {
                return enemyUnitDefinitions[3];
            }

            return roll < 0.52f ? enemyUnitDefinitions[0] : enemyUnitDefinitions[1];
        }

        private void RestartBattle()
        {
            SceneManager.LoadScene(BattleSceneName);
        }

        private void EndGame(bool playerWon)
        {
            gameOver = true;
            status = playerWon
                ? "\u80dc\u5229\uff01\u86ee\u8352\u90e8\u843d\u653b\u7834\u4e86\u654c\u65b9\u57fa\u5730\u3002"
                : "\u5931\u8d25\uff1a\u6211\u65b9\u57fa\u5730\u5df2\u88ab\u653b\u7834\u3002";
        }

        private void RefreshHud()
        {
            if (coinText != null)
            {
                coinText.text = "\u91d1\u5e01 " + Mathf.FloorToInt(coins);
            }

            if (playerHealthText != null)
            {
                playerHealthText.text = Mathf.CeilToInt(playerBaseHealth) + " / " + Mathf.CeilToInt(MaxBaseHealth);
            }

            if (enemyHealthText != null)
            {
                enemyHealthText.text = Mathf.CeilToInt(enemyBaseHealth) + " / " + Mathf.CeilToInt(MaxBaseHealth);
            }

            if (playerHealthFill != null)
            {
                playerHealthFill.fillAmount = playerBaseHealth / MaxBaseHealth;
            }

            if (enemyHealthFill != null)
            {
                enemyHealthFill.fillAmount = enemyBaseHealth / MaxBaseHealth;
            }

            if (laneText != null)
            {
                laneText.text = "\u51fa\u5175\u8def\u7ebf\uff1a" + laneNames[selectedLane];
            }

            if (statusText != null)
            {
                statusText.text = status;
            }

            for (var i = 0; i < laneButtons.Count; i++)
            {
                SetButtonColor(laneButtons[i], i == selectedLane ? new Color(0.36f, 0.28f, 0.1f, 1f) : new Color(0.13f, 0.17f, 0.14f, 1f));
            }

            for (var i = 0; i < unitButtons.Count; i++)
            {
                var binding = unitButtons[i];
                binding.Button.interactable = !gameOver && coins >= binding.Definition.Cost;
            }

            if (towerButton != null)
            {
                towerButton.interactable = !gameOver && coins >= TowerCost && playerTowers[selectedLane] == null;
            }

            if (restartButton != null)
            {
                restartButton.interactable = true;
            }
        }

        private Sprite[] LoadFrames(string prefix, int frameCount, float pixelsPerUnit)
        {
            var frames = new List<Sprite>();
            for (var i = 1; i <= frameCount; i++)
            {
                var frame = LoadSprite(prefix + i.ToString("00"), pixelsPerUnit);
                if (frame != null)
                {
                    frames.Add(frame);
                }
            }

            if (frames.Count == 0)
            {
                frames.Add(WhiteSprite);
            }

            return frames.ToArray();
        }

        private Sprite LoadSprite(string resourcePath, float pixelsPerUnit)
        {
            var texture = Resources.Load<Texture2D>(resourcePath);
            if (texture == null)
            {
                Debug.LogWarning("Missing battle art resource: " + resourcePath);
                return WhiteSprite;
            }

            return Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                pixelsPerUnit);
        }

        private Sprite LoadSprite(string resourcePath, float pixelsPerUnit, Rect spriteRect)
        {
            var texture = Resources.Load<Texture2D>(resourcePath);
            if (texture == null)
            {
                Debug.LogWarning("Missing battle art resource: " + resourcePath);
                return WhiteSprite;
            }

            var clampedRect = new Rect(
                Mathf.Clamp(spriteRect.x, 0f, texture.width - 1f),
                Mathf.Clamp(spriteRect.y, 0f, texture.height - 1f),
                Mathf.Min(spriteRect.width, texture.width - spriteRect.x),
                Mathf.Min(spriteRect.height, texture.height - spriteRect.y));

            return Sprite.Create(texture, clampedRect, new Vector2(0.5f, 0.5f), pixelsPerUnit);
        }

        private Button CreateButton(RectTransform parent, string name, string label, UnityEngine.Events.UnityAction onClick, Color normalColor)
        {
            var buttonRect = CreatePanel(name, parent, normalColor);
            var button = buttonRect.gameObject.AddComponent<Button>();
            button.targetGraphic = buttonRect.GetComponent<Image>();
            button.onClick.AddListener(onClick);
            SetButtonColor(button, normalColor);

            var text = CreateText(buttonRect, "Label", label, 18, FontStyle.Bold, TextAnchor.MiddleCenter);
            text.rectTransform.anchorMin = Vector2.zero;
            text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.offsetMin = new Vector2(8f, 4f);
            text.rectTransform.offsetMax = new Vector2(-8f, -4f);
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
            colors.disabledColor = new Color(0.19f, 0.19f, 0.18f, 0.72f);
            button.colors = colors;
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
            text.color = new Color(0.95f, 0.91f, 0.82f, 1f);
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

        internal static Sprite SharedWhiteSprite => WhiteSprite;

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

        private readonly struct UnitButtonBinding
        {
            public UnitButtonBinding(Button button, UnitDefinition definition)
            {
                Button = button;
                Definition = definition;
            }

            public Button Button { get; }
            public UnitDefinition Definition { get; }
        }
    }

    public sealed class UnitDefinition
    {
        public UnitDefinition(
            string displayName,
            string key,
            int cost,
            float maxHealth,
            float damage,
            float speed,
            float attackRange,
            float attackInterval,
            float scale,
            int reward,
            Sprite[] moveFrames,
            Sprite[] attackFrames)
        {
            DisplayName = displayName;
            Key = key;
            Cost = cost;
            MaxHealth = maxHealth;
            Damage = damage;
            Speed = speed;
            AttackRange = attackRange;
            AttackInterval = attackInterval;
            Scale = scale;
            Reward = reward;
            MoveFrames = moveFrames;
            AttackFrames = attackFrames;
        }

        public string DisplayName { get; }
        public string Key { get; }
        public int Cost { get; }
        public float MaxHealth { get; }
        public float Damage { get; }
        public float Speed { get; }
        public float AttackRange { get; }
        public float AttackInterval { get; }
        public float Scale { get; }
        public int Reward { get; }
        public Sprite[] MoveFrames { get; }
        public Sprite[] AttackFrames { get; }
    }

    public sealed class BattleUnit : MonoBehaviour
    {
        private BattleGameController controller;
        private SpriteRenderer spriteRenderer;
        private SpriteRenderer shadowRenderer;
        private Vector3[] routePoints;
        private float health;
        private float attackTimer;
        private float frameTimer;
        private float hitFlash;
        private int frameIndex;
        private int routeTargetIndex;
        private bool attacking;

        public UnitDefinition Definition { get; private set; }
        public int Team { get; private set; }
        public int LaneIndex { get; private set; }
        public bool IsAlive => health > 0f;

        public void Configure(BattleGameController owner, UnitDefinition definition, int team, int laneIndex, Vector3 position)
        {
            controller = owner;
            Definition = definition;
            Team = team;
            LaneIndex = laneIndex;
            health = definition.MaxHealth;
            routePoints = owner.GetLaneRoute(laneIndex);
            routeTargetIndex = team == 0 ? 1 : routePoints.Length - 2;

            transform.position = position;
            transform.localScale = Vector3.one * definition.Scale;

            CreateGroundShadow();

            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = definition.MoveFrames[0];
            spriteRenderer.flipX = team == 1;
            spriteRenderer.color = GetBaseTint();
            UpdateGroundShadow();
            UpdateSorting();
        }

        private void Update()
        {
            if (controller == null || !IsAlive)
            {
                return;
            }

            attackTimer -= Time.deltaTime;
            hitFlash = Mathf.Max(0f, hitFlash - Time.deltaTime);
            attacking = false;

            var target = controller.FindNearestEnemy(this);
            if (target != null && Mathf.Abs(target.transform.position.x - transform.position.x) <= Definition.AttackRange)
            {
                attacking = true;
                TryAttackUnit(target);
            }
            else if (IsAtEnemyBase())
            {
                attacking = true;
                TryAttackBase();
            }
            else
            {
                MoveForward();
            }

            UpdateAnimation();
            UpdateTint();
            UpdateSorting();
        }

        public void TakeDamage(float amount, int attackerTeam)
        {
            if (!IsAlive)
            {
                return;
            }

            health -= amount;
            hitFlash = 0.12f;

            if (health <= 0f)
            {
                controller.NotifyUnitKilled(this, attackerTeam);
                Destroy(gameObject);
            }
        }

        private void TryAttackUnit(BattleUnit target)
        {
            if (attackTimer > 0f)
            {
                return;
            }

            target.TakeDamage(Definition.Damage, Team);
            attackTimer = Definition.AttackInterval;
        }

        private void TryAttackBase()
        {
            if (attackTimer > 0f)
            {
                return;
            }

            controller.DamageBase(Team == 0 ? 1 : 0, Definition.Damage);
            attackTimer = Definition.AttackInterval;
        }

        private void MoveForward()
        {
            if (routePoints == null || routePoints.Length == 0 || routeTargetIndex < 0 || routeTargetIndex >= routePoints.Length)
            {
                var direction = Team == 0 ? 1f : -1f;
                transform.position += new Vector3(direction * Definition.Speed * Time.deltaTime, 0f, 0f);
                return;
            }

            var target = routePoints[routeTargetIndex];
            transform.position = Vector3.MoveTowards(transform.position, target, Definition.Speed * Time.deltaTime);

            if (Vector3.Distance(transform.position, target) <= 0.025f)
            {
                routeTargetIndex += Team == 0 ? 1 : -1;
            }
        }

        private bool IsAtEnemyBase()
        {
            if (routePoints != null && routePoints.Length > 0)
            {
                return Team == 0 ? routeTargetIndex >= routePoints.Length : routeTargetIndex < 0;
            }

            return Team == 0
                ? transform.position.x >= BattleGameController.EnemyBaseX - 0.55f
                : transform.position.x <= BattleGameController.PlayerBaseX + 0.55f;
        }

        private void UpdateAnimation()
        {
            var frames = attacking && Definition.AttackFrames.Length > 0 ? Definition.AttackFrames : Definition.MoveFrames;
            if (frames.Length == 0)
            {
                return;
            }

            frameTimer += Time.deltaTime;
            var frameDuration = attacking ? 0.11f : 0.16f;
            if (frameTimer < frameDuration)
            {
                return;
            }

            frameTimer = 0f;
            frameIndex = (frameIndex + 1) % frames.Length;
            spriteRenderer.sprite = frames[frameIndex];
            UpdateGroundShadow();
        }

        private void UpdateTint()
        {
            var baseColor = GetBaseTint();
            spriteRenderer.color = hitFlash > 0f ? Color.Lerp(baseColor, Color.red, 0.55f) : baseColor;
        }

        private Color GetBaseTint()
        {
            return Team == 0 ? Color.white : new Color(1f, 0.86f, 0.8f, 1f);
        }

        private void CreateGroundShadow()
        {
            var shadowObject = new GameObject("Ground Shadow", typeof(SpriteRenderer));
            shadowObject.transform.SetParent(transform, false);

            shadowRenderer = shadowObject.GetComponent<SpriteRenderer>();
            shadowRenderer.sprite = BattleGameController.SharedWhiteSprite;
            shadowRenderer.color = new Color(0.03f, 0.025f, 0.018f, 0.32f);
        }

        private void UpdateGroundShadow()
        {
            if (shadowRenderer == null || spriteRenderer == null || spriteRenderer.sprite == null)
            {
                return;
            }

            var parentScale = Mathf.Max(0.01f, transform.localScale.x);
            var spriteBounds = spriteRenderer.sprite.bounds;
            var worldWidth = Mathf.Clamp(spriteBounds.size.x * transform.localScale.x * 0.68f, 0.36f, 0.95f);
            shadowRenderer.transform.localScale = new Vector3(worldWidth / parentScale, 0.1f / parentScale, 1f);
            shadowRenderer.transform.localPosition = new Vector3(0f, -spriteBounds.extents.y + 0.08f / parentScale, 0f);
        }

        private void UpdateSorting()
        {
            if (spriteRenderer == null)
            {
                return;
            }

            var order = 30 + Mathf.RoundToInt((4.5f - transform.position.y) * 10f);
            spriteRenderer.sortingOrder = order + Team;
            if (shadowRenderer != null)
            {
                shadowRenderer.sortingOrder = order - 1;
            }
        }
    }

    public sealed class BattleTower : MonoBehaviour
    {
        private BattleGameController controller;
        private SpriteRenderer spriteRenderer;
        private SpriteRenderer shadowRenderer;
        private Sprite[] frames;
        private float attackTimer;
        private float frameTimer;
        private int laneIndex;
        private int frameIndex;

        public void Configure(BattleGameController owner, int lane, Sprite[] towerFrames)
        {
            controller = owner;
            laneIndex = lane;
            frames = towerFrames;

            transform.localScale = Vector3.one * 0.2f;
            CreateGroundShadow();

            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = frames != null && frames.Length > 0 ? frames[0] : null;
            spriteRenderer.color = new Color(1f, 0.94f, 0.78f, 1f);
            UpdateGroundShadow();
            UpdateSorting();
        }

        private void Update()
        {
            if (controller == null)
            {
                return;
            }

            attackTimer -= Time.deltaTime;
            Animate();

            if (attackTimer > 0f)
            {
                return;
            }

            var target = controller.FindTowerTarget(laneIndex, transform.position, 3.4f);
            if (target == null)
            {
                return;
            }

            target.TakeDamage(34f, 0);
            attackTimer = 1.05f;
        }

        private void Animate()
        {
            if (frames == null || frames.Length == 0)
            {
                return;
            }

            frameTimer += Time.deltaTime;
            if (frameTimer < 0.16f)
            {
                return;
            }

            frameTimer = 0f;
            frameIndex = (frameIndex + 1) % frames.Length;
            spriteRenderer.sprite = frames[frameIndex];
            UpdateGroundShadow();
        }

        private void CreateGroundShadow()
        {
            var shadowObject = new GameObject("Tower Shadow", typeof(SpriteRenderer));
            shadowObject.transform.SetParent(transform, false);

            shadowRenderer = shadowObject.GetComponent<SpriteRenderer>();
            shadowRenderer.sprite = BattleGameController.SharedWhiteSprite;
            shadowRenderer.color = new Color(0.025f, 0.02f, 0.015f, 0.28f);
        }

        private void UpdateGroundShadow()
        {
            if (shadowRenderer == null || spriteRenderer == null || spriteRenderer.sprite == null)
            {
                return;
            }

            var parentScale = Mathf.Max(0.01f, transform.localScale.x);
            var spriteBounds = spriteRenderer.sprite.bounds;
            var worldWidth = Mathf.Clamp(spriteBounds.size.x * transform.localScale.x * 0.72f, 0.65f, 1.35f);
            shadowRenderer.transform.localScale = new Vector3(worldWidth / parentScale, 0.16f / parentScale, 1f);
            shadowRenderer.transform.localPosition = new Vector3(0f, -spriteBounds.extents.y + 0.12f / parentScale, 0f);
        }

        private void UpdateSorting()
        {
            if (spriteRenderer == null)
            {
                return;
            }

            var order = 24 + Mathf.RoundToInt((4.5f - transform.position.y) * 10f);
            spriteRenderer.sortingOrder = order;
            if (shadowRenderer != null)
            {
                shadowRenderer.sortingOrder = order - 1;
            }
        }
    }
}
