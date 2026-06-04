using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
#endif
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace WarOfEras.Battle.Core
{
    public sealed partial class BattleGameController : MonoBehaviour
    {
        // 战斗场景的主控制器：负责生成战场、维护经济/时代/AI/HUD，并调度运行时单位与设施。
        public const float PlayerBaseX = -18.363636f;
        public const float EnemyBaseX = 18.363636f;

        private const string MainMenuSceneName = "MainMenu";
        private const string BattleSceneName = "Battle";
        private const string DefeatBackdropPath = "Battle/Outcome/DefeatBackdrop";
        private const string VictoryBackdropPath = "Battle/Outcome/VictoryBackdrop";
        private const string BuilderUnitMoveFramePrefix = "Battle/Facilities/Builder_move_";
        private const string BuilderUnitAttackFramePrefix = "Battle/Facilities/Builder_attack_";
        private const string FrontlineBedClipPath = "Audio/Ambience/00_three_lane_frontline_bed_loop";
        private const float CommandTooltipDelaySeconds = 0.5f;
        private const int CommandTooltipFontSize = 24;
        private const int CommandTooltipMinFontSize = 16;
        private const int CommandTooltipMaxFontSize = 24;
        private const float HudReferenceHeight = 1080f;
        private const float TopControlBarHeight = 207f;
        private const float BottomControlBarHeight = 231f;
        private const float MapViewportBottomNormalized = BottomControlBarHeight / HudReferenceHeight;
        private const float MapViewportHeightNormalized = 1f - ((TopControlBarHeight + BottomControlBarHeight) / HudReferenceHeight);
        private const float UnitCommandButtonSize = 90f;
        private const float SkillCommandButtonSize = 78f;
        private const float CommandGridGap = 8f;
        private const float EraValuePerSecond = 3.5f;
        private const float FrontlineBedVolume = 0.18f;
        private const float EraAmbienceVolume = 0.42f;
        private const float EraAmbienceFadeDuration = 1.35f;
        private const float MobilizationCostMultiplier = 0.8f;
        private const float MobilizationSpeedMultiplier = 1.15f;
        private const float ShieldBarrierCooldownSeconds = 50f;
        private const float ShieldBarrierDurationSeconds = 4f;
        private const float MobilizationCooldownSeconds = 60f;
        private const float MobilizationDurationSeconds = 8f;
        private const float MapTextureWidth = 2400f;
        private const float MapTextureHeight = 1350f;
        private const float MapPixelsPerUnit = 55f;
        // 基地先从地图中心线上移地图宽度的 1/30，再按整张地图宽度下调 1/40；最终等于上移地图宽度的 1/120。
        private const float RaisedBasePixelY = 675f - MapTextureWidth / 30f + MapTextureWidth / 40f;
        private const float CameraOrthographicSize = 5f;
        private const float CameraMinOrthographicSize = 3.4f;
        private const float CameraMaxOrthographicSize = 9f;
        private const float CameraZoomStep = 0.55f;
        private const float CameraEdgeScrollMargin = 36f;
        private const float CameraEdgeScrollSpeed = 20f;
        private const float DefaultUnitVisualScale = 1.24f;
        private const float DefaultTowerVisualScale = 0.76f;
        private const float DefaultBaseVisualScale = 0.56f;
        private const float DefaultResourceWellVisualScale = 0.84f;
        private const bool PlayerBaseFlipX = true;
        private const bool EnemyBaseFlipX = false;
        private const int ResourceWellCost = 120;
        private const float ResourceWellIncomeBonus = 2.5f;
        private const float ResourceWellEraValue = 65f;
        private const float BuildPlacementClickRadius = 1.05f;
        private const float BuilderConstructionRange = 2.9f;
        private const float RouteReachableRadius = 0.62f;
        private const float RouteRecoveryRadius = 1.15f;
        private const float UnitCombatContactPadding = 0.34f;
        private const float UnitSelectionDragThreshold = 10f;
        private const float UnitClickSelectionRadius = 0.75f;
        private const float UnitSelectionBoxBorderThickness = 2f;
        private const float ClickMarkerDuration = 0.72f;
        private const float EnemyEraValuePerSecond = 2.85f;
        private const float EnemySpawnEraCostMultiplier = 0.28f;
        private const float EnemyFacilityEraCostMultiplier = 0.24f;

        private static readonly string[] AgeNames =
        {
            "\u86ee\u8352\u90e8\u843d",
            "\u673a\u68b0\u5de5\u574a",
            "\u7535\u529b\u65f6\u4ee3",
            "\u6838\u80fd\u7eaa\u5143",
            "\u661f\u6d77\u6587\u660e"
        };

        private static readonly string[] AgeAmbienceClipPaths =
        {
            "Audio/Ambience/01_primitive_tribe_war_drums_loop",
            "Audio/Ambience/02_mechanical_workshop_steam_gears_loop",
            "Audio/Ambience/03_electric_tesla_grid_loop",
            "Audio/Ambience/04_nuclear_reactor_zone_loop",
            "Audio/Ambience/05_star_ocean_timeslow_loop"
        };

        private static readonly string[] AttackPathNames =
        {
            "\u8840\u6012\u72e9\u730e",
            "\u8d85\u538b\u8fde\u53d1",
            "\u8d85\u8f7d\u7a81\u88ad",
            "\u88c2\u53d8\u51b2\u950b",
            "\u866b\u6d1e\u673a\u52a8"
        };

        private static readonly string[] DefensePathNames =
        {
            "\u77f3\u58c1\u5de5\u4e8b",
            "\u94a2\u94c1\u5821\u5792",
            "\u7535\u7f51\u5c01\u9501",
            "\u53cd\u5e94\u5806\u58c1\u5792",
            "\u661f\u76fe\u7edf\u6cbb"
        };

        private static readonly int[] EraThresholds = { 500, 1800, 6000, 20000 };
        private static readonly float[] BaseHealthByAge = { 1200f, 2200f, 4200f, 7800f, 14000f };
        private static readonly int[] ShieldAbsorbByAge = { 600, 1000, 1600, 2500, 3800 };

        private static readonly Color[] AgeTints =
        {
            new Color(1f, 0.94f, 0.78f, 1f),
            new Color(0.88f, 0.86f, 0.78f, 1f),
            new Color(0.66f, 0.88f, 1f, 1f),
            new Color(0.76f, 1f, 0.64f, 1f),
            new Color(0.86f, 0.72f, 1f, 1f)
        };

        private static readonly AgeVisualSet[] AgeVisualSets =
        {
            new AgeVisualSet(
                "Barbarian",
                "Battle/Maps/PixelFrontline_Barbarian",
                "Barbarian/Base/Base",
                "Barbarian/Units",
                new[] { "Hunter", "Thrower", "BoneArcher", "TuskRider", "Champion" },
                new[]
                {
                    "Barbarian/Towers/BoneTower/attack_",
                    "Barbarian/Towers/SlingNest/attack_",
                    "Barbarian/Towers/MammothTotem/attack_"
                },
                new Color(1f, 0.94f, 0.78f, 1f)),
            new AgeVisualSet(
                "Machine",
                "Battle/Maps/PixelFrontline_Machine",
                "Machine/Base/Base",
                "Machine/Units",
                new[] { "GearSoldier", "SteamCrossbow", "BoilerGrenadier", "SiegeRoller", "ClockworkGuard" },
                new[]
                {
                    "Machine/Towers/GearTower/attack_",
                    "Machine/Towers/SteamCannonTower/attack_",
                    "Machine/Towers/RivetMortar/attack_"
                },
                new Color(0.88f, 0.86f, 0.78f, 1f)),
            new AgeVisualSet(
                "Electric",
                "Battle/Maps/PixelFrontline_Electric",
                "Electric/Base/Base",
                "Electric/Units",
                new[] { "VoltGuard", "ArcRunner", "CoilShooter", "CrawlerTank", "ThunderMech" },
                new[]
                {
                    "Electric/Towers/TeslaTower/attack_",
                    "Electric/Towers/ArcPylon/attack_",
                    "Electric/Towers/RailgunTower/attack_"
                },
                new Color(0.66f, 0.88f, 1f, 1f)),
            new AgeVisualSet(
                "Nuclear",
                "Battle/Maps/PixelFrontline_Nuclear",
                "Nuclear/Base/Base",
                "Nuclear/Units",
                new[] { "RadTrooper", "IsotopeScout", "FissionLancer", "ReactorWalker", "NuclearTank" },
                new[]
                {
                    "Nuclear/Towers/ParticleGunTower/attack_",
                    "Nuclear/Towers/ReactorMortar/attack_",
                    "Nuclear/Towers/FalloutObelisk/attack_"
                },
                new Color(0.76f, 1f, 0.64f, 1f)),
            new AgeVisualSet(
                "Starsea",
                "Battle/Maps/PixelFrontline_Starsea",
                "Starsea/Base/Base",
                "Starsea/Units",
                new[] { "LaserTrooper", "PhotonBlade", "SkimmerMech", "GravityDrone", "AntimatterColossus" },
                new[]
                {
                    "Starsea/Towers/TitaniumRayTower/attack_",
                    "Starsea/Towers/PlasmaSpire/attack_",
                    "Starsea/Towers/SingularityBeacon/attack_"
                },
                new Color(0.86f, 0.72f, 1f, 1f))
        };

        private static readonly AgePowerDefinition[] AgePowers =
        {
            new AgePowerDefinition("\u5730\u9707", 35f, 220f, true, 1.5f, 0.15f, 2.2f),
            new AgePowerDefinition("\u7a7a\u6295\u70b8\u5f39", 35f, 180f, true, 0f, 1f, 1f),
            new AgePowerDefinition("\u5168\u573a\u7535\u51fb", 38f, 200f, true, 3f, 0.7f, 1.15f),
            new AgePowerDefinition("\u8f90\u5c04\u7981\u533a", 42f, 280f, true, 8f, 0.82f, 1.05f),
            new AgePowerDefinition("\u65f6\u7a7a\u51cf\u901f", 45f, 180f, true, 6f, 0.5f, 1.35f)
        };

        private enum EvolutionPath
        {
            Balanced,
            Attack,
            Defense
        }

        private enum BuildPlacementKind
        {
            None,
            Tower,
            ResourceWell
        }

        private enum CommandIconKind
        {
            AgePower,
            Shield,
            Mobilization,
            AttackEvolution,
            DefenseEvolution,
            Restart
        }

        private static readonly float[] LaneY = { 3.2f, 0f, -5.1f };
        private static readonly Vector3[][] LaneRoutes =
        {
            new[]
            {
                MapPoint(190f, RaisedBasePixelY),
                MapPoint(214f, 575f),
                MapPoint(228f, 511f),
                MapPoint(271f, 478f),
                MapPoint(314f, 448f),
                MapPoint(357f, 408f),
                MapPoint(430f, 385f),
                MapPoint(616f, 385f),
                MapPoint(702f, 399f),
                MapPoint(831f, 386f),
                MapPoint(1003f, 379f),
                MapPoint(1133f, 388f),
                MapPoint(1348f, 384f),
                MapPoint(1434f, 384f),
                MapPoint(1477f, 344f),
                MapPoint(1520f, 304f),
                MapPoint(1563f, 355f),
                MapPoint(1606f, 378f),
                MapPoint(1778f, 382f),
                MapPoint(1951f, 391f),
                MapPoint(2037f, 397f),
                MapPoint(2080f, 440f),
                MapPoint(2123f, 511f),
                MapPoint(2166f, 560f),
                MapPoint(2210f, RaisedBasePixelY)
            },
            new[]
            {
                MapPoint(190f, RaisedBasePixelY),
                MapPoint(228f, 665f),
                MapPoint(314f, 676f),
                MapPoint(487f, 676f),
                MapPoint(659f, 672f),
                MapPoint(831f, 670f),
                MapPoint(1003f, 670f),
                MapPoint(1176f, 669f),
                MapPoint(1305f, 656f),
                MapPoint(1434f, 662f),
                MapPoint(1606f, 679f),
                MapPoint(1778f, 668f),
                MapPoint(1951f, 686f),
                MapPoint(2080f, 676f),
                MapPoint(2210f, RaisedBasePixelY)
            },
            new[]
            {
                MapPoint(190f, RaisedBasePixelY),
                MapPoint(185f, 739f),
                MapPoint(198f, 830f),
                MapPoint(228f, 909f),
                MapPoint(271f, 950f),
                MapPoint(357f, 971f),
                MapPoint(530f, 976f),
                MapPoint(702f, 945f),
                MapPoint(831f, 941f),
                MapPoint(1003f, 961f),
                MapPoint(1133f, 968f),
                MapPoint(1305f, 976f),
                MapPoint(1477f, 951f),
                MapPoint(1649f, 947f),
                MapPoint(1822f, 965f),
                MapPoint(1951f, 968f),
                MapPoint(2037f, 970f),
                MapPoint(2123f, 913f),
                MapPoint(2166f, 873f),
                MapPoint(2166f, 796f),
                MapPoint(2210f, RaisedBasePixelY)
            }
        };

        // 这些节点和边组成整张地图的可行走图。框选士兵重定向和建筑兵派工都会从这里计算最短路径。
        private static readonly Vector3[] RouteConnectorNodes =
        {
            MapPoint(993f, 341f),
            MapPoint(993f, 274f),
            MapPoint(702f, 544f),
            MapPoint(788f, 598f),
            MapPoint(874f, 832f),
            MapPoint(851f, 920f),
            MapPoint(1391f, 457f),
            MapPoint(1348f, 559f),
            MapPoint(1348f, 774f),
            MapPoint(1391f, 806f),
            MapPoint(1434f, 836f),
            MapPoint(1454f, 920f),
            MapPoint(1580f, 618f),
            MapPoint(1586f, 562f),
            MapPoint(1133f, 1026f),
            MapPoint(1168f, 1135f),
            MapPoint(1973f, 1019f),
            MapPoint(1973f, 1049f),
            MapPoint(2209f, 841f)
        };

        private static readonly Vector3[] RouteNodes = BuildRouteNodes(LaneRoutes);
        private static readonly RouteEdge[] RouteEdges = BuildRouteEdges(RouteNodes, LaneRoutes);
        private static readonly int[] PlayerRouteStartNodes = BuildPlayerRouteStartNodes(LaneRoutes);

        // 敌我双方共用同一批设施槽位；建造时会检查双方数组，避免同一位置重复占用。
        private static readonly Vector3[] SharedFacilityPositions =
        {
            MapPoint(191f, 363f),
            MapPoint(942f, 231f),
            MapPoint(1932f, 236f),
            MapPoint(2135f, 318f),
            MapPoint(764f, 793f),
            MapPoint(1923f, 848f),
            MapPoint(2243f, 1003f),
            MapPoint(763f, 1027f),
            MapPoint(321f, 1064f),
            MapPoint(1291f, 1116f),
            MapPoint(1886f, 1121f)
        };

        private static readonly Vector3[] SharedResourceWellPositions =
        {
            MapPoint(1586f, 562f)
        };

        private static readonly Vector3[] PlayerTowerPositions = SharedFacilityPositions;
        private static readonly Vector3[] EnemyTowerPositions = SharedFacilityPositions;

        private static readonly Vector3[] PlayerResourceWellPositions = SharedResourceWellPositions;
        private static readonly Vector3[] EnemyResourceWellPositions = SharedResourceWellPositions;

        private static Sprite whiteSprite;
        private static Sprite panelSprite;
        private static Sprite buttonSprite;
        private static Sprite iconDiscSprite;
        private static Sprite vfxCircleSprite;
        private static Sprite towerBuildMarkerSprite;
        private static Sprite resourceWellBuildMarkerSprite;
        private static Sprite resourceWellSiteSprite;
        private static Sprite resourceWellBuiltSprite;
        private static Sprite[] agePowerIconSprites;
        private static Sprite shieldIconSprite;
        private static Sprite mobilizationIconSprite;
        private static Sprite attackEvolutionIconSprite;
        private static Sprite defenseEvolutionIconSprite;
        private static Sprite restartIconSprite;

        private readonly List<BattleUnit> units = new List<BattleUnit>();
        private readonly List<BattleUnit> selectedPlayerUnits = new List<BattleUnit>();
        private readonly List<Button> laneButtons = new List<Button>();
        private readonly List<UnitButtonBinding> unitButtons = new List<UnitButtonBinding>();
        private readonly List<TowerButtonBinding> towerButtons = new List<TowerButtonBinding>();
        private readonly List<BuildPlacementPreview> buildPlacementPreviews = new List<BuildPlacementPreview>();
        private readonly List<string> statusLog = new List<string>();
        private BattleTower[] playerTowers = new BattleTower[PlayerTowerPositions.Length];
        private BattleTower[] enemyTowers = new BattleTower[EnemyTowerPositions.Length];
        private BattleResourceWell[] playerResourceWells = new BattleResourceWell[PlayerResourceWellPositions.Length];
        private BattleResourceWell[] enemyResourceWells = new BattleResourceWell[EnemyResourceWellPositions.Length];
        private bool[] pendingPlayerTowerBuilds = new bool[PlayerTowerPositions.Length];
        private int[] pendingPlayerTowerTypeIndexes = new int[PlayerTowerPositions.Length];
        private bool[] pendingPlayerResourceWellBuilds = new bool[PlayerResourceWellPositions.Length];
        private bool[] pendingEnemyTowerBuilds = new bool[EnemyTowerPositions.Length];
        private int[] pendingEnemyTowerTypeIndexes = new int[EnemyTowerPositions.Length];
        private bool[] pendingEnemyResourceWellBuilds = new bool[EnemyResourceWellPositions.Length];

        private readonly string[] laneNames =
        {
            "\u4e0a\u8def",
            "\u4e2d\u8def",
            "\u4e0b\u8def"
        };

        private UnitDefinition[] playerUnitDefinitions;
        private UnitDefinition[] enemyUnitDefinitions;
        private TowerDefinition[] currentTowerDefinitions;
        private TowerDefinition[] enemyTowerDefinitions;
        private TowerDefinition currentTowerDefinition;
        private Sprite[][] towerFrameSets;
        private Sprite[][] enemyTowerFrameSets;
        private Sprite[] towerFrames;
        private Font uiFont;
        private Camera gameplayCamera;
        private BattleLayout battleLayout;
        private Transform worldRoot;
        private Transform buildPreviewRoot;
        private Transform facilityMarkerRoot;
        private SpriteRenderer playerBaseRenderer;
        private SpriteRenderer enemyBaseRenderer;
        private Material vfxLineMaterial;
        private RectTransform startHintPanel;
        private RectTransform outcomeOverlay;
        private Text coinText;
        private Text ageText;
        private Text eraText;
        private Text playerHealthText;
        private Text enemyHealthText;
        private Text laneText;
        private Text statusText;
        private Text outcomeTitleShadowText;
        private Text outcomeTitleText;
        private Text outcomeSubtitleText;
        private Text outcomeStatsText;
        private RectTransform commandTooltip;
        private Text commandTooltipText;
        private Coroutine commandTooltipDelayRoutine;
        private Func<string> commandTooltipProvider;
        private Vector2 commandTooltipPointerPosition;
        private AudioSource frontlineBedSource;
        private AudioSource eraAmbienceSource;
        private AudioSource fadingEraAmbienceSource;
        private Button resourceWellButton;
        private Button agePowerButton;
        private Button shieldButton;
        private Button mobilizationButton;
        private Button attackUpgradeButton;
        private Button defenseUpgradeButton;
        private Button restartButton;
        private Image playerHealthFill;
        private Image enemyHealthFill;
        private Image eraFill;
        private Image outcomeBackdropImage;
        private BattleMapDefinition selectedMap;
        private SpriteRenderer battlefieldMapRenderer;
        private Vector3[][] laneRoutes = LaneRoutes;
        private Vector3[] routeNodes = RouteNodes;
        private RouteEdge[] routeEdges = RouteEdges;
        private int[] playerRouteStartNodes = PlayerRouteStartNodes;
        private Vector3[] playerTowerPositions = PlayerTowerPositions;
        private Vector3[] enemyTowerPositions = EnemyTowerPositions;
        private Vector3[] playerResourceWellPositions = PlayerResourceWellPositions;
        private Vector3[] enemyResourceWellPositions = EnemyResourceWellPositions;
        private Vector3 playerBasePosition = MapPoint(190f, RaisedBasePixelY);
        private Vector3 enemyBasePosition = MapPoint(2210f, RaisedBasePixelY);
        private float unitVisualScale = DefaultUnitVisualScale;
        private float towerVisualScale = DefaultTowerVisualScale;
        private float baseVisualScale = DefaultBaseVisualScale;
        private float resourceWellVisualScale = DefaultResourceWellVisualScale;
        private float incomePerSecond;
        private float enemySpawnIntervalScale;

        private float coins;
        private float playerBaseMaxHealth = BaseHealthByAge[0];
        private float enemyBaseMaxHealth = BaseHealthByAge[0];
        private float playerBaseHealth = BaseHealthByAge[0];
        private float enemyBaseHealth = BaseHealthByAge[0];
        private float eraValue;
        private float enemyEraValue;
        private float agePowerCooldown;
        private float shieldCooldown;
        private float mobilizationCooldown;
        private float shieldTimer;
        private float mobilizationTimer;
        private float playerShield;
        private float playerDamageMultiplier = 1f;
        private float playerHealthMultiplier = 1f;
        private float playerSpeedMultiplier = 1f;
        private float towerDamageMultiplier = 1f;
        private float baseDamageReduction;
        private float enemySpawnTimer = 2.5f;
        private float elapsedTime;
        private float ambienceFadeElapsed;
        private int ageIndex;
        private int enemyAgeIndex;
        private int unitDefinitionBuildAgeIndex;
        private int selectedTowerIndex;
        private int currentAmbienceAgeIndex = -1;
        private int selectedLane = 1;
        private Vector3 selectionStartScreenPosition;
        private Vector3 selectionCurrentScreenPosition;
        private bool gameStarted;
        private bool gameOver;
        private bool hasMapBounds;
        private bool isSelectingUnits;
        private bool isCameraDragging;
        private bool ambienceFadeActive;
        private BuildPlacementKind activeBuildPlacement = BuildPlacementKind.None;
        private Vector2 mapBoundsMin;
        private Vector2 mapBoundsMax;
        private Vector3 lastCameraDragScreenPosition;
        private string currentStatus;
        private EvolutionPath evolutionPath = EvolutionPath.Balanced;

        private string status
        {
            get => currentStatus;
            set => SetStatus(value);
        }

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
            InitializeBattleLayout();
            BuildDefinitions();
            ConfigureCamera();
            ConfigureAmbienceAudio();
            BuildWorld();
            EnsureEventSystem();
            BuildHud();
            RefreshHud();
        }

        private void Update()
        {
            UpdateCameraNavigation();
            UpdateAmbienceFade();

            if (!gameStarted)
            {
                TryStartGameFromMapClick();
                RefreshHud();
                return;
            }

            if (!gameOver)
            {
                elapsedTime += Time.deltaTime;
                coins += incomePerSecond * Time.deltaTime;
                GainEraValue(EraValuePerSecond * Time.deltaTime);
                GainEnemyEraValue(EnemyEraValuePerSecond * Time.deltaTime);
                UpdateTimedEffects();
                UpdateEnemyFacilities();
                UpdateEnemySpawns();
                if (!UpdateBuildPlacementInput())
                {
                    UpdateRoutePlanningInput();
                }
            }

            RefreshHud();
        }

        public IReadOnlyList<BattleUnit> Units => units;

        public float TowerDamageMultiplier => towerDamageMultiplier;

        public bool IsGameOver => gameOver;

        public float UnitVisualScale => unitVisualScale;

        public float TowerVisualScale => towerVisualScale;

        public float GetLaneY(int laneIndex)
        {
            var route = GetLaneRoute(laneIndex);
            if (route.Length > 0)
            {
                return route[Mathf.Clamp(route.Length / 2, 0, route.Length - 1)].y;
            }

            return LaneY[Mathf.Clamp(laneIndex, 0, LaneY.Length - 1)];
        }

        public Vector3[] GetLaneRoute(int laneIndex)
        {
            return laneRoutes[Mathf.Clamp(laneIndex, 0, laneRoutes.Length - 1)];
        }

        public static float GetUnitEngageDistance(UnitDefinition definition)
        {
            if (definition == null)
            {
                return 0.9f;
            }

            return definition.AttackRange > 1.25f
                ? definition.AttackRange
                : Mathf.Max(definition.AttackRange + UnitCombatContactPadding, 0.92f);
        }

        private static Vector3 MapPoint(float pixelX, float pixelY)
        {
            return new Vector3(
                (pixelX - MapTextureWidth * 0.5f) / MapPixelsPerUnit,
                (MapTextureHeight * 0.5f - pixelY) / MapPixelsPerUnit,
                0f);
        }

        private static Vector3[] BuildRouteNodes(Vector3[][] routes)
        {
            var count = RouteConnectorNodes.Length;
            for (var i = 0; i < routes.Length; i++)
            {
                count += routes[i] != null ? routes[i].Length : 0;
            }

            var nodes = new List<Vector3>(count);
            for (var i = 0; i < routes.Length; i++)
            {
                if (routes[i] != null)
                {
                    nodes.AddRange(routes[i]);
                }
            }

            nodes.AddRange(RouteConnectorNodes);
            return nodes.ToArray();
        }

        private static RouteEdge[] BuildRouteEdges(Vector3[] nodes, Vector3[][] routes)
        {
            var edges = new List<RouteEdge>();
            var offsets = BuildRouteOffsets(routes);
            for (var routeIndex = 0; routeIndex < routes.Length; routeIndex++)
            {
                var route = routes[routeIndex];
                if (route == null)
                {
                    continue;
                }

                AddSequentialRouteEdges(edges, offsets[routeIndex], route.Length);
            }

            if (routes.Length >= 3
                && routes[0] != null
                && routes[1] != null
                && routes[2] != null
                && routes[0].Length > 0
                && routes[1].Length > 0
                && routes[2].Length > 0)
            {
                AddRouteEdge(edges, offsets[0], offsets[1]);
                AddRouteEdge(edges, offsets[1], offsets[2]);
                AddRouteEdge(edges, offsets[0] + routes[0].Length - 1, offsets[1] + routes[1].Length - 1);
                AddRouteEdge(edges, offsets[1] + routes[1].Length - 1, offsets[2] + routes[2].Length - 1);
            }

            AddRouteConnection(edges, nodes, MapPoint(1003f, 379f), MapPoint(993f, 341f), MapPoint(993f, 274f));
            AddRouteConnection(edges, nodes, MapPoint(702f, 399f), MapPoint(702f, 544f), MapPoint(788f, 598f), MapPoint(831f, 670f), MapPoint(874f, 832f), MapPoint(851f, 920f), MapPoint(831f, 941f));
            AddRouteConnection(edges, nodes, MapPoint(1348f, 384f), MapPoint(1391f, 457f), MapPoint(1348f, 559f), MapPoint(1305f, 656f), MapPoint(1348f, 774f), MapPoint(1391f, 806f), MapPoint(1434f, 836f), MapPoint(1454f, 920f), MapPoint(1477f, 951f));
            AddRouteConnection(edges, nodes, MapPoint(1606f, 679f), MapPoint(1580f, 618f), MapPoint(1586f, 562f));
            AddRouteConnection(edges, nodes, MapPoint(1133f, 968f), MapPoint(1133f, 1026f), MapPoint(1168f, 1135f));
            AddRouteConnection(edges, nodes, MapPoint(1951f, 968f), MapPoint(1973f, 1019f), MapPoint(1973f, 1049f));
            AddRouteConnection(edges, nodes, MapPoint(2166f, 873f), MapPoint(2209f, 841f), MapPoint(2166f, 796f));
            return edges.ToArray();
        }

        private static int[] BuildPlayerRouteStartNodes(Vector3[][] routes)
        {
            var offsets = BuildRouteOffsets(routes);
            var starts = new List<int>(routes.Length);
            for (var i = 0; i < routes.Length; i++)
            {
                if (routes[i] != null && routes[i].Length > 0)
                {
                    starts.Add(offsets[i]);
                }
            }

            return starts.ToArray();
        }

        private static int[] BuildRouteOffsets(Vector3[][] routes)
        {
            var offsets = new int[routes.Length];
            var nextOffset = 0;
            for (var i = 0; i < routes.Length; i++)
            {
                offsets[i] = nextOffset;
                nextOffset += routes[i] != null ? routes[i].Length : 0;
            }

            return offsets;
        }

        private static void AddSequentialRouteEdges(List<RouteEdge> edges, int startIndex, int length)
        {
            for (var i = 0; i < length - 1; i++)
            {
                AddRouteEdge(edges, startIndex + i, startIndex + i + 1);
            }
        }

        private static void AddRouteConnection(List<RouteEdge> edges, Vector3[] nodes, params Vector3[] points)
        {
            for (var i = 0; i < points.Length - 1; i++)
            {
                AddRouteEdge(edges, FindClosestRouteNode(nodes, points[i]), FindClosestRouteNode(nodes, points[i + 1]));
            }
        }

        private static int FindClosestRouteNode(Vector3[] nodes, Vector3 point)
        {
            var bestIndex = -1;
            var bestDistance = float.PositiveInfinity;
            for (var i = 0; i < nodes.Length; i++)
            {
                var distance = (nodes[i] - point).sqrMagnitude;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestIndex = i;
                }
            }

            return bestIndex;
        }

        private static void AddRouteEdge(List<RouteEdge> edges, int a, int b)
        {
            if (a < 0 || b < 0 || a == b)
            {
                return;
            }

            for (var i = 0; i < edges.Count; i++)
            {
                var edge = edges[i];
                if ((edge.A == a && edge.B == b) || (edge.A == b && edge.B == a))
                {
                    return;
                }
            }

            edges.Add(new RouteEdge(a, b));
        }

        public Vector3 GetLaneSpawnPosition(int team, int laneIndex)
        {
            var route = GetLaneRoute(laneIndex);
            return team == 0 ? route[0] : route[route.Length - 1];
        }

        public Vector3 GetPlayerTowerPosition(int laneIndex)
        {
            return playerTowerPositions[Mathf.Clamp(laneIndex, 0, playerTowerPositions.Length - 1)];
        }

        public Vector3 GetEnemyTowerPosition(int laneIndex)
        {
            return enemyTowerPositions[Mathf.Clamp(laneIndex, 0, enemyTowerPositions.Length - 1)];
        }

        public Vector3 GetBasePosition(int team)
        {
            return team == 0 ? playerBasePosition : enemyBasePosition;
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
                if (candidate == null || !candidate.IsAlive || candidate.Team == seeker.Team)
                {
                    continue;
                }

                var signedDistance = (candidate.transform.position.x - seekerX) * direction;
                if (signedDistance < -0.25f)
                {
                    continue;
                }

                var distance = Vector2.Distance(candidate.transform.position, seeker.transform.position);
                if (distance > GetUnitEngageDistance(seeker.Definition))
                {
                    continue;
                }

                if (distance < bestDistance)
                {
                    best = candidate;
                    bestDistance = distance;
                }
            }

            return best;
        }

        public BattleFacility FindNearestEnemyFacility(BattleUnit seeker)
        {
            BattleFacility best = null;
            var bestDistance = float.MaxValue;

            FindNearestEnemyFacilityIn(playerTowers, seeker, ref best, ref bestDistance);
            FindNearestEnemyFacilityIn(enemyTowers, seeker, ref best, ref bestDistance);
            FindNearestEnemyFacilityIn(playerResourceWells, seeker, ref best, ref bestDistance);
            FindNearestEnemyFacilityIn(enemyResourceWells, seeker, ref best, ref bestDistance);

            return best;
        }

        private static void FindNearestEnemyFacilityIn<T>(T[] facilities, BattleUnit seeker, ref BattleFacility best, ref float bestDistance)
            where T : BattleFacility
        {
            if (facilities == null || seeker == null)
            {
                return;
            }

            var seekerPosition = seeker.transform.position;
            var direction = seeker.Team == 0 ? 1f : -1f;
            var engageDistance = Mathf.Max(GetUnitEngageDistance(seeker.Definition), 1.55f);
            for (var i = 0; i < facilities.Length; i++)
            {
                var candidate = facilities[i];
                if (candidate == null || !candidate.IsAlive || candidate.Team == seeker.Team)
                {
                    continue;
                }

                var signedDistance = (candidate.CenterPosition.x - seekerPosition.x) * direction;
                if (signedDistance < -0.45f)
                {
                    continue;
                }

                var distance = Vector2.Distance(candidate.CenterPosition, seekerPosition);
                if (distance > engageDistance || distance >= bestDistance)
                {
                    continue;
                }

                best = candidate;
                bestDistance = distance;
            }
        }

        public BattleFacility FindNearestDamagedFriendlyFacility(BattleUnit seeker, float range)
        {
            // 建筑兵只修自己队伍且已经受损的设施，防止在战斗中误把敌方建筑当成修复目标。
            BattleFacility best = null;
            var bestDistance = Mathf.Max(0f, range);

            if (seeker == null)
            {
                return null;
            }

            if (seeker.Team == 0)
            {
                FindNearestDamagedFriendlyFacilityIn(playerTowers, seeker, ref best, ref bestDistance);
                FindNearestDamagedFriendlyFacilityIn(playerResourceWells, seeker, ref best, ref bestDistance);
            }
            else
            {
                FindNearestDamagedFriendlyFacilityIn(enemyTowers, seeker, ref best, ref bestDistance);
                FindNearestDamagedFriendlyFacilityIn(enemyResourceWells, seeker, ref best, ref bestDistance);
            }

            return best;
        }

        private static void FindNearestDamagedFriendlyFacilityIn<T>(T[] facilities, BattleUnit seeker, ref BattleFacility best, ref float bestDistance)
            where T : BattleFacility
        {
            if (facilities == null || seeker == null)
            {
                return;
            }

            var seekerPosition = seeker.transform.position;
            for (var i = 0; i < facilities.Length; i++)
            {
                var candidate = facilities[i];
                if (candidate == null || !candidate.NeedsRepair || candidate.Team != seeker.Team)
                {
                    continue;
                }

                var distance = Vector2.Distance(candidate.CenterPosition, seekerPosition);
                if (distance >= bestDistance)
                {
                    continue;
                }

                best = candidate;
                bestDistance = distance;
            }
        }

        public bool IsBuilderTaskPending(BuilderTaskKind kind, int slotIndex, int towerTypeIndex, int team)
        {
            // 待建标记代表“金币已支付、建筑兵已派出、设施尚未落地”的中间状态。
            if (team != 0 && team != 1)
            {
                return false;
            }

            if (kind == BuilderTaskKind.Tower)
            {
                var pendingTowerBuilds = team == 0 ? pendingPlayerTowerBuilds : pendingEnemyTowerBuilds;
                var pendingTowerTypeIndexes = team == 0 ? pendingPlayerTowerTypeIndexes : pendingEnemyTowerTypeIndexes;
                return slotIndex >= 0
                    && slotIndex < pendingTowerBuilds.Length
                    && slotIndex < pendingTowerTypeIndexes.Length
                    && pendingTowerBuilds[slotIndex]
                    && pendingTowerTypeIndexes[slotIndex] == towerTypeIndex
                    && slotIndex < playerTowers.Length
                    && slotIndex < enemyTowers.Length
                    && playerTowers[slotIndex] == null
                    && enemyTowers[slotIndex] == null;
            }

            if (kind == BuilderTaskKind.ResourceWell)
            {
                var pendingResourceWellBuilds = team == 0 ? pendingPlayerResourceWellBuilds : pendingEnemyResourceWellBuilds;
                return slotIndex >= 0
                    && slotIndex < pendingResourceWellBuilds.Length
                    && pendingResourceWellBuilds[slotIndex]
                    && slotIndex < playerResourceWells.Length
                    && slotIndex < enemyResourceWells.Length
                    && playerResourceWells[slotIndex] == null
                    && enemyResourceWells[slotIndex] == null;
            }

            return false;
        }

        public Vector3 GetBuilderTaskPosition(BuilderTaskKind kind, int slotIndex, int team)
        {
            var taskTeam = team == 1 ? 1 : 0;
            if (kind == BuilderTaskKind.ResourceWell)
            {
                var resourceWellPositions = taskTeam == 1 ? enemyResourceWellPositions : playerResourceWellPositions;
                if (resourceWellPositions == null || resourceWellPositions.Length == 0)
                {
                    return GetBasePosition(taskTeam);
                }

                return resourceWellPositions[Mathf.Clamp(slotIndex, 0, resourceWellPositions.Length - 1)];
            }

            var towerPositions = taskTeam == 1 ? enemyTowerPositions : playerTowerPositions;
            if (towerPositions == null || towerPositions.Length == 0)
            {
                return GetBasePosition(taskTeam);
            }

            return towerPositions[Mathf.Clamp(slotIndex, 0, towerPositions.Length - 1)];
        }

        public bool TryCompleteBuilderTask(BattleUnit builder, BuilderTaskKind kind, int slotIndex, int towerTypeIndex)
        {
            // 只有建筑兵走到目标设施点的施工范围内，任务才真正转化为防御塔或资源点。
            if (builder == null || !builder.IsAlive || !IsBuilderTaskPending(kind, slotIndex, towerTypeIndex, builder.Team))
            {
                return false;
            }

            var targetPosition = GetBuilderTaskPosition(kind, slotIndex, builder.Team);
            if (Vector2.Distance(builder.transform.position, targetPosition) > BuilderConstructionRange)
            {
                return false;
            }

            if (kind == BuilderTaskKind.Tower)
            {
                if (builder.Team == 0)
                {
                    pendingPlayerTowerBuilds[slotIndex] = false;
                    BuildTowerAt(slotIndex, towerTypeIndex);
                }
                else
                {
                    pendingEnemyTowerBuilds[slotIndex] = false;
                    BuildEnemyTowerAt(slotIndex, towerTypeIndex);
                }

                return true;
            }

            if (kind == BuilderTaskKind.ResourceWell)
            {
                if (builder.Team == 0)
                {
                    pendingPlayerResourceWellBuilds[slotIndex] = false;
                    BuildResourceWellAt(slotIndex);
                }
                else
                {
                    pendingEnemyResourceWellBuilds[slotIndex] = false;
                    BuildEnemyResourceWellAt(slotIndex);
                }

                return true;
            }

            return false;
        }

        public BattleUnit FindTowerTarget(int towerTeam, int laneIndex, Vector3 towerPosition, float range)
        {
            BattleUnit best = null;
            var bestDistance = float.MaxValue;

            for (var i = 0; i < units.Count; i++)
            {
                var candidate = units[i];
                if (candidate == null || !candidate.IsAlive || candidate.Team == towerTeam)
                {
                    continue;
                }

                var distance = Vector2.Distance(candidate.transform.position, towerPosition);
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
                var effectiveDamage = damage * (1f - baseDamageReduction);
                if (playerShield > 0f)
                {
                    var absorbed = Mathf.Min(playerShield, effectiveDamage);
                    playerShield -= absorbed;
                    effectiveDamage -= absorbed;
                    if (playerShield <= 0f)
                    {
                        shieldTimer = 0f;
                    }
                }

                if (effectiveDamage <= 0f)
                {
                    status = "\u62a4\u76fe\u5c4f\u969c\u62b5\u6d88\u4e86\u654c\u65b9\u653b\u51fb\u3002";
                    return;
                }

                playerBaseHealth = Mathf.Max(0f, playerBaseHealth - effectiveDamage);
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

        public void SpawnCombatHitEffect(Vector3 position, int attackerTeam, bool ranged)
        {
            if (worldRoot == null)
            {
                return;
            }

            var root = new GameObject(ranged ? "Ranged Combat Impact" : "Melee Combat Impact");
            root.transform.SetParent(worldRoot, false);
            root.AddComponent<BattleTimedDestroy>().Configure(ranged ? 0.48f : 0.36f);

            var direction = attackerTeam == 0 ? 1f : -1f;
            var teamColor = attackerTeam == 0
                ? new Color(1f, 0.86f, 0.34f, 0.92f)
                : new Color(1f, 0.28f, 0.18f, 0.9f);
            var flashPosition = position + new Vector3(0f, 0.18f, 0f);
            var flash = CreateVfxDisc(
                root.transform,
                "Combat Flash",
                flashPosition,
                ranged ? new Color(teamColor.r, teamColor.g, teamColor.b, 0.55f) : teamColor,
                ranged ? 0.26f : 0.34f,
                136);
            flash.gameObject.AddComponent<BattleVfxFade>().Configure(0.26f, ranged ? 1.4f : 1.15f, 160f * direction);

            var slashColor = Color.Lerp(teamColor, Color.white, ranged ? 0.25f : 0.42f);
            CreateVfxLine(
                    root.transform,
                    ranged ? "Impact Streak" : "Attack Slash",
                    new[]
                    {
                        flashPosition + new Vector3(-direction * 0.38f, ranged ? 0.06f : -0.22f, 0f),
                        flashPosition + new Vector3(direction * 0.42f, ranged ? 0.02f : 0.24f, 0f)
                    },
                    slashColor,
                    ranged ? 0.04f : 0.065f,
                    138)
                .gameObject.AddComponent<BattleVfxFade>().Configure(ranged ? 0.28f : 0.22f, 0f, 0f);

            if (ranged)
            {
                return;
            }

            CreateVfxLine(
                    root.transform,
                    "Counter Spark",
                    new[]
                    {
                        flashPosition + new Vector3(-direction * 0.2f, 0.18f, 0f),
                        flashPosition + new Vector3(direction * 0.22f, -0.12f, 0f)
                    },
                    new Color(1f, 0.95f, 0.72f, 0.78f),
                    0.035f,
                    139)
                .gameObject.AddComponent<BattleVfxFade>().Configure(0.18f, 0f, 0f);
        }

        public void SpawnTowerAttackEffect(Vector3 origin, Vector3 target, int attackerTeam, int towerTypeIndex, string towerName)
        {
            if (IsTowerName(towerName, "钛晶"))
            {
                CreateTitaniumRayTowerAttackEffect(origin, target, attackerTeam);
                return;
            }

            if (IsTowerName(towerName, "等离子"))
            {
                CreatePlasmaSpireTowerAttackEffect(origin, target, attackerTeam, towerTypeIndex);
                return;
            }

            if (IsTowerName(towerName, "奇点"))
            {
                CreateSingularityBeaconTowerAttackEffect(origin, target, attackerTeam);
                return;
            }

            SpawnCombatHitEffect(target, attackerTeam, true);
        }

        private static bool IsTowerName(string towerName, string key)
        {
            return !string.IsNullOrEmpty(towerName)
                && towerName.IndexOf(key, StringComparison.Ordinal) >= 0;
        }

        private void CreateTitaniumRayTowerAttackEffect(Vector3 origin, Vector3 target, int attackerTeam)
        {
            if (worldRoot == null)
            {
                return;
            }

            var root = new GameObject("Titanium Ray Tower Attack");
            root.transform.SetParent(worldRoot, false);
            root.AddComponent<BattleTimedDestroy>().Configure(0.38f);

            var direction = attackerTeam == 0 ? 1f : -1f;
            var muzzle = origin + new Vector3(direction * 0.42f, 0.46f, 0f);
            var hit = target + new Vector3(0f, 0.18f, 0f);
            var rayColor = new Color(0.08f, 0.95f, 1f, 0.84f);
            var coreColor = new Color(0.88f, 1f, 1f, 0.96f);

            CreateVfxLine(root.transform, "Titanium Ray Glow", new[] { muzzle, hit }, rayColor, 0.16f, 146)
                .gameObject.AddComponent<BattleVfxFade>().Configure(0.3f, 0f, 0f);
            CreateVfxLine(root.transform, "Titanium Ray Core", new[] { muzzle, hit }, coreColor, 0.052f, 148)
                .gameObject.AddComponent<BattleVfxFade>().Configure(0.22f, 0f, 0f);

            CreateVfxDisc(root.transform, "Titanium Muzzle Flash", muzzle, new Color(0.2f, 1f, 1f, 0.62f), 0.18f, 149)
                .gameObject.AddComponent<BattleVfxFade>().Configure(0.22f, 1.45f, 260f);
            CreateVfxDisc(root.transform, "Titanium Impact Burst", hit, new Color(0.62f, 1f, 1f, 0.58f), 0.28f, 150)
                .gameObject.AddComponent<BattleVfxFade>().Configure(0.28f, 1.7f, -180f);

            for (var i = 0; i < 4; i++)
            {
                var angle = (i * 90f + 22f) * Mathf.Deg2Rad;
                var shardEnd = hit + new Vector3(Mathf.Cos(angle) * 0.34f, Mathf.Sin(angle) * 0.22f, 0f);
                CreateVfxLine(root.transform, "Titanium Crystal Spark " + i, new[] { hit, shardEnd }, coreColor, 0.028f, 151 + i)
                    .gameObject.AddComponent<BattleVfxFade>().Configure(0.18f, 0f, 0f);
            }
        }

        private void CreatePlasmaSpireTowerAttackEffect(Vector3 origin, Vector3 target, int attackerTeam, int towerTypeIndex)
        {
            if (worldRoot == null)
            {
                return;
            }

            var root = new GameObject("Plasma Spire Tower Attack");
            root.transform.SetParent(worldRoot, false);
            root.AddComponent<BattleTimedDestroy>().Configure(0.52f);

            var direction = attackerTeam == 0 ? 1f : -1f;
            var muzzle = origin + new Vector3(direction * 0.22f, 0.72f, 0f);
            var hit = target + new Vector3(0f, 0.2f, 0f);
            var plasmaA = new Color(0.95f, 0.16f, 1f, 0.78f);
            var plasmaB = new Color(0.12f, 0.92f, 1f, 0.74f);

            CreateVfxLine(root.transform, "Plasma Bolt", BuildLightningPoints(muzzle, hit, towerTypeIndex + attackerTeam * 7), plasmaB, 0.062f, 146)
                .gameObject.AddComponent<BattleVfxFade>().Configure(0.32f, 0f, 0f);

            for (var i = 0; i < 5; i++)
            {
                var t = (i + 1f) / 6f;
                var pulsePosition = Vector3.Lerp(muzzle, hit, t);
                var pulseColor = i % 2 == 0 ? plasmaA : plasmaB;
                CreateVfxDisc(root.transform, "Plasma Pulse Ring " + i, pulsePosition, pulseColor, 0.12f + i * 0.025f, 147 + i)
                    .gameObject.AddComponent<BattleVfxFade>().Configure(0.36f + i * 0.03f, 1.65f, direction * (120f + i * 18f));
            }

            CreateVfxDisc(root.transform, "Plasma Impact Bloom", hit, new Color(1f, 0.44f, 1f, 0.52f), 0.36f, 153)
                .gameObject.AddComponent<BattleVfxFade>().Configure(0.34f, 1.45f, 220f);
        }

        private void CreateSingularityBeaconTowerAttackEffect(Vector3 origin, Vector3 target, int attackerTeam)
        {
            if (worldRoot == null)
            {
                return;
            }

            var root = new GameObject("Singularity Beacon Tower Attack");
            root.transform.SetParent(worldRoot, false);
            root.AddComponent<BattleTimedDestroy>().Configure(0.72f);

            var direction = attackerTeam == 0 ? 1f : -1f;
            var beacon = origin + new Vector3(direction * 0.18f, 0.56f, 0f);
            var center = target + new Vector3(0f, 0.2f, 0f);
            var gravityColor = new Color(0.48f, 0.18f, 1f, 0.72f);
            var coreColor = new Color(0.04f, 0.02f, 0.12f, 0.92f);

            CreateVfxLine(root.transform, "Singularity Anchor Beam", new[] { beacon, center }, new Color(0.3f, 0.7f, 1f, 0.5f), 0.045f, 145)
                .gameObject.AddComponent<BattleVfxFade>().Configure(0.36f, 0f, 0f);
            CreateVfxDisc(root.transform, "Singularity Core", center, coreColor, 0.2f, 150)
                .gameObject.AddComponent<BattleVfxFade>().Configure(0.48f, 0.65f, -80f);
            CreateVfxDisc(root.transform, "Singularity Halo", center, gravityColor, 0.46f, 149)
                .gameObject.AddComponent<BattleVfxFade>().Configure(0.52f, 0.8f, 180f);

            for (var i = 0; i < 3; i++)
            {
                var ringPoints = BuildEllipsePoints(center, 0.48f + i * 0.16f, 0.18f + i * 0.07f, i * 33f);
                CreateVfxLine(root.transform, "Singularity Gravity Ring " + i, ringPoints, i % 2 == 0 ? gravityColor : new Color(0.1f, 0.9f, 1f, 0.55f), 0.028f, 151 + i)
                    .gameObject.AddComponent<BattleVfxFade>().Configure(0.5f + i * 0.05f, 0f, direction * (95f + i * 45f));
            }

            for (var i = 0; i < 5; i++)
            {
                var angle = (i * 72f + 14f) * Mathf.Deg2Rad;
                var outside = center + new Vector3(Mathf.Cos(angle) * 0.68f, Mathf.Sin(angle) * 0.34f, 0f);
                var inside = center + new Vector3(Mathf.Cos(angle) * 0.18f, Mathf.Sin(angle) * 0.08f, 0f);
                CreateVfxLine(root.transform, "Singularity Infall " + i, new[] { outside, inside }, new Color(0.82f, 0.42f, 1f, 0.65f), 0.026f, 154 + i)
                    .gameObject.AddComponent<BattleVfxFade>().Configure(0.32f, 0f, 0f);
            }
        }

        private static Vector3[] BuildEllipsePoints(Vector3 center, float radiusX, float radiusY, float angleOffsetDegrees)
        {
            var points = new Vector3[17];
            var tilt = angleOffsetDegrees * Mathf.Deg2Rad;
            var cosTilt = Mathf.Cos(tilt);
            var sinTilt = Mathf.Sin(tilt);

            for (var i = 0; i < points.Length; i++)
            {
                var angle = (i / (float)(points.Length - 1)) * Mathf.PI * 2f;
                var x = Mathf.Cos(angle) * radiusX;
                var y = Mathf.Sin(angle) * radiusY;
                points[i] = center + new Vector3(x * cosTilt - y * sinTilt, x * sinTilt + y * cosTilt, 0f);
            }

            return points;
        }

        public void SpawnBuilderWorkEffect(Vector3 position, int team)
        {
            if (worldRoot == null)
            {
                return;
            }

            var root = new GameObject("Builder Work Effect");
            root.transform.SetParent(worldRoot, false);
            root.AddComponent<BattleTimedDestroy>().Configure(0.62f);

            var color = team == 0
                ? new Color(1f, 0.82f, 0.24f, 0.9f)
                : new Color(1f, 0.48f, 0.28f, 0.82f);
            var center = position + new Vector3(0f, 0.18f, 0f);

            CreateVfxDisc(root.transform, "Builder Work Glow", center, new Color(color.r, color.g, color.b, 0.36f), 0.3f, 146)
                .gameObject.AddComponent<BattleVfxFade>().Configure(0.42f, 1.25f, 70f);

            CreateVfxLine(
                    root.transform,
                    "Builder Tool Spark",
                    new[]
                    {
                        center + new Vector3(-0.18f, -0.05f, 0f),
                        center + new Vector3(0.18f, 0.12f, 0f)
                    },
                    Color.Lerp(color, Color.white, 0.36f),
                    0.045f,
                    148)
                .gameObject.AddComponent<BattleVfxFade>().Configure(0.28f, 0f, 0f);

            CreateVfxLine(
                    root.transform,
                    "Builder Repair Spark",
                    new[]
                    {
                        center + new Vector3(-0.12f, 0.14f, 0f),
                        center + new Vector3(0.2f, -0.08f, 0f)
                    },
                    new Color(0.72f, 1f, 0.52f, 0.72f),
                    0.032f,
                    149)
                .gameObject.AddComponent<BattleVfxFade>().Configure(0.24f, 0f, 0f);
        }

        private void SpawnClickPointMarker(Vector3 position)
        {
            if (worldRoot == null)
            {
                return;
            }

            var root = new GameObject("Click Point Marker");
            root.transform.SetParent(worldRoot, false);
            root.transform.position = position;
            root.AddComponent<BattleTimedDestroy>().Configure(ClickMarkerDuration + 0.08f);

            var halo = CreateVfxDisc(
                root.transform,
                "Click Point Halo",
                position,
                new Color(0.16f, 1f, 0.18f, 0.44f),
                0.48f,
                152);
            halo.gameObject.AddComponent<BattleClickMarkerPulse>().Configure(ClickMarkerDuration, 0.55f, 1.35f, 16f);

            var dot = CreateVfxDisc(
                root.transform,
                "Click Point Dot",
                position,
                new Color(0.36f, 1f, 0.22f, 0.96f),
                0.18f,
                153);
            dot.gameObject.AddComponent<BattleClickMarkerPulse>().Configure(ClickMarkerDuration, 0.8f, 1.18f, 22f);
        }

        public void NotifyUnitKilled(BattleUnit unit, int attackerTeam)
        {
            var hadAssignedBuilderTask = unit != null && unit.HasAssignedBuilderTask;
            if (hadAssignedBuilderTask)
            {
                CancelPendingBuilderTask(unit.AssignedBuilderTaskKind, unit.AssignedBuilderTaskSlotIndex, unit.Team);
            }

            units.Remove(unit);
            var wasSelected = selectedPlayerUnits.Remove(unit);
            if (wasSelected && unit != null && unit.IsBuilder && activeBuildPlacement != BuildPlacementKind.None && !HasSelectedPlayerBuilder())
            {
                ClearBuildPlacementPreviews();
            }

            if (attackerTeam == 0 && !gameOver)
            {
                coins += unit.Definition.Reward;
                GainEraValue(unit.Definition.Cost * 0.25f);
                status = unit.Definition.DisplayName + "\u51fb\u6e83\u4e86\u654c\u4eba\uff0c\u83b7\u5f97 " + unit.Definition.Reward + " \u91d1\u5e01\u3002";
            }
            else if (attackerTeam == 1 && !gameOver)
            {
                GainEnemyEraValue(unit.Definition.Cost * 0.2f);
                if (unit.Definition.Role == UnitRole.Builder)
                {
                    status = hadAssignedBuilderTask
                        ? "\u5efa\u7b51\u5175\u9635\u4ea1\uff0c\u6b63\u5728\u4fee\u5efa\u7684\u8bbe\u65bd\u5df2\u4e2d\u65ad\u3002"
                        : "\u5efa\u7b51\u5175\u9635\u4ea1\uff0c\u9644\u8fd1\u8bbe\u65bd\u7684\u4fee\u590d\u80fd\u529b\u4e0b\u964d\u3002";
                }
            }
        }

        public void NotifyTowerDestroyed(BattleTower tower, int attackerTeam)
        {
            if (tower == null)
            {
                return;
            }

            ReleaseFacilitySlot(playerTowers, tower);
            ReleaseFacilitySlot(enemyTowers, tower);
            if (attackerTeam == 0 && !gameOver)
            {
                coins += 45;
                GainEraValue(35f);
            }
            else if (attackerTeam == 1 && !gameOver)
            {
                GainEnemyEraValue(35f);
            }

            status = attackerTeam == 0
                ? "\u6211\u65b9\u6467\u6bc1\u4e86\u654c\u65b9\u8bbe\u65bd\uff0c\u8be5\u70b9\u4f4d\u53ef\u91cd\u65b0\u62a2\u5360\u3002"
                : "\u654c\u65b9\u6467\u6bc1\u4e86\u6211\u65b9\u8bbe\u65bd\uff0c\u8be5\u70b9\u4f4d\u5df2\u7a7a\u51fa\u3002";
        }

        public void NotifyResourceWellDestroyed(BattleResourceWell well, int attackerTeam)
        {
            if (well == null)
            {
                return;
            }

            if (ReleaseFacilitySlot(playerResourceWells, well))
            {
                incomePerSecond = GameSession.IncomePerSecond + ageIndex * 2f + CountBuiltResourceWells(playerResourceWells) * ResourceWellIncomeBonus;
            }

            ReleaseFacilitySlot(enemyResourceWells, well);
            if (attackerTeam == 0 && !gameOver)
            {
                coins += 35;
                GainEraValue(25f);
            }
            else if (attackerTeam == 1 && !gameOver)
            {
                GainEnemyEraValue(25f);
            }

            status = attackerTeam == 0
                ? "\u6211\u65b9\u593a\u56de\u4e86\u8d44\u6e90\u70b9\uff0c\u53ef\u91cd\u65b0\u5360\u9886\u3002"
                : "\u654c\u65b9\u7834\u574f\u4e86\u6211\u65b9\u8d44\u6e90\u70b9\u3002";
        }

        private static bool ReleaseFacilitySlot<T>(T[] facilities, T facility)
            where T : BattleFacility
        {
            if (facilities == null || facility == null)
            {
                return false;
            }

            for (var i = 0; i < facilities.Length; i++)
            {
                if (facilities[i] != facility)
                {
                    continue;
                }

                facilities[i] = null;
                return true;
            }

            return false;
        }

        private void CancelPendingBuilderTask(BuilderTaskKind kind, int slotIndex, int team)
        {
            if (team != 0 && team != 1)
            {
                return;
            }

            var pendingTowerBuilds = team == 0 ? pendingPlayerTowerBuilds : pendingEnemyTowerBuilds;
            var pendingTowerTypeIndexes = team == 0 ? pendingPlayerTowerTypeIndexes : pendingEnemyTowerTypeIndexes;
            var pendingResourceWellBuilds = team == 0 ? pendingPlayerResourceWellBuilds : pendingEnemyResourceWellBuilds;

            if (kind == BuilderTaskKind.Tower && slotIndex >= 0 && slotIndex < pendingTowerBuilds.Length)
            {
                pendingTowerBuilds[slotIndex] = false;
                if (slotIndex < pendingTowerTypeIndexes.Length)
                {
                    pendingTowerTypeIndexes[slotIndex] = 0;
                }
            }
            else if (kind == BuilderTaskKind.ResourceWell && slotIndex >= 0 && slotIndex < pendingResourceWellBuilds.Length)
            {
                pendingResourceWellBuilds[slotIndex] = false;
            }
        }

        private void BuildDefinitions()
        {
            playerUnitDefinitions = BuildPlayerUnitDefinitionsForAge(ageIndex);
            enemyUnitDefinitions = BuildEnemyDefinitions(BuildPlayerUnitDefinitionsForAge(enemyAgeIndex));
            currentTowerDefinitions = BuildTowerDefinitionsForAge(ageIndex);
            enemyTowerDefinitions = BuildTowerDefinitionsForAge(enemyAgeIndex);
            towerFrameSets = BuildTowerFrameSets(ageIndex, currentTowerDefinitions.Length);
            enemyTowerFrameSets = BuildTowerFrameSets(enemyAgeIndex, enemyTowerDefinitions.Length);
            selectedTowerIndex = Mathf.Clamp(selectedTowerIndex, 0, Mathf.Max(0, currentTowerDefinitions.Length - 1));
            currentTowerDefinition = GetTowerDefinition(selectedTowerIndex);
            towerFrames = GetTowerFrames(selectedTowerIndex);
        }

        private static AgeVisualSet GetAgeVisualSet(int index)
        {
            return AgeVisualSets[Mathf.Clamp(index, 0, AgeVisualSets.Length - 1)];
        }

        private UnitDefinition[] BuildPlayerUnitDefinitionsForAge(int index)
        {
            var combatDefinitions = BuildUnitDefinitionsForAge(index);
            var definitions = new UnitDefinition[combatDefinitions.Length + 1];
            for (var i = 0; i < combatDefinitions.Length; i++)
            {
                definitions[i] = combatDefinitions[i];
            }

            definitions[definitions.Length - 1] = CreateBuilderUnitDefinition(index);
            return definitions;
        }

        private UnitDefinition CreateBuilderUnitDefinition(int index)
        {
            var clampedIndex = Mathf.Clamp(index, 0, AgeNames.Length - 1);
            var tint = Color.Lerp(AgeTints[clampedIndex], new Color(1f, 0.84f, 0.36f, 1f), 0.28f);
            var costs = new[] { 45, 150, 440, 1450, 5200 };
            var health = new[] { 82f, 190f, 360f, 720f, 1500f };
            var damage = new[] { 6f, 18f, 42f, 86f, 170f };
            var speeds = new[] { 1.05f, 1f, 1.08f, 1.12f, 1.18f };

            return CreateUnitDefinition(
                GetBuilderDisplayName(clampedIndex),
                "Builder",
                costs[clampedIndex],
                health[clampedIndex],
                damage[clampedIndex],
                speeds[clampedIndex],
                0.58f,
                1.15f,
                0.35f,
                0,
                tint,
                UnitRole.Builder);
        }

        private static string GetBuilderDisplayName(int index)
        {
            switch (Mathf.Clamp(index, 0, AgeNames.Length - 1))
            {
                case 1:
                    return "\u9f7f\u8f6e\u5de5\u5175";
                case 2:
                    return "\u7535\u710a\u5de5\u5175";
                case 3:
                    return "\u5806\u82af\u5de5\u5175";
                case 4:
                    return "\u661f\u6e2f\u5de5\u5175";
                default:
                    return "\u7b51\u8425\u5de5\u5175";
            }
        }

        private UnitDefinition[] BuildUnitDefinitionsForAge(int index)
        {
            var tint = AgeTints[Mathf.Clamp(index, 0, AgeTints.Length - 1)];
            unitDefinitionBuildAgeIndex = Mathf.Clamp(index, 0, AgeNames.Length - 1);
            switch (Mathf.Clamp(index, 0, AgeNames.Length - 1))
            {
                case 1:
                    return new[]
                    {
                        CreateUnitDefinition("齿轮兵", "GearSoldier", 50, 115f, 30f, 1.05f, 0.58f, 0.95f, 0.35f, 0, tint),
                        CreateUnitDefinition("蒸汽弩手", "SteamCrossbow", 75, 90f, 14f, 0.95f, 2.75f, 0.8f, 0.34f, 1, tint),
                        CreateUnitDefinition("锅炉掷弹兵", "BoilerGrenadier", 110, 125f, 38f, 0.88f, 2.45f, 1.15f, 0.36f, 2, tint),
                        CreateUnitDefinition("铁轮破城车", "SiegeRoller", 350, 320f, 72f, 0.72f, 1.1f, 1.45f, 0.44f, 3, tint),
                        CreateUnitDefinition("发条卫士", "ClockworkGuard", 520, 430f, 86f, 0.82f, 0.72f, 1.05f, 0.44f, 4, tint)
                    };
                case 2:
                    return new[]
                    {
                        CreateUnitDefinition("电击兵", "VoltGuard", 180, 230f, 82f, 1.12f, 0.6f, 0.95f, 0.36f, 0, tint),
                        CreateUnitDefinition("电弧疾行者", "ArcRunner", 230, 180f, 70f, 1.55f, 0.56f, 0.82f, 0.34f, 1, tint),
                        CreateUnitDefinition("线圈射手", "CoilShooter", 380, 180f, 32f, 1f, 3.0f, 0.62f, 0.35f, 2, tint),
                        CreateUnitDefinition("履带战车", "CrawlerTank", 820, 720f, 150f, 0.65f, 1.55f, 1.5f, 0.46f, 3, tint),
                        CreateUnitDefinition("雷霆机甲", "ThunderMech", 1150, 620f, 178f, 0.78f, 2.2f, 1.25f, 0.48f, 4, tint)
                    };
                case 3:
                    return new[]
                    {
                        CreateUnitDefinition("辐射步兵", "RadTrooper", 1300, 420f, 120f, 1.2f, 0.62f, 0.82f, 0.36f, 0, tint),
                        CreateUnitDefinition("同位素侦察兵", "IsotopeScout", 1600, 360f, 108f, 1.48f, 0.6f, 0.76f, 0.34f, 1, tint),
                        CreateUnitDefinition("裂变枪兵", "FissionLancer", 2200, 360f, 46f, 1.05f, 3.08f, 0.46f, 0.35f, 2, tint),
                        CreateUnitDefinition("反应堆行者", "ReactorWalker", 4200, 820f, 210f, 0.82f, 2.1f, 1.18f, 0.45f, 3, tint),
                        CreateUnitDefinition("核能坦克", "NuclearTank", 7000, 1500f, 320f, 0.6f, 1.8f, 1.64f, 0.48f, 4, tint)
                    };
                case 4:
                    return new[]
                    {
                        CreateUnitDefinition("激光兵", "LaserTrooper", 4800, 1000f, 260f, 1.3f, 0.75f, 0.8f, 0.37f, 0, tint),
                        CreateUnitDefinition("光子刀锋", "PhotonBlade", 5600, 820f, 240f, 1.58f, 0.6f, 0.72f, 0.35f, 1, tint),
                        CreateUnitDefinition("浮游机甲", "SkimmerMech", 6800, 820f, 95f, 1.15f, 3.25f, 0.38f, 0.36f, 2, tint),
                        CreateUnitDefinition("重力无人机", "GravityDrone", 11000, 1450f, 420f, 0.9f, 2.35f, 1.08f, 0.47f, 3, tint),
                        CreateUnitDefinition("反物质巨像", "AntimatterColossus", 20000, 3200f, 720f, 0.5f, 2.1f, 1.8f, 0.5f, 4, tint)
                    };
                default:
                    return new[]
                    {
                        CreateUnitDefinition("石棒战士", "Hunter", 15, 60f, 16f, 1f, 0.55f, 1f, 0.34f, 0, tint),
                        CreateUnitDefinition("投石猎手", "Thrower", 22, 48f, 9f, 0.9f, 2.35f, 0.95f, 0.33f, 1, tint),
                        CreateUnitDefinition("骨弓猎手", "BoneArcher", 35, 46f, 12f, 0.95f, 3.05f, 0.82f, 0.34f, 2, tint),
                        CreateUnitDefinition("獠牙骑手", "TuskRider", 65, 120f, 24f, 1.35f, 0.72f, 0.92f, 0.38f, 3, tint),
                        CreateUnitDefinition("巨骨勇士", "Champion", 110, 180f, 42f, 0.7f, 0.95f, 1.35f, 0.42f, 4, tint)
                    };
            }
        }

        private UnitDefinition CreateUnitDefinition(
            string displayName,
            string key,
            int cost,
            float maxHealth,
            float damage,
            float speed,
            float attackRange,
            float attackInterval,
            float scale,
            int visualSlot,
            Color tint,
            UnitRole role = UnitRole.Combat)
        {
            var visualSet = GetAgeVisualSet(unitDefinitionBuildAgeIndex);
            visualSlot = Mathf.Clamp(visualSlot, 0, visualSet.UnitFrameFolders.Length - 1);
            var frameFolder = visualSet.UnitFrameFolders[visualSlot];
            var fallbackFolder = AgeVisualSets[0].UnitFrameFolders[Mathf.Clamp(visualSlot, 0, AgeVisualSets[0].UnitFrameFolders.Length - 1)];
            var moveFramePrefix = visualSet.UnitRoot + "/" + frameFolder + "/move_";
            var attackFramePrefix = visualSet.UnitRoot + "/" + frameFolder + "/attack_";
            var fallbackMoveFramePrefix = AgeVisualSets[0].UnitRoot + "/" + fallbackFolder + "/move_";
            var fallbackAttackFramePrefix = AgeVisualSets[0].UnitRoot + "/" + fallbackFolder + "/attack_";
            if (role == UnitRole.Builder)
            {
                moveFramePrefix = BuilderUnitMoveFramePrefix;
                attackFramePrefix = BuilderUnitAttackFramePrefix;
                fallbackMoveFramePrefix = null;
                fallbackAttackFramePrefix = null;
            }

            return new UnitDefinition(
                displayName,
                key,
                cost,
                maxHealth,
                damage,
                speed,
                attackRange,
                attackInterval,
                scale,
                Mathf.Max(1, Mathf.RoundToInt(cost * 0.35f)),
                LoadFrames(moveFramePrefix, 5, 100f, fallbackMoveFramePrefix),
                LoadFrames(attackFramePrefix, 5, 100f, fallbackAttackFramePrefix),
                tint,
                role);
        }

        private TowerDefinition[] BuildTowerDefinitionsForAge(int index)
        {
            var tint = AgeTints[Mathf.Clamp(index, 0, AgeTints.Length - 1)];
            switch (Mathf.Clamp(index, 0, AgeNames.Length - 1))
            {
                case 1:
                    return new[]
                    {
                        new TowerDefinition("齿轮抛炮塔", 460, 40f, 1.75f, 4f, tint),
                        new TowerDefinition("蒸汽加农塔", 650, 68f, 1.45f, 4.4f, tint),
                        new TowerDefinition("铆钉迫击塔", 900, 140f, 2.6f, 5f, tint)
                    };
                case 2:
                    return new[]
                    {
                        new TowerDefinition("特斯拉塔", 1350, 34f, 1.75f, 5f, tint),
                        new TowerDefinition("电弧塔", 1800, 48f, 1.2f, 4.2f, tint),
                        new TowerDefinition("轨道炮塔", 2600, 210f, 2.85f, 5.4f, tint)
                    };
                case 3:
                    return new[]
                    {
                        new TowerDefinition("粒子机枪塔", 6200, 70f, 1f, 5f, tint),
                        new TowerDefinition("反应堆迫击塔", 7600, 145f, 1.9f, 5.5f, tint),
                        new TowerDefinition("辐尘尖塔", 10500, 270f, 2.7f, 5.8f, tint)
                    };
                case 4:
                    return new[]
                    {
                        new TowerDefinition("钛晶射线塔", 22000, 100f, 1f, 4f, tint),
                        new TowerDefinition("等离子尖塔", 26000, 160f, 0.78f, 4.6f, tint),
                        new TowerDefinition("奇点信标", 34000, 520f, 3.0f, 6f, tint)
                    };
                default:
                    return new[]
                    {
                        new TowerDefinition("骨石塔", 90, 12f, 0.75f, 3.5f, tint),
                        new TowerDefinition("投石巢", 140, 24f, 1.15f, 4.25f, tint),
                        new TowerDefinition("猛犸图腾", 230, 58f, 2.35f, 4.75f, tint)
                    };
            }
        }

        private Sprite[][] BuildTowerFrameSets(int index, int towerCount)
        {
            var visualSet = GetAgeVisualSet(index);
            var frames = new Sprite[Mathf.Max(0, towerCount)][];
            for (var i = 0; i < frames.Length; i++)
            {
                var visualIndex = Mathf.Clamp(i, 0, visualSet.TowerFramePrefixes.Length - 1);
                var fallbackIndex = Mathf.Clamp(i, 0, AgeVisualSets[0].TowerFramePrefixes.Length - 1);
                frames[i] = LoadFrames(
                    visualSet.TowerFramePrefixes[visualIndex],
                    5,
                    100f,
                    AgeVisualSets[0].TowerFramePrefixes[fallbackIndex]);
            }

            return frames;
        }

        private TowerDefinition GetTowerDefinition(int towerIndex)
        {
            if (currentTowerDefinitions == null || currentTowerDefinitions.Length == 0)
            {
                return null;
            }

            return currentTowerDefinitions[Mathf.Clamp(towerIndex, 0, currentTowerDefinitions.Length - 1)];
        }

        private TowerDefinition GetEnemyTowerDefinition(int towerIndex)
        {
            if (enemyTowerDefinitions == null || enemyTowerDefinitions.Length == 0)
            {
                return null;
            }

            return enemyTowerDefinitions[Mathf.Clamp(towerIndex, 0, enemyTowerDefinitions.Length - 1)];
        }

        private Sprite[] GetTowerFrames(int towerIndex)
        {
            if (towerFrameSets == null || towerFrameSets.Length == 0)
            {
                return new Sprite[0];
            }

            return towerFrameSets[Mathf.Clamp(towerIndex, 0, towerFrameSets.Length - 1)];
        }

        private Sprite[] GetEnemyTowerFrames(int towerIndex)
        {
            if (enemyTowerFrameSets == null || enemyTowerFrameSets.Length == 0)
            {
                return new Sprite[0];
            }

            return enemyTowerFrameSets[Mathf.Clamp(towerIndex, 0, enemyTowerFrameSets.Length - 1)];
        }

        private void ApplyGameSetup()
        {
            selectedMap = GameSession.SelectedMap;
            coins = GameSession.PlayerStartingCoins;
            incomePerSecond = GameSession.IncomePerSecond;
            enemySpawnIntervalScale = GameSession.EnemySpawnIntervalScale;
            enemySpawnTimer = GameSession.InitialEnemySpawnDelay;
            ageIndex = 0;
            enemyAgeIndex = 0;
            eraValue = 0f;
            enemyEraValue = 0f;
            playerBaseMaxHealth = BaseHealthByAge[ageIndex];
            enemyBaseMaxHealth = BaseHealthByAge[enemyAgeIndex];
            playerBaseHealth = playerBaseMaxHealth;
            enemyBaseHealth = enemyBaseMaxHealth;
            gameStarted = false;
            status = "\u5df2\u9009\u62e9" + selectedMap.DisplayName + "\uff0c\u96be\u5ea6\uff1a" + GameSession.DifficultyName + "\u3002\u70b9\u51fb\u5730\u56fe\u4efb\u610f\u4f4d\u7f6e\u5f00\u59cb\u3002";
        }

        private void InitializeBattleLayout()
        {
            battleLayout = FindFirstObjectByType<BattleLayout>();
            if (battleLayout == null)
            {
                battleLayout = gameObject.AddComponent<BattleLayout>();
                CreateFallbackLayoutMarkers(battleLayout.transform);
                Debug.LogWarning("BattleLayout was missing. Created runtime fallback Layout markers so Battle can run from the current scene state.");
            }
            else if (!battleLayout.Validate(out _))
            {
                CreateFallbackLayoutMarkers(battleLayout.transform);
                Debug.LogWarning("BattleLayout marker hierarchy was incomplete. Rebuilt runtime fallback Layout markers.");
            }

            CreateFallbackLayoutMarkers(battleLayout.transform);

            if (!battleLayout.Validate(out var validationErrors))
            {
                Debug.LogError(validationErrors);
                return;
            }

            laneRoutes = new[]
            {
                battleLayout.GetLaneRoute(0),
                battleLayout.GetLaneRoute(1),
                battleLayout.GetLaneRoute(2)
            };
            RebuildRouteGraphFromLayout();
            playerTowerPositions = battleLayout.GetPlayerTowerPositions();
            enemyTowerPositions = battleLayout.GetEnemyTowerPositions();
            playerResourceWellPositions = battleLayout.GetPlayerResourceWellPositions();
            enemyResourceWellPositions = battleLayout.GetEnemyResourceWellPositions();
            playerBasePosition = battleLayout.PlayerBasePosition;
            enemyBasePosition = battleLayout.EnemyBasePosition;
            unitVisualScale = battleLayout.UnitVisualScale;
            towerVisualScale = battleLayout.TowerVisualScale;
            baseVisualScale = battleLayout.BaseVisualScale;
            resourceWellVisualScale = battleLayout.ResourceWellVisualScale;
            ResizeFacilityStateArrays();
        }

        private static void CreateFallbackLayoutMarkers(Transform root)
        {
            var routesRoot = EnsureMarker(root, "Layout/Routes");
            for (var laneIndex = 0; laneIndex < LaneRoutes.Length; laneIndex++)
            {
                var laneRoot = EnsureMarker(routesRoot, "Lane_" + laneIndex);
                SyncMarkerChildren(laneRoot, "Point_", LaneRoutes[laneIndex]);
            }

            var basesRoot = EnsureMarker(root, "Layout/Bases");
            EnsureMarker(basesRoot, "PlayerBasePoint").position = MapPoint(190f, RaisedBasePixelY);
            EnsureMarker(basesRoot, "EnemyBasePoint").position = MapPoint(2210f, RaisedBasePixelY);

            var towersRoot = EnsureMarker(root, "Layout/Towers");
            SyncPrefixedMarkerChildren(towersRoot, "PlayerTowerSlot_", PlayerTowerPositions);
            SyncPrefixedMarkerChildren(towersRoot, "EnemyTowerSlot_", EnemyTowerPositions);

            var wellsRoot = EnsureMarker(root, "Layout/ResourceWells");
            SyncPrefixedMarkerChildren(wellsRoot, "PlayerWellSlot_", PlayerResourceWellPositions);
            SyncPrefixedMarkerChildren(wellsRoot, "EnemyWellSlot_", EnemyResourceWellPositions);
        }

        private static Transform EnsureMarker(Transform root, string path)
        {
            var current = root;
            var parts = path.Split('/');
            for (var i = 0; i < parts.Length; i++)
            {
                var child = current.Find(parts[i]);
                if (child == null)
                {
                    child = new GameObject(parts[i]).transform;
                    child.SetParent(current, false);
                }

                current = child;
            }

            return current;
        }

        private static void SyncMarkerChildren(Transform root, string prefix, Vector3[] positions)
        {
            while (root.childCount > positions.Length)
            {
                DestroyImmediate(root.GetChild(root.childCount - 1).gameObject);
            }

            for (var i = 0; i < positions.Length; i++)
            {
                var marker = i < root.childCount ? root.GetChild(i) : null;
                if (marker == null)
                {
                    marker = new GameObject(prefix + i.ToString("00")).transform;
                    marker.SetParent(root, false);
                }

                marker.name = prefix + i.ToString("00");
                marker.position = positions[i];
            }
        }

        private static void SyncPrefixedMarkerChildren(Transform root, string prefix, Vector3[] positions)
        {
            for (var i = root.childCount - 1; i >= 0; i--)
            {
                var child = root.GetChild(i);
                if (child.name.StartsWith(prefix, StringComparison.Ordinal))
                {
                    DestroyImmediate(child.gameObject);
                }
            }

            for (var i = 0; i < positions.Length; i++)
            {
                var marker = new GameObject(prefix + i).transform;
                marker.SetParent(root, false);
                marker.position = positions[i];
            }
        }

        private void ResizeFacilityStateArrays()
        {
            playerTowers = new BattleTower[playerTowerPositions.Length];
            enemyTowers = new BattleTower[enemyTowerPositions.Length];
            playerResourceWells = new BattleResourceWell[playerResourceWellPositions.Length];
            enemyResourceWells = new BattleResourceWell[enemyResourceWellPositions.Length];
            pendingPlayerTowerBuilds = new bool[playerTowerPositions.Length];
            pendingPlayerTowerTypeIndexes = new int[playerTowerPositions.Length];
            pendingPlayerResourceWellBuilds = new bool[playerResourceWellPositions.Length];
            pendingEnemyTowerBuilds = new bool[enemyTowerPositions.Length];
            pendingEnemyTowerTypeIndexes = new int[enemyTowerPositions.Length];
            pendingEnemyResourceWellBuilds = new bool[enemyResourceWellPositions.Length];
        }

        private void RebuildRouteGraphFromLayout()
        {
            if (laneRoutes.Length < 3 || laneRoutes[0].Length < 2 || laneRoutes[1].Length < 2 || laneRoutes[2].Length < 2)
            {
                routeNodes = RouteNodes;
                routeEdges = RouteEdges;
                playerRouteStartNodes = PlayerRouteStartNodes;
                return;
            }

            routeNodes = BuildRouteNodes(laneRoutes);
            routeEdges = BuildRouteEdges(routeNodes, laneRoutes);
            playerRouteStartNodes = BuildPlayerRouteStartNodes(laneRoutes);
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
                    source.AttackFrames,
                    source.Tint,
                    source.Role);
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

            gameplayCamera = camera;
            camera.transform.position = new Vector3(0f, 0f, -10f);
            camera.orthographic = true;
            camera.orthographicSize = CameraOrthographicSize;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.12f, 0.15f, 0.12f, 1f);
            camera.rect = new Rect(0f, MapViewportBottomNormalized, 1f, MapViewportHeightNormalized);
        }

        private void ConfigureAmbienceAudio()
        {
            var audioRoot = new GameObject("Battle Ambience Audio").transform;
            audioRoot.SetParent(transform, false);

            frontlineBedSource = CreateAmbienceSource(audioRoot, "Three Lane Frontline Bed", FrontlineBedVolume);
            eraAmbienceSource = CreateAmbienceSource(audioRoot, "Era Ambience A", EraAmbienceVolume);
            fadingEraAmbienceSource = CreateAmbienceSource(audioRoot, "Era Ambience B", 0f);

            PlayAmbienceLoop(frontlineBedSource, FrontlineBedClipPath, FrontlineBedVolume);
            SwitchEraAmbience(ageIndex, true);
        }

        private static AudioSource CreateAmbienceSource(Transform parent, string name, float volume)
        {
            var sourceObject = new GameObject(name, typeof(AudioSource));
            sourceObject.transform.SetParent(parent, false);

            var source = sourceObject.GetComponent<AudioSource>();
            source.loop = true;
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.dopplerLevel = 0f;
            source.priority = 96;
            source.volume = volume;
            return source;
        }

        private static void PlayAmbienceLoop(AudioSource source, string resourcePath, float volume)
        {
            if (source == null)
            {
                return;
            }

            var clip = Resources.Load<AudioClip>(resourcePath);
            if (clip == null)
            {
                Debug.LogWarning("Missing battle ambience resource: " + resourcePath);
                return;
            }

            source.clip = clip;
            source.volume = volume;
            source.Play();
        }

        private void SwitchEraAmbience(int newAgeIndex, bool immediate)
        {
            if (eraAmbienceSource == null || fadingEraAmbienceSource == null)
            {
                return;
            }

            newAgeIndex = Mathf.Clamp(newAgeIndex, 0, AgeAmbienceClipPaths.Length - 1);
            if (newAgeIndex == currentAmbienceAgeIndex && eraAmbienceSource.isPlaying)
            {
                return;
            }

            var clip = Resources.Load<AudioClip>(AgeAmbienceClipPaths[newAgeIndex]);
            if (clip == null)
            {
                Debug.LogWarning("Missing battle ambience resource: " + AgeAmbienceClipPaths[newAgeIndex]);
                return;
            }

            if (immediate || eraAmbienceSource.clip == null || !eraAmbienceSource.isPlaying)
            {
                ambienceFadeActive = false;
                fadingEraAmbienceSource.Stop();
                fadingEraAmbienceSource.volume = 0f;

                eraAmbienceSource.clip = clip;
                eraAmbienceSource.volume = EraAmbienceVolume;
                eraAmbienceSource.Play();
                currentAmbienceAgeIndex = newAgeIndex;
                return;
            }

            fadingEraAmbienceSource.Stop();
            fadingEraAmbienceSource.clip = clip;
            fadingEraAmbienceSource.volume = 0f;
            fadingEraAmbienceSource.Play();

            ambienceFadeElapsed = 0f;
            ambienceFadeActive = true;
            currentAmbienceAgeIndex = newAgeIndex;
        }

        private void UpdateAmbienceFade()
        {
            if (!ambienceFadeActive)
            {
                return;
            }

            if (eraAmbienceSource == null || fadingEraAmbienceSource == null)
            {
                ambienceFadeActive = false;
                return;
            }

            ambienceFadeElapsed += Time.deltaTime;
            var progress = Mathf.Clamp01(ambienceFadeElapsed / EraAmbienceFadeDuration);
            eraAmbienceSource.volume = Mathf.Lerp(EraAmbienceVolume, 0f, progress);
            fadingEraAmbienceSource.volume = Mathf.Lerp(0f, EraAmbienceVolume, progress);

            if (progress < 1f)
            {
                return;
            }

            eraAmbienceSource.Stop();
            eraAmbienceSource.volume = 0f;

            var oldSource = eraAmbienceSource;
            eraAmbienceSource = fadingEraAmbienceSource;
            fadingEraAmbienceSource = oldSource;
            ambienceFadeActive = false;
        }

        private void BuildWorld()
        {
            // 战场对象全部在运行时生成；场景只提供可选 BattleLayout，便于快速替换地图和槽位。
            worldRoot = new GameObject("Playable Barbarian Battlefield").transform;
            worldRoot.SetParent(transform, false);

            battlefieldMapRenderer = CreateSprite(selectedMap.DisplayName + " Map", LoadAgeMapSprite(), Vector3.zero, 0);
            battlefieldMapRenderer.transform.localScale = Vector3.one;
            CacheMapBounds(battlefieldMapRenderer.bounds);

            facilityMarkerRoot = new GameObject("Facility Marker Root").transform;
            facilityMarkerRoot.SetParent(worldRoot, false);

            buildPreviewRoot = new GameObject("Build Preview Root").transform;
            buildPreviewRoot.SetParent(worldRoot, false);

            CreateBaseArt();
            CreateResourceWellSiteMarkers();
            MoveCameraToStartView();
        }

        private void CacheMapBounds(Bounds bounds)
        {
            mapBoundsMin = new Vector2(bounds.min.x, bounds.min.y);
            mapBoundsMax = new Vector2(bounds.max.x, bounds.max.y);
            hasMapBounds = true;
        }

        private void MoveCameraToStartView()
        {
            var camera = GetGameplayCamera();
            if (camera == null || !hasMapBounds)
            {
                return;
            }

            var halfHeight = camera.orthographicSize;
            var halfWidth = halfHeight * camera.aspect;
            var target = new Vector3(mapBoundsMin.x + halfWidth, 0f, camera.transform.position.z);
            camera.transform.position = ClampCameraPosition(target);
        }

        private void UpdateCameraNavigation()
        {
            var camera = GetGameplayCamera();
            if (camera == null || !hasMapBounds)
            {
                return;
            }

            var pointerPosition = GetPointerScreenPosition();
            ApplyCameraZoom(camera, pointerPosition);
            if (selectedPlayerUnits.Count > 0
                && IsSecondaryPointerPressed()
                && !IsPointerOverUi()
                && IsPointerInsideMapViewport(camera, pointerPosition))
            {
                ReleaseSelectedPlayerUnitsControl();
                isCameraDragging = false;
                return;
            }

            if (IsSecondaryPointerHeld())
            {
                if (!isCameraDragging && !IsPointerOverUi() && IsPointerInsideMapViewport(camera, pointerPosition))
                {
                    isCameraDragging = true;
                    lastCameraDragScreenPosition = pointerPosition;
                }
                else if (isCameraDragging)
                {
                    if (!IsPointerInsideMapViewport(camera, pointerPosition))
                    {
                        lastCameraDragScreenPosition = pointerPosition;
                        return;
                    }

                    if (!IsPointerInsideMapViewport(camera, lastCameraDragScreenPosition))
                    {
                        lastCameraDragScreenPosition = pointerPosition;
                        return;
                    }

                    var previousWorld = GetWorldPointFromScreen(camera, lastCameraDragScreenPosition);
                    var currentWorld = GetWorldPointFromScreen(camera, pointerPosition);
                    MoveCameraBy(previousWorld - currentWorld);
                    lastCameraDragScreenPosition = pointerPosition;
                }

                return;
            }

            isCameraDragging = false;

            if (IsPointerOverUi() || !IsPointerInsideMapViewport(camera, pointerPosition))
            {
                ClampCameraToMap();
                return;
            }

            var direction = Vector3.zero;
            var mapViewport = camera.pixelRect;
            if (pointerPosition.x <= mapViewport.xMin + CameraEdgeScrollMargin)
            {
                direction.x -= 1f;
            }
            else if (pointerPosition.x >= mapViewport.xMax - CameraEdgeScrollMargin)
            {
                direction.x += 1f;
            }

            if (pointerPosition.y <= mapViewport.yMin + CameraEdgeScrollMargin)
            {
                direction.y -= 1f;
            }
            else if (pointerPosition.y >= mapViewport.yMax - CameraEdgeScrollMargin)
            {
                direction.y += 1f;
            }

            if (direction.sqrMagnitude > 0f)
            {
                MoveCameraBy(direction.normalized * CameraEdgeScrollSpeed * Time.unscaledDeltaTime);
            }
            else
            {
                ClampCameraToMap();
            }
        }

        private void TryStartGameFromMapClick()
        {
            var camera = GetGameplayCamera();
            var pointerPosition = GetPointerScreenPosition();
            if (!IsPrimaryPointerPressed()
                || IsPointerOverUi()
                || !IsPointerInsideMapViewport(camera, pointerPosition))
            {
                return;
            }

            SpawnClickPointMarker(GetWorldPointFromScreen(camera, pointerPosition));
            gameStarted = true;
            if (startHintPanel != null)
            {
                startHintPanel.gameObject.SetActive(false);
            }

            status = "\u6218\u6597\u5f00\u59cb\uff01\u53f3\u952e\u62d6\u52a8\u3001\u6eda\u8f6e\u7f29\u653e\uff0c\u6216\u628a\u9f20\u6807\u9760\u8fd1\u5c4f\u5e55\u8fb9\u7f18\u67e5\u770b\u6218\u573a\u3002";
        }

        private void ApplyCameraZoom(Camera camera, Vector3 pointerPosition)
        {
            if (IsPointerOverUi() || !IsPointerInsideMapViewport(camera, pointerPosition))
            {
                return;
            }

            var scroll = GetScrollDelta();
            if (Mathf.Abs(scroll) <= 0.001f)
            {
                return;
            }

            var focusBeforeZoom = GetWorldPointFromScreen(camera, pointerPosition);
            var targetSize = Mathf.Clamp(
                camera.orthographicSize - scroll * CameraZoomStep,
                CameraMinOrthographicSize,
                CameraMaxOrthographicSize);

            if (Mathf.Approximately(camera.orthographicSize, targetSize))
            {
                return;
            }

            camera.orthographicSize = targetSize;
            var focusAfterZoom = GetWorldPointFromScreen(camera, pointerPosition);
            var cameraPosition = camera.transform.position + focusBeforeZoom - focusAfterZoom;
            cameraPosition.z = camera.transform.position.z;
            camera.transform.position = ClampCameraPosition(cameraPosition);
        }

        private void SetStatus(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                return;
            }

            currentStatus = message;
            if (statusLog.Count > 0 && statusLog[statusLog.Count - 1].EndsWith(message, StringComparison.Ordinal))
            {
                return;
            }

            var prefix = gameStarted ? FormatBattleClock() : "\u51c6\u5907";
            statusLog.Add("[" + prefix + "] " + message);
            while (statusLog.Count > 6)
            {
                statusLog.RemoveAt(0);
            }
        }

        private string BuildStatusLogText()
        {
            if (statusLog.Count == 0)
            {
                return currentStatus ?? string.Empty;
            }

            return string.Join("\n", statusLog);
        }

        private string FormatBattleClock()
        {
            var seconds = Mathf.Max(0, Mathf.FloorToInt(elapsedTime));
            return (seconds / 60).ToString("00") + ":" + (seconds % 60).ToString("00");
        }

        private Camera GetGameplayCamera()
        {
            if (gameplayCamera != null)
            {
                return gameplayCamera;
            }

            gameplayCamera = Camera.main;
            return gameplayCamera;
        }

        private void MoveCameraBy(Vector3 delta)
        {
            var camera = GetGameplayCamera();
            if (camera == null)
            {
                return;
            }

            camera.transform.position = ClampCameraPosition(camera.transform.position + new Vector3(delta.x, delta.y, 0f));
        }

        private void ClampCameraToMap()
        {
            var camera = GetGameplayCamera();
            if (camera != null)
            {
                camera.transform.position = ClampCameraPosition(camera.transform.position);
            }
        }

        private Vector3 ClampCameraPosition(Vector3 position)
        {
            var camera = GetGameplayCamera();
            if (camera == null || !hasMapBounds)
            {
                return position;
            }

            var halfHeight = camera.orthographicSize;
            var halfWidth = halfHeight * camera.aspect;
            var minX = mapBoundsMin.x + halfWidth;
            var maxX = mapBoundsMax.x - halfWidth;
            var minY = mapBoundsMin.y + halfHeight;
            var maxY = mapBoundsMax.y - halfHeight;
            var centerX = (mapBoundsMin.x + mapBoundsMax.x) * 0.5f;
            var centerY = (mapBoundsMin.y + mapBoundsMax.y) * 0.5f;

            position.x = minX <= maxX ? Mathf.Clamp(position.x, minX, maxX) : centerX;
            position.y = minY <= maxY ? Mathf.Clamp(position.y, minY, maxY) : centerY;
            return position;
        }

        private static bool IsPointerInsideScreen(Vector3 pointerPosition)
        {
            return pointerPosition.x >= 0f
                && pointerPosition.x <= Screen.width
                && pointerPosition.y >= 0f
                && pointerPosition.y <= Screen.height;
        }

        private static bool IsPointerInsideMapViewport(Camera camera, Vector3 pointerPosition)
        {
            if (camera == null || !IsPointerInsideScreen(pointerPosition))
            {
                return false;
            }

            var viewport = camera.pixelRect;
            return pointerPosition.x >= viewport.xMin
                && pointerPosition.x <= viewport.xMax
                && pointerPosition.y >= viewport.yMin
                && pointerPosition.y <= viewport.yMax;
        }

        private static Vector3 GetWorldPointFromScreen(Camera camera, Vector3 screenPosition)
        {
            screenPosition.z = -camera.transform.position.z;
            var worldPoint = camera.ScreenToWorldPoint(screenPosition);
            worldPoint.z = 0f;
            return worldPoint;
        }

        private void CreateBaseArt()
        {
            playerBaseRenderer = CreateSprite("Player Base Art", LoadAgeBaseSprite(ageIndex), playerBasePosition, 3);
            enemyBaseRenderer = CreateSprite("Enemy Base Art", LoadAgeBaseSprite(enemyAgeIndex), enemyBasePosition, 3);
            playerBaseRenderer.flipX = PlayerBaseFlipX;
            enemyBaseRenderer.flipX = EnemyBaseFlipX;
            RefreshBaseVisuals();
        }

        private Sprite LoadAgeBaseSprite(int index)
        {
            var visualSet = GetAgeVisualSet(index);
            return LoadSprite(visualSet.BaseSpritePath, 100f, AgeVisualSets[0].BaseSpritePath);
        }

        private Sprite LoadAgeMapSprite()
        {
            var visualSet = GetAgeVisualSet(ageIndex);
            var fallbackPath = selectedMap != null ? selectedMap.ResourcePath : AgeVisualSets[0].MapSpritePath;
            return LoadSprite(visualSet.MapSpritePath, MapPixelsPerUnit, fallbackPath);
        }

        private void RefreshMapVisuals()
        {
            if (battlefieldMapRenderer == null)
            {
                return;
            }

            battlefieldMapRenderer.sprite = LoadAgeMapSprite();
            battlefieldMapRenderer.transform.localScale = Vector3.one;
            CacheMapBounds(battlefieldMapRenderer.bounds);
            ClampCameraToMap();
        }

        private void RefreshBaseVisuals()
        {
            var playerBaseSprite = LoadAgeBaseSprite(ageIndex);
            var playerVisualSet = GetAgeVisualSet(ageIndex);
            var enemyBaseSprite = LoadAgeBaseSprite(enemyAgeIndex);
            var enemyVisualSet = GetAgeVisualSet(enemyAgeIndex);
            if (playerBaseRenderer != null)
            {
                playerBaseRenderer.sprite = playerBaseSprite;
                playerBaseRenderer.flipX = PlayerBaseFlipX;
                playerBaseRenderer.transform.localScale = Vector3.one * baseVisualScale;
                playerBaseRenderer.color = Color.Lerp(new Color(1f, 0.96f, 0.86f, 0.9f), playerVisualSet.FallbackTint, 0.28f);
            }

            if (enemyBaseRenderer != null)
            {
                enemyBaseRenderer.sprite = enemyBaseSprite;
                enemyBaseRenderer.flipX = EnemyBaseFlipX;
                enemyBaseRenderer.transform.localScale = Vector3.one * baseVisualScale;
                enemyBaseRenderer.color = Color.Lerp(new Color(1f, 0.66f, 0.58f, 0.82f), enemyVisualSet.FallbackTint, 0.18f);
            }
        }

        private void CreateResourceWellSiteMarkers()
        {
            for (var i = 0; i < playerResourceWellPositions.Length; i++)
            {
                var marker = CreateSprite("Shared Resource Point Site " + (i + 1), ResourceWellSiteSprite, playerResourceWellPositions[i], 4);
                marker.transform.SetParent(facilityMarkerRoot, true);
                marker.transform.localScale = Vector3.one * 0.86f;
                marker.color = new Color(1f, 0.9f, 0.28f, 0.88f);
            }
        }

        private void CreateLaneMarker(int laneIndex)
        {
            var marker = CreateSprite("Lane " + laneIndex + " Combat Guide", WhiteSprite, new Vector3(0f, GetLaneY(laneIndex), 0f), 5);
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
            root.offsetMin = Vector2.zero;
            root.offsetMax = Vector2.zero;

            BuildTopHud(root);
            BuildCommandPanel(root);
            BuildStartHintPanel(root);
            BuildStatusPanel(root);
            BuildCommandTooltip(root);
            BuildOutcomeOverlay(canvasObject.transform);
        }

        private void BuildTopHud(RectTransform root)
        {
            CreateControlBarBackground("Top Embedded Control Frame", root, true, TopControlBarHeight);

            RectTransform topContent;
            CreateControlFrame(
                "Top Battle Status Frame",
                root,
                "\u6218\u573a\u72b6\u6001",
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(980f, 166f),
                new Vector2(0f, -TopControlBarHeight * 0.5f),
                new Color(0.92f, 0.62f, 0.24f, 1f),
                out topContent);

            var layout = topContent.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(8, 8, 4, 4);
            layout.spacing = 14f;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = true;
            layout.childForceExpandWidth = true;

            var playerPanel = CreateHudCell("Player Base Cell", topContent, "\u6211\u65b9\u57fa\u5730");
            playerHealthText = CreateText(playerPanel, "Player Health", string.Empty, 18, FontStyle.Bold, TextAnchor.MiddleLeft);
            playerHealthText.rectTransform.anchorMin = new Vector2(0.04f, 0.48f);
            playerHealthText.rectTransform.anchorMax = new Vector2(0.96f, 0.9f);
            playerHealthText.rectTransform.offsetMin = Vector2.zero;
            playerHealthText.rectTransform.offsetMax = Vector2.zero;
            playerHealthFill = CreateProgressBar(playerPanel, new Color(0.16f, 0.68f, 0.42f, 1f));

            var resourcePanel = CreateHudCell("Resource Cell", topContent, "\u8d44\u6e90\u4e0e\u65f6\u4ee3");
            coinText = CreateText(resourcePanel, "Coins", string.Empty, 24, FontStyle.Bold, TextAnchor.MiddleCenter);
            coinText.rectTransform.anchorMin = new Vector2(0.04f, 0.5f);
            coinText.rectTransform.anchorMax = new Vector2(0.96f, 0.86f);
            coinText.rectTransform.offsetMin = Vector2.zero;
            coinText.rectTransform.offsetMax = Vector2.zero;

            ageText = CreateText(resourcePanel, "Age", string.Empty, 17, FontStyle.Bold, TextAnchor.MiddleCenter);
            ageText.rectTransform.anchorMin = new Vector2(0.04f, 0.3f);
            ageText.rectTransform.anchorMax = new Vector2(0.96f, 0.56f);
            ageText.rectTransform.offsetMin = Vector2.zero;
            ageText.rectTransform.offsetMax = Vector2.zero;

            eraText = CreateText(resourcePanel, "Era Value", string.Empty, 14, FontStyle.Normal, TextAnchor.MiddleLeft);
            eraText.rectTransform.anchorMin = new Vector2(0.04f, 0.1f);
            eraText.rectTransform.anchorMax = new Vector2(0.4f, 0.32f);
            eraText.rectTransform.offsetMin = Vector2.zero;
            eraText.rectTransform.offsetMax = Vector2.zero;
            eraFill = CreateProgressBar(resourcePanel, new Color(0.95f, 0.65f, 0.25f, 1f));
            var eraBackground = eraFill.transform.parent as RectTransform;
            if (eraBackground != null)
            {
                eraBackground.anchorMin = new Vector2(0.42f, 0.12f);
                eraBackground.anchorMax = new Vector2(0.96f, 0.32f);
            }

            var enemyPanel = CreateHudCell("Enemy Base Cell", topContent, "\u654c\u65b9\u57fa\u5730");
            enemyHealthText = CreateText(enemyPanel, "Enemy Health", string.Empty, 18, FontStyle.Bold, TextAnchor.MiddleRight);
            enemyHealthText.rectTransform.anchorMin = new Vector2(0.04f, 0.48f);
            enemyHealthText.rectTransform.anchorMax = new Vector2(0.96f, 0.9f);
            enemyHealthText.rectTransform.offsetMin = Vector2.zero;
            enemyHealthText.rectTransform.offsetMax = Vector2.zero;
            enemyHealthFill = CreateProgressBar(enemyPanel, new Color(0.82f, 0.23f, 0.17f, 1f));
        }

        private RectTransform CreateHudCell(string name, RectTransform parent, string title)
        {
            var cell = CreatePanel(name, parent, new Color(0.24f, 0.19f, 0.12f, 0.98f));
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

        private void BuildStartHintPanel(RectTransform root)
        {
            RectTransform content;
            var panel = CreateControlFrame(
                "Start Hint Panel",
                root,
                "\u6307\u6325\u63d0\u793a",
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(330f, 166f),
                new Vector2(-105f, BottomControlBarHeight * 0.5f),
                new Color(0.82f, 0.66f, 0.28f, 1f),
                out content);
            startHintPanel = panel;

            laneText = CreateText(content, "Start Hint Body", string.Empty, 15, FontStyle.Bold, TextAnchor.UpperLeft);
            laneText.color = new Color(0.96f, 0.92f, 0.78f, 1f);
            laneText.resizeTextForBestFit = true;
            laneText.resizeTextMinSize = 12;
            laneText.resizeTextMaxSize = 15;
            laneText.rectTransform.anchorMin = Vector2.zero;
            laneText.rectTransform.anchorMax = Vector2.one;
            laneText.rectTransform.offsetMin = new Vector2(4f, 2f);
            laneText.rectTransform.offsetMax = new Vector2(-4f, -2f);
        }

        private void BuildCommandPanel(RectTransform root)
        {
            CreateControlBarBackground("Bottom Embedded Control Frame", root, false, BottomControlBarHeight);

            RectTransform combatContent;
            CreateControlFrame(
                "Combat Skill Frame",
                root,
                "\u6218\u6597\u6280\u80fd",
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(340f, 150f),
                new Vector2(194f, -TopControlBarHeight * 0.5f),
                new Color(0.95f, 0.42f, 0.22f, 1f),
                out combatContent);

            RectTransform actionContent;
            CreateControlFrame(
                "Era System Frame",
                root,
                "\u65f6\u4ee3 / \u7cfb\u7edf",
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Vector2(340f, 150f),
                new Vector2(-194f, -TopControlBarHeight * 0.5f),
                new Color(0.55f, 0.78f, 1f, 1f),
                out actionContent);

            RectTransform unitContent;
            CreateControlFrame(
                "Soldier Role Frame",
                root,
                "\u5175\u79cd\u89d2\u8272",
                new Vector2(0f, 0f),
                new Vector2(0f, 0f),
                new Vector2(630f, 166f),
                new Vector2(339f, BottomControlBarHeight * 0.5f),
                new Color(0.92f, 0.68f, 0.28f, 1f),
                out unitContent);

            RectTransform facilityContent;
            CreateControlFrame(
                "Defense Facility Frame",
                root,
                "\u9632\u5fa1\u5854 / \u8d44\u6e90\u4e95",
                new Vector2(1f, 0f),
                new Vector2(1f, 0f),
                new Vector2(430f, 166f),
                new Vector2(-239f, BottomControlBarHeight * 0.5f),
                new Color(0.38f, 0.82f, 0.86f, 1f),
                out facilityContent);

            var unitGrid = CreateCommandButtonGrid(unitContent, "Soldier Image Buttons", new Vector2(UnitCommandButtonSize, UnitCommandButtonSize), playerUnitDefinitions.Length, 0f);
            var facilityGrid = CreateCommandButtonGrid(facilityContent, "Facility Image Buttons", new Vector2(UnitCommandButtonSize, UnitCommandButtonSize), currentTowerDefinitions.Length + 1, 0f);
            var combatGrid = CreateCommandButtonGrid(combatContent, "Combat Skill Image Buttons", new Vector2(SkillCommandButtonSize, SkillCommandButtonSize), 3, 0f);
            var actionGrid = CreateCommandButtonGrid(actionContent, "Era System Image Buttons", new Vector2(SkillCommandButtonSize, SkillCommandButtonSize), 3, 0f);

            for (var i = 0; i < playerUnitDefinitions.Length; i++)
            {
                var definition = playerUnitDefinitions[i];
                var slotIndex = i;
                var button = CreateImageButton(
                    unitGrid,
                    definition.Key,
                    GetUnitButtonSprite(definition),
                    () => SelectPlayerUnit(unitButtons[slotIndex].Definition),
                    new Color(0.43f, 0.31f, 0.17f, 1f),
                    () => FormatUnitTooltip(unitButtons[slotIndex].Definition));
                unitButtons.Add(new UnitButtonBinding(button, GetButtonLabel(button), GetButtonIcon(button), definition));
            }

            for (var i = 0; i < currentTowerDefinitions.Length; i++)
            {
                var towerIndex = i;
                var definition = currentTowerDefinitions[i];
                var button = CreateImageButton(
                    facilityGrid,
                    "Tower " + (i + 1),
                    GetTowerButtonSprite(towerIndex),
                    () => SelectTowerForBuild(towerIndex),
                    new Color(0.31f, 0.43f, 0.45f, 1f),
                    () => FormatTowerTooltip(GetTowerDefinition(towerIndex)));
                towerButtons.Add(new TowerButtonBinding(button, GetButtonLabel(button), GetButtonIcon(button), towerIndex, definition));
            }

            resourceWellButton = CreateImageButton(
                facilityGrid,
                "Resource Well",
                ResourceWellBuiltSprite,
                TryBuildResourceWell,
                new Color(0.24f, 0.46f, 0.36f, 1f),
                FormatResourceWellTooltip);

            agePowerButton = CreateImageButton(
                combatGrid,
                "Age Power",
                GetAgePowerButtonSprite(ageIndex),
                UseAgePower,
                new Color(0.58f, 0.31f, 0.18f, 1f),
                FormatAgePowerTooltip);
            shieldButton = CreateImageButton(
                combatGrid,
                "Shield Barrier",
                ShieldIconSprite,
                UseShieldBarrier,
                new Color(0.24f, 0.42f, 0.58f, 1f),
                FormatShieldBarrierTooltip);
            mobilizationButton = CreateImageButton(
                combatGrid,
                "Mobilization",
                MobilizationIconSprite,
                UseMobilization,
                new Color(0.52f, 0.44f, 0.18f, 1f),
                FormatMobilizationTooltip);
            attackUpgradeButton = CreateImageButton(
                actionGrid,
                "Attack Evolution",
                AttackEvolutionIconSprite,
                () => UpgradeAge(EvolutionPath.Attack),
                new Color(0.62f, 0.22f, 0.16f, 1f),
                () => FormatEvolutionTooltip(EvolutionPath.Attack));
            defenseUpgradeButton = CreateImageButton(
                actionGrid,
                "Defense Evolution",
                DefenseEvolutionIconSprite,
                () => UpgradeAge(EvolutionPath.Defense),
                new Color(0.22f, 0.38f, 0.56f, 1f),
                () => FormatEvolutionTooltip(EvolutionPath.Defense));
            restartButton = CreateImageButton(
                actionGrid,
                "Restart",
                RestartIconSprite,
                RestartBattle,
                new Color(0.38f, 0.32f, 0.46f, 1f),
                FormatRestartTooltip);
        }

        private RectTransform CreateCommandButtonGrid(RectTransform parent, string name, Vector2 cellSize, int columnCount, float preferredWidth)
        {
            var gridRoot = CreateRect(name, parent);
            gridRoot.anchorMin = Vector2.zero;
            gridRoot.anchorMax = Vector2.one;
            gridRoot.offsetMin = Vector2.zero;
            gridRoot.offsetMax = Vector2.zero;

            if (preferredWidth > 0f)
            {
                var layoutElement = gridRoot.gameObject.AddComponent<LayoutElement>();
                layoutElement.preferredWidth = preferredWidth;
                layoutElement.minWidth = preferredWidth;
                layoutElement.flexibleHeight = 1f;
            }

            var grid = gridRoot.gameObject.AddComponent<GridLayoutGroup>();
            grid.padding = new RectOffset(4, 4, 4, 4);
            grid.spacing = new Vector2(CommandGridGap, CommandGridGap);
            grid.cellSize = cellSize;
            grid.childAlignment = TextAnchor.MiddleCenter;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = Mathf.Max(1, columnCount);
            return gridRoot;
        }

        private void CreateControlBarBackground(string name, RectTransform parent, bool top, float height)
        {
            var bar = CreateRect(name, parent);
            bar.anchorMin = top ? new Vector2(0f, 1f) : Vector2.zero;
            bar.anchorMax = top ? Vector2.one : new Vector2(1f, 0f);
            bar.sizeDelta = new Vector2(0f, height);
            bar.anchoredPosition = new Vector2(0f, top ? -height * 0.5f : height * 0.5f);

            var image = bar.gameObject.AddComponent<Image>();
            image.sprite = WhiteSprite;
            image.color = new Color(0.16f, 0.1f, 0.055f, 0.98f);
            image.raycastTarget = true;

            AddFrameLine(
                bar,
                "Outer Bronze Edge",
                top ? Vector2.zero : new Vector2(0f, 1f),
                top ? new Vector2(1f, 0f) : Vector2.one,
                top ? Vector2.zero : new Vector2(0f, -5f),
                top ? new Vector2(0f, 5f) : Vector2.zero,
                new Color(0.72f, 0.48f, 0.2f, 1f));
            AddFrameLine(bar, "Inner Amber Glow", top ? new Vector2(0f, 0.08f) : new Vector2(0f, 0.9f), top ? new Vector2(1f, 0.08f) : new Vector2(1f, 0.9f), Vector2.zero, new Vector2(0f, 3f), new Color(0.95f, 0.62f, 0.22f, 0.72f));
        }

        private RectTransform CreateControlFrame(
            string name,
            RectTransform parent,
            string title,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 sizeDelta,
            Vector2 anchoredPosition,
            Color accentColor,
            out RectTransform content)
        {
            var frame = CreatePanel(name, parent, new Color(0.25f, 0.16f, 0.085f, 0.97f));
            frame.anchorMin = anchorMin;
            frame.anchorMax = anchorMax;
            frame.sizeDelta = sizeDelta;
            frame.anchoredPosition = anchoredPosition;

            var frameImage = frame.GetComponent<Image>();
            if (frameImage != null)
            {
                frameImage.raycastTarget = true;
            }

            var outline = frame.gameObject.AddComponent<Outline>();
            outline.effectColor = Color.Lerp(accentColor, new Color(0.16f, 0.08f, 0.02f, 1f), 0.38f);
            outline.effectDistance = new Vector2(2f, -2f);

            AddFrameLine(frame, "Top Accent", new Vector2(0.04f, 1f), new Vector2(0.96f, 1f), new Vector2(0f, -10f), new Vector2(0f, -7f), accentColor);
            AddFrameLine(frame, "Bottom Accent", new Vector2(0.04f, 0f), new Vector2(0.96f, 0f), new Vector2(0f, 7f), new Vector2(0f, 10f), Color.Lerp(accentColor, Color.black, 0.25f));
            AddFrameBolt(frame, "Top Left Rivet", new Vector2(0f, 1f), new Vector2(15f, -15f), accentColor);
            AddFrameBolt(frame, "Top Right Rivet", new Vector2(1f, 1f), new Vector2(-15f, -15f), accentColor);
            AddFrameBolt(frame, "Bottom Left Rivet", Vector2.zero, new Vector2(15f, 15f), accentColor);
            AddFrameBolt(frame, "Bottom Right Rivet", new Vector2(1f, 0f), new Vector2(-15f, 15f), accentColor);

            var titleText = CreateText(frame, "Frame Title", title, 17, FontStyle.Bold, TextAnchor.MiddleCenter);
            titleText.color = new Color(1f, 0.88f, 0.58f, 1f);
            titleText.resizeTextForBestFit = true;
            titleText.resizeTextMinSize = 12;
            titleText.resizeTextMaxSize = 17;
            titleText.rectTransform.anchorMin = new Vector2(0.08f, 1f);
            titleText.rectTransform.anchorMax = new Vector2(0.92f, 1f);
            titleText.rectTransform.sizeDelta = new Vector2(0f, 30f);
            titleText.rectTransform.anchoredPosition = new Vector2(0f, -20f);

            content = CreateRect("Content", frame);
            content.anchorMin = Vector2.zero;
            content.anchorMax = Vector2.one;
            content.offsetMin = new Vector2(16f, 14f);
            content.offsetMax = new Vector2(-16f, -42f);
            return frame;
        }

        private void AddFrameLine(RectTransform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, Color color)
        {
            var line = CreateRect(name, parent);
            line.anchorMin = anchorMin;
            line.anchorMax = anchorMax;
            line.offsetMin = offsetMin;
            line.offsetMax = offsetMax;

            var image = line.gameObject.AddComponent<Image>();
            image.sprite = WhiteSprite;
            image.color = color;
            image.raycastTarget = false;
        }

        private void AddFrameBolt(RectTransform parent, string name, Vector2 anchor, Vector2 anchoredPosition, Color accentColor)
        {
            var bolt = CreateRect(name, parent);
            bolt.anchorMin = anchor;
            bolt.anchorMax = anchor;
            bolt.sizeDelta = new Vector2(16f, 16f);
            bolt.anchoredPosition = anchoredPosition;

            var image = bolt.gameObject.AddComponent<Image>();
            image.sprite = IconDiscSprite;
            image.color = Color.Lerp(accentColor, new Color(0.12f, 0.07f, 0.025f, 1f), 0.2f);
            image.raycastTarget = false;
        }

        private void BuildCommandTooltip(RectTransform root)
        {
            commandTooltip = CreatePanel("Command Detail Tooltip", root, new Color(0.08f, 0.1f, 0.09f, 0.96f));
            var tooltipImage = commandTooltip.GetComponent<Image>();
            if (tooltipImage != null)
            {
                tooltipImage.raycastTarget = false;
            }

            commandTooltip.anchorMin = new Vector2(0.5f, 0.5f);
            commandTooltip.anchorMax = new Vector2(0.5f, 0.5f);
            commandTooltip.pivot = Vector2.zero;
            commandTooltip.sizeDelta = new Vector2(495f, 285f);
            commandTooltip.gameObject.SetActive(false);

            var outline = commandTooltip.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.88f, 0.68f, 0.32f, 0.72f);
            outline.effectDistance = new Vector2(2f, -2f);

            commandTooltipText = CreateText(commandTooltip, "Command Detail Text", string.Empty, CommandTooltipFontSize, FontStyle.Bold, TextAnchor.UpperLeft);
            commandTooltipText.color = new Color(0.96f, 0.91f, 0.78f, 1f);
            commandTooltipText.resizeTextForBestFit = true;
            commandTooltipText.resizeTextMinSize = CommandTooltipMinFontSize;
            commandTooltipText.resizeTextMaxSize = CommandTooltipMaxFontSize;
            commandTooltipText.raycastTarget = false;
            commandTooltipText.rectTransform.anchorMin = Vector2.zero;
            commandTooltipText.rectTransform.anchorMax = Vector2.one;
            commandTooltipText.rectTransform.offsetMin = new Vector2(21f, 18f);
            commandTooltipText.rectTransform.offsetMax = new Vector2(-21f, -18f);
        }

        private void ShowCommandTooltip(string message, Vector2 screenPosition)
        {
            if (commandTooltip == null || commandTooltipText == null || string.IsNullOrEmpty(message))
            {
                return;
            }

            commandTooltipText.text = message;
            commandTooltip.gameObject.SetActive(true);
            commandTooltip.SetAsLastSibling();
            MoveCommandTooltip(screenPosition);
        }

        private void BeginCommandTooltipHover(Func<string> tooltipProvider, Vector2 screenPosition)
        {
            commandTooltipProvider = tooltipProvider;
            commandTooltipPointerPosition = screenPosition;
            if (commandTooltipDelayRoutine != null)
            {
                StopCoroutine(commandTooltipDelayRoutine);
            }

            if (commandTooltip != null)
            {
                commandTooltip.gameObject.SetActive(false);
            }

            commandTooltipDelayRoutine = StartCoroutine(ShowCommandTooltipAfterDelay());
        }

        private IEnumerator ShowCommandTooltipAfterDelay()
        {
            yield return new WaitForSecondsRealtime(CommandTooltipDelaySeconds);
            commandTooltipDelayRoutine = null;
            ShowCommandTooltip(commandTooltipProvider?.Invoke(), commandTooltipPointerPosition);
        }

        private void HideCommandTooltip()
        {
            if (commandTooltipDelayRoutine != null)
            {
                StopCoroutine(commandTooltipDelayRoutine);
                commandTooltipDelayRoutine = null;
            }

            commandTooltipProvider = null;
            if (commandTooltip != null)
            {
                commandTooltip.gameObject.SetActive(false);
            }
        }

        private void MoveCommandTooltip(Vector2 screenPosition)
        {
            if (commandTooltip == null)
            {
                return;
            }

            var parent = commandTooltip.parent as RectTransform;
            if (parent == null)
            {
                commandTooltip.position = new Vector3(screenPosition.x, screenPosition.y, 0f);
                return;
            }

            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screenPosition, null, out localPoint);
            var anchoredPosition = localPoint + new Vector2(18f, 18f);
            var parentRect = parent.rect;
            var size = commandTooltip.sizeDelta;
            anchoredPosition.x = Mathf.Clamp(anchoredPosition.x, parentRect.xMin + 8f, parentRect.xMax - size.x - 8f);
            anchoredPosition.y = Mathf.Clamp(anchoredPosition.y, parentRect.yMin + 8f, parentRect.yMax - size.y - 8f);
            commandTooltip.anchoredPosition = anchoredPosition;
        }

        private void BuildStatusPanel(RectTransform root)
        {
            RectTransform content;
            CreateControlFrame(
                "Battle Log Panel",
                root,
                "\u6218\u62a5",
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(390f, 166f),
                new Vector2(265f, BottomControlBarHeight * 0.5f),
                new Color(0.72f, 0.9f, 0.52f, 1f),
                out content);

            statusText = CreateText(content, "Battle Log", string.Empty, 14, FontStyle.Bold, TextAnchor.LowerLeft);
            statusText.color = new Color(0.94f, 0.91f, 0.76f, 1f);
            statusText.rectTransform.anchorMin = Vector2.zero;
            statusText.rectTransform.anchorMax = Vector2.one;
            statusText.rectTransform.offsetMin = new Vector2(4f, 2f);
            statusText.rectTransform.offsetMax = new Vector2(-4f, -2f);
        }

        private void BuildOutcomeOverlay(Transform canvasRoot)
        {
            outcomeOverlay = CreateRect("Outcome Overlay", canvasRoot);
            outcomeOverlay.anchorMin = Vector2.zero;
            outcomeOverlay.anchorMax = Vector2.one;
            outcomeOverlay.offsetMin = Vector2.zero;
            outcomeOverlay.offsetMax = Vector2.zero;
            outcomeOverlay.gameObject.SetActive(false);

            var backdropRect = CreateRect("Outcome Backdrop", outcomeOverlay);
            backdropRect.anchorMin = Vector2.zero;
            backdropRect.anchorMax = Vector2.one;
            backdropRect.offsetMin = Vector2.zero;
            backdropRect.offsetMax = Vector2.zero;
            outcomeBackdropImage = backdropRect.gameObject.AddComponent<Image>();
            outcomeBackdropImage.sprite = WhiteSprite;
            outcomeBackdropImage.color = Color.white;
            outcomeBackdropImage.preserveAspect = false;
            outcomeBackdropImage.raycastTarget = true;

            var shade = CreateRect("Outcome Shade", outcomeOverlay);
            shade.anchorMin = Vector2.zero;
            shade.anchorMax = Vector2.one;
            shade.offsetMin = Vector2.zero;
            shade.offsetMax = Vector2.zero;
            var shadeImage = shade.gameObject.AddComponent<Image>();
            shadeImage.sprite = WhiteSprite;
            shadeImage.color = new Color(0.015f, 0.012f, 0.01f, 0.44f);
            shadeImage.raycastTarget = true;

            var topFade = CreateRect("Outcome Top Fade", outcomeOverlay);
            topFade.anchorMin = new Vector2(0f, 0.64f);
            topFade.anchorMax = Vector2.one;
            topFade.offsetMin = Vector2.zero;
            topFade.offsetMax = Vector2.zero;
            var topFadeImage = topFade.gameObject.AddComponent<Image>();
            topFadeImage.sprite = WhiteSprite;
            topFadeImage.color = new Color(0f, 0f, 0f, 0.22f);
            topFadeImage.raycastTarget = false;

            outcomeTitleShadowText = CreateText(outcomeOverlay, "Outcome Title Shadow", string.Empty, 86, FontStyle.Bold, TextAnchor.MiddleCenter);
            outcomeTitleShadowText.color = new Color(0f, 0f, 0f, 0.82f);
            outcomeTitleShadowText.resizeTextForBestFit = true;
            outcomeTitleShadowText.resizeTextMinSize = 42;
            outcomeTitleShadowText.resizeTextMaxSize = 86;
            outcomeTitleShadowText.rectTransform.anchorMin = new Vector2(0.08f, 0.54f);
            outcomeTitleShadowText.rectTransform.anchorMax = new Vector2(0.92f, 0.76f);
            outcomeTitleShadowText.rectTransform.offsetMin = new Vector2(5f, -6f);
            outcomeTitleShadowText.rectTransform.offsetMax = new Vector2(5f, -6f);

            outcomeTitleText = CreateText(outcomeOverlay, "Outcome Title", string.Empty, 86, FontStyle.Bold, TextAnchor.MiddleCenter);
            outcomeTitleText.resizeTextForBestFit = true;
            outcomeTitleText.resizeTextMinSize = 42;
            outcomeTitleText.resizeTextMaxSize = 86;
            outcomeTitleText.rectTransform.anchorMin = new Vector2(0.08f, 0.54f);
            outcomeTitleText.rectTransform.anchorMax = new Vector2(0.92f, 0.76f);
            outcomeTitleText.rectTransform.offsetMin = Vector2.zero;
            outcomeTitleText.rectTransform.offsetMax = Vector2.zero;

            outcomeSubtitleText = CreateText(outcomeOverlay, "Outcome Subtitle", string.Empty, 30, FontStyle.Bold, TextAnchor.MiddleCenter);
            outcomeSubtitleText.resizeTextForBestFit = true;
            outcomeSubtitleText.resizeTextMinSize = 16;
            outcomeSubtitleText.resizeTextMaxSize = 30;
            outcomeSubtitleText.color = new Color(0.95f, 0.9f, 0.78f, 1f);
            outcomeSubtitleText.rectTransform.anchorMin = new Vector2(0.12f, 0.44f);
            outcomeSubtitleText.rectTransform.anchorMax = new Vector2(0.88f, 0.54f);
            outcomeSubtitleText.rectTransform.offsetMin = Vector2.zero;
            outcomeSubtitleText.rectTransform.offsetMax = Vector2.zero;

            outcomeStatsText = CreateText(outcomeOverlay, "Outcome Stats", string.Empty, 21, FontStyle.Bold, TextAnchor.MiddleCenter);
            outcomeStatsText.resizeTextForBestFit = true;
            outcomeStatsText.resizeTextMinSize = 13;
            outcomeStatsText.resizeTextMaxSize = 21;
            outcomeStatsText.color = new Color(0.78f, 0.84f, 0.78f, 1f);
            outcomeStatsText.rectTransform.anchorMin = new Vector2(0.16f, 0.36f);
            outcomeStatsText.rectTransform.anchorMax = new Vector2(0.84f, 0.43f);
            outcomeStatsText.rectTransform.offsetMin = Vector2.zero;
            outcomeStatsText.rectTransform.offsetMax = Vector2.zero;

            var buttonRow = CreateRect("Outcome Button Row", outcomeOverlay);
            buttonRow.anchorMin = new Vector2(0.5f, 0.22f);
            buttonRow.anchorMax = new Vector2(0.5f, 0.22f);
            buttonRow.pivot = new Vector2(0.5f, 0.5f);
            buttonRow.sizeDelta = new Vector2(520f, 74f);
            buttonRow.anchoredPosition = Vector2.zero;

            var rowLayout = buttonRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            rowLayout.spacing = 18f;
            rowLayout.childControlHeight = true;
            rowLayout.childControlWidth = true;
            rowLayout.childForceExpandHeight = true;
            rowLayout.childForceExpandWidth = true;

            var restart = CreateButton(buttonRow, "Outcome Restart", "\u518d\u6218\u4e00\u5c40", RestartBattle, new Color(0.55f, 0.24f, 0.16f, 1f));
            restart.gameObject.AddComponent<LayoutElement>().minWidth = 248f;
            var menu = CreateButton(buttonRow, "Outcome Main Menu", "\u8fd4\u56de\u4e3b\u83dc\u5355", ReturnToMainMenu, new Color(0.24f, 0.34f, 0.42f, 1f));
            menu.gameObject.AddComponent<LayoutElement>().minWidth = 248f;
        }

        private void SelectLane(int laneIndex)
        {
            selectedLane = Mathf.Clamp(laneIndex, 0, laneNames.Length - 1);
            status = "\u9ed8\u8ba4\u51fa\u5175\u8def\u7ebf\u5df2\u5207\u6362\u5230" + laneNames[selectedLane] + "\uff0c\u4e4b\u540e\u65b0\u51fa\u7684\u58eb\u5175\u4f1a\u8d70\u8fd9\u6761\u8def\u3002";
        }

        private void SelectPlayerUnit(UnitDefinition definition)
        {
            if (gameOver || definition == null)
            {
                return;
            }

            if (activeBuildPlacement != BuildPlacementKind.None)
            {
                activeBuildPlacement = BuildPlacementKind.None;
                ClearBuildPlacementPreviews();
            }

            if (coins < GetUnitCost(definition))
            {
                status = "\u91d1\u5e01\u4e0d\u8db3\uff0c\u8fd8\u4e0d\u80fd\u6d3e\u51fa" + definition.DisplayName + "\u3002";
                return;
            }

            TrySpawnPlayerUnit(definition);
        }

        private bool UpdateBuildPlacementInput()
        {
            if (activeBuildPlacement == BuildPlacementKind.None)
            {
                return false;
            }

            if (IsCancelPressed())
            {
                CancelBuildPlacement("\u5df2\u53d6\u6d88\u8bbe\u65bd\u5efa\u9020\u3002");
                return true;
            }

            if (!IsPrimaryPointerPressed())
            {
                return true;
            }

            if (IsPointerOverUi())
            {
                return true;
            }

            var camera = GetGameplayCamera();
            var pointerPosition = GetPointerScreenPosition();
            if (!IsPointerInsideMapViewport(camera, pointerPosition))
            {
                return true;
            }

            var worldPoint = GetWorldPointFromScreen(camera, pointerPosition);
            if (!HasSelectedPlayerBuilder())
            {
                if (TrySelectPlayerBuilderAt(worldPoint))
                {
                    ShowBuildPlacementPreviews(activeBuildPlacement);
                    status = "\u5df2\u9009\u4e2d\u5efa\u7b51\u5175\uff0c\u70b9\u51fb\u53ef\u5efa\u9020\u4f4d\u7f6e\u8ba9\u4ed6\u524d\u5f80\u65bd\u5de5\u3002";
                    return true;
                }

                SpawnClickPointMarker(worldPoint);
                status = "\u8bf7\u5148\u70b9\u51fb\u573a\u4e0a\u5df2\u6709\u7684\u5efa\u7b51\u5175\uff0c\u4e0d\u4f1a\u81ea\u52a8\u751f\u6210\u65b0\u5efa\u7b51\u5175\u3002";
                return true;
            }

            SpawnClickPointMarker(worldPoint);
            if (TryGetBuildSlotAt(activeBuildPlacement, worldPoint, out var slotIndex))
            {
                ConfirmBuildPlacement(slotIndex);
            }
            else
            {
                status = "\u8bf7\u70b9\u51fb\u53ef\u6d3e\u5efa\u7b51\u5175\u4fee\u5efa\u7684\u6807\u5fd7\uff0c\u6309 Esc \u53d6\u6d88\u3002";
            }

            return true;
        }

        private void BeginBuildPlacement(BuildPlacementKind kind)
        {
            if (!gameStarted || gameOver)
            {
                return;
            }

            if (!HasAvailableBuildSlot(kind))
            {
                status = GetBuildPlacementName(kind) + "\u6ca1\u6709\u53ef\u7528\u7a7a\u4f4d\u3002";
                return;
            }

            var cost = GetBuildPlacementCost(kind);
            if (coins < cost)
            {
                status = "\u91d1\u5e01\u4e0d\u8db3\uff0c\u8fd8\u4e0d\u80fd\u5efa\u9020" + GetBuildPlacementName(kind) + "\u3002";
                return;
            }

            activeBuildPlacement = kind;
            if (HasSelectedPlayerBuilder())
            {
                ShowBuildPlacementPreviews(kind);
                status = "\u5df2\u9009\u62e9" + GetBuildPlacementName(kind) + "\uff0c\u70b9\u51fb\u53ef\u5efa\u9020\u4f4d\u7f6e\uff0c\u5df2\u9009\u4e2d\u7684\u5efa\u7b51\u5175\u4f1a\u524d\u5f80\u65bd\u5de5\u3002";
                return;
            }

            ClearBuildPlacementPreviews();
            status = "\u5df2\u9009\u62e9" + GetBuildPlacementName(kind) + "\uff0c\u8bf7\u5148\u70b9\u51fb\u573a\u4e0a\u5df2\u6709\u7684\u5efa\u7b51\u5175\uff0c\u7136\u540e\u518d\u9009\u65bd\u5de5\u4f4d\u7f6e\u3002";
        }

        private void ConfirmBuildPlacement(int slotIndex)
        {
            var kind = activeBuildPlacement;
            if (kind == BuildPlacementKind.None)
            {
                return;
            }

            var cost = GetBuildPlacementCost(kind);
            if (coins < cost)
            {
                status = "\u91d1\u5e01\u4e0d\u8db3\uff0c\u5efa\u9020\u5df2\u4fdd\u7559\uff0c\u53ef\u7a0d\u540e\u518d\u9009\u4f4d\u3002";
                return;
            }

            if (DispatchBuilderToBuild(kind, slotIndex))
            {
                activeBuildPlacement = BuildPlacementKind.None;
                ClearBuildPlacementPreviews();
            }
        }

        private void CancelBuildPlacement(string message)
        {
            activeBuildPlacement = BuildPlacementKind.None;
            ClearBuildPlacementPreviews();
            status = message;
        }

        private void ShowBuildPlacementPreviews(BuildPlacementKind kind)
        {
            ClearBuildPlacementPreviews();

            var positions = GetBuildPlacementPositions(kind);
            for (var i = 0; i < positions.Length; i++)
            {
                if (!IsBuildSlotAvailable(kind, i))
                {
                    continue;
                }

                var previewRoot = new GameObject(GetBuildPlacementName(kind) + " Build Preview " + (i + 1)).transform;
                previewRoot.SetParent(buildPreviewRoot, false);
                previewRoot.position = positions[i];

                CreateBuilderConstructionRangePreview(previewRoot, positions[i], kind, i);

                var marker = CreateSprite(GetBuildPlacementName(kind) + " Build Prompt " + (i + 1), GetBuildPlacementMarkerSprite(kind), positions[i], 92 + i);
                marker.transform.SetParent(previewRoot, true);
                marker.transform.localScale = Vector3.one * (kind == BuildPlacementKind.ResourceWell ? 0.78f : 0.76f);
                marker.color = Color.white;
                marker.gameObject.AddComponent<BattleBuildPromptPulse>().Configure(0.82f, 1.18f, 2.7f);
                buildPlacementPreviews.Add(new BuildPlacementPreview(previewRoot.gameObject, i));
            }
        }

        private void CreateBuilderConstructionRangePreview(Transform parent, Vector3 center, BuildPlacementKind kind, int index)
        {
            var tint = kind == BuildPlacementKind.ResourceWell
                ? new Color(0.25f, 1f, 0.55f, 1f)
                : new Color(1f, 0.74f, 0.24f, 1f);

            var rangeDisc = CreateSprite("Builder Construction Range Fill", VfxCircleSprite, center, 88 + index);
            rangeDisc.transform.SetParent(parent, true);
            rangeDisc.color = new Color(tint.r, tint.g, tint.b, 0.16f);
            var spriteDiameter = Mathf.Max(0.01f, rangeDisc.sprite.bounds.size.x);
            rangeDisc.transform.localScale = Vector3.one * (BuilderConstructionRange * 2f / spriteDiameter);

            CreateVfxLine(
                parent,
                "Builder Construction Range Ring",
                BuildCirclePoints(center, BuilderConstructionRange, 72),
                new Color(tint.r, tint.g, tint.b, 0.72f),
                0.04f,
                91 + index);
        }

        private static Vector3[] BuildCirclePoints(Vector3 center, float radius, int segments)
        {
            var pointCount = Mathf.Max(12, segments) + 1;
            var points = new Vector3[pointCount];
            for (var i = 0; i < pointCount; i++)
            {
                var angle = (i / (float)(pointCount - 1)) * Mathf.PI * 2f;
                points[i] = center + new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f);
            }

            return points;
        }

        private Sprite GetBuildPlacementMarkerSprite(BuildPlacementKind kind)
        {
            return kind == BuildPlacementKind.ResourceWell ? ResourceWellBuildMarkerSprite : TowerBuildMarkerSprite;
        }

        private void ClearBuildPlacementPreviews()
        {
            for (var i = 0; i < buildPlacementPreviews.Count; i++)
            {
                if (buildPlacementPreviews[i].Root != null)
                {
                    Destroy(buildPlacementPreviews[i].Root);
                }
            }

            buildPlacementPreviews.Clear();
        }

        private bool TryGetBuildSlotAt(BuildPlacementKind kind, Vector3 worldPoint, out int slotIndex)
        {
            slotIndex = -1;
            var positions = GetBuildPlacementPositions(kind);
            var bestDistance = BuildPlacementClickRadius;
            for (var i = 0; i < positions.Length; i++)
            {
                if (!IsBuildSlotAvailable(kind, i))
                {
                    continue;
                }

                var distance = Vector2.Distance(worldPoint, positions[i]);
                if (distance <= bestDistance)
                {
                    bestDistance = distance;
                    slotIndex = i;
                }
            }

            return slotIndex >= 0;
        }

        private bool HasAvailableBuildSlot(BuildPlacementKind kind)
        {
            var positions = GetBuildPlacementPositions(kind);
            for (var i = 0; i < positions.Length; i++)
            {
                if (IsBuildSlotAvailable(kind, i))
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsBuildSlotAvailable(BuildPlacementKind kind, int slotIndex)
        {
            if (kind == BuildPlacementKind.Tower)
            {
                return slotIndex >= 0
                    && slotIndex < playerTowers.Length
                    && slotIndex < enemyTowers.Length
                    && slotIndex < pendingPlayerTowerBuilds.Length
                    && slotIndex < pendingEnemyTowerBuilds.Length
                    && playerTowers[slotIndex] == null
                    && enemyTowers[slotIndex] == null
                    && !pendingPlayerTowerBuilds[slotIndex]
                    && !pendingEnemyTowerBuilds[slotIndex];
            }

            if (kind == BuildPlacementKind.ResourceWell)
            {
                return slotIndex >= 0
                    && slotIndex < playerResourceWells.Length
                    && slotIndex < enemyResourceWells.Length
                    && slotIndex < pendingPlayerResourceWellBuilds.Length
                    && slotIndex < pendingEnemyResourceWellBuilds.Length
                    && playerResourceWells[slotIndex] == null
                    && enemyResourceWells[slotIndex] == null
                    && !pendingPlayerResourceWellBuilds[slotIndex]
                    && !pendingEnemyResourceWellBuilds[slotIndex];
            }

            return false;
        }

        private Vector3[] GetBuildPlacementPositions(BuildPlacementKind kind)
        {
            return GetBuildPlacementPositions(kind, 0);
        }

        private Vector3[] GetBuildPlacementPositions(BuildPlacementKind kind, int team)
        {
            if (kind == BuildPlacementKind.ResourceWell)
            {
                return team == 1 ? enemyResourceWellPositions : playerResourceWellPositions;
            }

            return team == 1 ? enemyTowerPositions : playerTowerPositions;
        }

        private int GetBuildPlacementCost(BuildPlacementKind kind)
        {
            return kind == BuildPlacementKind.ResourceWell
                ? ResourceWellCost
                : currentTowerDefinition != null ? currentTowerDefinition.Cost : 0;
        }

        private string GetBuildPlacementName(BuildPlacementKind kind)
        {
            if (kind == BuildPlacementKind.ResourceWell)
            {
                return "\u8d44\u6e90\u70b9";
            }

            return currentTowerDefinition != null ? currentTowerDefinition.DisplayName : "\u70ae\u5854";
        }

        private void UpdateRoutePlanningInput()
        {
            // 鼠标左键短按用于切换默认线路或下达移动命令，拖拽则进入士兵框选。
            if (IsCancelPressed())
            {
                EndUnitSelectionDrag();
                ClearSelectedPlayerUnits();
                ClearRoutePlanning("\u5df2\u53d6\u6d88\u5f53\u524d\u58eb\u5175\u9009\u62e9\u3002");
                return;
            }

            var pointerPosition = GetPointerScreenPosition();
            if (isSelectingUnits)
            {
                selectionCurrentScreenPosition = pointerPosition;
                if (!IsPrimaryPointerReleased())
                {
                    return;
                }

                var startPosition = selectionStartScreenPosition;
                var endPosition = selectionCurrentScreenPosition;
                EndUnitSelectionDrag();

                if (IsSelectionDrag(startPosition, endPosition))
                {
                    SelectPlayerUnitsInScreenRect(startPosition, endPosition);
                    return;
                }

                var camera = GetGameplayCamera();
                if (IsPointerOverUi() || !IsPointerInsideMapViewport(camera, pointerPosition))
                {
                    return;
                }

                var worldPoint = GetWorldPointFromScreen(camera, pointerPosition);
                if (selectedPlayerUnits.Count > 0)
                {
                    CommandSelectedPlayerUnitsTo(worldPoint);
                }
                else if (TrySelectSinglePlayerUnitAt(worldPoint))
                {
                    return;
                }
                else
                {
                    SpawnClickPointMarker(worldPoint);
                    SelectDefaultSpawnLaneAt(worldPoint);
                }

                return;
            }

            if (!IsPrimaryPointerPressed()
                || IsPointerOverUi()
                || !IsPointerInsideMapViewport(GetGameplayCamera(), pointerPosition))
            {
                return;
            }

            selectionStartScreenPosition = pointerPosition;
            selectionCurrentScreenPosition = pointerPosition;
            isSelectingUnits = true;
        }

        private void EndUnitSelectionDrag()
        {
            isSelectingUnits = false;
            selectionStartScreenPosition = Vector3.zero;
            selectionCurrentScreenPosition = Vector3.zero;
        }

        private static bool IsSelectionDrag(Vector3 startPosition, Vector3 endPosition)
        {
            return (startPosition - endPosition).sqrMagnitude >= UnitSelectionDragThreshold * UnitSelectionDragThreshold;
        }

        private void SelectPlayerUnitsInScreenRect(Vector3 startPosition, Vector3 endPosition)
        {
            ClearSelectedPlayerUnits();
            var camera = GetGameplayCamera();
            if (camera == null)
            {
                return;
            }

            var selectionRect = GetScreenSelectionRect(startPosition, endPosition);
            for (var i = 0; i < units.Count; i++)
            {
                var unit = units[i];
                if (unit == null || !unit.IsAlive || unit.Team != 0)
                {
                    continue;
                }

                var screenPosition = camera.WorldToScreenPoint(unit.transform.position);
                if (screenPosition.z < 0f || !selectionRect.Contains(new Vector2(screenPosition.x, screenPosition.y)))
                {
                    continue;
                }

                selectedPlayerUnits.Add(unit);
                unit.SetSelectionVisible(true);
            }

            status = selectedPlayerUnits.Count > 0
                ? "\u5df2\u9009\u4e2d " + selectedPlayerUnits.Count + " \u540d\u58eb\u5175\uff0c\u70b9\u51fb\u9053\u8def\u4e0b\u8fbe\u884c\u519b\u76ee\u6807\uff0c\u53f3\u952e\u89e3\u9664\u7ba1\u63a7\u3002"
                : "\u6846\u9009\u8303\u56f4\u5185\u6ca1\u6709\u53ef\u6307\u6325\u7684\u58eb\u5175\u3002";
            SyncBuildPlacementWithSelectedBuilder();
        }

        private bool TrySelectSinglePlayerUnitAt(Vector3 worldPoint)
        {
            var unit = FindPlayerUnitAt(worldPoint);
            if (unit == null)
            {
                return false;
            }

            ClearSelectedPlayerUnits();
            selectedPlayerUnits.Add(unit);
            unit.SetSelectionVisible(true);
            status = unit.IsBuilder
                ? "\u5df2\u9009\u4e2d\u5efa\u7b51\u5175\uff0c\u9009\u62e9\u8981\u4fee\u5efa\u7684\u8bbe\u65bd\uff0c\u518d\u70b9\u51fb\u53ef\u5efa\u9020\u4f4d\u7f6e\u3002"
                : "\u5df2\u9009\u4e2d 1 \u540d\u58eb\u5175\uff0c\u70b9\u51fb\u9053\u8def\u4e0b\u8fbe\u884c\u519b\u76ee\u6807\uff0c\u53f3\u952e\u89e3\u9664\u7ba1\u63a7\u3002";
            SyncBuildPlacementWithSelectedBuilder();
            return true;
        }

        private bool TrySelectPlayerBuilderAt(Vector3 worldPoint)
        {
            var unit = FindPlayerUnitAt(worldPoint);
            if (unit == null || !unit.IsBuilder)
            {
                return false;
            }

            ClearSelectedPlayerUnits();
            selectedPlayerUnits.Add(unit);
            unit.SetSelectionVisible(true);
            return true;
        }

        private BattleUnit FindPlayerUnitAt(Vector3 worldPoint)
        {
            BattleUnit bestUnit = null;
            var bestDistanceSqr = UnitClickSelectionRadius * UnitClickSelectionRadius;
            for (var i = 0; i < units.Count; i++)
            {
                var unit = units[i];
                if (unit == null || !unit.IsAlive || unit.Team != 0)
                {
                    continue;
                }

                var unitPosition = unit.transform.position;
                var offsetX = worldPoint.x - unitPosition.x;
                var offsetY = worldPoint.y - unitPosition.y;
                var distanceSqr = offsetX * offsetX + offsetY * offsetY;
                if (distanceSqr > bestDistanceSqr)
                {
                    continue;
                }

                bestDistanceSqr = distanceSqr;
                bestUnit = unit;
            }

            return bestUnit;
        }

        private void SelectDefaultSpawnLaneAt(Vector3 worldPoint)
        {
            if (!TrySelectAuthoredLaneRoute(worldPoint, out var laneIndex))
            {
                status = "\u8bf7\u70b9\u51fb\u4e0a\u8def\u3001\u4e2d\u8def\u6216\u4e0b\u8def\u9053\u8def\u9644\u8fd1\uff0c\u5207\u6362\u4e4b\u540e\u7684\u9ed8\u8ba4\u51fa\u5175\u8def\u7ebf\u3002";
                return;
            }

            selectedLane = Mathf.Clamp(laneIndex, 0, laneNames.Length - 1);
            status = "\u9ed8\u8ba4\u51fa\u5175\u8def\u7ebf\u5df2\u5207\u6362\u4e3a" + laneNames[selectedLane] + "\uff0c\u4e4b\u540e\u65b0\u51fa\u7684\u58eb\u5175\u4f1a\u8d70\u8fd9\u6761\u8def\u3002";
        }

        private void CommandSelectedPlayerUnitsTo(Vector3 worldPoint)
        {
            PruneSelectedPlayerUnits();
            if (selectedPlayerUnits.Count == 0)
            {
                status = "\u8bf7\u5148\u70b9\u51fb\u6216\u62d6\u62fd\u6846\u9009\u58eb\u5175\uff0c\u518d\u70b9\u51fb\u9053\u8def\u4e0b\u8fbe\u884c\u519b\u76ee\u6807\u3002";
                return;
            }

            if (!TryFindReachableTarget(worldPoint, out var target))
            {
                status = "\u672a\u627e\u5230\u53ef\u8fbe\u8def\u7ebf\uff0c\u8bf7\u70b9\u51fb\u5730\u56fe\u9053\u8def\u9644\u8fd1\u7684\u4f4d\u7f6e\u3002";
                return;
            }

            SpawnClickPointMarker(target.Position);
            var redirectedCount = RedirectPlayerUnitsToTarget(target, selectedPlayerUnits);
            if (redirectedCount <= 0)
            {
                status = "\u5df2\u9009\u58eb\u5175\u6682\u65f6\u65e0\u6cd5\u5230\u8fbe\u8be5\u76ee\u6807\u3002";
                return;
            }

            status = "\u5df2\u547d\u4ee4 " + redirectedCount + " \u540d\u5df2\u9009\u58eb\u5175\u524d\u5f80\u76ee\u6807\u70b9\uff0c\u53f3\u952e\u53ef\u89e3\u9664\u7ba1\u63a7\u6807\u8bc6\u3002";
        }

        private void ClearSelectedPlayerUnits()
        {
            for (var i = 0; i < selectedPlayerUnits.Count; i++)
            {
                var unit = selectedPlayerUnits[i];
                if (unit != null)
                {
                    unit.SetSelectionVisible(false);
                }
            }

            selectedPlayerUnits.Clear();
            if (activeBuildPlacement != BuildPlacementKind.None)
            {
                ClearBuildPlacementPreviews();
            }
        }

        private void ReleaseSelectedPlayerUnitsControl()
        {
            var releasedCount = selectedPlayerUnits.Count;
            EndUnitSelectionDrag();
            ClearSelectedPlayerUnits();
            status = releasedCount > 0
                ? "\u5df2\u89e3\u9664\u5bf9 " + releasedCount + " \u540d\u58eb\u5175\u7684\u6301\u7eed\u7ba1\u63a7\uff0c\u4ed6\u4eec\u4f1a\u7ee7\u7eed\u524d\u5f80\u4e0a\u4e00\u4e2a\u6307\u5b9a\u4f4d\u7f6e\u3002"
                : "\u5df2\u89e3\u9664\u5f53\u524d\u58eb\u5175\u7ba1\u63a7\u3002";
        }

        private void PruneSelectedPlayerUnits()
        {
            for (var i = selectedPlayerUnits.Count - 1; i >= 0; i--)
            {
                var unit = selectedPlayerUnits[i];
                if (unit != null && unit.IsAlive && unit.Team == 0)
                {
                    continue;
                }

                if (unit != null)
                {
                    unit.SetSelectionVisible(false);
                }

                selectedPlayerUnits.RemoveAt(i);
            }
        }

        private bool HasSelectedPlayerBuilder()
        {
            for (var i = 0; i < selectedPlayerUnits.Count; i++)
            {
                var unit = selectedPlayerUnits[i];
                if (unit != null && unit.IsAlive && unit.Team == 0 && unit.IsBuilder)
                {
                    return true;
                }
            }

            return false;
        }

        private bool TryGetSelectedPlayerBuilderForTask(out BattleUnit builder)
        {
            PruneSelectedPlayerUnits();
            var hasBusyBuilder = false;
            for (var i = 0; i < selectedPlayerUnits.Count; i++)
            {
                var unit = selectedPlayerUnits[i];
                if (unit == null || !unit.IsAlive || unit.Team != 0 || !unit.IsBuilder)
                {
                    continue;
                }

                if (!unit.HasAssignedBuilderTask)
                {
                    builder = unit;
                    return true;
                }

                hasBusyBuilder = true;
            }

            builder = null;
            if (hasBusyBuilder)
            {
                status = "\u5df2\u9009\u4e2d\u7684\u5efa\u7b51\u5175\u6b63\u5728\u65bd\u5de5\uff0c\u8bf7\u9009\u62e9\u4e00\u540d\u7a7a\u95f2\u5efa\u7b51\u5175\u3002";
            }

            return false;
        }

        private void SyncBuildPlacementWithSelectedBuilder()
        {
            if (!HasSelectedPlayerBuilder())
            {
                return;
            }

            if (activeBuildPlacement == BuildPlacementKind.None)
            {
                BeginBuildPlacement(BuildPlacementKind.Tower);
                return;
            }

            if (!HasAvailableBuildSlot(activeBuildPlacement))
            {
                ClearBuildPlacementPreviews();
                status = GetBuildPlacementName(activeBuildPlacement) + "\u6ca1\u6709\u53ef\u7528\u7a7a\u4f4d\u3002";
                return;
            }

            var cost = GetBuildPlacementCost(activeBuildPlacement);
            if (coins < cost)
            {
                ClearBuildPlacementPreviews();
                status = "\u91d1\u5e01\u4e0d\u8db3\uff0c\u8fd8\u4e0d\u80fd\u5efa\u9020" + GetBuildPlacementName(activeBuildPlacement) + "\u3002";
                return;
            }

            ShowBuildPlacementPreviews(activeBuildPlacement);
            status = "\u5df2\u9009\u4e2d\u5efa\u7b51\u5175\uff0c\u70b9\u51fb\u53ef\u5efa\u9020\u4f4d\u7f6e\u8ba9\u4ed6\u524d\u5f80\u65bd\u5de5\u3002";
        }

        private void OnGUI()
        {
            if (!isSelectingUnits || !gameStarted || gameOver)
            {
                return;
            }

            var selectionRect = GetGuiSelectionRect(selectionStartScreenPosition, selectionCurrentScreenPosition);
            if (selectionRect.width < 1f || selectionRect.height < 1f)
            {
                return;
            }

            var previousColor = GUI.color;
            GUI.color = new Color(0.12f, 0.95f, 0.22f, 0.15f);
            GUI.DrawTexture(selectionRect, Texture2D.whiteTexture);
            GUI.color = new Color(0.26f, 1f, 0.28f, 0.88f);
            DrawGuiRectBorder(selectionRect, UnitSelectionBoxBorderThickness);
            GUI.color = previousColor;
        }

        private static Rect GetScreenSelectionRect(Vector3 startPosition, Vector3 endPosition)
        {
            return Rect.MinMaxRect(
                Mathf.Min(startPosition.x, endPosition.x),
                Mathf.Min(startPosition.y, endPosition.y),
                Mathf.Max(startPosition.x, endPosition.x),
                Mathf.Max(startPosition.y, endPosition.y));
        }

        private static Rect GetGuiSelectionRect(Vector3 startPosition, Vector3 endPosition)
        {
            var startGui = new Vector2(startPosition.x, Screen.height - startPosition.y);
            var endGui = new Vector2(endPosition.x, Screen.height - endPosition.y);
            return Rect.MinMaxRect(
                Mathf.Min(startGui.x, endGui.x),
                Mathf.Min(startGui.y, endGui.y),
                Mathf.Max(startGui.x, endGui.x),
                Mathf.Max(startGui.y, endGui.y));
        }

        private static void DrawGuiRectBorder(Rect rect, float thickness)
        {
            GUI.DrawTexture(new Rect(rect.xMin, rect.yMin, rect.width, thickness), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.xMin, rect.yMax - thickness, rect.width, thickness), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.xMin, rect.yMin, thickness, rect.height), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.xMax - thickness, rect.yMin, thickness, rect.height), Texture2D.whiteTexture);
        }

        private Vector3 GetMouseWorldPoint()
        {
            var camera = GetGameplayCamera();
            if (camera == null)
            {
                return Vector3.zero;
            }

            return GetWorldPointFromScreen(camera, GetPointerScreenPosition());
        }

        private static bool IsPrimaryPointerPressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
#else
            return Input.GetMouseButtonDown(0);
#endif
        }

        private static bool IsPrimaryPointerReleased()
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame;
#else
            return Input.GetMouseButtonUp(0);
#endif
        }

        private static bool IsSecondaryPointerHeld()
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current != null && Mouse.current.rightButton.isPressed;
#else
            return Input.GetMouseButton(1);
#endif
        }

        private static bool IsSecondaryPointerPressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame;
#else
            return Input.GetMouseButtonDown(1);
#endif
        }

        private static bool IsCancelPressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.Escape);
#endif
        }

        private static Vector3 GetPointerScreenPosition()
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current != null ? (Vector3)Mouse.current.position.ReadValue() : Vector3.zero;
#else
            return Input.mousePosition;
#endif
        }

        private static float GetScrollDelta()
        {
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current == null)
            {
                return 0f;
            }

            var scrollY = Mouse.current.scroll.ReadValue().y;
            return Mathf.Abs(scrollY) >= 10f ? scrollY / 120f : scrollY;
#else
            return Input.mouseScrollDelta.y;
#endif
        }

        private static bool IsPointerOverUi()
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        }

        private bool TrySelectAuthoredLaneRoute(Vector3 worldPoint, out int laneIndex)
        {
            laneIndex = -1;
            var bestLane = -1;
            var bestDistance = RouteRecoveryRadius;

            for (var candidateLaneIndex = 0; candidateLaneIndex < laneRoutes.Length; candidateLaneIndex++)
            {
                var route = laneRoutes[candidateLaneIndex];
                if (route == null || route.Length < 2)
                {
                    continue;
                }

                var distance = GetDistanceToRoute(worldPoint, route);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestLane = candidateLaneIndex;
                }
            }

            if (bestLane < 0)
            {
                return false;
            }

            laneIndex = bestLane;
            return true;
        }

        private static float GetDistanceToRoute(Vector3 point, Vector3[] route)
        {
            var bestDistance = float.MaxValue;
            for (var i = 0; i < route.Length - 1; i++)
            {
                var projected = ClosestPointOnSegment(point, route[i], route[i + 1]);
                bestDistance = Mathf.Min(bestDistance, Vector2.Distance(point, projected));
            }

            return bestDistance;
        }

        private bool IsRouteEndpointNearBase(Vector3[] route, int baseTeam)
        {
            if (route == null || route.Length == 0)
            {
                return false;
            }

            var endpoint = route[route.Length - 1];
            return Vector2.Distance(endpoint, GetBasePosition(baseTeam)) <= 0.95f;
        }

        private void ClearRoutePlanning(string message)
        {
            status = message;
        }

        private bool TryFindReachableTarget(Vector3 worldPoint, out RouteTarget target)
        {
            return TryFindReachableTarget(worldPoint, RouteReachableRadius, out target);
        }

        private bool TryFindReachableTarget(Vector3 worldPoint, float maxSnapDistance, out RouteTarget target)
        {
            // 把鼠标点吸附到最近的路线边上，后续最短路只在路线图内计算，避免单位走进不可达区域。
            var bestDistance = float.PositiveInfinity;
            var bestA = -1;
            var bestB = -1;
            var bestPoint = Vector3.zero;

            for (var i = 0; i < routeEdges.Length; i++)
            {
                var edge = routeEdges[i];
                var projected = ClosestPointOnSegment(worldPoint, routeNodes[edge.A], routeNodes[edge.B]);
                var distance = Vector2.Distance(worldPoint, projected);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestA = edge.A;
                    bestB = edge.B;
                    bestPoint = projected;
                }
            }

            if (bestA < 0 || bestDistance > maxSnapDistance)
            {
                target = default(RouteTarget);
                return false;
            }

            target = new RouteTarget(bestA, bestB, bestPoint);
            return true;
        }

        private int RedirectPlayerUnitsToTarget(RouteTarget target, List<BattleUnit> candidates)
        {
            var redirectedCount = 0;
            for (var i = 0; i < candidates.Count; i++)
            {
                var unit = candidates[i];
                if (unit == null || !unit.IsAlive || unit.Team != 0)
                {
                    continue;
                }

                if (!TryFindReachableTarget(unit.transform.position, RouteRecoveryRadius, out var start)
                    || !TryBuildShortestRoute(start, target, out var points, out var ignoredCost))
                {
                    continue;
                }

                var route = points.ToArray();
                unit.RedirectToRoute(EstimateLaneIndex(target.Position), route, !IsRouteEndpointNearBase(route, 1));
                redirectedCount++;
            }

            return redirectedCount;
        }

        private bool TryBuildShortestRoute(int startNode, RouteTarget target, out List<Vector3> points, out float totalCost)
        {
            // 简化版 Dijkstra：节点数量很小，直接数组扫描比引入优先队列更清晰。
            var targetNode = routeNodes.Length;
            var nodeCount = targetNode + 1;
            var distance = new float[nodeCount];
            var previous = new int[nodeCount];
            var visited = new bool[nodeCount];

            for (var i = 0; i < nodeCount; i++)
            {
                distance[i] = float.PositiveInfinity;
                previous[i] = -1;
            }

            distance[startNode] = 0f;

            for (var step = 0; step < nodeCount; step++)
            {
                var current = -1;
                var bestDistance = float.PositiveInfinity;
                for (var i = 0; i < nodeCount; i++)
                {
                    if (!visited[i] && distance[i] < bestDistance)
                    {
                        bestDistance = distance[i];
                        current = i;
                    }
                }

                if (current < 0 || current == targetNode)
                {
                    break;
                }

                visited[current] = true;
                RelaxRouteEdges(current, target, targetNode, distance, previous);
            }

            totalCost = distance[targetNode];
            points = new List<Vector3>();
            if (float.IsInfinity(totalCost))
            {
                return false;
            }

            var nodePath = new List<int>();
            for (var node = targetNode; node >= 0; node = previous[node])
            {
                nodePath.Add(node);
                if (node == startNode)
                {
                    break;
                }
            }

            nodePath.Reverse();
            for (var i = 0; i < nodePath.Count; i++)
            {
                points.Add(nodePath[i] == targetNode ? target.Position : routeNodes[nodePath[i]]);
            }

            return points.Count >= 2;
        }

        private bool TryBuildShortestRoute(RouteTarget start, RouteTarget target, out List<Vector3> points, out float totalCost)
        {
            var startNode = routeNodes.Length;
            var targetNode = routeNodes.Length + 1;
            var nodeCount = targetNode + 1;
            var distance = new float[nodeCount];
            var previous = new int[nodeCount];
            var visited = new bool[nodeCount];

            for (var i = 0; i < nodeCount; i++)
            {
                distance[i] = float.PositiveInfinity;
                previous[i] = -1;
            }

            distance[startNode] = 0f;

            for (var step = 0; step < nodeCount; step++)
            {
                var current = -1;
                var bestDistance = float.PositiveInfinity;
                for (var i = 0; i < nodeCount; i++)
                {
                    if (!visited[i] && distance[i] < bestDistance)
                    {
                        bestDistance = distance[i];
                        current = i;
                    }
                }

                if (current < 0 || current == targetNode)
                {
                    break;
                }

                visited[current] = true;
                RelaxRouteEdges(current, target, targetNode, distance, previous);
                RelaxStartRouteEdges(current, start, startNode, distance, previous);
            }

            totalCost = distance[targetNode];
            points = new List<Vector3>();
            if (float.IsInfinity(totalCost))
            {
                return false;
            }

            var nodePath = new List<int>();
            for (var node = targetNode; node >= 0; node = previous[node])
            {
                nodePath.Add(node);
                if (node == startNode)
                {
                    break;
                }
            }

            nodePath.Reverse();
            for (var i = 0; i < nodePath.Count; i++)
            {
                if (nodePath[i] == startNode)
                {
                    points.Add(start.Position);
                }
                else if (nodePath[i] == targetNode)
                {
                    points.Add(target.Position);
                }
                else
                {
                    points.Add(routeNodes[nodePath[i]]);
                }
            }

            return points.Count >= 2;
        }

        private void RelaxRouteEdges(int current, RouteTarget target, int targetNode, float[] distance, int[] previous)
        {
            for (var i = 0; i < routeEdges.Length; i++)
            {
                var edge = routeEdges[i];
                if (edge.A == current)
                {
                    RelaxEdge(edge.A, edge.B, Vector2.Distance(routeNodes[edge.A], routeNodes[edge.B]), distance, previous);
                }
                else if (edge.B == current)
                {
                    RelaxEdge(edge.B, edge.A, Vector2.Distance(routeNodes[edge.A], routeNodes[edge.B]), distance, previous);
                }
            }

            if (current == target.EdgeA)
            {
                RelaxEdge(current, targetNode, Vector2.Distance(routeNodes[target.EdgeA], target.Position), distance, previous);
            }

            if (current == target.EdgeB)
            {
                RelaxEdge(current, targetNode, Vector2.Distance(routeNodes[target.EdgeB], target.Position), distance, previous);
            }
        }

        private void RelaxStartRouteEdges(int current, RouteTarget start, int startNode, float[] distance, int[] previous)
        {
            if (current != startNode)
            {
                return;
            }

            RelaxEdge(startNode, start.EdgeA, Vector2.Distance(start.Position, routeNodes[start.EdgeA]), distance, previous);
            RelaxEdge(startNode, start.EdgeB, Vector2.Distance(start.Position, routeNodes[start.EdgeB]), distance, previous);
        }

        private void RelaxEdge(int from, int to, float cost, float[] distance, int[] previous)
        {
            var nextDistance = distance[from] + cost;
            if (nextDistance >= distance[to])
            {
                return;
            }

            distance[to] = nextDistance;
            previous[to] = from;
        }

        private Material GetVfxLineMaterial()
        {
            if (vfxLineMaterial != null)
            {
                return vfxLineMaterial;
            }

            var shader = Shader.Find("Sprites/Default") ?? Shader.Find("Universal Render Pipeline/Unlit");
            vfxLineMaterial = new Material(shader);
            return vfxLineMaterial;
        }

        private int EstimateLaneIndex(Vector3 position)
        {
            var lane = 0;
            var bestDistance = float.MaxValue;
            for (var i = 0; i < laneRoutes.Length; i++)
            {
                var distance = Mathf.Abs(position.y - GetLaneY(i));
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    lane = i;
                }
            }

            return lane;
        }

        private static Vector3 ClosestPointOnSegment(Vector3 point, Vector3 start, Vector3 end)
        {
            var segment = end - start;
            var lengthSquared = segment.sqrMagnitude;
            if (lengthSquared <= 0.0001f)
            {
                return start;
            }

            var t = Mathf.Clamp01(Vector3.Dot(point - start, segment) / lengthSquared);
            return start + segment * t;
        }

        private static float DistanceToPolyline(Vector3 point, List<Vector3> polyline)
        {
            if (polyline == null || polyline.Count == 0)
            {
                return float.MaxValue;
            }

            var bestDistance = float.MaxValue;
            for (var i = 0; i < polyline.Count - 1; i++)
            {
                var closest = ClosestPointOnSegment(point, polyline[i], polyline[i + 1]);
                bestDistance = Mathf.Min(bestDistance, Vector2.Distance(point, closest));
            }

            return bestDistance;
        }

        private int GetUnitCost(UnitDefinition definition)
        {
            var multiplier = mobilizationTimer > 0f ? MobilizationCostMultiplier : 1f;
            return Mathf.Max(1, Mathf.CeilToInt(definition.Cost * multiplier));
        }

        private float GetPlayerSpeedMultiplier()
        {
            return playerSpeedMultiplier * (mobilizationTimer > 0f ? MobilizationSpeedMultiplier : 1f);
        }

        private int GetCurrentEraThreshold()
        {
            return GetEraThreshold(ageIndex);
        }

        private static int GetEraThreshold(int index)
        {
            if (index >= EraThresholds.Length)
            {
                return 0;
            }

            return EraThresholds[index];
        }

        private static Sprite GetUnitButtonSprite(UnitDefinition definition)
        {
            return definition != null && definition.MoveFrames != null && definition.MoveFrames.Length > 0
                ? definition.MoveFrames[0]
                : null;
        }

        private Sprite GetTowerButtonSprite(int towerIndex)
        {
            var frames = GetTowerFrames(towerIndex);
            return frames != null && frames.Length > 0 ? frames[0] : WhiteSprite;
        }

        private string FormatUnitTooltip(UnitDefinition definition)
        {
            if (definition == null)
            {
                return string.Empty;
            }

            var roleText = definition.Role == UnitRole.Builder ? "\u5efa\u7b51\u5175" : "\u6218\u6597\u58eb\u5175";
            var detail = definition.DisplayName
                + "\n\u7c7b\u578b\uff1a" + roleText
                + "\n\u91d1\u94b1\u6d88\u8017\uff1a" + GetUnitCost(definition) + " \u91d1\u5e01"
                + "\n\u751f\u547d\uff1a" + Mathf.CeilToInt(definition.MaxHealth)
                + "\n\u4f24\u5bb3\uff1a" + FormatStat(definition.Damage)
                + "\n\u5c04\u7a0b\uff1a" + FormatStat(definition.AttackRange)
                + "\n\u653b\u51fb\u95f4\u9694\uff1a" + FormatStat(definition.AttackInterval) + "s"
                + "\n\u79fb\u52a8\u901f\u5ea6\uff1a" + FormatStat(definition.Speed);

            if (definition.Role == UnitRole.Builder)
            {
                detail += "\n\u80fd\u529b\uff1a\u540c\u4e00\u7c7b\u5efa\u7b51\u5175\u53ef\u4fee\u5efa\u9632\u5fa1\u5854\u548c\u8d44\u6e90\u70b9\uff0c\u5e76\u81ea\u52a8\u4fee\u590d\u9644\u8fd1\u53d7\u635f\u8bbe\u65bd\u3002";
            }

            return detail;
        }

        private string FormatTowerTooltip(TowerDefinition definition)
        {
            if (definition == null)
            {
                return string.Empty;
            }

            return definition.DisplayName
                + "\n\u7c7b\u578b\uff1a\u9632\u5fa1\u5854"
                + "\n\u91d1\u94b1\u6d88\u8017\uff1a" + definition.Cost + " \u91d1\u5e01"
                + "\n\u751f\u547d\uff1a" + Mathf.CeilToInt(EstimateTowerMaxHealth(definition))
                + "\n\u4f24\u5bb3\uff1a" + FormatStat(definition.Damage)
                + "\n\u5c04\u7a0b\uff1a" + FormatStat(definition.Range)
                + "\n\u653b\u51fb\u95f4\u9694\uff1a" + FormatStat(definition.AttackInterval) + "s"
                + "\n\u65bd\u5de5\uff1a\u7531\u7edf\u4e00\u5efa\u7b51\u5175\u524d\u5f80\u8bbe\u65bd\u70b9\u4fee\u5efa\u3002";
        }

        private string FormatResourceWellTooltip()
        {
            return "\u8d44\u6e90\u70b9"
                + "\n\u7c7b\u578b\uff1a\u8d44\u6e90\u8bbe\u65bd"
                + "\n\u91d1\u94b1\u6d88\u8017\uff1a" + ResourceWellCost + " \u91d1\u5e01"
                + "\n\u751f\u547d\uff1a360"
                + "\n\u6536\u76ca\uff1a+" + ResourceWellIncomeBonus.ToString("0.#") + " \u91d1\u5e01/s"
                + "\n\u65f6\u4ee3\u503c\uff1a+" + ResourceWellEraValue.ToString("0.#")
                + "\n\u65bd\u5de5\uff1a\u7531\u7edf\u4e00\u5efa\u7b51\u5175\u524d\u5f80\u8bbe\u65bd\u70b9\u4fee\u5efa\u3002";
        }

        private string FormatAgePowerTooltip()
        {
            var power = AgePowers[Mathf.Clamp(ageIndex, 0, AgePowers.Length - 1)];
            var detail = power.DisplayName
                + "\n\u7c7b\u578b\uff1a\u65f6\u4ee3\u5927\u62db"
                + "\n\u4f5c\u7528\u8303\u56f4\uff1a" + (power.IsGlobal ? "\u5168\u573a\u654c\u519b" : "\u5f53\u524d\u8def\u7ebf\u654c\u519b")
                + "\n\u4f24\u5bb3\uff1a" + FormatStat(power.Damage)
                + "\n\u51b7\u5374\uff1a" + FormatStat(power.Cooldown) + "s";

            if (power.StatusDuration > 0f)
            {
                detail += "\n\u72b6\u6001\u6301\u7eed\uff1a" + FormatStat(power.StatusDuration) + "s";
                if (power.SpeedMultiplier < 1f)
                {
                    detail += "\n\u79fb\u52a8\u901f\u5ea6\uff1a-" + Mathf.RoundToInt((1f - power.SpeedMultiplier) * 100f) + "%";
                }

                if (power.AttackIntervalMultiplier > 1f)
                {
                    detail += "\n\u653b\u51fb\u95f4\u9694\uff1a+" + Mathf.RoundToInt((power.AttackIntervalMultiplier - 1f) * 100f) + "%";
                }
            }

            return detail + FormatCooldownState(agePowerCooldown);
        }

        private string FormatShieldBarrierTooltip()
        {
            var detail = "\u62a4\u76fe\u5c4f\u969c"
                + "\n\u7c7b\u578b\uff1a\u9632\u5fa1\u6280\u80fd"
                + "\n\u5438\u6536\uff1a" + ShieldAbsorbByAge[Mathf.Clamp(ageIndex, 0, ShieldAbsorbByAge.Length - 1)] + " \u70b9\u4f24\u5bb3"
                + "\n\u6301\u7eed\uff1a" + FormatStat(ShieldBarrierDurationSeconds) + "s"
                + "\n\u51b7\u5374\uff1a" + FormatStat(ShieldBarrierCooldownSeconds) + "s"
                + "\n\u6548\u679c\uff1a\u4e3a\u6211\u65b9\u57fa\u5730\u62b5\u6d88\u5373\u5c06\u5230\u6765\u7684\u4f24\u5bb3\u3002";

            if (shieldTimer > 0f)
            {
                detail += "\n\u5f53\u524d\u62a4\u76fe\uff1a" + Mathf.CeilToInt(playerShield)
                    + "\n\u5269\u4f59\u6301\u7eed\uff1a" + Mathf.CeilToInt(shieldTimer) + "s";
            }

            return detail + FormatCooldownState(shieldCooldown);
        }

        private string FormatMobilizationTooltip()
        {
            var detail = "\u6218\u4e89\u52a8\u5458"
                + "\n\u7c7b\u578b\uff1a\u652f\u63f4\u6280\u80fd"
                + "\n\u6301\u7eed\uff1a" + FormatStat(MobilizationDurationSeconds) + "s"
                + "\n\u51b7\u5374\uff1a" + FormatStat(MobilizationCooldownSeconds) + "s"
                + "\n\u51fa\u5175\u4ef7\u683c\uff1a-" + Mathf.RoundToInt((1f - MobilizationCostMultiplier) * 100f) + "%"
                + "\n\u65b0\u5175\u901f\u5ea6\uff1a+" + Mathf.RoundToInt((MobilizationSpeedMultiplier - 1f) * 100f) + "%"
                + "\n\u6548\u679c\uff1a\u77ed\u65f6\u95f4\u538b\u4f4e\u51fa\u5175\u6210\u672c\uff0c\u8ba9\u65b0\u90e8\u961f\u66f4\u5feb\u63a8\u8fdb\u3002";

            if (mobilizationTimer > 0f)
            {
                detail += "\n\u5269\u4f59\u6301\u7eed\uff1a" + Mathf.CeilToInt(mobilizationTimer) + "s";
            }

            return detail + FormatCooldownState(mobilizationCooldown);
        }

        private string FormatEvolutionTooltip(EvolutionPath path)
        {
            var isAttack = path == EvolutionPath.Attack;
            var title = isAttack ? "\u8fdb\u653b\u8fdb\u5316" : "\u9632\u5b88\u8fdb\u5316";
            var detail = title + "\n\u7c7b\u578b\uff1a\u65f6\u4ee3\u8fdb\u5316";

            if (ageIndex >= AgeNames.Length - 1)
            {
                return detail
                    + "\n\u5f53\u524d\u65f6\u4ee3\uff1a" + AgeNames[ageIndex]
                    + "\n\u5f53\u524d\u8def\u7ebf\uff1a" + GetEvolutionPathLabel()
                    + "\n\u72b6\u6001\uff1a\u5df2\u5230\u8fbe\u6700\u9ad8\u65f6\u4ee3";
            }

            var threshold = GetCurrentEraThreshold();
            var pathName = isAttack ? AttackPathNames[ageIndex] : DefensePathNames[ageIndex];
            detail += "\n\u8def\u7ebf\uff1a" + pathName
                + "\n\u8fdb\u5165\uff1a" + AgeNames[ageIndex + 1]
                + "\n\u65f6\u4ee3\u503c\uff1a" + Mathf.FloorToInt(eraValue) + "/" + threshold
                + "\n\u72b6\u6001\uff1a" + (eraValue >= threshold ? "\u53ef\u8fdb\u5316" : "\u8fd8\u9700\u8981 " + Mathf.CeilToInt(threshold - eraValue));

            return detail + (isAttack
                ? "\n\u6536\u76ca\uff1a\u63d0\u5347\u6211\u65b9\u4f24\u5bb3 18%\uff0c\u65b0\u5175\u901f\u5ea6 8%\u3002"
                : "\n\u6536\u76ca\uff1a\u63d0\u5347\u65b0\u5175\u751f\u547d 16%\uff0c\u9632\u5fa1\u5854\u4f24\u5bb3 12%\uff0c\u57fa\u5730\u51cf\u4f24 8%\u3002");
        }

        private string FormatRestartTooltip()
        {
            return "\u91cd\u7f6e\u6218\u6597"
                + "\n\u7c7b\u578b\uff1a\u7cfb\u7edf\u6307\u4ee4"
                + "\n\u6548\u679c\uff1a\u91cd\u65b0\u8f7d\u5165\u6218\u6597\u573a\u666f\uff0c\u6e05\u7a7a\u672c\u5c40\u8fdb\u5ea6\u3002";
        }

        private string FormatCooldownState(float cooldown)
        {
            if (!gameStarted)
            {
                return "\n\u72b6\u6001\uff1a\u6218\u6597\u5f00\u59cb\u540e\u53ef\u7528";
            }

            if (gameOver)
            {
                return "\n\u72b6\u6001\uff1a\u6218\u6597\u5df2\u7ed3\u675f";
            }

            return cooldown > 0f
                ? "\n\u5269\u4f59\u51b7\u5374\uff1a" + Mathf.CeilToInt(cooldown) + "s"
                : "\n\u72b6\u6001\uff1a\u53ef\u4f7f\u7528";
        }

        private static float EstimateTowerMaxHealth(TowerDefinition definition)
        {
            return definition == null ? 260f : 220f + definition.Damage * 6f + definition.Range * 20f;
        }

        private static string FormatStat(float value)
        {
            return value.ToString(value >= 10f ? "0" : "0.##");
        }

        private string GetEvolutionPathLabel()
        {
            switch (evolutionPath)
            {
                case EvolutionPath.Attack:
                    return "\u8fdb\u653b";
                case EvolutionPath.Defense:
                    return "\u9632\u5b88";
                default:
                    return "\u5747\u8861";
            }
        }

        private void UpdateUnitButtonDefinitions()
        {
            for (var i = 0; i < unitButtons.Count && i < playerUnitDefinitions.Length; i++)
            {
                var binding = unitButtons[i];
                binding.Definition = playerUnitDefinitions[i];
                binding.Button.name = binding.Definition.Key;
                SetButtonIcon(binding.Button, GetUnitButtonSprite(binding.Definition));
            }

            for (var i = 0; i < towerButtons.Count && i < currentTowerDefinitions.Length; i++)
            {
                var binding = towerButtons[i];
                binding.Definition = currentTowerDefinitions[i];
                binding.Button.name = "Tower " + (i + 1);
                SetButtonIcon(binding.Button, GetTowerButtonSprite(i));
            }

            SetButtonIcon(agePowerButton, GetAgePowerButtonSprite(ageIndex));
        }

        private void SetButtonLabel(Button button, string label)
        {
            if (button == null)
            {
                return;
            }

            var text = GetButtonLabel(button);
            if (text != null)
            {
                text.text = label;
            }
        }

        private void TrySpawnPlayerUnit(UnitDefinition definition)
        {
            var cost = GetUnitCost(definition);
            if (gameOver || coins < cost)
            {
                return;
            }

            coins -= cost;
            SpawnUnit(definition, 0, selectedLane, playerHealthMultiplier, playerDamageMultiplier, GetPlayerSpeedMultiplier());
            GainEraValue(definition.Cost * 0.45f);
            status = definition.DisplayName + "\u5df2\u6d3e\u5f80" + laneNames[selectedLane] + "\u3002";
        }

        private void TryBuildTower()
        {
            if (currentTowerDefinition == null)
            {
                return;
            }

            BeginBuildPlacement(BuildPlacementKind.Tower);
        }

        private void SelectTowerForBuild(int towerIndex)
        {
            if (currentTowerDefinitions == null || currentTowerDefinitions.Length == 0)
            {
                return;
            }

            selectedTowerIndex = Mathf.Clamp(towerIndex, 0, Mathf.Max(0, currentTowerDefinitions.Length - 1));
            currentTowerDefinition = GetTowerDefinition(selectedTowerIndex);
            towerFrames = GetTowerFrames(selectedTowerIndex);
            TryBuildTower();
        }

        private void TryBuildResourceWell()
        {
            BeginBuildPlacement(BuildPlacementKind.ResourceWell);
        }

        private bool DispatchBuilderToBuild(BuildPlacementKind kind, int slotIndex)
        {
            if (!IsBuildSlotAvailable(kind, slotIndex))
            {
                status = GetBuildPlacementName(kind) + "\u8be5\u70b9\u4f4d\u5df2\u88ab\u5360\u7528\u6216\u6b63\u5728\u65bd\u5de5\u3002";
                return false;
            }

            if (!TryGetSelectedPlayerBuilderForTask(out var builder))
            {
                if (string.IsNullOrEmpty(status) || status.IndexOf("\u5efa\u7b51\u5175", StringComparison.Ordinal) < 0)
                {
                    status = "\u8bf7\u5148\u70b9\u51fb\u6216\u6846\u9009\u4e00\u540d\u573a\u4e0a\u5df2\u6709\u7684\u7a7a\u95f2\u5efa\u7b51\u5175\u3002";
                }

                return false;
            }

            var cost = GetBuildPlacementCost(kind);
            if (coins < cost)
            {
                status = "\u91d1\u5e01\u4e0d\u8db3\uff0c\u65e0\u6cd5\u8ba9\u5efa\u7b51\u5175\u4fee\u5efa" + GetBuildPlacementName(kind) + "\u3002";
                return false;
            }

            var targetPosition = GetBuildPlacementPositions(kind)[slotIndex];
            if (!TryBuildBuilderRoute(builder, targetPosition, out var route, out var laneIndex))
            {
                status = builder.Definition.DisplayName + "\u6682\u65f6\u627e\u4e0d\u5230\u901a\u5f80\u8be5\u8bbe\u65bd\u70b9\u7684\u8def\u7ebf\u3002";
                return false;
            }

            var taskKind = ToBuilderTaskKind(kind);
            var towerTypeIndex = kind == BuildPlacementKind.Tower ? selectedTowerIndex : -1;
            MarkBuilderTaskPending(taskKind, slotIndex, towerTypeIndex, 0);
            coins -= cost;
            builder.RedirectToRoute(laneIndex, route, true);
            builder.AssignBuilderTask(taskKind, slotIndex, towerTypeIndex);
            builder.SetSelectionVisible(false);
            selectedPlayerUnits.Remove(builder);
            selectedLane = laneIndex;
            status = "\u5df2\u547d\u4ee4" + builder.Definition.DisplayName + "\u524d\u5f80" + GetFacilitySlotName(slotIndex) + "\u4fee\u5efa" + GetBuildPlacementName(kind) + "\uff0c\u62b5\u8fbe\u8303\u56f4\u5185\u624d\u4f1a\u5f00\u5de5\u3002";
            return true;
        }

        private bool TryBuildBuilderRoute(BattleUnit builder, Vector3 targetPosition, out Vector3[] route, out int laneIndex)
        {
            // 建筑兵从当前位置吸附到路线网，再前往目标设施点附近施工。
            route = null;
            laneIndex = EstimateLaneIndex(targetPosition);

            if (builder == null || !builder.IsAlive)
            {
                return false;
            }

            if (!TryFindReachableTarget(builder.transform.position, RouteRecoveryRadius, out var startTarget)
                || !TryFindReachableTarget(targetPosition, BuilderConstructionRange, out var routeTarget)
                || !TryBuildShortestRoute(startTarget, routeTarget, out var points, out var ignoredCost)
                || points == null
                || points.Count < 2)
            {
                return false;
            }

            route = points.ToArray();
            return true;
        }

        private void MarkBuilderTaskPending(BuilderTaskKind kind, int slotIndex, int towerTypeIndex, int team)
        {
            if (team != 0 && team != 1)
            {
                return;
            }

            var pendingTowerBuilds = team == 0 ? pendingPlayerTowerBuilds : pendingEnemyTowerBuilds;
            var pendingTowerTypeIndexes = team == 0 ? pendingPlayerTowerTypeIndexes : pendingEnemyTowerTypeIndexes;
            var pendingResourceWellBuilds = team == 0 ? pendingPlayerResourceWellBuilds : pendingEnemyResourceWellBuilds;

            if (kind == BuilderTaskKind.Tower && slotIndex >= 0 && slotIndex < pendingTowerBuilds.Length)
            {
                pendingTowerBuilds[slotIndex] = true;
                if (slotIndex < pendingTowerTypeIndexes.Length)
                {
                    pendingTowerTypeIndexes[slotIndex] = Mathf.Max(0, towerTypeIndex);
                }
            }
            else if (kind == BuilderTaskKind.ResourceWell && slotIndex >= 0 && slotIndex < pendingResourceWellBuilds.Length)
            {
                pendingResourceWellBuilds[slotIndex] = true;
            }
        }

        private static BuilderTaskKind ToBuilderTaskKind(BuildPlacementKind kind)
        {
            return kind == BuildPlacementKind.ResourceWell ? BuilderTaskKind.ResourceWell : BuilderTaskKind.Tower;
        }

        private void BuildTowerAt(int slotIndex, int towerTypeIndex = -1)
        {
            var resolvedTowerTypeIndex = towerTypeIndex >= 0 ? towerTypeIndex : selectedTowerIndex;
            var towerDefinition = GetTowerDefinition(resolvedTowerTypeIndex);
            var frames = GetTowerFrames(resolvedTowerTypeIndex);

            if (towerDefinition == null
                || slotIndex < 0
                || slotIndex >= playerTowers.Length
                || slotIndex >= enemyTowers.Length
                || slotIndex >= pendingEnemyTowerBuilds.Length
                || pendingEnemyTowerBuilds[slotIndex]
                || playerTowers[slotIndex] != null
                || enemyTowers[slotIndex] != null)
            {
                return;
            }

            var position = GetPlayerTowerPosition(slotIndex);
            var laneIndex = EstimateLaneIndex(position);
            var towerObject = new GameObject(towerDefinition.DisplayName + " - " + GetFacilitySlotName(slotIndex));
            towerObject.transform.SetParent(worldRoot, false);
            towerObject.transform.position = position;

            var tower = towerObject.AddComponent<BattleTower>();
            tower.Configure(this, laneIndex, 0, slotIndex, resolvedTowerTypeIndex, towerDefinition, frames);
            playerTowers[slotIndex] = tower;
            selectedLane = laneIndex;
            GainEraValue(towerDefinition.Cost * 0.32f);
            status = "\u5efa\u7b51\u5175\u5df2\u5728" + GetFacilitySlotName(slotIndex) + "\u5b8c\u6210" + towerDefinition.DisplayName + "\u3002";
        }

        private void BuildResourceWellAt(int slotIndex)
        {
            if (slotIndex < 0
                || slotIndex >= playerResourceWells.Length
                || slotIndex >= enemyResourceWells.Length
                || slotIndex >= pendingEnemyResourceWellBuilds.Length
                || pendingEnemyResourceWellBuilds[slotIndex]
                || playerResourceWells[slotIndex] != null
                || enemyResourceWells[slotIndex] != null)
            {
                return;
            }

            incomePerSecond += ResourceWellIncomeBonus;
            GainEraValue(ResourceWellEraValue);

            var wellObject = new GameObject("Player Resource Point " + (slotIndex + 1));
            wellObject.transform.SetParent(facilityMarkerRoot, false);
            wellObject.transform.position = playerResourceWellPositions[slotIndex];
            var well = wellObject.AddComponent<BattleResourceWell>();
            well.Configure(this, slotIndex, 0, ResourceWellBuiltSprite, resourceWellVisualScale);
            playerResourceWells[slotIndex] = well;
            status = "\u5efa\u7b51\u5175\u5df2\u5360\u9886\u8d44\u6e90\u70b9\uff0c\u91d1\u5e01\u4ea7\u51fa +" + ResourceWellIncomeBonus.ToString("0.#") + "/s\u3002";
        }

        private string GetFacilitySlotName(int slotIndex)
        {
            return "\u8bbe\u65bd\u70b9 " + (slotIndex + 1);
        }

        private int CountBuiltResourceWells(BattleResourceWell[] resourceWells)
        {
            var count = 0;
            for (var i = 0; i < resourceWells.Length; i++)
            {
                if (resourceWells[i] != null && resourceWells[i].IsAlive)
                {
                    count++;
                }
            }

            return count;
        }

        private void UpdateEnemyFacilities()
        {
            if (elapsedTime >= 28f)
            {
                DispatchEnemyBuilderToBuild(BuildPlacementKind.ResourceWell, 0);
            }

            if (elapsedTime >= 55f)
            {
                DispatchEnemyBuilderToBuild(BuildPlacementKind.Tower, 5);
            }

            if (elapsedTime >= 82f)
            {
                DispatchEnemyBuilderToBuild(BuildPlacementKind.Tower, 3);
            }

            if (elapsedTime >= 115f)
            {
                DispatchEnemyBuilderToBuild(BuildPlacementKind.Tower, 10);
            }

            if (elapsedTime >= 150f)
            {
                DispatchEnemyBuilderToBuild(BuildPlacementKind.Tower, 1);
            }
        }

        private bool DispatchEnemyBuilderToBuild(BuildPlacementKind kind, int slotIndex)
        {
            if (!IsBuildSlotAvailable(kind, slotIndex))
            {
                return false;
            }

            var targetPosition = GetBuildPlacementPositions(kind, 1)[slotIndex];
            var builder = FindIdleEnemyBuilderForTask(targetPosition);
            if (builder == null)
            {
                return false;
            }

            if (!TryBuildBuilderRoute(builder, targetPosition, out var route, out var laneIndex))
            {
                return false;
            }

            var taskKind = ToBuilderTaskKind(kind);
            var towerTypeIndex = kind == BuildPlacementKind.Tower ? GetEnemyTowerTypeIndex(slotIndex) : -1;
            MarkBuilderTaskPending(taskKind, slotIndex, towerTypeIndex, 1);
            builder.RedirectToRoute(laneIndex, route, true);
            builder.AssignBuilderTask(taskKind, slotIndex, towerTypeIndex);
            status = "\u654c\u65b9\u5efa\u7b51\u5175\u6b63\u524d\u5f80" + GetFacilitySlotName(slotIndex) + "\u4fee\u5efa" + GetBuildPlacementNameForEnemy(kind, towerTypeIndex) + "\u3002";
            return true;
        }

        private BattleUnit FindIdleEnemyBuilderForTask(Vector3 targetPosition)
        {
            BattleUnit best = null;
            var bestDistance = float.PositiveInfinity;
            for (var i = 0; i < units.Count; i++)
            {
                var unit = units[i];
                if (unit == null || !unit.IsAlive || unit.Team != 1 || !unit.IsBuilder || unit.HasAssignedBuilderTask)
                {
                    continue;
                }

                var distance = Vector2.Distance(unit.transform.position, targetPosition);
                if (distance >= bestDistance)
                {
                    continue;
                }

                best = unit;
                bestDistance = distance;
            }

            return best;
        }

        private string GetBuildPlacementNameForEnemy(BuildPlacementKind kind, int towerTypeIndex)
        {
            if (kind == BuildPlacementKind.ResourceWell)
            {
                return "\u8d44\u6e90\u70b9";
            }

            var towerDefinition = GetEnemyTowerDefinition(towerTypeIndex);
            return towerDefinition != null ? towerDefinition.DisplayName : "\u70ae\u5854";
        }

        private int GetEnemyTowerTypeIndex(int slotIndex)
        {
            if (enemyTowerDefinitions == null || enemyTowerDefinitions.Length == 0)
            {
                return 0;
            }

            if (slotIndex == 1)
            {
                return 0;
            }

            return Mathf.Clamp(slotIndex, 0, enemyTowerDefinitions.Length - 1);
        }

        private void BuildEnemyTowerAt(int slotIndex, int towerTypeIndex = -1)
        {
            if (enemyTowerDefinitions == null
                || enemyTowerDefinitions.Length == 0
                || slotIndex < 0
                || slotIndex >= enemyTowers.Length
                || slotIndex >= playerTowers.Length
                || slotIndex >= pendingPlayerTowerBuilds.Length
                || slotIndex >= pendingEnemyTowerBuilds.Length
                || pendingPlayerTowerBuilds[slotIndex]
                || pendingEnemyTowerBuilds[slotIndex]
                || enemyTowers[slotIndex] != null
                || playerTowers[slotIndex] != null)
            {
                return;
            }

            var resolvedTowerTypeIndex = towerTypeIndex >= 0 ? towerTypeIndex : GetEnemyTowerTypeIndex(slotIndex);
            var towerDefinition = GetEnemyTowerDefinition(resolvedTowerTypeIndex);
            var frames = GetEnemyTowerFrames(resolvedTowerTypeIndex);
            if (towerDefinition == null)
            {
                return;
            }

            var position = GetEnemyTowerPosition(slotIndex);
            var laneIndex = EstimateLaneIndex(position);
            var towerObject = new GameObject("Enemy " + towerDefinition.DisplayName + " - " + GetFacilitySlotName(slotIndex));
            towerObject.transform.SetParent(worldRoot, false);
            towerObject.transform.position = position;

            var tower = towerObject.AddComponent<BattleTower>();
            tower.Configure(this, laneIndex, 1, slotIndex, resolvedTowerTypeIndex, towerDefinition, frames);
            enemyTowers[slotIndex] = tower;
            status = "\u654c\u65b9\u5728" + GetFacilitySlotName(slotIndex) + "\u5efa\u8d77\u4e86" + towerDefinition.DisplayName + "\u3002";
            GainEnemyEraValue(towerDefinition.Cost * EnemyFacilityEraCostMultiplier);
        }

        private void BuildEnemyResourceWellAt(int slotIndex)
        {
            if (slotIndex < 0
                || slotIndex >= enemyResourceWells.Length
                || slotIndex >= playerResourceWells.Length
                || slotIndex >= pendingPlayerResourceWellBuilds.Length
                || slotIndex >= pendingEnemyResourceWellBuilds.Length
                || pendingPlayerResourceWellBuilds[slotIndex]
                || pendingEnemyResourceWellBuilds[slotIndex]
                || enemyResourceWells[slotIndex] != null
                || playerResourceWells[slotIndex] != null)
            {
                return;
            }

            var wellObject = new GameObject("Enemy Resource Point " + (slotIndex + 1));
            wellObject.transform.SetParent(facilityMarkerRoot, false);
            wellObject.transform.position = enemyResourceWellPositions[slotIndex];
            var well = wellObject.AddComponent<BattleResourceWell>();
            well.Configure(this, slotIndex, 1, ResourceWellBuiltSprite, resourceWellVisualScale);
            enemyResourceWells[slotIndex] = well;
            status = "\u654c\u65b9\u5360\u9886\u4e86\u8d44\u6e90\u70b9\u3002";
            GainEnemyEraValue(ResourceWellEraValue);
        }

        private BattleUnit SpawnUnit(
            UnitDefinition definition,
            int team,
            int laneIndex,
            float healthMultiplier = 1f,
            float damageMultiplier = 1f,
            float speedMultiplier = 1f,
            Vector3[] customRoute = null,
            bool stopAtRouteEnd = false)
        {
            if (gameOver)
            {
                return null;
            }

            if (definition == null)
            {
                Debug.LogWarning("Skipping unit spawn because the unit definition is missing.");
                return null;
            }

            var unitObject = new GameObject((team == 0 ? "Player " : "Enemy ") + definition.Key);
            unitObject.transform.SetParent(worldRoot, false);
            var spawnPosition = customRoute != null && customRoute.Length > 0
                ? customRoute[0]
                : GetLaneSpawnPosition(team, laneIndex);

            var unit = unitObject.AddComponent<BattleUnit>();
            unit.Configure(
                this,
                definition,
                team,
                laneIndex,
                spawnPosition,
                healthMultiplier,
                damageMultiplier,
                speedMultiplier,
                customRoute,
                stopAtRouteEnd);
            units.Add(unit);
            return unit;
        }

        private void UseAgePower()
        {
            if (gameOver || agePowerCooldown > 0f)
            {
                return;
            }

            var power = AgePowers[ageIndex];
            agePowerCooldown = power.Cooldown;
            var affected = ApplyAgePower(power);
            PlayAgePowerVisual(ageIndex, power);
            status = power.DisplayName + "\u5df2\u91ca\u653e\uff0c\u5f71\u54cd " + affected + " \u540d\u654c\u519b\u3002";
        }

        private int ApplyAgePower(AgePowerDefinition power)
        {
            var affected = 0;
            for (var i = units.Count - 1; i >= 0; i--)
            {
                var unit = units[i];
                if (unit == null || !unit.IsAlive || unit.Team != 1)
                {
                    continue;
                }

                if (!power.IsGlobal && unit.LaneIndex != selectedLane)
                {
                    continue;
                }

                affected++;
                unit.TakeDamage(power.Damage, 0);
                if (unit != null && unit.IsAlive && power.StatusDuration > 0f)
                {
                    unit.ApplyStatusEffect(power.StatusDuration, power.SpeedMultiplier, power.AttackIntervalMultiplier);
                }
            }

            return affected;
        }

        private void PlayAgePowerVisual(int powerIndex, AgePowerDefinition power)
        {
            var root = new GameObject(power.DisplayName + " Visual");
            root.transform.SetParent(worldRoot, false);
            root.AddComponent<BattleTimedDestroy>().Configure(3f);
            var visualTargets = GetGlobalPowerVisualTargets();
            var center = GetPowerVisualCenter(visualTargets);

            switch (powerIndex)
            {
                case 0:
                    CreateEarthquakeVisual(root.transform, visualTargets);
                    break;
                case 1:
                    CreateBombingVisual(root.transform, visualTargets);
                    break;
                case 2:
                    CreateLightningVisual(root.transform, center, visualTargets);
                    break;
                case 3:
                    CreateRadiationVisual(root.transform, visualTargets);
                    break;
                default:
                    CreateTimeSlowVisual(root.transform, visualTargets);
                    break;
            }
        }

        private Vector3 GetPowerVisualCenter(List<Vector3> visualTargets)
        {
            if (visualTargets != null && visualTargets.Count > 0)
            {
                var sum = Vector3.zero;
                for (var i = 0; i < visualTargets.Count; i++)
                {
                    sum += visualTargets[i];
                }

                return sum / visualTargets.Count;
            }

            var route = GetLaneRoute(selectedLane);
            return route[Mathf.Clamp(route.Length / 2, 0, route.Length - 1)];
        }

        private List<Vector3> GetGlobalPowerVisualTargets()
        {
            var targets = new List<Vector3>(6);
            for (var laneIndex = 0; laneIndex < laneRoutes.Length; laneIndex++)
            {
                var route = GetLaneRoute(laneIndex);
                if (route.Length == 0)
                {
                    continue;
                }

                targets.Add(GetRoutePointAtProgress(route, 0.28f));
                targets.Add(GetRoutePointAtProgress(route, 0.72f));
            }

            if (targets.Count == 0)
            {
                targets.Add(Vector3.zero);
            }

            return targets;
        }

        private Vector3 GetRoutePointAtProgress(Vector3[] route, float progress)
        {
            if (route == null || route.Length == 0)
            {
                return Vector3.zero;
            }

            if (route.Length == 1)
            {
                return route[0];
            }

            var totalDistance = 0f;
            for (var i = 0; i < route.Length - 1; i++)
            {
                totalDistance += Vector3.Distance(route[i], route[i + 1]);
            }

            if (totalDistance <= 0f)
            {
                return route[0];
            }

            var targetDistance = totalDistance * Mathf.Clamp01(progress);
            var walkedDistance = 0f;
            for (var i = 0; i < route.Length - 1; i++)
            {
                var segmentDistance = Vector3.Distance(route[i], route[i + 1]);
                if (walkedDistance + segmentDistance >= targetDistance)
                {
                    var segmentProgress = segmentDistance > 0f ? (targetDistance - walkedDistance) / segmentDistance : 0f;
                    return Vector3.Lerp(route[i], route[i + 1], segmentProgress);
                }

                walkedDistance += segmentDistance;
            }

            return route[route.Length - 1];
        }

        private void CreateEarthquakeVisual(Transform root, List<Vector3> targets)
        {
            for (var i = 0; i < targets.Count; i++)
            {
                var ring = CreateVfxDisc(root, "Earthquake Shockwave " + i, targets[i], new Color(1f, 0.73f, 0.28f, 0.38f), 0.9f + (i % 2) * 0.35f, 96 + i);
                var effect = ring.gameObject.AddComponent<BattleVfxFade>();
                effect.Configure(1.05f + i * 0.05f, 2.3f + (i % 2) * 0.45f, 18f);
                CreateCrackLine(root, targets[i], i);
            }
        }

        private void CreateBombingVisual(Transform root, List<Vector3> targets)
        {
            for (var i = 0; i < targets.Count; i++)
            {
                var target = targets[i];
                CreateVfxLine(root, "Bomb Trail " + i, new[] { target + new Vector3(-0.15f, 4.2f, 0f), target }, new Color(1f, 0.92f, 0.45f, 0.95f), 0.08f, 102 + i)
                    .gameObject.AddComponent<BattleVfxFade>().Configure(0.75f, 0f, 0f);

                var blast = CreateVfxDisc(root, "Bomb Blast " + i, target, new Color(1f, 0.36f, 0.12f, 0.58f), 0.55f, 104 + i);
                blast.gameObject.AddComponent<BattleVfxFade>().Configure(0.9f, 2.4f, 0f);
            }
        }

        private void CreateLightningVisual(Transform root, Vector3 center, List<Vector3> targets)
        {
            var flash = CreateVfxDisc(root, "Lightning Field", center, new Color(0.45f, 0.9f, 1f, 0.22f), 3.8f, 95);
            flash.gameObject.AddComponent<BattleVfxFade>().Configure(0.7f, 1.6f, 45f);

            for (var i = 0; i < targets.Count; i++)
            {
                var target = targets[i];
                CreateVfxLine(root, "Lightning Bolt " + i, BuildLightningPoints(target + new Vector3(0f, 4.2f, 0f), target, i), new Color(0.72f, 0.96f, 1f, 1f), 0.075f, 110 + i)
                    .gameObject.AddComponent<BattleVfxFade>().Configure(0.55f, 0f, 0f);
            }
        }

        private void CreateRadiationVisual(Transform root, List<Vector3> targets)
        {
            for (var i = 0; i < targets.Count; i++)
            {
                var zone = CreateVfxDisc(root, "Radiation Zone " + i, targets[i], new Color(0.35f, 1f, 0.24f, 0.24f), 1.25f, 96 + i);
                zone.gameObject.AddComponent<BattleVfxFade>().Configure(2.2f, 0.65f, 12f);

                var ring = CreateVfxDisc(root, "Radiation Pulse " + i, targets[i], new Color(0.72f, 1f, 0.2f, 0.22f), 0.7f + (i % 2) * 0.22f, 104 + i);
                ring.gameObject.AddComponent<BattleVfxFade>().Configure(1.5f + i * 0.08f, 1.35f, -30f);
            }
        }

        private void CreateTimeSlowVisual(Transform root, List<Vector3> targets)
        {
            for (var i = 0; i < targets.Count; i++)
            {
                var ring = CreateVfxDisc(root, "Time Ring " + i, targets[i], new Color(0.5f, 0.68f, 1f, 0.3f), 0.75f, 105 + i);
                ring.gameObject.AddComponent<BattleVfxFade>().Configure(1.7f, 1.4f, 120f);
                CreateVfxLine(root, "Time Hand " + i, new[] { targets[i] + new Vector3(-0.55f, 0f, 0f), targets[i] + new Vector3(0.55f, 0f, 0f) }, new Color(0.82f, 0.9f, 1f, 0.9f), 0.035f, 112 + i)
                    .gameObject.AddComponent<BattleVfxFade>().Configure(1.2f, 0f, 0f);
            }
        }

        private void CreateCrackLine(Transform root, Vector3 center, int index)
        {
            var points = new[]
            {
                center + new Vector3(-0.62f, 0.05f, 0f),
                center + new Vector3(-0.25f, -0.08f, 0f),
                center + new Vector3(0.08f, 0.1f, 0f),
                center + new Vector3(0.52f, -0.02f, 0f)
            };
            CreateVfxLine(root, "Earth Crack " + index, points, new Color(0.28f, 0.12f, 0.04f, 0.95f), 0.055f, 100 + index)
                .gameObject.AddComponent<BattleVfxFade>().Configure(1.1f, 0f, 0f);
        }

        private SpriteRenderer CreateVfxDisc(Transform parent, string name, Vector3 position, Color color, float scale, int sortingOrder)
        {
            var renderer = CreateSprite(name, VfxCircleSprite, position, sortingOrder);
            renderer.transform.SetParent(parent, true);
            renderer.color = color;
            renderer.transform.localScale = Vector3.one * scale;
            return renderer;
        }

        private LineRenderer CreateVfxLine(Transform parent, string name, Vector3[] points, Color color, float width, int sortingOrder)
        {
            var lineObject = new GameObject(name);
            lineObject.transform.SetParent(parent, false);
            var line = lineObject.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.positionCount = points.Length;
            line.SetPositions(points);
            line.material = GetVfxLineMaterial();
            line.startColor = color;
            line.endColor = color;
            line.widthMultiplier = width;
            line.numCornerVertices = 3;
            line.numCapVertices = 3;
            line.sortingOrder = sortingOrder;
            return line;
        }

        private Vector3[] BuildLightningPoints(Vector3 start, Vector3 end, int seed)
        {
            var points = new Vector3[6];
            for (var i = 0; i < points.Length; i++)
            {
                var t = i / (float)(points.Length - 1);
                var point = Vector3.Lerp(start, end, t);
                var offset = Mathf.Sin((seed + 1) * 12.37f + i * 2.1f) * 0.32f;
                points[i] = point + new Vector3(offset, 0f, 0f);
            }

            return points;
        }

        private void UseShieldBarrier()
        {
            if (gameOver || shieldCooldown > 0f)
            {
                return;
            }

            shieldCooldown = ShieldBarrierCooldownSeconds;
            shieldTimer = ShieldBarrierDurationSeconds;
            playerShield = ShieldAbsorbByAge[ageIndex];
            status = "\u62a4\u76fe\u5c4f\u969c\u542f\u52a8\uff0c\u5438\u6536 " + Mathf.CeilToInt(playerShield) + " \u70b9\u4f24\u5bb3\u3002";
        }

        private void UseMobilization()
        {
            if (gameOver || mobilizationCooldown > 0f)
            {
                return;
            }

            mobilizationCooldown = MobilizationCooldownSeconds;
            mobilizationTimer = MobilizationDurationSeconds;
            status = "\u6218\u4e89\u52a8\u5458\u542f\u52a8\uff0c\u77ed\u65f6\u95f4\u964d\u4f4e\u51fa\u5175\u4ef7\u683c\u5e76\u63d0\u5347\u65b0\u5175\u63a8\u8fdb\u901f\u5ea6\u3002";
        }

        private void UpgradeAge(EvolutionPath path)
        {
            if (ageIndex >= AgeNames.Length - 1)
            {
                status = "\u5df2\u7ecf\u5230\u8fbe\u6700\u9ad8\u65f6\u4ee3\u3002";
                return;
            }

            var threshold = GetCurrentEraThreshold();
            if (eraValue < threshold)
            {
                status = "\u65f6\u4ee3\u503c\u4e0d\u8db3\uff0c\u8fd8\u9700\u8981 " + Mathf.CeilToInt(threshold - eraValue) + "\u3002";
                return;
            }

            var oldBaseMaxHealth = playerBaseMaxHealth;
            ageIndex++;
            evolutionPath = path;
            eraValue = 0f;
            incomePerSecond = GameSession.IncomePerSecond + ageIndex * 2f + CountBuiltResourceWells(playerResourceWells) * ResourceWellIncomeBonus;
            playerBaseMaxHealth = BaseHealthByAge[ageIndex];
            var healRatio = path == EvolutionPath.Defense ? 0.75f : 0.55f;
            playerBaseHealth = Mathf.Min(playerBaseMaxHealth, playerBaseHealth + (playerBaseMaxHealth - oldBaseMaxHealth) * healRatio);

            if (path == EvolutionPath.Attack)
            {
                playerDamageMultiplier += 0.18f;
                playerSpeedMultiplier += 0.08f;
            }
            else if (path == EvolutionPath.Defense)
            {
                playerHealthMultiplier += 0.16f;
                towerDamageMultiplier += 0.12f;
                baseDamageReduction = Mathf.Min(0.3f, baseDamageReduction + 0.08f);
            }

            BuildDefinitions();
            RefreshMapVisuals();
            RefreshBaseVisuals();
            RefreshPlayerTowers();
            UpdateUnitButtonDefinitions();
            SwitchEraAmbience(ageIndex, false);
            var pathName = path == EvolutionPath.Attack ? AttackPathNames[ageIndex - 1] : DefensePathNames[ageIndex - 1];
            status = "\u9009\u62e9\u300c" + pathName + "\u300d\uff0c\u8fdb\u5165" + AgeNames[ageIndex] + "\u3002";
        }

        private void RefreshPlayerTowers()
        {
            for (var i = 0; i < playerTowers.Length; i++)
            {
                if (playerTowers[i] != null)
                {
                    var towerTypeIndex = playerTowers[i].TowerTypeIndex;
                    playerTowers[i].RefreshVisuals(GetTowerDefinition(towerTypeIndex), GetTowerFrames(towerTypeIndex));
                }
            }
        }

        private void RefreshEnemyTowers()
        {
            for (var i = 0; i < enemyTowers.Length; i++)
            {
                if (enemyTowers[i] != null)
                {
                    var towerTypeIndex = enemyTowers[i].TowerTypeIndex;
                    enemyTowers[i].RefreshVisuals(GetEnemyTowerDefinition(towerTypeIndex), GetEnemyTowerFrames(towerTypeIndex));
                }
            }
        }

        private void GainEraValue(float amount)
        {
            if (ageIndex >= AgeNames.Length - 1 || amount <= 0f)
            {
                return;
            }

            eraValue = Mathf.Min(GetCurrentEraThreshold(), eraValue + amount);
        }

        private void GainEnemyEraValue(float amount)
        {
            if (enemyAgeIndex >= AgeNames.Length - 1 || amount <= 0f)
            {
                return;
            }

            enemyEraValue += amount;
            while (enemyAgeIndex < AgeNames.Length - 1 && enemyEraValue >= GetEraThreshold(enemyAgeIndex))
            {
                enemyEraValue -= GetEraThreshold(enemyAgeIndex);
                UpgradeEnemyAge();
            }
        }

        private void UpgradeEnemyAge()
        {
            if (enemyAgeIndex >= AgeNames.Length - 1)
            {
                return;
            }

            var oldEnemyBaseMaxHealth = enemyBaseMaxHealth;
            enemyAgeIndex++;
            enemyUnitDefinitions = BuildEnemyDefinitions(BuildPlayerUnitDefinitionsForAge(enemyAgeIndex));
            enemyTowerDefinitions = BuildTowerDefinitionsForAge(enemyAgeIndex);
            enemyTowerFrameSets = BuildTowerFrameSets(enemyAgeIndex, enemyTowerDefinitions.Length);
            enemyBaseMaxHealth = BaseHealthByAge[enemyAgeIndex];
            enemyBaseHealth = Mathf.Min(enemyBaseMaxHealth, enemyBaseHealth + (enemyBaseMaxHealth - oldEnemyBaseMaxHealth) * 0.55f);
            RefreshBaseVisuals();
            RefreshEnemyTowers();
            status = "\u654c\u65b9\u8fdb\u5165" + AgeNames[enemyAgeIndex] + "\uff0c\u5176\u51fa\u5175\u548c\u9632\u5fa1\u5df2\u72ec\u7acb\u5347\u7ea7\u3002";
        }

        private void UpdateTimedEffects()
        {
            agePowerCooldown = Mathf.Max(0f, agePowerCooldown - Time.deltaTime);
            shieldCooldown = Mathf.Max(0f, shieldCooldown - Time.deltaTime);
            mobilizationCooldown = Mathf.Max(0f, mobilizationCooldown - Time.deltaTime);

            if (shieldTimer > 0f)
            {
                shieldTimer = Mathf.Max(0f, shieldTimer - Time.deltaTime);
                if (shieldTimer <= 0f)
                {
                    playerShield = 0f;
                }
            }

            if (mobilizationTimer > 0f)
            {
                mobilizationTimer = Mathf.Max(0f, mobilizationTimer - Time.deltaTime);
            }
        }

        private void UpdateEnemySpawns()
        {
            enemySpawnTimer -= Time.deltaTime;
            if (enemySpawnTimer > 0f)
            {
                return;
            }

            var lane = UnityEngine.Random.Range(0, LaneY.Length);
            var definition = ChooseEnemyDefinition();
            SpawnUnit(definition, 1, lane);
            status = "\u654c\u65b9" + definition.DisplayName + "\u51fa\u73b0\u5728" + laneNames[lane] + "\u3002";
            GainEnemyEraValue(definition.Cost * EnemySpawnEraCostMultiplier);

            var pressure = Mathf.Clamp01(elapsedTime / 150f);
            enemySpawnTimer = Mathf.Lerp(4.7f, 2.35f, pressure) * enemySpawnIntervalScale;
        }

        private UnitDefinition ChooseEnemyDefinition()
        {
            var roll = UnityEngine.Random.value;
            var builderDefinition = GetEnemyBuilderDefinition();
            if (builderDefinition != null && elapsedTime > 18f && (!HasAliveEnemyBuilder() || roll > 0.92f))
            {
                return builderDefinition;
            }

            if (elapsedTime > 130f && roll > 0.88f && enemyUnitDefinitions.Length > 4)
            {
                return enemyUnitDefinitions[4];
            }

            if (elapsedTime > 95f && roll > 0.7f && enemyUnitDefinitions.Length > 3)
            {
                return enemyUnitDefinitions[3];
            }

            if (elapsedTime > 60f && roll > 0.52f && enemyUnitDefinitions.Length > 2)
            {
                return enemyUnitDefinitions[2];
            }

            if (elapsedTime > 28f && roll > 0.35f && enemyUnitDefinitions.Length > 1)
            {
                return enemyUnitDefinitions[1];
            }

            return enemyUnitDefinitions[0];
        }

        private UnitDefinition GetEnemyBuilderDefinition()
        {
            if (enemyUnitDefinitions == null)
            {
                return null;
            }

            for (var i = 0; i < enemyUnitDefinitions.Length; i++)
            {
                if (enemyUnitDefinitions[i] != null && enemyUnitDefinitions[i].Role == UnitRole.Builder)
                {
                    return enemyUnitDefinitions[i];
                }
            }

            return null;
        }

        private bool HasAliveEnemyBuilder()
        {
            for (var i = 0; i < units.Count; i++)
            {
                var unit = units[i];
                if (unit != null && unit.IsAlive && unit.Team == 1 && unit.IsBuilder)
                {
                    return true;
                }
            }

            return false;
        }

        private void RestartBattle()
        {
            SceneManager.LoadScene(BattleSceneName);
        }

        private void ReturnToMainMenu()
        {
            SceneManager.LoadScene(MainMenuSceneName);
        }

        private void EndGame(bool playerWon)
        {
            gameOver = true;
            activeBuildPlacement = BuildPlacementKind.None;
            EndUnitSelectionDrag();
            ClearSelectedPlayerUnits();
            ClearBuildPlacementPreviews();
            status = playerWon
                ? "\u80dc\u5229\uff01\u86ee\u8352\u90e8\u843d\u653b\u7834\u4e86\u654c\u65b9\u57fa\u5730\u3002"
                : "\u5931\u8d25\uff1a\u6211\u65b9\u57fa\u5730\u5df2\u88ab\u653b\u7834\u3002";
            ShowOutcomeOverlay(playerWon);
        }

        private void ShowOutcomeOverlay(bool playerWon)
        {
            if (outcomeOverlay == null)
            {
                return;
            }

            outcomeOverlay.SetAsLastSibling();
            outcomeOverlay.gameObject.SetActive(true);

            if (outcomeBackdropImage != null)
            {
                outcomeBackdropImage.sprite = LoadSprite(playerWon ? VictoryBackdropPath : DefeatBackdropPath, 100f);
                outcomeBackdropImage.color = Color.white;
            }

            var title = playerWon ? "\u80dc\u5229" : "\u5931\u8d25";
            if (outcomeTitleShadowText != null)
            {
                outcomeTitleShadowText.text = title;
            }

            if (outcomeTitleText != null)
            {
                outcomeTitleText.text = title;
                outcomeTitleText.color = playerWon
                    ? new Color(1f, 0.86f, 0.38f, 1f)
                    : new Color(1f, 0.22f, 0.16f, 1f);
            }

            if (outcomeSubtitleText != null)
            {
                outcomeSubtitleText.text = playerWon
                    ? "\u654c\u65b9\u57fa\u5730\u5df2\u88ab\u653b\u7834\uff0c\u6218\u7ebf\u63a8\u8fdb\u6210\u529f"
                    : "\u6211\u65b9\u57fa\u5730\u5df2\u88ab\u653b\u7834\uff0c\u6218\u7ebf\u5168\u9762\u5931\u5b88";
            }

            if (outcomeStatsText != null)
            {
                outcomeStatsText.text = "\u7528\u65f6 " + FormatBattleDuration()
                    + "    \u6211\u65b9 " + AgeNames[ageIndex]
                    + "    \u654c\u65b9 " + AgeNames[enemyAgeIndex]
                    + "    \u91d1\u5e01 " + Mathf.FloorToInt(coins);
            }
        }

        private string FormatBattleDuration()
        {
            var totalSeconds = Mathf.Max(0, Mathf.FloorToInt(elapsedTime));
            return (totalSeconds / 60).ToString("00") + ":" + (totalSeconds % 60).ToString("00");
        }

        private void RefreshHud()
        {
            if (coinText != null)
            {
                coinText.text = "\u91d1\u5e01 " + Mathf.FloorToInt(coins) + "  (+" + incomePerSecond.ToString("0.#") + "/s)";
            }

            if (ageText != null)
            {
                ageText.text = "\u6211\u65b9 " + AgeNames[ageIndex] + "  " + GetEvolutionPathLabel()
                    + "    \u654c\u65b9 " + AgeNames[enemyAgeIndex];
            }

            if (eraText != null)
            {
                eraText.text = ageIndex >= AgeNames.Length - 1
                    ? "\u6211\u65b9\u65f6\u4ee3\u503c \u5df2\u6ee1\u7ea7"
                    : "\u6211\u65b9\u65f6\u4ee3\u503c " + Mathf.FloorToInt(eraValue) + " / " + GetCurrentEraThreshold();
            }

            if (eraFill != null)
            {
                eraFill.fillAmount = ageIndex >= AgeNames.Length - 1 ? 1f : Mathf.Clamp01(eraValue / GetCurrentEraThreshold());
            }

            if (playerHealthText != null)
            {
                var shieldText = playerShield > 0f ? "  \u62a4\u76fe " + Mathf.CeilToInt(playerShield) : string.Empty;
                playerHealthText.text = Mathf.CeilToInt(playerBaseHealth) + " / " + Mathf.CeilToInt(playerBaseMaxHealth) + shieldText;
            }

            if (enemyHealthText != null)
            {
                enemyHealthText.text = AgeNames[enemyAgeIndex] + "  " + Mathf.CeilToInt(enemyBaseHealth) + " / " + Mathf.CeilToInt(enemyBaseMaxHealth);
            }

            if (playerHealthFill != null)
            {
                playerHealthFill.fillAmount = playerBaseHealth / playerBaseMaxHealth;
            }

            if (enemyHealthFill != null)
            {
                enemyHealthFill.fillAmount = enemyBaseHealth / enemyBaseMaxHealth;
            }

            if (laneText != null)
            {
                if (!gameStarted)
                {
                    laneText.text = "\u70b9\u51fb\u5730\u56fe\u4efb\u610f\u4f4d\u7f6e\u5f00\u59cb\u6218\u6597\n\u53f3\u952e\u62d6\u52a8\u89c6\u91ce\n\u9f20\u6807\u9760\u8fd1\u8fb9\u7f18\u6eda\u52a8\u5730\u56fe";
                }
                else if (activeBuildPlacement != BuildPlacementKind.None)
                {
                    laneText.text = HasSelectedPlayerBuilder()
                        ? "\u5df2\u9009\u4e2d\u5efa\u7b51\u5175\n\u70b9\u51fb\u53ef\u5efa\u9020\u4f4d\u7f6e\u4fee\u5efa" + GetBuildPlacementName(activeBuildPlacement) + "\nEsc \u53d6\u6d88\u672c\u6b21\u6d3e\u5de5"
                        : "\u5df2\u9009\u62e9" + GetBuildPlacementName(activeBuildPlacement) + "\n\u70b9\u51fb\u573a\u4e0a\u5df2\u6709\u5efa\u7b51\u5175\n\u4e0d\u4f1a\u81ea\u52a8\u751f\u6210\u5efa\u7b51\u5175";
                }
                else if (selectedPlayerUnits.Count > 0)
                {
                    laneText.text = "\u5df2\u9009\u4e2d " + selectedPlayerUnits.Count + " \u540d\u58eb\u5175\n\u70b9\u51fb\u9053\u8def\u4e0b\u8fbe\u884c\u519b\u76ee\u6807\n\u53f3\u952e\u89e3\u9664\u7ba1\u63a7\u6807\u8bc6";
                }
                else
                {
                    laneText.text = "\u9ed8\u8ba4\u51fa\u5175\u8def\u7ebf\uff1a" + laneNames[selectedLane] + "\n\u70b9\u51fb\u4e0a/\u4e2d/\u4e0b\u8def\u5207\u6362\u4e4b\u540e\u51fa\u5175\n\u70b9\u51fb\u5355\u4e2a\u58eb\u5175\u6216\u62d6\u62fd\u6846\u9009\u540e\u5355\u72ec\u6307\u6325";
                }
            }

            if (statusText != null)
            {
                statusText.text = BuildStatusLogText();
            }

            for (var i = 0; i < laneButtons.Count; i++)
            {
                SetButtonColor(laneButtons[i], i == selectedLane ? new Color(0.36f, 0.28f, 0.1f, 1f) : new Color(0.13f, 0.17f, 0.14f, 1f));
            }

            for (var i = 0; i < unitButtons.Count; i++)
            {
                var binding = unitButtons[i];
                var canSpawn = gameStarted && !gameOver && coins >= GetUnitCost(binding.Definition);
                binding.Button.interactable = canSpawn;
                SetButtonIconAlpha(binding.Icon, canSpawn);
                SetButtonColor(binding.Button, new Color(0.43f, 0.31f, 0.17f, 1f));
            }

            for (var i = 0; i < towerButtons.Count; i++)
            {
                var binding = towerButtons[i];
                var canBuildTower = gameStarted
                    && !gameOver
                    && binding.Definition != null
                    && coins >= binding.Definition.Cost
                    && HasAvailableBuildSlot(BuildPlacementKind.Tower);
                binding.Button.interactable = canBuildTower;
                SetButtonIconAlpha(binding.Icon, canBuildTower);

                var selected = i == selectedTowerIndex;
                SetButtonColor(binding.Button, activeBuildPlacement == BuildPlacementKind.Tower && selected
                    ? new Color(0.78f, 0.16f, 0.1f, 1f)
                    : selected ? new Color(0.4f, 0.54f, 0.5f, 1f) : new Color(0.31f, 0.43f, 0.45f, 1f));
            }

            if (resourceWellButton != null)
            {
                var canBuildResourceWell = gameStarted
                    && !gameOver
                    && coins >= ResourceWellCost
                    && HasAvailableBuildSlot(BuildPlacementKind.ResourceWell);
                resourceWellButton.interactable = canBuildResourceWell;
                SetButtonIconAlpha(GetButtonIcon(resourceWellButton), canBuildResourceWell);
                SetButtonColor(resourceWellButton, activeBuildPlacement == BuildPlacementKind.ResourceWell
                    ? new Color(0.78f, 0.16f, 0.1f, 1f)
                    : new Color(0.24f, 0.46f, 0.36f, 1f));
            }

            SetCooldownButton(agePowerButton, agePowerCooldown, AgePowers[ageIndex].DisplayName, gameStarted && !gameOver);
            SetCooldownButton(shieldButton, shieldCooldown, "\u62a4\u76fe\u5c4f\u969c", gameStarted && !gameOver);
            SetCooldownButton(mobilizationButton, mobilizationCooldown, "\u6218\u4e89\u52a8\u5458", gameStarted && !gameOver);

            var canUpgrade = gameStarted && !gameOver && ageIndex < AgeNames.Length - 1 && eraValue >= GetCurrentEraThreshold();
            if (attackUpgradeButton != null)
            {
                attackUpgradeButton.interactable = canUpgrade;
                SetButtonIconAlpha(GetButtonIcon(attackUpgradeButton), canUpgrade);
            }

            if (defenseUpgradeButton != null)
            {
                defenseUpgradeButton.interactable = canUpgrade;
                SetButtonIconAlpha(GetButtonIcon(defenseUpgradeButton), canUpgrade);
            }

            if (restartButton != null)
            {
                restartButton.interactable = true;
                SetButtonIconAlpha(GetButtonIcon(restartButton), true);
            }
        }

        private Sprite[] LoadFrames(string prefix, int frameCount, float pixelsPerUnit, string fallbackPrefix = null)
        {
            var frames = new List<Sprite>();
            for (var i = 1; i <= frameCount; i++)
            {
                var frame = TryLoadSprite(prefix + i.ToString("00"), pixelsPerUnit);
                if (frame == null && !string.IsNullOrEmpty(fallbackPrefix))
                {
                    frame = TryLoadSprite(fallbackPrefix + i.ToString("00"), pixelsPerUnit);
                }

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
            var sprite = TryLoadSprite(resourcePath, pixelsPerUnit);
            if (sprite != null)
            {
                return sprite;
            }

            return WhiteSprite;
        }

        private Sprite LoadSprite(string resourcePath, float pixelsPerUnit, string fallbackResourcePath)
        {
            var sprite = TryLoadSprite(resourcePath, pixelsPerUnit);
            if (sprite != null)
            {
                return sprite;
            }

            if (!string.IsNullOrEmpty(fallbackResourcePath))
            {
                sprite = TryLoadSprite(fallbackResourcePath, pixelsPerUnit);
                if (sprite != null)
                {
                    return sprite;
                }
            }

            return WhiteSprite;
        }

        private Sprite TryLoadSprite(string resourcePath, float pixelsPerUnit)
        {
            var texture = Resources.Load<Texture2D>(resourcePath);
            if (texture == null)
            {
                Debug.LogWarning("Missing battle art resource: " + resourcePath);
                return null;
            }

            var sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                pixelsPerUnit);
            sprite.name = resourcePath;
            return sprite;
        }

        private Sprite LoadSprite(string resourcePath, float pixelsPerUnit, Rect spriteRect, string fallbackResourcePath = null)
        {
            var texture = Resources.Load<Texture2D>(resourcePath);
            var actualResourcePath = resourcePath;
            if (texture == null)
            {
                Debug.LogWarning("Missing battle art resource: " + resourcePath);
                if (string.IsNullOrEmpty(fallbackResourcePath))
                {
                    return WhiteSprite;
                }

                actualResourcePath = fallbackResourcePath;
                texture = Resources.Load<Texture2D>(fallbackResourcePath);
                if (texture == null)
                {
                    Debug.LogWarning("Missing battle art resource: " + fallbackResourcePath);
                    return WhiteSprite;
                }
            }

            var clampedRect = new Rect(
                Mathf.Clamp(spriteRect.x, 0f, texture.width - 1f),
                Mathf.Clamp(spriteRect.y, 0f, texture.height - 1f),
                Mathf.Min(spriteRect.width, texture.width - spriteRect.x),
                Mathf.Min(spriteRect.height, texture.height - spriteRect.y));

            var sprite = Sprite.Create(texture, clampedRect, new Vector2(0.5f, 0.5f), pixelsPerUnit);
            sprite.name = actualResourcePath;
            return sprite;
        }

        private Button CreateImageButton(RectTransform parent, string name, Sprite icon, UnityEngine.Events.UnityAction onClick, Color normalColor, Func<string> tooltipProvider)
        {
            var buttonRect = CreatePanel(name, parent, normalColor);
            var buttonImage = buttonRect.GetComponent<Image>();
            buttonImage.sprite = ButtonSprite;
            buttonImage.type = Image.Type.Sliced;
            var button = buttonRect.gameObject.AddComponent<Button>();
            button.targetGraphic = buttonImage;
            button.onClick.AddListener(onClick);

            var outline = buttonRect.gameObject.AddComponent<Outline>();
            outline.effectColor = Color.Lerp(normalColor, Color.black, 0.58f);
            outline.effectDistance = new Vector2(2f, -2f);
            SetButtonColor(button, normalColor);

            var iconBackplate = CreatePanel("Artwork Backplate", buttonRect, new Color(0.06f, 0.07f, 0.06f, 0.56f));
            iconBackplate.anchorMin = new Vector2(0.03f, 0.03f);
            iconBackplate.anchorMax = new Vector2(0.97f, 0.97f);
            iconBackplate.offsetMin = Vector2.zero;
            iconBackplate.offsetMax = Vector2.zero;
            iconBackplate.GetComponent<Image>().raycastTarget = false;

            var artworkObject = new GameObject("Artwork", typeof(RectTransform), typeof(Image));
            artworkObject.transform.SetParent(buttonRect, false);
            var artwork = artworkObject.GetComponent<Image>();
            artwork.sprite = icon != null ? icon : WhiteSprite;
            artwork.preserveAspect = true;
            artwork.raycastTarget = false;
            artwork.color = Color.white;
            artwork.rectTransform.anchorMin = new Vector2(0.03f, 0.03f);
            artwork.rectTransform.anchorMax = new Vector2(0.97f, 0.97f);
            artwork.rectTransform.offsetMin = Vector2.zero;
            artwork.rectTransform.offsetMax = Vector2.zero;

            AddCommandTooltip(button, tooltipProvider);
            return button;
        }

        private void AddCommandTooltip(Button button, Func<string> tooltipProvider)
        {
            if (button == null || tooltipProvider == null)
            {
                return;
            }

            var eventTrigger = button.gameObject.GetComponent<EventTrigger>();
            if (eventTrigger == null)
            {
                eventTrigger = button.gameObject.AddComponent<EventTrigger>();
            }

            AddPointerEvent(eventTrigger, EventTriggerType.PointerEnter, eventData =>
            {
                var pointerEvent = eventData as PointerEventData;
                BeginCommandTooltipHover(tooltipProvider, pointerEvent != null ? pointerEvent.position : Vector2.zero);
            });
            AddPointerEvent(eventTrigger, EventTriggerType.PointerExit, eventData => HideCommandTooltip());
        }

        private static void AddPointerEvent(EventTrigger trigger, EventTriggerType eventType, UnityEngine.Events.UnityAction<BaseEventData> callback)
        {
            var entry = new EventTrigger.Entry { eventID = eventType };
            entry.callback.AddListener(callback);
            trigger.triggers.Add(entry);
        }

        private Button CreateButton(RectTransform parent, string name, string label, UnityEngine.Events.UnityAction onClick, Color normalColor)
        {
            var buttonRect = CreatePanel(name, parent, normalColor);
            var buttonImage = buttonRect.GetComponent<Image>();
            buttonImage.sprite = ButtonSprite;
            buttonImage.type = Image.Type.Sliced;
            var button = buttonRect.gameObject.AddComponent<Button>();
            button.targetGraphic = buttonImage;
            button.onClick.AddListener(onClick);

            var outline = buttonRect.gameObject.AddComponent<Outline>();
            outline.effectColor = Color.Lerp(normalColor, Color.black, 0.6f);
            outline.effectDistance = new Vector2(2f, -2f);
            SetButtonColor(button, normalColor);

            var iconFrame = CreatePanel("Icon Frame", buttonRect, new Color(0.9f, 0.72f, 0.42f, 1f));
            iconFrame.GetComponent<Image>().sprite = IconDiscSprite;
            iconFrame.anchorMin = new Vector2(0f, 0.5f);
            iconFrame.anchorMax = new Vector2(0f, 0.5f);
            iconFrame.sizeDelta = new Vector2(34f, 34f);
            iconFrame.anchoredPosition = new Vector2(26f, 0f);

            var glyph = CreateText(iconFrame, "Glyph", GetButtonGlyph(name), 18, FontStyle.Bold, TextAnchor.MiddleCenter);
            glyph.color = new Color(0.12f, 0.08f, 0.04f, 1f);
            glyph.rectTransform.anchorMin = Vector2.zero;
            glyph.rectTransform.anchorMax = Vector2.one;
            glyph.rectTransform.offsetMin = Vector2.zero;
            glyph.rectTransform.offsetMax = Vector2.zero;

            var text = CreateText(buttonRect, "Label", label, 16, FontStyle.Bold, TextAnchor.MiddleCenter);
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 11;
            text.resizeTextMaxSize = 16;
            text.rectTransform.anchorMin = new Vector2(0.24f, 0f);
            text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.offsetMin = new Vector2(4f, 5f);
            text.rectTransform.offsetMax = new Vector2(-10f, -5f);
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
                image.sprite = ButtonSprite;
                image.color = normalColor;
            }

            var colors = button.colors;
            colors.normalColor = normalColor;
            colors.highlightedColor = Color.Lerp(normalColor, new Color(1f, 0.9f, 0.5f, 1f), 0.22f);
            colors.pressedColor = Color.Lerp(normalColor, Color.black, 0.3f);
            colors.selectedColor = Color.Lerp(normalColor, new Color(1f, 0.86f, 0.36f, 1f), 0.2f);
            colors.disabledColor = new Color(0.24f, 0.24f, 0.22f, 0.82f);
            button.colors = colors;
        }

        private static Text GetButtonLabel(Button button)
        {
            if (button == null)
            {
                return null;
            }

            var labelTransform = button.transform.Find("Label");
            return labelTransform != null ? labelTransform.GetComponent<Text>() : button.GetComponentInChildren<Text>();
        }

        private static Image GetButtonIcon(Button button)
        {
            if (button == null)
            {
                return null;
            }

            var iconTransform = button.transform.Find("Artwork");
            return iconTransform != null ? iconTransform.GetComponent<Image>() : null;
        }

        private void SetButtonIcon(Button button, Sprite sprite)
        {
            var icon = GetButtonIcon(button);
            if (icon != null)
            {
                icon.sprite = sprite != null ? sprite : WhiteSprite;
                icon.preserveAspect = true;
            }
        }

        private static void SetButtonIconAlpha(Image icon, bool enabled)
        {
            if (icon == null)
            {
                return;
            }

            var color = icon.color;
            color.a = enabled ? 1f : 0.38f;
            icon.color = color;
        }

        private static string GetButtonGlyph(string name)
        {
            if (name.Contains("Builder"))
            {
                return "\u5efa";
            }

            if (name.Contains("Tower"))
            {
                return "\u5854";
            }

            if (name.Contains("Well"))
            {
                return "\u4e95";
            }

            if (name.Contains("Power"))
            {
                return "\u672f";
            }

            if (name.Contains("Shield"))
            {
                return "\u76fe";
            }

            if (name.Contains("Mobilization"))
            {
                return "\u4ee4";
            }

            if (name.Contains("Attack"))
            {
                return "\u653b";
            }

            if (name.Contains("Defense"))
            {
                return "\u5b88";
            }

            if (name.Contains("Restart"))
            {
                return "\u56de";
            }

            if (name.Contains("Menu"))
            {
                return "\u8fd4";
            }

            return "\u5175";
        }

        private void SetCooldownButton(Button button, float cooldown, string readyLabel, bool canUse)
        {
            if (button == null)
            {
                return;
            }

            button.interactable = canUse && cooldown <= 0f;
            SetButtonLabel(button, cooldown > 0f ? readyLabel + "\n" + Mathf.CeilToInt(cooldown) + "s" : readyLabel);
            SetButtonIconAlpha(GetButtonIcon(button), button.interactable);
        }

        private RectTransform CreatePanel(string name, Transform parent, Color color)
        {
            var rect = CreateRect(name, parent);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = PanelSprite;
            image.type = Image.Type.Sliced;
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


    }
}
