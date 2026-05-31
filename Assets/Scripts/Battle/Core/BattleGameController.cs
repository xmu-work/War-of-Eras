using System;
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
    public sealed class BattleGameController : MonoBehaviour
    {
        public const float PlayerBaseX = -15.05f;
        public const float EnemyBaseX = 15.05f;

        private const string MainMenuSceneName = "MainMenu";
        private const string BattleSceneName = "Battle";
        private const string DefeatBackdropPath = "Battle/Outcome/DefeatBackdrop";
        private const string VictoryBackdropPath = "Battle/Outcome/VictoryBackdrop";
        private const string FrontlineBedClipPath = "Audio/Ambience/00_three_lane_frontline_bed_loop";
        private const float EraValuePerSecond = 3.5f;
        private const float FrontlineBedVolume = 0.18f;
        private const float EraAmbienceVolume = 0.42f;
        private const float EraAmbienceFadeDuration = 1.35f;
        private const float MobilizationCostMultiplier = 0.8f;
        private const float MobilizationSpeedMultiplier = 1.15f;
        private const float MapTextureWidth = 1672f;
        private const float MapTextureHeight = 941f;
        private const float MapPixelsPerUnit = 55f;
        private const float CameraOrthographicSize = 5f;
        private const float CameraMinOrthographicSize = 3.4f;
        private const float CameraMaxOrthographicSize = 7.2f;
        private const float CameraZoomStep = 0.55f;
        private const float CameraEdgeScrollMargin = 36f;
        private const float CameraEdgeScrollSpeed = 20f;
        private const float DefaultUnitVisualScale = 0.82f;
        private const float DefaultTowerVisualScale = 0.16f / 3f;
        private const float DefaultBaseVisualScale = 0.16f;
        private const float DefaultResourceWellVisualScale = 0.72f / 3f;
        private const int ResourceWellCost = 120;
        private const float ResourceWellIncomeBonus = 2.5f;
        private const float ResourceWellEraValue = 65f;
        private const float BuildPlacementClickRadius = 0.85f;
        private const float UnitCombatContactPadding = 0.34f;

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
                "Barbarian/Maps/ForestThreeLanes",
                "Barbarian/Base/Base",
                "Barbarian/Units",
                new[] { "Hunter", "Thrower", "Champion" },
                "Barbarian/Towers/BoneTower/attack_",
                new Color(1f, 0.94f, 0.78f, 1f)),
            new AgeVisualSet(
                "Machine",
                "Machine/Maps/ForestThreeLanes",
                "Machine/Base/Base",
                "Machine/Units",
                new[] { "GearSoldier", "SteamCrossbow", "SiegeRoller" },
                "Machine/Towers/GearTower/attack_",
                new Color(0.88f, 0.86f, 0.78f, 1f)),
            new AgeVisualSet(
                "Electric",
                "Electric/Maps/ForestThreeLanes",
                "Electric/Base/Base",
                "Electric/Units",
                new[] { "VoltGuard", "CoilShooter", "CrawlerTank" },
                "Electric/Towers/TeslaTower/attack_",
                new Color(0.66f, 0.88f, 1f, 1f)),
            new AgeVisualSet(
                "Nuclear",
                "Nuclear/Maps/ForestThreeLanes",
                "Nuclear/Base/Base",
                "Nuclear/Units",
                new[] { "RadTrooper", "FissionLancer", "NuclearTank" },
                "Nuclear/Towers/ParticleGunTower/attack_",
                new Color(0.76f, 1f, 0.64f, 1f)),
            new AgeVisualSet(
                "Starsea",
                "Starsea/Maps/ForestThreeLanes",
                "Starsea/Base/Base",
                "Starsea/Units",
                new[] { "LaserTrooper", "SkimmerMech", "AntimatterColossus" },
                "Starsea/Towers/TitaniumRayTower/attack_",
                new Color(0.86f, 0.72f, 1f, 1f))
        };

        private static readonly AgePowerDefinition[] AgePowers =
        {
            new AgePowerDefinition("\u5730\u9707", 35f, 220f, false, 1.5f, 0.15f, 2.2f),
            new AgePowerDefinition("\u7a7a\u6295\u70b8\u5f39", 35f, 180f, false, 0f, 1f, 1f),
            new AgePowerDefinition("\u5168\u573a\u7535\u51fb", 38f, 200f, true, 3f, 0.7f, 1.15f),
            new AgePowerDefinition("\u8f90\u5c04\u7981\u533a", 42f, 280f, false, 8f, 0.82f, 1.05f),
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

        private static readonly float[] LaneY = { 4.45f, 0.5f, -4.18f };
        private static readonly Vector3[][] LaneRoutes =
        {
            new[]
            {
                MapPoint(468f, 196f),
                MapPoint(555f, 198f),
                MapPoint(672f, 207f),
                MapPoint(790f, 214f),
                MapPoint(878f, 232f),
                MapPoint(916f, 278f),
                MapPoint(1006f, 218f),
                MapPoint(1122f, 174f),
                MapPoint(1242f, 162f),
                MapPoint(1370f, 174f),
                MapPoint(1484f, 201f),
                MapPoint(1580f, 211f),
                MapPoint(1664f, 214f)
            },
            new[]
            {
                MapPoint(156f, 490f),
                MapPoint(270f, 460f),
                MapPoint(430f, 459f),
                MapPoint(548f, 464f),
                MapPoint(648f, 464f),
                MapPoint(760f, 464f),
                MapPoint(865f, 466f),
                MapPoint(1010f, 456f),
                MapPoint(1224f, 454f),
                MapPoint(1488f, 452f)
            },
            new[]
            {
                MapPoint(490f, 724f),
                MapPoint(610f, 716f),
                MapPoint(822f, 716f),
                MapPoint(935f, 710f),
                MapPoint(1218f, 721f),
                MapPoint(1360f, 704f),
                MapPoint(1482f, 660f),
                MapPoint(1560f, 645f),
                MapPoint(1664f, 636f)
            }
        };

        private static readonly Vector3[] RouteNodes =
        {
            MapPoint(468f, 196f),
            MapPoint(555f, 198f),
            MapPoint(672f, 207f),
            MapPoint(790f, 214f),
            MapPoint(878f, 232f),
            MapPoint(916f, 278f),
            MapPoint(1006f, 218f),
            MapPoint(1122f, 174f),
            MapPoint(1242f, 162f),
            MapPoint(1370f, 174f),
            MapPoint(1484f, 201f),
            MapPoint(1580f, 211f),
            MapPoint(1664f, 214f),
            MapPoint(156f, 490f),
            MapPoint(270f, 460f),
            MapPoint(430f, 459f),
            MapPoint(548f, 464f),
            MapPoint(648f, 464f),
            MapPoint(760f, 464f),
            MapPoint(865f, 466f),
            MapPoint(1010f, 456f),
            MapPoint(1224f, 454f),
            MapPoint(1488f, 452f),
            MapPoint(490f, 724f),
            MapPoint(610f, 716f),
            MapPoint(822f, 716f),
            MapPoint(935f, 710f),
            MapPoint(1218f, 721f),
            MapPoint(1360f, 704f),
            MapPoint(1482f, 660f),
            MapPoint(1560f, 645f),
            MapPoint(1664f, 636f),
            MapPoint(930f, 548f),
            MapPoint(958f, 635f)
        };

        private static readonly RouteEdge[] RouteEdges =
        {
            new RouteEdge(0, 1),
            new RouteEdge(1, 2),
            new RouteEdge(2, 3),
            new RouteEdge(3, 4),
            new RouteEdge(4, 5),
            new RouteEdge(5, 6),
            new RouteEdge(6, 7),
            new RouteEdge(7, 8),
            new RouteEdge(8, 9),
            new RouteEdge(9, 10),
            new RouteEdge(10, 11),
            new RouteEdge(11, 12),
            new RouteEdge(13, 14),
            new RouteEdge(14, 15),
            new RouteEdge(15, 16),
            new RouteEdge(16, 17),
            new RouteEdge(17, 18),
            new RouteEdge(18, 19),
            new RouteEdge(19, 20),
            new RouteEdge(20, 21),
            new RouteEdge(21, 22),
            new RouteEdge(23, 24),
            new RouteEdge(24, 25),
            new RouteEdge(25, 26),
            new RouteEdge(26, 27),
            new RouteEdge(27, 28),
            new RouteEdge(28, 29),
            new RouteEdge(29, 30),
            new RouteEdge(30, 31),
            new RouteEdge(5, 19),
            new RouteEdge(19, 32),
            new RouteEdge(32, 33),
            new RouteEdge(33, 26)
        };

        private static readonly int[] PlayerRouteStartNodes = { 13 };

        private static readonly Color[] RoutePreviewColors =
        {
            new Color(1f, 0.78f, 0.2f, 0.95f),
            new Color(0.25f, 0.85f, 1f, 0.95f),
            new Color(0.95f, 0.42f, 1f, 0.95f)
        };

        private static readonly Vector3[] PlayerTowerPositions =
        {
            new Vector3(-9.45f, 3.9f, 0f),
            new Vector3(-9.35f, 0.95f, 0f),
            new Vector3(-8.15f, -2.7f, 0f)
        };

        private static readonly Vector3[] EnemyTowerPositions =
        {
            new Vector3(9.45f, 3.9f, 0f),
            new Vector3(9.35f, 0.95f, 0f),
            new Vector3(8.15f, -2.7f, 0f)
        };

        private static readonly Vector3[] PlayerResourceWellPositions =
        {
            MapPoint(330f, 528f),
            MapPoint(662f, 790f)
        };

        private static readonly Vector3[] EnemyResourceWellPositions =
        {
            MapPoint(1342f, 528f),
            MapPoint(1010f, 790f)
        };

        private static Sprite whiteSprite;
        private static Sprite panelSprite;
        private static Sprite buttonSprite;
        private static Sprite iconDiscSprite;
        private static Sprite vfxCircleSprite;
        private static Sprite resourceWellSiteSprite;
        private static Sprite resourceWellBuiltSprite;

        private readonly List<BattleUnit> units = new List<BattleUnit>();
        private readonly List<Button> laneButtons = new List<Button>();
        private readonly List<UnitButtonBinding> unitButtons = new List<UnitButtonBinding>();
        private readonly List<RouteCandidate> pendingRouteCandidates = new List<RouteCandidate>();
        private readonly List<RoutePreview> routePreviews = new List<RoutePreview>();
        private readonly List<BuildPlacementPreview> buildPlacementPreviews = new List<BuildPlacementPreview>();
        private readonly List<string> statusLog = new List<string>();
        private readonly Dictionary<string, int> routeHoldCounts = new Dictionary<string, int>();
        private readonly BattleTower[] playerTowers = new BattleTower[3];
        private readonly BattleTower[] enemyTowers = new BattleTower[3];
        private readonly bool[] playerResourceWells = new bool[PlayerResourceWellPositions.Length];
        private readonly bool[] enemyResourceWells = new bool[EnemyResourceWellPositions.Length];

        private readonly string[] laneNames =
        {
            "\u4e0a\u8def",
            "\u4e2d\u8def",
            "\u4e0b\u8def"
        };

        private UnitDefinition[] playerUnitDefinitions;
        private UnitDefinition[] enemyUnitDefinitions;
        private TowerDefinition currentTowerDefinition;
        private Sprite[] towerFrames;
        private Font uiFont;
        private Camera gameplayCamera;
        private BattleLayout battleLayout;
        private Transform worldRoot;
        private Transform routePreviewRoot;
        private Transform buildPreviewRoot;
        private Transform facilityMarkerRoot;
        private SpriteRenderer playerBaseRenderer;
        private SpriteRenderer enemyBaseRenderer;
        private Material routePreviewMaterial;
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
        private AudioSource frontlineBedSource;
        private AudioSource eraAmbienceSource;
        private AudioSource fadingEraAmbienceSource;
        private RouteCandidate activeRouteCandidate;
        private Button towerButton;
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
        private Vector3[] playerTowerPositions = PlayerTowerPositions;
        private Vector3[] enemyTowerPositions = EnemyTowerPositions;
        private Vector3[] playerResourceWellPositions = PlayerResourceWellPositions;
        private Vector3[] enemyResourceWellPositions = EnemyResourceWellPositions;
        private Vector3 playerBasePosition = new Vector3(PlayerBaseX + 1.25f, -0.16f, 0f);
        private Vector3 enemyBasePosition = new Vector3(EnemyBaseX - 1.25f, -0.16f, 0f);
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
        private int currentAmbienceAgeIndex = -1;
        private int selectedLane = 1;
        private bool gameStarted;
        private bool gameOver;
        private bool hasMapBounds;
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

        public BattleUnit FindTowerTarget(int towerTeam, int laneIndex, Vector3 towerPosition, float range)
        {
            BattleUnit best = null;
            var bestDistance = float.MaxValue;

            for (var i = 0; i < units.Count; i++)
            {
                var candidate = units[i];
                if (candidate == null || !candidate.IsAlive || candidate.Team == towerTeam || candidate.LaneIndex != laneIndex)
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

        public void NotifyUnitKilled(BattleUnit unit, int attackerTeam)
        {
            units.Remove(unit);
            if (attackerTeam == 0 && !gameOver)
            {
                coins += unit.Definition.Reward;
                GainEraValue(unit.Definition.Cost * 0.25f);
                status = unit.Definition.DisplayName + "\u51fb\u6e83\u4e86\u654c\u4eba\uff0c\u83b7\u5f97 " + unit.Definition.Reward + " \u91d1\u5e01\u3002";
            }
        }

        private void BuildDefinitions()
        {
            playerUnitDefinitions = BuildUnitDefinitionsForAge(ageIndex);
            enemyUnitDefinitions = BuildEnemyDefinitions(playerUnitDefinitions);
            currentTowerDefinition = BuildTowerDefinitionForAge(ageIndex);
            towerFrames = LoadFrames(GetAgeVisualSet(ageIndex).TowerFramePrefix, 5, 100f, AgeVisualSets[0].TowerFramePrefix);
        }

        private static AgeVisualSet GetAgeVisualSet(int index)
        {
            return AgeVisualSets[Mathf.Clamp(index, 0, AgeVisualSets.Length - 1)];
        }

        private UnitDefinition[] BuildUnitDefinitionsForAge(int index)
        {
            var tint = AgeTints[Mathf.Clamp(index, 0, AgeTints.Length - 1)];
            switch (Mathf.Clamp(index, 0, AgeNames.Length - 1))
            {
                case 1:
                    return new[]
                    {
                        CreateUnitDefinition("\u9f7f\u8f6e\u5175", "GearSoldier", 50, 115f, 30f, 1.05f, 0.58f, 0.95f, 0.35f, 0, tint),
                        CreateUnitDefinition("\u84b8\u6c7d\u5f29\u624b", "SteamCrossbow", 75, 90f, 14f, 0.95f, 2.75f, 0.8f, 0.34f, 1, tint),
                        CreateUnitDefinition("\u94c1\u8f6e\u7834\u57ce\u8f66", "SiegeRoller", 500, 340f, 78f, 0.72f, 1.1f, 1.45f, 0.44f, 2, tint)
                    };
                case 2:
                    return new[]
                    {
                        CreateUnitDefinition("\u7535\u51fb\u5175", "VoltGuard", 200, 230f, 82f, 1.12f, 0.6f, 0.95f, 0.36f, 0, tint),
                        CreateUnitDefinition("\u7ebf\u5708\u5c04\u624b", "CoilShooter", 400, 180f, 32f, 1f, 3.0f, 0.62f, 0.35f, 1, tint),
                        CreateUnitDefinition("\u5c65\u5e26\u6218\u8f66", "CrawlerTank", 1000, 720f, 150f, 0.65f, 1.55f, 1.5f, 0.46f, 2, tint)
                    };
                case 3:
                    return new[]
                    {
                        CreateUnitDefinition("\u8f90\u5c04\u6b65\u5175", "RadTrooper", 1500, 420f, 120f, 1.2f, 0.62f, 0.82f, 0.36f, 0, tint),
                        CreateUnitDefinition("\u88c2\u53d8\u67aa\u5175", "FissionLancer", 2000, 360f, 46f, 1.05f, 3.08f, 0.46f, 0.35f, 1, tint),
                        CreateUnitDefinition("\u6838\u80fd\u5766\u514b", "NuclearTank", 7000, 1500f, 320f, 0.6f, 1.8f, 1.64f, 0.48f, 2, tint)
                    };
                case 4:
                    return new[]
                    {
                        CreateUnitDefinition("\u6fc0\u5149\u5175", "LaserTrooper", 5000, 1000f, 260f, 1.3f, 0.75f, 0.8f, 0.37f, 0, tint),
                        CreateUnitDefinition("\u6d6e\u6e38\u673a\u7532", "SkimmerMech", 6000, 820f, 95f, 1.15f, 3.25f, 0.38f, 0.36f, 1, tint),
                        CreateUnitDefinition("\u53cd\u7269\u8d28\u5de8\u50cf", "AntimatterColossus", 20000, 3200f, 720f, 0.5f, 2.1f, 1.8f, 0.5f, 2, tint)
                    };
                default:
                    return new[]
                    {
                        CreateUnitDefinition("\u77f3\u68d2\u6218\u58eb", "Hunter", 15, 60f, 16f, 1f, 0.55f, 1f, 0.34f, 0, tint),
                        CreateUnitDefinition("\u6295\u77f3\u730e\u624b", "Thrower", 25, 48f, 9f, 0.9f, 2.35f, 0.95f, 0.33f, 1, tint),
                        CreateUnitDefinition("\u5de8\u9aa8\u52c7\u58eb", "Champion", 100, 180f, 42f, 0.7f, 0.95f, 1.35f, 0.42f, 2, tint)
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
            Color tint)
        {
            var visualSet = GetAgeVisualSet(ageIndex);
            visualSlot = Mathf.Clamp(visualSlot, 0, visualSet.UnitFrameFolders.Length - 1);
            var frameFolder = visualSet.UnitFrameFolders[visualSlot];
            var fallbackFolder = AgeVisualSets[0].UnitFrameFolders[Mathf.Clamp(visualSlot, 0, AgeVisualSets[0].UnitFrameFolders.Length - 1)];

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
                LoadFrames(visualSet.UnitRoot + "/" + frameFolder + "/move_", 5, 100f, AgeVisualSets[0].UnitRoot + "/" + fallbackFolder + "/move_"),
                LoadFrames(visualSet.UnitRoot + "/" + frameFolder + "/attack_", 5, 100f, AgeVisualSets[0].UnitRoot + "/" + fallbackFolder + "/attack_"),
                tint);
        }

        private TowerDefinition BuildTowerDefinitionForAge(int index)
        {
            var tint = AgeTints[Mathf.Clamp(index, 0, AgeTints.Length - 1)];
            switch (Mathf.Clamp(index, 0, AgeNames.Length - 1))
            {
                case 1:
                    return new TowerDefinition("\u9f7f\u8f6e\u629b\u70ae\u5854", 500, 40f, 1.75f, 4f, tint);
                case 2:
                    return new TowerDefinition("\u7279\u65af\u62c9\u5854", 1500, 34f, 1.75f, 5f, tint);
                case 3:
                    return new TowerDefinition("\u7c92\u5b50\u673a\u67aa\u5854", 7000, 70f, 1f, 5f, tint);
                case 4:
                    return new TowerDefinition("\u949b\u6676\u5c04\u7ebf\u5854", 24000, 100f, 1f, 4f, tint);
                default:
                    return new TowerDefinition("\u9aa8\u77f3\u5854", 100, 12f, 0.75f, 3.5f, tint);
            }
        }

        private void ApplyGameSetup()
        {
            selectedMap = GameSession.SelectedMap;
            coins = GameSession.PlayerStartingCoins;
            incomePerSecond = GameSession.IncomePerSecond;
            enemySpawnIntervalScale = GameSession.EnemySpawnIntervalScale;
            enemySpawnTimer = GameSession.InitialEnemySpawnDelay;
            ageIndex = 0;
            eraValue = 0f;
            playerBaseMaxHealth = BaseHealthByAge[ageIndex];
            enemyBaseMaxHealth = BaseHealthByAge[ageIndex];
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
            EnsureMarker(basesRoot, "PlayerBasePoint").position = MapPoint(156f, 464f);
            EnsureMarker(basesRoot, "EnemyBasePoint").position = MapPoint(1488f, 464f);

            var towersRoot = EnsureMarker(root, "Layout/Towers");
            EnsureMarker(towersRoot, "PlayerTowerSlot_0").position = new Vector3(-9.45f, 3.9f, 0f);
            EnsureMarker(towersRoot, "PlayerTowerSlot_1").position = new Vector3(-9.35f, 0.95f, 0f);
            EnsureMarker(towersRoot, "PlayerTowerSlot_2").position = new Vector3(-8.15f, -2.7f, 0f);
            EnsureMarker(towersRoot, "EnemyTowerSlot_0").position = new Vector3(9.45f, 3.9f, 0f);
            EnsureMarker(towersRoot, "EnemyTowerSlot_1").position = new Vector3(9.35f, 0.95f, 0f);
            EnsureMarker(towersRoot, "EnemyTowerSlot_2").position = new Vector3(8.15f, -2.7f, 0f);

            var wellsRoot = EnsureMarker(root, "Layout/ResourceWells");
            EnsureMarker(wellsRoot, "PlayerWellSlot_0").position = MapPoint(488f, 578f);
            EnsureMarker(wellsRoot, "PlayerWellSlot_1").position = MapPoint(820f, 840f);
            EnsureMarker(wellsRoot, "EnemyWellSlot_0").position = MapPoint(1500f, 578f);
            EnsureMarker(wellsRoot, "EnemyWellSlot_1").position = MapPoint(1152f, 840f);
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

        private void RebuildRouteGraphFromLayout()
        {
            if (laneRoutes.Length < 3 || laneRoutes[0].Length < 13 || laneRoutes[1].Length < 10 || laneRoutes[2].Length < 9)
            {
                routeNodes = RouteNodes;
                routeEdges = RouteEdges;
                return;
            }

            var nodes = new List<Vector3>(34);
            nodes.AddRange(laneRoutes[0]);
            nodes.AddRange(laneRoutes[1]);
            nodes.AddRange(laneRoutes[2]);
            nodes.Add(MapPoint(930f, 548f));
            nodes.Add(MapPoint(958f, 635f));
            routeNodes = nodes.ToArray();
            routeEdges = RouteEdges;
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
                    source.Tint);
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

            routePreviewRoot = new GameObject("Route Preview Root").transform;
            routePreviewRoot.SetParent(worldRoot, false);
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
            if (IsSecondaryPointerHeld())
            {
                if (!isCameraDragging && !IsPointerOverUi())
                {
                    isCameraDragging = true;
                    lastCameraDragScreenPosition = pointerPosition;
                }
                else if (isCameraDragging)
                {
                    var previousWorld = GetWorldPointFromScreen(camera, lastCameraDragScreenPosition);
                    var currentWorld = GetWorldPointFromScreen(camera, pointerPosition);
                    MoveCameraBy(previousWorld - currentWorld);
                    lastCameraDragScreenPosition = pointerPosition;
                }

                return;
            }

            isCameraDragging = false;

            if (IsPointerOverUi() || !IsPointerInsideScreen(pointerPosition))
            {
                ClampCameraToMap();
                return;
            }

            var direction = Vector3.zero;
            if (pointerPosition.x <= CameraEdgeScrollMargin)
            {
                direction.x -= 1f;
            }
            else if (pointerPosition.x >= Screen.width - CameraEdgeScrollMargin)
            {
                direction.x += 1f;
            }

            if (pointerPosition.y <= CameraEdgeScrollMargin)
            {
                direction.y -= 1f;
            }
            else if (pointerPosition.y >= Screen.height - CameraEdgeScrollMargin)
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
            if (!IsPrimaryPointerPressed() || IsPointerOverUi())
            {
                return;
            }

            gameStarted = true;
            if (startHintPanel != null)
            {
                startHintPanel.gameObject.SetActive(false);
            }

            status = "\u6218\u6597\u5f00\u59cb\uff01\u53f3\u952e\u62d6\u52a8\u3001\u6eda\u8f6e\u7f29\u653e\uff0c\u6216\u628a\u9f20\u6807\u9760\u8fd1\u5c4f\u5e55\u8fb9\u7f18\u67e5\u770b\u6218\u573a\u3002";
        }

        private void ApplyCameraZoom(Camera camera, Vector3 pointerPosition)
        {
            if (IsPointerOverUi() || !IsPointerInsideScreen(pointerPosition))
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

        private static Vector3 GetWorldPointFromScreen(Camera camera, Vector3 screenPosition)
        {
            screenPosition.z = -camera.transform.position.z;
            var worldPoint = camera.ScreenToWorldPoint(screenPosition);
            worldPoint.z = 0f;
            return worldPoint;
        }

        private void CreateBaseArt()
        {
            var baseSprite = LoadAgeBaseSprite();
            playerBaseRenderer = CreateSprite("Player Base Art", baseSprite, playerBasePosition, 3);
            enemyBaseRenderer = CreateSprite("Enemy Base Art", baseSprite, enemyBasePosition, 3);
            enemyBaseRenderer.flipX = true;
            RefreshBaseVisuals();
        }

        private Sprite LoadAgeBaseSprite()
        {
            var visualSet = GetAgeVisualSet(ageIndex);
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
            var baseSprite = LoadAgeBaseSprite();
            var visualSet = GetAgeVisualSet(ageIndex);
            if (playerBaseRenderer != null)
            {
                playerBaseRenderer.sprite = baseSprite;
                playerBaseRenderer.transform.localScale = Vector3.one * baseVisualScale;
                playerBaseRenderer.color = Color.Lerp(new Color(1f, 0.96f, 0.86f, 0.9f), visualSet.FallbackTint, 0.28f);
            }

            if (enemyBaseRenderer != null)
            {
                enemyBaseRenderer.sprite = baseSprite;
                enemyBaseRenderer.flipX = true;
                enemyBaseRenderer.transform.localScale = Vector3.one * baseVisualScale;
                enemyBaseRenderer.color = Color.Lerp(new Color(1f, 0.66f, 0.58f, 0.82f), visualSet.FallbackTint, 0.18f);
            }
        }

        private void CreateResourceWellSiteMarkers()
        {
            for (var i = 0; i < playerResourceWellPositions.Length; i++)
            {
                var marker = CreateSprite("Player Resource Well Site " + (i + 1), ResourceWellSiteSprite, playerResourceWellPositions[i], 4);
                marker.transform.SetParent(facilityMarkerRoot, true);
                marker.transform.localScale = Vector3.one * 0.72f;
                marker.color = new Color(0.42f, 1f, 0.82f, 0.78f);
            }

            for (var i = 0; i < enemyResourceWellPositions.Length; i++)
            {
                var marker = CreateSprite("Enemy Resource Well Site " + (i + 1), ResourceWellSiteSprite, enemyResourceWellPositions[i], 4);
                marker.transform.SetParent(facilityMarkerRoot, true);
                marker.transform.localScale = Vector3.one * 0.72f;
                marker.color = new Color(1f, 0.48f, 0.34f, 0.72f);
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
            root.offsetMin = new Vector2(24f, 18f);
            root.offsetMax = new Vector2(-24f, -18f);

            BuildTopHud(root);
            BuildStartHintPanel(root);
            BuildStatusPanel(root);
            BuildCommandPanel(root);
            BuildOutcomeOverlay(canvasObject.transform);
        }

        private void BuildTopHud(RectTransform root)
        {
            var top = CreateRect("Top HUD", root);
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

            var resourcePanel = CreateHudCell("Resource Cell", top, "\u8d44\u6e90\u4e0e\u65f6\u4ee3");
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
            var cell = CreatePanel(name, parent, new Color(0.24f, 0.28f, 0.24f, 0.98f));
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
            var panel = CreatePanel("Start Hint Panel", root, new Color(0.23f, 0.29f, 0.22f, 0.96f));
            startHintPanel = panel;
            panel.anchorMin = new Vector2(0f, 0.42f);
            panel.anchorMax = new Vector2(0f, 0.42f);
            panel.sizeDelta = new Vector2(330f, 230f);
            panel.anchoredPosition = new Vector2(165f, 0f);

            var titleText = CreateText(panel, "Start Hint Title", "\u6218\u573a\u5f85\u547d", 22, FontStyle.Bold, TextAnchor.UpperCenter);
            titleText.color = new Color(0.95f, 0.88f, 0.66f, 1f);
            titleText.rectTransform.anchorMin = new Vector2(0.08f, 0.74f);
            titleText.rectTransform.anchorMax = new Vector2(0.92f, 0.95f);
            titleText.rectTransform.offsetMin = Vector2.zero;
            titleText.rectTransform.offsetMax = Vector2.zero;

            laneText = CreateText(panel, "Start Hint Body", string.Empty, 17, FontStyle.Bold, TextAnchor.UpperLeft);
            laneText.color = new Color(0.96f, 0.92f, 0.78f, 1f);
            laneText.rectTransform.anchorMin = new Vector2(0.08f, 0.1f);
            laneText.rectTransform.anchorMax = new Vector2(0.92f, 0.72f);
            laneText.rectTransform.offsetMin = Vector2.zero;
            laneText.rectTransform.offsetMax = Vector2.zero;
        }

        private void BuildCommandPanel(RectTransform root)
        {
            var panel = CreateRect("Command Dock", root);
            panel.anchorMin = new Vector2(0.23f, 0f);
            panel.anchorMax = new Vector2(1f, 0f);
            panel.sizeDelta = new Vector2(0f, 132f);
            panel.anchoredPosition = new Vector2(0f, 66f);

            var grid = panel.gameObject.AddComponent<GridLayoutGroup>();
            grid.padding = new RectOffset(8, 8, 8, 8);
            grid.spacing = new Vector2(12f, 12f);
            grid.cellSize = new Vector2(182f, 54f);
            grid.childAlignment = TextAnchor.LowerRight;
            grid.constraint = GridLayoutGroup.Constraint.FixedRowCount;
            grid.constraintCount = 2;

            for (var i = 0; i < playerUnitDefinitions.Length; i++)
            {
                var definition = playerUnitDefinitions[i];
                var slotIndex = i;
                var button = CreateButton(panel, definition.Key, FormatUnitButtonLabel(definition), () => SelectPlayerUnit(unitButtons[slotIndex].Definition), new Color(0.43f, 0.31f, 0.17f, 1f));
                unitButtons.Add(new UnitButtonBinding(button, GetButtonLabel(button), definition));
            }

            towerButton = CreateButton(panel, "Tower", FormatTowerButtonLabel(), TryBuildTower, new Color(0.31f, 0.43f, 0.45f, 1f));
            resourceWellButton = CreateButton(panel, "Resource Well", FormatResourceWellButtonLabel(), TryBuildResourceWell, new Color(0.24f, 0.46f, 0.36f, 1f));
            agePowerButton = CreateButton(panel, "Age Power", AgePowers[ageIndex].DisplayName, UseAgePower, new Color(0.58f, 0.31f, 0.18f, 1f));
            shieldButton = CreateButton(panel, "Shield Barrier", "\u62a4\u76fe\u5c4f\u969c", UseShieldBarrier, new Color(0.24f, 0.42f, 0.58f, 1f));
            mobilizationButton = CreateButton(panel, "Mobilization", "\u6218\u4e89\u52a8\u5458", UseMobilization, new Color(0.52f, 0.44f, 0.18f, 1f));
            attackUpgradeButton = CreateButton(panel, "Attack Evolution", "\u8fdb\u653b\u8fdb\u5316", () => UpgradeAge(EvolutionPath.Attack), new Color(0.62f, 0.22f, 0.16f, 1f));
            defenseUpgradeButton = CreateButton(panel, "Defense Evolution", "\u9632\u5b88\u8fdb\u5316", () => UpgradeAge(EvolutionPath.Defense), new Color(0.22f, 0.38f, 0.56f, 1f));
            restartButton = CreateButton(panel, "Restart", "\u91cd\u7f6e\u6218\u6597", RestartBattle, new Color(0.38f, 0.32f, 0.46f, 1f));
        }

        private void BuildStatusPanel(RectTransform root)
        {
            var panel = CreatePanel("Battle Log Panel", root, new Color(0.18f, 0.23f, 0.2f, 0.96f));
            panel.anchorMin = new Vector2(0f, 0f);
            panel.anchorMax = new Vector2(0f, 0f);
            panel.sizeDelta = new Vector2(430f, 154f);
            panel.anchoredPosition = new Vector2(215f, 77f);

            statusText = CreateText(panel, "Battle Log", string.Empty, 15, FontStyle.Bold, TextAnchor.LowerLeft);
            statusText.color = new Color(0.94f, 0.91f, 0.76f, 1f);
            statusText.rectTransform.anchorMin = Vector2.zero;
            statusText.rectTransform.anchorMax = Vector2.one;
            statusText.rectTransform.offsetMin = new Vector2(16f, 12f);
            statusText.rectTransform.offsetMax = new Vector2(-16f, -12f);
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
            status = "\u5df2\u5207\u6362\u5230" + laneNames[selectedLane] + "\u3002";
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

            if (activeRouteCandidate == null)
            {
                status = "\u8bf7\u5148\u70b9\u51fb\u5730\u56fe\u9053\u8def\u9009\u5b9a\u884c\u519b\u8def\u7ebf\uff0c\u518d\u70b9\u51fb\u5175\u79cd\u6d3e\u5175\u3002";
                return;
            }

            if (coins < GetUnitCost(definition))
            {
                status = "\u91d1\u5e01\u4e0d\u8db3\uff0c\u8fd8\u4e0d\u80fd\u6d3e\u51fa" + definition.DisplayName + "\u3002";
                return;
            }

            DispatchUnitOnActiveRoute(definition);
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

            var worldPoint = GetMouseWorldPoint();
            if (TryGetBuildSlotAt(activeBuildPlacement, worldPoint, out var slotIndex))
            {
                ConfirmBuildPlacement(slotIndex);
            }
            else
            {
                status = "\u8bf7\u70b9\u51fb\u7ea2\u8272\u53ef\u5efa\u9020\u63d0\u793a\uff0c\u6309 Esc \u53d6\u6d88\u3002";
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
            ClearRoutePreviews();
            ShowBuildPlacementPreviews(kind);
            status = "\u5df2\u8fdb\u5165" + GetBuildPlacementName(kind) + "\u653e\u7f6e\u6a21\u5f0f\uff1a\u70b9\u51fb\u7ea2\u8272\u63d0\u793a\u4f4d\u786e\u8ba4\u5efa\u9020\uff0c\u6309 Esc \u53d6\u6d88\u3002";
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

            coins -= cost;
            if (kind == BuildPlacementKind.Tower)
            {
                BuildTowerAt(slotIndex);
            }
            else if (kind == BuildPlacementKind.ResourceWell)
            {
                BuildResourceWellAt(slotIndex);
            }

            activeBuildPlacement = BuildPlacementKind.None;
            ClearBuildPlacementPreviews();
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

                var marker = CreateSprite(GetBuildPlacementName(kind) + " Build Prompt " + (i + 1), VfxCircleSprite, positions[i], 92 + i);
                marker.transform.SetParent(buildPreviewRoot, true);
                marker.transform.localScale = Vector3.one * (kind == BuildPlacementKind.ResourceWell ? 0.86f : 0.74f);
                marker.color = new Color(1f, 0.08f, 0.04f, 0.72f);
                marker.gameObject.AddComponent<BattleBuildPromptPulse>().Configure(0.82f, 1.18f, 2.7f);
                buildPlacementPreviews.Add(new BuildPlacementPreview(marker.gameObject, i));
            }
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
                return slotIndex >= 0 && slotIndex < playerTowers.Length && playerTowers[slotIndex] == null;
            }

            if (kind == BuildPlacementKind.ResourceWell)
            {
                return slotIndex >= 0 && slotIndex < playerResourceWells.Length && !playerResourceWells[slotIndex];
            }

            return false;
        }

        private Vector3[] GetBuildPlacementPositions(BuildPlacementKind kind)
        {
            return kind == BuildPlacementKind.ResourceWell ? playerResourceWellPositions : playerTowerPositions;
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
                return "\u8d44\u6e90\u4e95";
            }

            return currentTowerDefinition != null ? currentTowerDefinition.DisplayName : "\u70ae\u5854";
        }

        private void UpdateRoutePlanningInput()
        {
            if (IsCancelPressed())
            {
                ClearRoutePlanning("\u5df2\u53d6\u6d88\u5f53\u524d\u884c\u519b\u8def\u7ebf\u3002");
                return;
            }

            if (!IsPrimaryPointerPressed())
            {
                return;
            }

            if (IsPointerOverUi())
            {
                return;
            }

            var worldPoint = GetMouseWorldPoint();
            SelectRouteTo(worldPoint);
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

        private static bool IsSecondaryPointerHeld()
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current != null && Mouse.current.rightButton.isPressed;
#else
            return Input.GetMouseButton(1);
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

        private void SelectRouteTo(Vector3 worldPoint)
        {
            if (!TrySelectAuthoredLaneRoute(worldPoint, out var candidate))
            {
                if (activeRouteCandidate == null)
                {
                    ClearRoutePreviews();
                }

                status = "\u8bf7\u70b9\u51fb\u9053\u8def\u9644\u8fd1\u7684\u53ef\u8fbe\u70b9\u3002";
                return;
            }

            activeRouteCandidate = candidate;
            pendingRouteCandidates.Clear();
            pendingRouteCandidates.Add(activeRouteCandidate);
            ShowRoutePreviews();
            selectedLane = activeRouteCandidate.LaneIndex;
            status = "\u884c\u519b\u8def\u7ebf\u5df2\u9501\u5b9a\uff0c\u73b0\u5728\u53ef\u4ee5\u8fde\u7eed\u70b9\u51fb\u5175\u79cd\u6d3e\u5175\uff0c\u6309 Esc \u53d6\u6d88\u8def\u7ebf\u3002";
        }

        private void DispatchUnitOnActiveRoute(UnitDefinition definition)
        {
            if (activeRouteCandidate == null || definition == null)
            {
                return;
            }

            var cost = GetUnitCost(definition);
            if (coins < cost)
            {
                status = "\u91d1\u5e01\u4e0d\u8db3\uff0c\u8def\u7ebf\u5df2\u4fdd\u7559\uff0c\u53ef\u7a0d\u540e\u518d\u6d3e\u5175\u3002";
                return;
            }

            coins -= cost;
            selectedLane = activeRouteCandidate.LaneIndex;
            SpawnUnit(
                definition,
                0,
                activeRouteCandidate.LaneIndex,
                playerHealthMultiplier,
                playerDamageMultiplier,
                GetPlayerSpeedMultiplier());
            GainEraValue(definition.Cost * 0.45f);
            status = definition.DisplayName + "\u5df2\u4ece\u57fa\u5730\u6d1e\u53e3\u51fa\u53d1\uff0c\u5f53\u524d\u8def\u7ebf\u7ee7\u7eed\u4fdd\u7559\u3002";
        }

        private bool TrySelectAuthoredLaneRoute(Vector3 worldPoint, out RouteCandidate candidate)
        {
            candidate = null;
            var bestLane = -1;
            var bestDistance = 1.15f;
            var bestCost = float.PositiveInfinity;

            for (var laneIndex = 0; laneIndex < laneRoutes.Length; laneIndex++)
            {
                var route = laneRoutes[laneIndex];
                if (route == null || route.Length < 2)
                {
                    continue;
                }

                var distance = GetDistanceToRoute(worldPoint, route);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestLane = laneIndex;
                    bestCost = GetRouteLength(route);
                }
            }

            if (bestLane < 0)
            {
                return false;
            }

            candidate = new RouteCandidate(new List<Vector3>(laneRoutes[bestLane]), bestCost, bestLane);
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

        private static float GetRouteLength(Vector3[] route)
        {
            var length = 0f;
            for (var i = 0; i < route.Length - 1; i++)
            {
                length += Vector2.Distance(route[i], route[i + 1]);
            }

            return length;
        }

        private Vector3[] BuildRouteWithHoldSlot(RouteCandidate candidate)
        {
            var points = new List<Vector3>(candidate.Points);
            if (points.Count == 0)
            {
                return points.ToArray();
            }

            var key = GetRouteHoldKey(candidate);
            routeHoldCounts.TryGetValue(key, out var count);
            routeHoldCounts[key] = count + 1;

            var row = count / 5;
            var column = count % 5;
            var offset = new Vector3((column - 2) * 0.32f, -row * 0.26f, 0f);
            points[points.Count - 1] += offset;
            return points.ToArray();
        }

        private static string GetRouteHoldKey(RouteCandidate candidate)
        {
            if (candidate == null || candidate.Points.Count == 0)
            {
                return "empty";
            }

            var end = candidate.Points[candidate.Points.Count - 1];
            return Mathf.RoundToInt(end.x * 10f) + ":" + Mathf.RoundToInt(end.y * 10f);
        }

        private void ClearRoutePlanning(string message)
        {
            ClearRoutePreviews();
            status = message;
        }

        private void ClearRoutePreviews()
        {
            for (var i = 0; i < routePreviews.Count; i++)
            {
                if (routePreviews[i].Root != null)
                {
                    Destroy(routePreviews[i].Root);
                }
            }

            routePreviews.Clear();
            pendingRouteCandidates.Clear();
            activeRouteCandidate = null;
        }

        private bool TryFindReachableTarget(Vector3 worldPoint, out RouteTarget target)
        {
            var bestDistance = 0.32f;
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

            if (bestA < 0)
            {
                target = default(RouteTarget);
                return false;
            }

            target = new RouteTarget(bestA, bestB, bestPoint);
            return true;
        }

        private void BuildRouteCandidates(RouteTarget target)
        {
            pendingRouteCandidates.Clear();

            for (var i = 0; i < PlayerRouteStartNodes.Length; i++)
            {
                if (!TryBuildShortestRoute(PlayerRouteStartNodes[i], target, out var points, out var cost))
                {
                    continue;
                }

                pendingRouteCandidates.Add(new RouteCandidate(points, cost, EstimateLaneIndex(target.Position)));
            }

            pendingRouteCandidates.Sort((left, right) => left.Cost.CompareTo(right.Cost));

            for (var i = pendingRouteCandidates.Count - 1; i >= 3; i--)
            {
                pendingRouteCandidates.RemoveAt(i);
            }
        }

        private bool TryBuildShortestRoute(int startNode, RouteTarget target, out List<Vector3> points, out float totalCost)
        {
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

        private void ShowRoutePreviews()
        {
            for (var i = 0; i < routePreviews.Count; i++)
            {
                if (routePreviews[i].Root != null)
                {
                    Destroy(routePreviews[i].Root);
                }
            }

            routePreviews.Clear();

            for (var i = 0; i < pendingRouteCandidates.Count; i++)
            {
                var candidate = pendingRouteCandidates[i];
                var root = new GameObject("Route Candidate " + (i + 1));
                root.transform.SetParent(routePreviewRoot, false);

                var line = root.AddComponent<LineRenderer>();
                line.useWorldSpace = true;
                line.positionCount = candidate.Points.Count;
                line.SetPositions(candidate.Points.ToArray());
                line.material = GetRoutePreviewMaterial();
                line.widthMultiplier = 0.08f + i * 0.025f;
                line.numCornerVertices = 5;
                line.numCapVertices = 5;
                line.sortingOrder = 80 + i;

                var color = RoutePreviewColors[i % RoutePreviewColors.Length];
                line.startColor = color;
                line.endColor = color;

                var marker = CreateSprite("Route Target Marker " + (i + 1), WhiteSprite, candidate.Points[candidate.Points.Count - 1], 85 + i);
                marker.transform.SetParent(root.transform, true);
                marker.color = color;
                marker.transform.localScale = Vector3.one * 0.18f;

                routePreviews.Add(new RoutePreview(root, candidate));
            }
        }

        private Material GetRoutePreviewMaterial()
        {
            if (routePreviewMaterial != null)
            {
                return routePreviewMaterial;
            }

            var shader = Shader.Find("Sprites/Default") ?? Shader.Find("Universal Render Pipeline/Unlit");
            routePreviewMaterial = new Material(shader);
            return routePreviewMaterial;
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
            if (ageIndex >= EraThresholds.Length)
            {
                return 0;
            }

            return EraThresholds[ageIndex];
        }

        private string FormatUnitButtonLabel(UnitDefinition definition)
        {
            return definition.DisplayName + "\n" + GetUnitCost(definition) + " \u91d1\u5e01";
        }

        private string FormatTowerButtonLabel()
        {
            if (currentTowerDefinition == null)
            {
                return "\u5efa\u9020\u70ae\u5854";
            }

            return "\u5efa\u9020" + currentTowerDefinition.DisplayName + "\n" + currentTowerDefinition.Cost + " \u91d1\u5e01";
        }

        private string FormatResourceWellButtonLabel()
        {
            return "\u5efa\u9020\u8d44\u6e90\u4e95\n" + ResourceWellCost + " \u91d1\u5e01";
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
                if (binding.Label != null)
                {
                    binding.Label.text = FormatUnitButtonLabel(binding.Definition);
                }
            }

            SetButtonLabel(towerButton, FormatTowerButtonLabel());
            SetButtonLabel(resourceWellButton, FormatResourceWellButtonLabel());
            SetButtonLabel(agePowerButton, AgePowers[ageIndex].DisplayName);
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

        private void TryBuildResourceWell()
        {
            BeginBuildPlacement(BuildPlacementKind.ResourceWell);
        }

        private void BuildTowerAt(int slotIndex)
        {
            if (currentTowerDefinition == null || slotIndex < 0 || slotIndex >= playerTowers.Length || playerTowers[slotIndex] != null)
            {
                return;
            }

            var towerObject = new GameObject(currentTowerDefinition.DisplayName + " - " + laneNames[slotIndex]);
            towerObject.transform.SetParent(worldRoot, false);
            towerObject.transform.position = GetPlayerTowerPosition(slotIndex);

            var tower = towerObject.AddComponent<BattleTower>();
            tower.Configure(this, slotIndex, 0, currentTowerDefinition, towerFrames);
            playerTowers[slotIndex] = tower;
            selectedLane = slotIndex;
            GainEraValue(currentTowerDefinition.Cost * 0.32f);
            status = "\u5df2\u5728" + laneNames[slotIndex] + "\u5efa\u9020" + currentTowerDefinition.DisplayName + "\u3002";
        }

        private void BuildResourceWellAt(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= playerResourceWells.Length || playerResourceWells[slotIndex])
            {
                return;
            }

            playerResourceWells[slotIndex] = true;
            incomePerSecond += ResourceWellIncomeBonus;
            GainEraValue(ResourceWellEraValue);

            var well = CreateSprite("Player Resource Well " + (slotIndex + 1), ResourceWellBuiltSprite, playerResourceWellPositions[slotIndex], 8);
            well.transform.SetParent(facilityMarkerRoot, true);
            well.transform.localScale = Vector3.one * resourceWellVisualScale;
            well.color = Color.white;
            status = "\u5df2\u5efa\u6210\u8d44\u6e90\u4e95\uff0c\u91d1\u5e01\u4ea7\u51fa +" + ResourceWellIncomeBonus.ToString("0.#") + "/s\u3002";
        }

        private int CountBuiltResourceWells(bool[] resourceWells)
        {
            var count = 0;
            for (var i = 0; i < resourceWells.Length; i++)
            {
                if (resourceWells[i])
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
                BuildEnemyResourceWellAt(0);
            }

            if (elapsedTime >= 55f)
            {
                BuildEnemyTowerAt(1);
            }

            if (elapsedTime >= 82f)
            {
                BuildEnemyResourceWellAt(1);
            }

            if (elapsedTime >= 115f)
            {
                BuildEnemyTowerAt(0);
            }

            if (elapsedTime >= 150f)
            {
                BuildEnemyTowerAt(2);
            }
        }

        private void BuildEnemyTowerAt(int slotIndex)
        {
            if (currentTowerDefinition == null || slotIndex < 0 || slotIndex >= enemyTowers.Length || enemyTowers[slotIndex] != null)
            {
                return;
            }

            var towerObject = new GameObject("Enemy " + currentTowerDefinition.DisplayName + " - " + laneNames[slotIndex]);
            towerObject.transform.SetParent(worldRoot, false);
            towerObject.transform.position = GetEnemyTowerPosition(slotIndex);

            var tower = towerObject.AddComponent<BattleTower>();
            tower.Configure(this, slotIndex, 1, currentTowerDefinition, towerFrames);
            enemyTowers[slotIndex] = tower;
            status = "\u654c\u65b9\u5728" + laneNames[slotIndex] + "\u5efa\u8d77\u4e86" + currentTowerDefinition.DisplayName + "\u3002";
        }

        private void BuildEnemyResourceWellAt(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= enemyResourceWells.Length || enemyResourceWells[slotIndex])
            {
                return;
            }

            enemyResourceWells[slotIndex] = true;
            var well = CreateSprite("Enemy Resource Well " + (slotIndex + 1), ResourceWellBuiltSprite, enemyResourceWellPositions[slotIndex], 8);
            well.transform.SetParent(facilityMarkerRoot, true);
            well.transform.localScale = Vector3.one * resourceWellVisualScale;
            well.flipX = true;
            well.color = new Color(1f, 0.72f, 0.62f, 1f);
            status = "\u654c\u65b9\u5efa\u6210\u4e86\u8d44\u6e90\u4e95\u3002";
        }

        private void SpawnUnit(
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
                return;
            }

            if (definition == null)
            {
                Debug.LogWarning("Skipping unit spawn because the unit definition is missing.");
                return;
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
        }

        private void UseAgePower()
        {
            if (gameOver || agePowerCooldown > 0f)
            {
                return;
            }

            var power = AgePowers[ageIndex];
            agePowerCooldown = power.Cooldown;
            var affectedPositions = new List<Vector3>();
            var affected = ApplyAgePower(power, affectedPositions);
            PlayAgePowerVisual(ageIndex, power, affectedPositions);
            status = power.DisplayName + "\u5df2\u91ca\u653e\uff0c\u5f71\u54cd " + affected + " \u540d\u654c\u519b\u3002";
        }

        private int ApplyAgePower(AgePowerDefinition power, List<Vector3> affectedPositions)
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
                affectedPositions.Add(unit.transform.position);
                unit.TakeDamage(power.Damage, 0);
                if (unit != null && unit.IsAlive && power.StatusDuration > 0f)
                {
                    unit.ApplyStatusEffect(power.StatusDuration, power.SpeedMultiplier, power.AttackIntervalMultiplier);
                }
            }

            return affected;
        }

        private void PlayAgePowerVisual(int powerIndex, AgePowerDefinition power, List<Vector3> affectedPositions)
        {
            var root = new GameObject(power.DisplayName + " Visual");
            root.transform.SetParent(worldRoot, false);
            root.AddComponent<BattleTimedDestroy>().Configure(3f);
            var center = GetPowerVisualCenter(affectedPositions);

            switch (powerIndex)
            {
                case 0:
                    CreateEarthquakeVisual(root.transform, center, affectedPositions);
                    break;
                case 1:
                    CreateBombingVisual(root.transform, center, affectedPositions);
                    break;
                case 2:
                    CreateLightningVisual(root.transform, center, affectedPositions);
                    break;
                case 3:
                    CreateRadiationVisual(root.transform, center);
                    break;
                default:
                    CreateTimeSlowVisual(root.transform, center, affectedPositions);
                    break;
            }
        }

        private Vector3 GetPowerVisualCenter(List<Vector3> affectedPositions)
        {
            if (affectedPositions != null && affectedPositions.Count > 0)
            {
                var sum = Vector3.zero;
                for (var i = 0; i < affectedPositions.Count; i++)
                {
                    sum += affectedPositions[i];
                }

                return sum / affectedPositions.Count;
            }

            var route = GetLaneRoute(selectedLane);
            return route[Mathf.Clamp(route.Length / 2, 0, route.Length - 1)];
        }

        private List<Vector3> GetVisualTargets(List<Vector3> affectedPositions, Vector3 fallback)
        {
            var targets = new List<Vector3>();
            if (affectedPositions != null)
            {
                for (var i = 0; i < affectedPositions.Count && targets.Count < 8; i++)
                {
                    targets.Add(affectedPositions[i]);
                }
            }

            if (targets.Count == 0)
            {
                targets.Add(fallback);
            }

            return targets;
        }

        private void CreateEarthquakeVisual(Transform root, Vector3 center, List<Vector3> affectedPositions)
        {
            for (var i = 0; i < 3; i++)
            {
                var ring = CreateVfxDisc(root, "Earthquake Shockwave " + i, center, new Color(1f, 0.73f, 0.28f, 0.38f), 0.9f + i * 0.55f, 96 + i);
                var effect = ring.gameObject.AddComponent<BattleVfxFade>();
                effect.Configure(1.05f + i * 0.12f, 2.8f + i * 0.8f, 18f);
            }

            var targets = GetVisualTargets(affectedPositions, center);
            for (var i = 0; i < targets.Count; i++)
            {
                CreateCrackLine(root, targets[i], i);
            }
        }

        private void CreateBombingVisual(Transform root, Vector3 center, List<Vector3> affectedPositions)
        {
            var targets = GetVisualTargets(affectedPositions, center);
            for (var i = 0; i < targets.Count; i++)
            {
                var target = targets[i];
                CreateVfxLine(root, "Bomb Trail " + i, new[] { target + new Vector3(-0.15f, 4.2f, 0f), target }, new Color(1f, 0.92f, 0.45f, 0.95f), 0.08f, 102 + i)
                    .gameObject.AddComponent<BattleVfxFade>().Configure(0.75f, 0f, 0f);

                var blast = CreateVfxDisc(root, "Bomb Blast " + i, target, new Color(1f, 0.36f, 0.12f, 0.58f), 0.55f, 104 + i);
                blast.gameObject.AddComponent<BattleVfxFade>().Configure(0.9f, 2.4f, 0f);
            }
        }

        private void CreateLightningVisual(Transform root, Vector3 center, List<Vector3> affectedPositions)
        {
            var flash = CreateVfxDisc(root, "Lightning Field", center, new Color(0.45f, 0.9f, 1f, 0.22f), 3.8f, 95);
            flash.gameObject.AddComponent<BattleVfxFade>().Configure(0.7f, 1.6f, 45f);

            var targets = GetVisualTargets(affectedPositions, center);
            for (var i = 0; i < targets.Count; i++)
            {
                var target = targets[i];
                CreateVfxLine(root, "Lightning Bolt " + i, BuildLightningPoints(target + new Vector3(0f, 4.2f, 0f), target, i), new Color(0.72f, 0.96f, 1f, 1f), 0.075f, 110 + i)
                    .gameObject.AddComponent<BattleVfxFade>().Configure(0.55f, 0f, 0f);
            }
        }

        private void CreateRadiationVisual(Transform root, Vector3 center)
        {
            var zone = CreateVfxDisc(root, "Radiation Zone", center, new Color(0.35f, 1f, 0.24f, 0.24f), 2.3f, 96);
            zone.gameObject.AddComponent<BattleVfxFade>().Configure(2.2f, 0.65f, 12f);

            for (var i = 0; i < 4; i++)
            {
                var ring = CreateVfxDisc(root, "Radiation Pulse " + i, center, new Color(0.72f, 1f, 0.2f, 0.22f), 0.7f + i * 0.34f, 99 + i);
                ring.gameObject.AddComponent<BattleVfxFade>().Configure(1.5f + i * 0.18f, 1.6f, -30f);
            }
        }

        private void CreateTimeSlowVisual(Transform root, Vector3 center, List<Vector3> affectedPositions)
        {
            var targets = GetVisualTargets(affectedPositions, center);
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

            shieldCooldown = 50f;
            shieldTimer = 4f;
            playerShield = ShieldAbsorbByAge[ageIndex];
            status = "\u62a4\u76fe\u5c4f\u969c\u542f\u52a8\uff0c\u5438\u6536 " + Mathf.CeilToInt(playerShield) + " \u70b9\u4f24\u5bb3\u3002";
        }

        private void UseMobilization()
        {
            if (gameOver || mobilizationCooldown > 0f)
            {
                return;
            }

            mobilizationCooldown = 60f;
            mobilizationTimer = 8f;
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
            var oldEnemyBaseMaxHealth = enemyBaseMaxHealth;
            ageIndex++;
            evolutionPath = path;
            eraValue = 0f;
            incomePerSecond = GameSession.IncomePerSecond + ageIndex * 2f + CountBuiltResourceWells(playerResourceWells) * ResourceWellIncomeBonus;
            playerBaseMaxHealth = BaseHealthByAge[ageIndex];
            enemyBaseMaxHealth = BaseHealthByAge[ageIndex];
            var healRatio = path == EvolutionPath.Defense ? 0.75f : 0.55f;
            playerBaseHealth = Mathf.Min(playerBaseMaxHealth, playerBaseHealth + (playerBaseMaxHealth - oldBaseMaxHealth) * healRatio);
            enemyBaseHealth = Mathf.Min(enemyBaseMaxHealth, enemyBaseHealth + (enemyBaseMaxHealth - oldEnemyBaseMaxHealth) * 0.45f);

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

            activeRouteCandidate = null;
            ClearRoutePreviews();
            BuildDefinitions();
            RefreshMapVisuals();
            RefreshBaseVisuals();
            RefreshExistingTowers();
            UpdateUnitButtonDefinitions();
            SwitchEraAmbience(ageIndex, false);
            var pathName = path == EvolutionPath.Attack ? AttackPathNames[ageIndex - 1] : DefensePathNames[ageIndex - 1];
            status = "\u9009\u62e9\u300c" + pathName + "\u300d\uff0c\u8fdb\u5165" + AgeNames[ageIndex] + "\u3002";
        }

        private void RefreshExistingTowers()
        {
            for (var i = 0; i < playerTowers.Length; i++)
            {
                if (playerTowers[i] != null)
                {
                    playerTowers[i].RefreshVisuals(currentTowerDefinition, towerFrames);
                }
            }

            for (var i = 0; i < enemyTowers.Length; i++)
            {
                if (enemyTowers[i] != null)
                {
                    enemyTowers[i].RefreshVisuals(currentTowerDefinition, towerFrames);
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

            var pressure = Mathf.Clamp01(elapsedTime / 150f);
            enemySpawnTimer = Mathf.Lerp(4.7f, 2.35f, pressure) * enemySpawnIntervalScale;
        }

        private UnitDefinition ChooseEnemyDefinition()
        {
            var roll = UnityEngine.Random.value;
            if (elapsedTime > 75f && roll > 0.76f && enemyUnitDefinitions.Length > 2)
            {
                return enemyUnitDefinitions[2];
            }

            if (elapsedTime > 45f && roll > 0.58f && enemyUnitDefinitions.Length > 1)
            {
                return enemyUnitDefinitions[1];
            }

            return roll < 0.52f ? enemyUnitDefinitions[0] : enemyUnitDefinitions[1];
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
            activeRouteCandidate = null;
            activeBuildPlacement = BuildPlacementKind.None;
            ClearRoutePreviews();
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
                    + "    \u65f6\u4ee3 " + AgeNames[ageIndex]
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
                ageText.text = "\u65f6\u4ee3 " + AgeNames[ageIndex] + "  " + GetEvolutionPathLabel();
            }

            if (eraText != null)
            {
                eraText.text = ageIndex >= AgeNames.Length - 1
                    ? "\u65f6\u4ee3\u503c \u5df2\u6ee1\u7ea7"
                    : "\u65f6\u4ee3\u503c " + Mathf.FloorToInt(eraValue) + " / " + GetCurrentEraThreshold();
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
                enemyHealthText.text = Mathf.CeilToInt(enemyBaseHealth) + " / " + Mathf.CeilToInt(enemyBaseMaxHealth);
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
                    laneText.text = "\u5efa\u9020" + GetBuildPlacementName(activeBuildPlacement) + "\u4e2d\n\u70b9\u51fb\u7ea2\u8272\u53ef\u5efa\u9020\u4f4d\nEsc \u53d6\u6d88\u672c\u6b21\u5efa\u9020";
                }
                else if (activeRouteCandidate != null)
                {
                    laneText.text = "\u8def\u7ebf\u5df2\u9501\u5b9a\n\u70b9\u51fb\u4e0b\u65b9\u5175\u79cd\u8fde\u7eed\u6d3e\u5175\nEsc \u53d6\u6d88\u5f53\u524d\u8def\u7ebf";
                }
                else
                {
                    laneText.text = "\u5148\u70b9\u51fb\u5730\u56fe\u9053\u8def\u9009\u5b9a\u884c\u519b\u76ee\u6807\n\u518d\u70b9\u51fb\u5175\u79cd\u751f\u6210\u58eb\u5175\n\u58eb\u5175\u4f1a\u6cbf\u9053\u8def\u524d\u8fdb";
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
                if (binding.Label != null)
                {
                    binding.Label.text = FormatUnitButtonLabel(binding.Definition);
                }

                binding.Button.interactable = gameStarted && !gameOver && coins >= GetUnitCost(binding.Definition);
                if (activeRouteCandidate != null)
                {
                    SetButtonColor(binding.Button, new Color(0.72f, 0.48f, 0.18f, 1f));
                }
                else
                {
                    SetButtonColor(binding.Button, new Color(0.43f, 0.31f, 0.17f, 1f));
                }
            }

            if (towerButton != null)
            {
                SetButtonLabel(towerButton, FormatTowerButtonLabel());
                towerButton.interactable = gameStarted
                    && !gameOver
                    && currentTowerDefinition != null
                    && coins >= currentTowerDefinition.Cost
                    && HasAvailableBuildSlot(BuildPlacementKind.Tower);
                SetButtonColor(towerButton, activeBuildPlacement == BuildPlacementKind.Tower
                    ? new Color(0.78f, 0.16f, 0.1f, 1f)
                    : new Color(0.31f, 0.43f, 0.45f, 1f));
            }

            if (resourceWellButton != null)
            {
                SetButtonLabel(resourceWellButton, FormatResourceWellButtonLabel());
                resourceWellButton.interactable = gameStarted
                    && !gameOver
                    && coins >= ResourceWellCost
                    && HasAvailableBuildSlot(BuildPlacementKind.ResourceWell);
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
                SetButtonLabel(attackUpgradeButton, ageIndex < AgeNames.Length - 1 ? "\u8fdb\u653b\u8fdb\u5316\n" + AttackPathNames[ageIndex] : "\u8fdb\u653b\u8fdb\u5316");
                attackUpgradeButton.interactable = canUpgrade;
            }

            if (defenseUpgradeButton != null)
            {
                SetButtonLabel(defenseUpgradeButton, ageIndex < AgeNames.Length - 1 ? "\u9632\u5b88\u8fdb\u5316\n" + DefensePathNames[ageIndex] : "\u9632\u5b88\u8fdb\u5316");
                defenseUpgradeButton.interactable = canUpgrade;
            }

            if (restartButton != null)
            {
                restartButton.interactable = true;
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

        private static string GetButtonGlyph(string name)
        {
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

        internal static Sprite SharedWhiteSprite => WhiteSprite;

        private static Sprite PanelSprite
        {
            get
            {
                if (panelSprite == null)
                {
                    panelSprite = CreateBeveledUiSprite(96, 64, 10, 0.72f, 1f);
                }

                return panelSprite;
            }
        }

        private static Sprite ButtonSprite
        {
            get
            {
                if (buttonSprite == null)
                {
                    buttonSprite = CreateBeveledUiSprite(120, 48, 14, 0.62f, 1f);
                }

                return buttonSprite;
            }
        }

        private static Sprite IconDiscSprite
        {
            get
            {
                if (iconDiscSprite == null)
                {
                    iconDiscSprite = CreateDiscUiSprite(48);
                }

                return iconDiscSprite;
            }
        }

        private static Sprite VfxCircleSprite
        {
            get
            {
                if (vfxCircleSprite == null)
                {
                    vfxCircleSprite = CreateSoftCircleSprite(96);
                }

                return vfxCircleSprite;
            }
        }

        private static Sprite ResourceWellSiteSprite
        {
            get
            {
                if (resourceWellSiteSprite == null)
                {
                    resourceWellSiteSprite = CreateResourceWellSiteSprite(96);
                }

                return resourceWellSiteSprite;
            }
        }

        private static Sprite ResourceWellBuiltSprite
        {
            get
            {
                if (resourceWellBuiltSprite == null)
                {
                    resourceWellBuiltSprite = CreateResourceWellBuiltSprite(128);
                }

                return resourceWellBuiltSprite;
            }
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

        private static Sprite CreateBeveledUiSprite(int width, int height, int cornerRadius, float baseShade, float alpha)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;

            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    if (!IsInsideRoundedRect(x, y, width, height, cornerRadius))
                    {
                        texture.SetPixel(x, y, Color.clear);
                        continue;
                    }

                    var left = x / (float)(width - 1);
                    var top = y / (float)(height - 1);
                    var edge = Mathf.Min(Mathf.Min(x, width - 1 - x), Mathf.Min(y, height - 1 - y));
                    var bevel = edge < 3f ? -0.24f : edge < 8f ? 0.18f : 0f;
                    var diagonalLight = Mathf.Lerp(0.12f, -0.1f, (left + (1f - top)) * 0.5f);
                    var shade = Mathf.Clamp01(baseShade + bevel + diagonalLight);
                    texture.SetPixel(x, y, new Color(shade, shade, shade, alpha));
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
                new Vector4(cornerRadius, cornerRadius, cornerRadius, cornerRadius));
        }

        private static Sprite CreateDiscUiSprite(int size)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;

            var center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
            var radius = size * 0.48f;
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var distance = Vector2.Distance(new Vector2(x, y), center);
                    if (distance > radius)
                    {
                        texture.SetPixel(x, y, Color.clear);
                        continue;
                    }

                    var ring = distance > radius - 4f ? 0.48f : 0f;
                    var highlight = y > center.y ? 0.16f : -0.06f;
                    var shade = Mathf.Clamp01(0.7f + ring + highlight);
                    texture.SetPixel(x, y, new Color(shade, shade, shade, 1f));
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f));
        }

        private static Sprite CreateSoftCircleSprite(int size)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;

            var center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
            var radius = size * 0.48f;
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var distance = Vector2.Distance(new Vector2(x, y), center);
                    if (distance > radius)
                    {
                        texture.SetPixel(x, y, Color.clear);
                        continue;
                    }

                    var normalized = Mathf.Clamp01(distance / radius);
                    var alpha = Mathf.SmoothStep(0.85f, 0.05f, normalized);
                    if (normalized > 0.72f)
                    {
                        alpha = Mathf.Max(alpha, Mathf.SmoothStep(1f, 0.72f, normalized) * 0.75f);
                    }

                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        private static Sprite CreateResourceWellSiteSprite(int size)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;

            var center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
            var half = size * 0.5f;
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var dx = (x - center.x) / half;
                    var dy = (y - center.y) / half;
                    var distance = Mathf.Sqrt(dx * dx + dy * dy);
                    var diamond = Mathf.Abs(dx) + Mathf.Abs(dy);
                    var alpha = 0f;

                    if (distance > 0.36f && distance < 0.46f)
                    {
                        alpha = Mathf.Max(alpha, 0.82f);
                    }

                    if (diamond > 0.62f && diamond < 0.72f)
                    {
                        alpha = Mathf.Max(alpha, 0.62f);
                    }

                    if (Mathf.Abs(dx) < 0.045f && Mathf.Abs(dy) < 0.32f)
                    {
                        alpha = Mathf.Max(alpha, 0.7f);
                    }

                    if (Mathf.Abs(dy) < 0.045f && Mathf.Abs(dx) < 0.32f)
                    {
                        alpha = Mathf.Max(alpha, 0.7f);
                    }

                    if (distance < 0.12f)
                    {
                        alpha = Mathf.Max(alpha, 0.9f);
                    }

                    texture.SetPixel(x, y, alpha > 0f ? new Color(1f, 1f, 1f, alpha) : Color.clear);
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        private static Sprite CreateResourceWellBuiltSprite(int size)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;

            var center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.45f);
            var half = size * 0.5f;
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var dx = (x - center.x) / half;
                    var dy = (y - center.y) / half;
                    var body = dx * dx / 0.48f + dy * dy / 0.24f;
                    var inner = dx * dx / 0.26f + dy * dy / 0.1f;
                    var post = Mathf.Abs(dx) > 0.3f && Mathf.Abs(dx) < 0.42f && dy > -0.06f && dy < 0.48f;
                    var roof = Mathf.Abs(dy - 0.5f) < 0.08f && Mathf.Abs(dx) < 0.5f - Mathf.Abs(dy - 0.5f);

                    if (inner <= 1f)
                    {
                        var shimmer = 0.08f * Mathf.Sin((x + y) * 0.18f);
                        texture.SetPixel(x, y, new Color(0.16f + shimmer, 0.54f + shimmer, 0.78f + shimmer, 0.96f));
                    }
                    else if (body <= 1f || post || roof)
                    {
                        var shade = Mathf.Clamp01(0.58f + 0.12f * Mathf.Sin((x * 3f + y * 5f) * 0.05f) - Mathf.Abs(dy) * 0.1f);
                        texture.SetPixel(x, y, new Color(shade, shade * 0.92f, shade * 0.76f, 1f));
                    }
                    else if (body <= 1.18f)
                    {
                        texture.SetPixel(x, y, new Color(0.08f, 0.06f, 0.04f, 0.36f));
                    }
                    else
                    {
                        texture.SetPixel(x, y, Color.clear);
                    }
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        private static bool IsInsideRoundedRect(int x, int y, int width, int height, int radius)
        {
            var cornerX = x < radius ? radius : width - 1 - radius;
            var cornerY = y < radius ? radius : height - 1 - radius;
            if ((x >= radius && x < width - radius) || (y >= radius && y < height - radius))
            {
                return true;
            }

            return Vector2.Distance(new Vector2(x, y), new Vector2(cornerX, cornerY)) <= radius;
        }

        private sealed class UnitButtonBinding
        {
            public UnitButtonBinding(Button button, Text label, UnitDefinition definition)
            {
                Button = button;
                Label = label;
                Definition = definition;
            }

            public Button Button { get; }
            public Text Label { get; }
            public UnitDefinition Definition { get; set; }
        }

        private sealed class AgeVisualSet
        {
            public AgeVisualSet(string key, string mapSpritePath, string baseSpritePath, string unitRoot, string[] unitFrameFolders, string towerFramePrefix, Color fallbackTint)
            {
                Key = key;
                MapSpritePath = mapSpritePath;
                BaseSpritePath = baseSpritePath;
                UnitRoot = unitRoot;
                UnitFrameFolders = unitFrameFolders;
                TowerFramePrefix = towerFramePrefix;
                FallbackTint = fallbackTint;
            }

            public string Key { get; }
            public string MapSpritePath { get; }
            public string BaseSpritePath { get; }
            public string UnitRoot { get; }
            public string[] UnitFrameFolders { get; }
            public string TowerFramePrefix { get; }
            public Color FallbackTint { get; }
        }

        private readonly struct RouteEdge
        {
            public RouteEdge(int a, int b)
            {
                A = a;
                B = b;
            }

            public int A { get; }
            public int B { get; }
        }

        private readonly struct RouteTarget
        {
            public RouteTarget(int edgeA, int edgeB, Vector3 position)
            {
                EdgeA = edgeA;
                EdgeB = edgeB;
                Position = position;
            }

            public int EdgeA { get; }
            public int EdgeB { get; }
            public Vector3 Position { get; }
        }

        private sealed class RouteCandidate
        {
            public RouteCandidate(List<Vector3> points, float cost, int laneIndex)
            {
                Points = points;
                Cost = cost;
                LaneIndex = laneIndex;
            }

            public List<Vector3> Points { get; }
            public float Cost { get; }
            public int LaneIndex { get; }
        }

        private sealed class RoutePreview
        {
            public RoutePreview(GameObject root, RouteCandidate candidate)
            {
                Root = root;
                Candidate = candidate;
            }

            public GameObject Root { get; }
            public RouteCandidate Candidate { get; }
        }

        private sealed class BuildPlacementPreview
        {
            public BuildPlacementPreview(GameObject root, int slotIndex)
            {
                Root = root;
                SlotIndex = slotIndex;
            }

            public GameObject Root { get; }
            public int SlotIndex { get; }
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
            Sprite[] attackFrames,
            Color tint)
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
            Tint = tint;
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
        public Color Tint { get; }
    }

    public sealed class BattleTimedDestroy : MonoBehaviour
    {
        private float remaining = 1f;

        public void Configure(float duration)
        {
            remaining = Mathf.Max(0.05f, duration);
        }

        private void Update()
        {
            remaining -= Time.deltaTime;
            if (remaining <= 0f)
            {
                Destroy(gameObject);
            }
        }
    }

    public sealed class BattleVfxFade : MonoBehaviour
    {
        private SpriteRenderer spriteRenderer;
        private LineRenderer lineRenderer;
        private Vector3 initialScale;
        private Color initialSpriteColor;
        private Color initialLineStartColor;
        private Color initialLineEndColor;
        private float initialLineWidth;
        private float duration = 1f;
        private float elapsed;
        private float expandRate;
        private float rotationSpeed;

        public void Configure(float effectDuration, float scaleExpansion, float degreesPerSecond)
        {
            duration = Mathf.Max(0.05f, effectDuration);
            expandRate = scaleExpansion;
            rotationSpeed = degreesPerSecond;
            spriteRenderer = GetComponent<SpriteRenderer>();
            lineRenderer = GetComponent<LineRenderer>();
            initialScale = transform.localScale;

            if (spriteRenderer != null)
            {
                initialSpriteColor = spriteRenderer.color;
            }

            if (lineRenderer != null)
            {
                initialLineStartColor = lineRenderer.startColor;
                initialLineEndColor = lineRenderer.endColor;
                initialLineWidth = lineRenderer.widthMultiplier;
            }
        }

        private void Update()
        {
            elapsed += Time.deltaTime;
            var t = Mathf.Clamp01(elapsed / duration);
            var alpha = 1f - t;

            if (expandRate != 0f)
            {
                transform.localScale = initialScale * (1f + expandRate * t);
            }

            if (rotationSpeed != 0f)
            {
                transform.Rotate(0f, 0f, rotationSpeed * Time.deltaTime);
            }

            if (spriteRenderer != null)
            {
                var color = initialSpriteColor;
                color.a *= alpha;
                spriteRenderer.color = color;
            }

            if (lineRenderer != null)
            {
                var start = initialLineStartColor;
                var end = initialLineEndColor;
                start.a *= alpha;
                end.a *= alpha;
                lineRenderer.startColor = start;
                lineRenderer.endColor = end;
                lineRenderer.widthMultiplier = initialLineWidth * Mathf.Lerp(1f, 0.45f, t);
            }

            if (elapsed >= duration)
            {
                Destroy(gameObject);
            }
        }
    }

    public sealed class BattleBuildPromptPulse : MonoBehaviour
    {
        private SpriteRenderer spriteRenderer;
        private Vector3 baseScale;
        private Color baseColor;
        private float minScale = 0.85f;
        private float maxScale = 1.15f;
        private float pulseSpeed = 2.4f;

        public void Configure(float minimumScale, float maximumScale, float speed)
        {
            minScale = minimumScale;
            maxScale = maximumScale;
            pulseSpeed = speed;
            baseScale = transform.localScale;
            spriteRenderer = GetComponent<SpriteRenderer>();
            baseColor = spriteRenderer != null ? spriteRenderer.color : Color.white;
        }

        private void Update()
        {
            if (baseScale == Vector3.zero)
            {
                baseScale = transform.localScale;
            }

            var wave = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f;
            transform.localScale = baseScale * Mathf.Lerp(minScale, maxScale, wave);

            if (spriteRenderer != null)
            {
                var color = baseColor;
                color.a = baseColor.a * Mathf.Lerp(0.68f, 1f, wave);
                spriteRenderer.color = color;
            }
        }
    }

    public sealed class TowerDefinition
    {
        public TowerDefinition(string displayName, int cost, float damage, float attackInterval, float range, Color tint)
        {
            DisplayName = displayName;
            Cost = cost;
            Damage = damage;
            AttackInterval = attackInterval;
            Range = range;
            Tint = tint;
        }

        public string DisplayName { get; }
        public int Cost { get; }
        public float Damage { get; }
        public float AttackInterval { get; }
        public float Range { get; }
        public Color Tint { get; }
    }

    public sealed class AgePowerDefinition
    {
        public AgePowerDefinition(string displayName, float cooldown, float damage, bool isGlobal, float statusDuration, float speedMultiplier, float attackIntervalMultiplier)
        {
            DisplayName = displayName;
            Cooldown = cooldown;
            Damage = damage;
            IsGlobal = isGlobal;
            StatusDuration = statusDuration;
            SpeedMultiplier = speedMultiplier;
            AttackIntervalMultiplier = attackIntervalMultiplier;
        }

        public string DisplayName { get; }
        public float Cooldown { get; }
        public float Damage { get; }
        public bool IsGlobal { get; }
        public float StatusDuration { get; }
        public float SpeedMultiplier { get; }
        public float AttackIntervalMultiplier { get; }
    }

    public sealed class BattleUnit : MonoBehaviour
    {
        private const float AttackImpulseDuration = 0.18f;
        private const float HitReactionDuration = 0.22f;

        private BattleGameController controller;
        private Transform visualRoot;
        private SpriteRenderer spriteRenderer;
        private SpriteRenderer shadowRenderer;
        private Vector3[] routePoints;
        private Sprite holdSprite;
        private float health;
        private float damage;
        private float speed;
        private float attackTimer;
        private float frameTimer;
        private float hitFlash;
        private float attackImpulseTimer;
        private float hitReactionTimer;
        private float attackImpulseDirection;
        private float hitReactionDirection;
        private float statusTimer;
        private float statusSpeedMultiplier = 1f;
        private float statusAttackIntervalMultiplier = 1f;
        private int frameIndex;
        private int routeTargetIndex;
        private bool attacking;
        private bool stopAtRouteEnd;
        private bool reachedHoldPoint;

        public UnitDefinition Definition { get; private set; }
        public int Team { get; private set; }
        public int LaneIndex { get; private set; }
        public bool IsAlive => health > 0f;

        public void Configure(
            BattleGameController owner,
            UnitDefinition definition,
            int team,
            int laneIndex,
            Vector3 position,
            float healthMultiplier,
            float damageMultiplier,
            float speedMultiplier,
            Vector3[] customRoute = null,
            bool stopWhenRouteEnds = false)
        {
            controller = owner;
            Definition = definition;
            Team = team;
            LaneIndex = laneIndex;
            health = definition.MaxHealth * healthMultiplier;
            damage = definition.Damage * damageMultiplier;
            speed = definition.Speed * speedMultiplier;
            var usesCustomRoute = customRoute != null && customRoute.Length > 0;
            routePoints = usesCustomRoute ? customRoute : owner.GetLaneRoute(laneIndex);
            routeTargetIndex = team == 0 ? 1 : routePoints.Length - 2;
            stopAtRouteEnd = stopWhenRouteEnds;

            transform.position = usesCustomRoute ? routePoints[0] : position;
            transform.localScale = Vector3.one * definition.Scale * owner.UnitVisualScale;

            CreateGroundShadow();

            var visualObject = new GameObject("Unit Visual", typeof(SpriteRenderer));
            visualObject.transform.SetParent(transform, false);
            visualRoot = visualObject.transform;

            spriteRenderer = visualObject.GetComponent<SpriteRenderer>();
            spriteRenderer.sprite = definition.MoveFrames[0];
            holdSprite = spriteRenderer.sprite;
            spriteRenderer.flipX = team == 1;
            spriteRenderer.color = GetBaseTint();
            UpdateGroundShadow();
            UpdateSorting();
        }

        private void Update()
        {
            if (controller == null || Definition == null || !IsAlive || controller.IsGameOver)
            {
                return;
            }

            attackTimer -= Time.deltaTime;
            hitFlash = Mathf.Max(0f, hitFlash - Time.deltaTime);
            attackImpulseTimer = Mathf.Max(0f, attackImpulseTimer - Time.deltaTime);
            hitReactionTimer = Mathf.Max(0f, hitReactionTimer - Time.deltaTime);
            UpdateStatusEffect();
            attacking = false;

            var target = controller.FindNearestEnemy(this);
            if (target != null && Vector2.Distance(target.transform.position, transform.position) <= BattleGameController.GetUnitEngageDistance(Definition))
            {
                attacking = true;
                TryAttackUnit(target);
            }
            else if (reachedHoldPoint)
            {
                attacking = false;
                HoldPosition();
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
            UpdateVisualPose();
            UpdateSorting();
        }

        public void TakeDamage(float amount, int attackerTeam)
        {
            if (!IsAlive)
            {
                return;
            }

            health -= amount;
            hitFlash = 0.22f;
            hitReactionTimer = HitReactionDuration;
            hitReactionDirection = attackerTeam == 0 ? 1f : -1f;

            if (health <= 0f)
            {
                controller.NotifyUnitKilled(this, attackerTeam);
                Destroy(gameObject);
            }
        }

        public void ApplyStatusEffect(float duration, float speedMultiplier, float attackIntervalMultiplier)
        {
            if (!IsAlive || duration <= 0f)
            {
                return;
            }

            statusTimer = Mathf.Max(statusTimer, duration);
            statusSpeedMultiplier = Mathf.Min(statusSpeedMultiplier, speedMultiplier);
            statusAttackIntervalMultiplier = Mathf.Max(statusAttackIntervalMultiplier, attackIntervalMultiplier);
            hitFlash = Mathf.Max(hitFlash, 0.18f);
        }

        private void UpdateStatusEffect()
        {
            if (statusTimer <= 0f)
            {
                return;
            }

            statusTimer = Mathf.Max(0f, statusTimer - Time.deltaTime);
            if (statusTimer <= 0f)
            {
                statusSpeedMultiplier = 1f;
                statusAttackIntervalMultiplier = 1f;
            }
        }

        private void TryAttackUnit(BattleUnit target)
        {
            if (attackTimer > 0f)
            {
                return;
            }

            attackImpulseTimer = AttackImpulseDuration;
            attackImpulseDirection = Team == 0 ? 1f : -1f;
            var hitPosition = Vector3.Lerp(transform.position, target.transform.position, 0.58f);
            controller.SpawnCombatHitEffect(hitPosition, Team, Definition.AttackRange > 1.3f);
            target.TakeDamage(damage, Team);
            attackTimer = Definition.AttackInterval * statusAttackIntervalMultiplier;
        }

        private void TryAttackBase()
        {
            if (attackTimer > 0f)
            {
                return;
            }

            attackImpulseTimer = AttackImpulseDuration;
            attackImpulseDirection = Team == 0 ? 1f : -1f;
            var basePosition = controller.GetBasePosition(Team == 0 ? 1 : 0);
            controller.SpawnCombatHitEffect(new Vector3(basePosition.x, transform.position.y, 0f), Team, Definition.AttackRange > 1.3f);
            controller.DamageBase(Team == 0 ? 1 : 0, damage);
            attackTimer = Definition.AttackInterval * statusAttackIntervalMultiplier;
        }

        private void MoveForward()
        {
            if (routePoints == null || routePoints.Length == 0 || routeTargetIndex < 0 || routeTargetIndex >= routePoints.Length)
            {
                var direction = Team == 0 ? 1f : -1f;
                transform.position += new Vector3(direction * speed * statusSpeedMultiplier * Time.deltaTime, 0f, 0f);
                return;
            }

            var target = routePoints[routeTargetIndex];
            transform.position = Vector3.MoveTowards(transform.position, target, speed * statusSpeedMultiplier * Time.deltaTime);

            if (Vector3.Distance(transform.position, target) <= 0.025f)
            {
                routeTargetIndex += Team == 0 ? 1 : -1;
                if (stopAtRouteEnd && (routeTargetIndex >= routePoints.Length || routeTargetIndex < 0))
                {
                    reachedHoldPoint = true;
                    holdSprite = spriteRenderer != null ? spriteRenderer.sprite : holdSprite;
                }
            }
        }

        private void HoldPosition()
        {
            if (spriteRenderer != null && holdSprite != null)
            {
                spriteRenderer.sprite = holdSprite;
            }
        }

        private bool IsAtEnemyBase()
        {
            if (stopAtRouteEnd)
            {
                return false;
            }

            if (routePoints != null && routePoints.Length > 0)
            {
                return Team == 0 ? routeTargetIndex >= routePoints.Length : routeTargetIndex < 0;
            }

            var targetBase = controller.GetBasePosition(Team == 0 ? 1 : 0);
            return Team == 0
                ? transform.position.x >= targetBase.x - 0.55f
                : transform.position.x <= targetBase.x + 0.55f;
        }

        private void UpdateAnimation()
        {
            var frames = attacking && Definition.AttackFrames.Length > 0 ? Definition.AttackFrames : Definition.MoveFrames;
            if (reachedHoldPoint && !attacking)
            {
                HoldPosition();
                return;
            }

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

        private void UpdateVisualPose()
        {
            if (visualRoot == null)
            {
                return;
            }

            var parentScale = Mathf.Max(0.01f, transform.localScale.x);
            var localOffset = Vector3.zero;
            var rotation = 0f;

            if (attackImpulseTimer > 0f)
            {
                var pulse = Mathf.Sin((attackImpulseTimer / AttackImpulseDuration) * Mathf.PI);
                localOffset.x += attackImpulseDirection * pulse * 0.12f / parentScale;
                rotation += attackImpulseDirection * pulse * -3.5f;
            }

            if (hitReactionTimer > 0f)
            {
                var pulse = Mathf.Sin((hitReactionTimer / HitReactionDuration) * Mathf.PI);
                localOffset.x += hitReactionDirection * pulse * 0.08f / parentScale;
                localOffset.y += pulse * 0.03f / parentScale;
                rotation += hitReactionDirection * pulse * 4.5f;
            }

            visualRoot.localPosition = localOffset;
            visualRoot.localRotation = Quaternion.Euler(0f, 0f, rotation);
        }

        private Color GetBaseTint()
        {
            var tint = Definition != null ? Definition.Tint : Color.white;
            if (Team == 1)
            {
                tint = Color.Lerp(tint, new Color(1f, 0.5f, 0.42f, 1f), 0.35f);
            }

            if (statusTimer > 0f)
            {
                tint = Color.Lerp(tint, new Color(0.55f, 0.9f, 1f, 1f), 0.25f);
            }

            return tint;
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
        private TowerDefinition definition;
        private float attackTimer;
        private float frameTimer;
        private int laneIndex;
        private int team;
        private int frameIndex;

        public void Configure(BattleGameController owner, int lane, int towerTeam, TowerDefinition towerDefinition, Sprite[] towerFrames)
        {
            controller = owner;
            laneIndex = lane;
            team = towerTeam;
            definition = towerDefinition;
            frames = towerFrames;

            transform.localScale = Vector3.one * owner.TowerVisualScale;
            CreateGroundShadow();

            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = frames != null && frames.Length > 0 ? frames[0] : null;
            spriteRenderer.color = definition != null ? definition.Tint : new Color(1f, 0.94f, 0.78f, 1f);
            spriteRenderer.flipX = team == 1;
            if (team == 1)
            {
                spriteRenderer.color = Color.Lerp(spriteRenderer.color, new Color(1f, 0.42f, 0.32f, 1f), 0.38f);
            }

            UpdateGroundShadow();
            UpdateSorting();
        }

        public void RefreshVisuals(TowerDefinition towerDefinition, Sprite[] towerFrames)
        {
            definition = towerDefinition;
            frames = towerFrames;
            frameIndex = 0;
            frameTimer = 0f;

            if (spriteRenderer == null)
            {
                return;
            }

            spriteRenderer.sprite = frames != null && frames.Length > 0 ? frames[0] : null;
            spriteRenderer.color = definition != null ? definition.Tint : new Color(1f, 0.94f, 0.78f, 1f);
            spriteRenderer.flipX = team == 1;
            if (team == 1)
            {
                spriteRenderer.color = Color.Lerp(spriteRenderer.color, new Color(1f, 0.42f, 0.32f, 1f), 0.38f);
            }

            UpdateGroundShadow();
            UpdateSorting();
        }

        private void Update()
        {
            if (controller == null || controller.IsGameOver)
            {
                return;
            }

            attackTimer -= Time.deltaTime;
            Animate();

            if (attackTimer > 0f)
            {
                return;
            }

            var range = definition != null ? definition.Range : 3.4f;
            var target = controller.FindTowerTarget(team, laneIndex, transform.position, range);
            if (target == null)
            {
                return;
            }

            var damage = definition != null ? definition.Damage : 34f;
            var interval = definition != null ? definition.AttackInterval : 1.05f;
            var multiplier = team == 0 ? controller.TowerDamageMultiplier : 1f;
            controller.SpawnCombatHitEffect(target.transform.position, team, true);
            target.TakeDamage(damage * multiplier, team);
            attackTimer = interval;
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
