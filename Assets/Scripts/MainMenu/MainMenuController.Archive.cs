using UnityEngine;
using UnityEngine.UI;

namespace WarOfEras.MainMenu
{
    public sealed partial class MainMenuController
    {
        private enum ArchiveCategory
        {
            Units,
            Facilities
        }

        private const float ResourceWellCost = 120f;
        private const float ResourceWellHealth = 360f;
        private const float ResourceWellIncomeBonus = 2.5f;
        private const float ResourceWellEraValue = 65f;

        private static readonly string[] FallbackUnitFolders =
        {
            "Hunter",
            "Thrower",
            "BoneArcher",
            "TuskRider",
            "Champion"
        };

        private static readonly string[] FallbackTowerFolders =
        {
            "BoneTower",
            "SlingNest",
            "MammothTotem"
        };

        private static readonly ArchiveEraData[] ArchiveEras =
        {
            new ArchiveEraData(
                "蛮荒部落",
                new[]
                {
                    Unit("石棒战士", "Barbarian", "Hunter", 0, 15, 60f, 16f, 1f, 0.55f, 1f, "低价近战主力，适合前期顶住上中下路压力。"),
                    Unit("投石猎手", "Barbarian", "Thrower", 1, 22, 48f, 9f, 0.9f, 2.35f, 0.95f, "基础远程单位，可躲在前排后方持续消耗。"),
                    Unit("骨弓猎手", "Barbarian", "BoneArcher", 2, 35, 46f, 12f, 0.95f, 3.05f, 0.82f, "射程最长的蛮荒兵，适合点杀慢速目标。"),
                    Unit("獠牙骑手", "Barbarian", "TuskRider", 3, 65, 120f, 24f, 1.35f, 0.72f, 0.92f, "高速突进单位，用来压线和追击残血敌人。"),
                    Unit("巨骨勇士", "Barbarian", "Champion", 4, 110, 180f, 42f, 0.7f, 0.95f, 1.35f, "高生命高伤害前排，推进敌方基地的核心。"),
                    Builder("筑营工兵", "Barbarian", 45, 82f, 6f, 1.05f, "负责修建防御塔和占领资源点，也会修复附近受损设施。")
                },
                new[]
                {
                    Base("蛮荒部落基地", "Barbarian", 1200, "玩家左侧与敌方右侧各一座，基地生命归零决定胜负。"),
                    Tower("骨石塔", "Barbarian", "BoneTower", 0, 90, 12f, 0.75f, 3.5f, "便宜且攻速快，适合早期补足路口火力。"),
                    Tower("投石巢", "Barbarian", "SlingNest", 1, 140, 24f, 1.15f, 4.25f, "射程更稳的中档防御塔，适合保护关键道路。"),
                    Tower("猛犸图腾", "Barbarian", "MammothTotem", 2, 230, 58f, 2.35f, 4.75f, "重击型设施，单发伤害高，用来压制厚血单位。"),
                    ResourceWell()
                }),
            new ArchiveEraData(
                "机械工坊",
                new[]
                {
                    Unit("齿轮兵", "Machine", "GearSoldier", 0, 50, 115f, 30f, 1.05f, 0.58f, 0.95f, "机械时代基础前排，费用适中且承伤稳定。"),
                    Unit("蒸汽弩手", "Machine", "SteamCrossbow", 1, 75, 90f, 14f, 0.95f, 2.75f, 0.8f, "远程连射单位，适合跟随前排持续输出。"),
                    Unit("锅炉掷弹兵", "Machine", "BoilerGrenadier", 2, 110, 125f, 38f, 0.88f, 2.45f, 1.15f, "中距离爆发单位，适合打散密集敌人。"),
                    Unit("铁轮破城车", "Machine", "SiegeRoller", 3, 350, 320f, 72f, 0.72f, 1.1f, 1.45f, "攻城型厚血单位，推进速度慢但压制力强。"),
                    Unit("发条卫士", "Machine", "ClockworkGuard", 4, 520, 430f, 86f, 0.82f, 0.72f, 1.05f, "重装护卫，适合作为主力兵团的前线核心。"),
                    Builder("齿轮工兵", "Machine", 150, 190f, 18f, 1f, "机械化施工单位，能在设施点建塔或占领资源点。")
                },
                new[]
                {
                    Base("机械工坊基地", "Machine", 2200, "升级后基地生命大幅提升，能承受更长时间的拉锯。"),
                    Tower("齿轮抛炮塔", "Machine", "GearTower", 0, 460, 40f, 1.75f, 4f, "机械基础炮塔，适合覆盖敌军必经节点。"),
                    Tower("蒸汽加农塔", "Machine", "SteamCannonTower", 1, 650, 68f, 1.45f, 4.4f, "更高伤害和射程的主力防御塔。"),
                    Tower("铆钉迫击塔", "Machine", "RivetMortar", 2, 900, 140f, 2.6f, 5f, "慢速重炮，适合打击高生命目标。"),
                    ResourceWell()
                }),
            new ArchiveEraData(
                "电力时代",
                new[]
                {
                    Unit("电击兵", "Electric", "VoltGuard", 0, 180, 230f, 82f, 1.12f, 0.6f, 0.95f, "电力时代基础战士，攻防都比机械兵更强。"),
                    Unit("电弧疾行者", "Electric", "ArcRunner", 1, 230, 180f, 70f, 1.55f, 0.56f, 0.82f, "高速近战单位，可快速支援薄弱路线。"),
                    Unit("线圈射手", "Electric", "CoilShooter", 2, 380, 180f, 32f, 1f, 3f, 0.62f, "高射程高频远程输出，适合后排持续压制。"),
                    Unit("履带战车", "Electric", "CrawlerTank", 3, 820, 720f, 150f, 0.65f, 1.55f, 1.5f, "重型装甲单位，用来吸收塔火和开路。"),
                    Unit("雷霆机甲", "Electric", "ThunderMech", 4, 1150, 620f, 178f, 0.78f, 2.2f, 1.25f, "高伤中远程机甲，适合突破敌方防线。"),
                    Builder("电焊工兵", "Electric", 440, 360f, 42f, 1.08f, "电力施工单位，能让中后期防线快速成型。")
                },
                new[]
                {
                    Base("电力时代基地", "Electric", 4200, "进入电力时代后，基地耐久与时代技能强度同步提升。"),
                    Tower("特斯拉塔", "Electric", "TeslaTower", 0, 1350, 34f, 1.75f, 5f, "远射程电击塔，适合提前削弱推进队伍。"),
                    Tower("电弧塔", "Electric", "ArcPylon", 1, 1800, 48f, 1.2f, 4.2f, "高频输出设施，适合清理中等生命敌军。"),
                    Tower("轨道炮塔", "Electric", "RailgunTower", 2, 2600, 210f, 2.85f, 5.4f, "重型穿透火力，适合压制坦克和机甲。"),
                    ResourceWell()
                }),
            new ArchiveEraData(
                "核能纪元",
                new[]
                {
                    Unit("辐射步兵", "Nuclear", "RadTrooper", 0, 1300, 420f, 120f, 1.2f, 0.62f, 0.82f, "核能基础兵，单兵质量显著提升。"),
                    Unit("同位素侦察兵", "Nuclear", "IsotopeScout", 1, 1600, 360f, 108f, 1.48f, 0.6f, 0.76f, "高速突击角色，用来抢路线与追击。"),
                    Unit("裂变枪兵", "Nuclear", "FissionLancer", 2, 2200, 360f, 46f, 1.05f, 3.08f, 0.46f, "远程高频火力，适合在重装后方输出。"),
                    Unit("反应堆行者", "Nuclear", "ReactorWalker", 3, 4200, 820f, 210f, 0.82f, 2.1f, 1.18f, "重装远程单位，能承担攻坚主力。"),
                    Unit("核能坦克", "Nuclear", "NuclearTank", 4, 7000, 1500f, 320f, 0.6f, 1.8f, 1.64f, "超重型推进单位，慢速但生命和伤害极高。"),
                    Builder("堆芯工兵", "Nuclear", 1450, 720f, 86f, 1.12f, "核能施工单位，能维护高价值防御塔和资源点。")
                },
                new[]
                {
                    Base("核能纪元基地", "Nuclear", 7800, "核能阶段基地耐久高，适合拉长战线等待大招冷却。"),
                    Tower("粒子机枪塔", "Nuclear", "ParticleGunTower", 0, 6200, 70f, 1f, 5f, "高频粒子火力，适合持续切割敌军队列。"),
                    Tower("反应堆迫击塔", "Nuclear", "ReactorMortar", 1, 7600, 145f, 1.9f, 5.5f, "高伤中慢速炮塔，适合守关键设施点。"),
                    Tower("辐尘尖塔", "Nuclear", "FalloutObelisk", 2, 10500, 270f, 2.7f, 5.8f, "核能重型设施，适合对抗后期厚血单位。"),
                    ResourceWell()
                }),
            new ArchiveEraData(
                "星海文明",
                new[]
                {
                    Unit("激光兵", "Starsea", "LaserTrooper", 0, 4800, 1000f, 260f, 1.3f, 0.75f, 0.8f, "星海基础兵，数值全面超越前代单位。"),
                    Unit("光子刀锋", "Starsea", "PhotonBlade", 1, 5600, 820f, 240f, 1.58f, 0.6f, 0.72f, "极速近战精锐，适合快速撕开防线。"),
                    Unit("浮游机甲", "Starsea", "SkimmerMech", 2, 6800, 820f, 95f, 1.15f, 3.25f, 0.38f, "远程高频单位，适合提供稳定后排火力。"),
                    Unit("重力无人机", "Starsea", "GravityDrone", 3, 11000, 1450f, 420f, 0.9f, 2.35f, 1.08f, "高伤中远程单位，适合清除重型敌军。"),
                    Unit("反物质巨像", "Starsea", "AntimatterColossus", 4, 20000, 3200f, 720f, 0.5f, 2.1f, 1.8f, "终局攻城单位，费用极高但能决定战线走向。"),
                    Builder("星港工兵", "Starsea", 5200, 1500f, 170f, 1.18f, "星海施工单位，可维护终局设施并抢占资源点。")
                },
                new[]
                {
                    Base("星海文明基地", "Starsea", 14000, "终局基地生命最高，配合星盾和防御进化极难被快速攻破。"),
                    Tower("钛晶射线塔", "Starsea", "TitaniumRayTower", 0, 22000, 100f, 1f, 4f, "稳定射线火力，适合持续压低敌军血线。"),
                    Tower("等离子尖塔", "Starsea", "PlasmaSpire", 1, 26000, 160f, 0.78f, 4.6f, "高频高伤终局设施，适合守住核心路口。"),
                    Tower("奇点信标", "Starsea", "SingularityBeacon", 2, 34000, 520f, 3f, 6f, "超远程重击设施，适合终局反推和清场。"),
                    ResourceWell()
                })
        };

        private int selectedArchiveEraIndex;
        private ArchiveCategory selectedArchiveCategory = ArchiveCategory.Units;

        private void ShowDetailedTutorialScreen()
        {
            ClearContent();
            SetContentBounds(0.08f, 0.12f, 0.92f, 0.76f);

            CreateScreenTitle("玩法教程");
            CreateTopBackButton();

            CreateTutorialCard(
                0,
                "胜负目标",
                "你控制左侧基地，敌军从右侧推进。保护我方基地并摧毁敌方基地即可胜利；任一方基地生命归零，战斗立刻结算。");
            CreateTutorialCard(
                1,
                "资源循环",
                "金币会随时间增长，简单/中等/困难分别影响初始金币、收入和敌军强度。训练兵种、建造防御塔、占领资源点都会消耗金币。");
            CreateTutorialCard(
                2,
                "出兵与路线",
                "先点击上路/中路/下路切换默认出兵路线，再点击底部兵种按钮训练单位。新兵会沿当前路线推进，遇敌后自动攻击。");
            CreateTutorialCard(
                3,
                "单兵指挥",
                "左键点击单个士兵，或按住左键拖拽框选多个士兵；随后点击道路附近位置下达行军目标。右键可解除持续管控。");
            CreateTutorialCard(
                4,
                "建造设施",
                "先训练并选中建筑兵，再点击防御塔或资源点按钮，最后点击场上的可建造点。建筑兵必须走到施工范围内才会开工，也会自动修复附近受损设施。");
            CreateTutorialCard(
                5,
                "时代与技能",
                "时代值会随时间、出兵、建塔和占领资源点积累。进度满后选择攻击进化或防御进化进入下一时代；大招、护盾屏障和战争动员都有冷却时间。");

            var controls = CreateStyledPanel("Tutorial Controls Summary", contentRoot, new Color(0.04f, 0.07f, 0.11f, 0.9f));
            controls.anchorMin = new Vector2(0.18f, 0.02f);
            controls.anchorMax = new Vector2(0.82f, 0.115f);
            controls.offsetMin = Vector2.zero;
            controls.offsetMax = Vector2.zero;

            var controlsText = CreateText(
                controls,
                "Controls Summary",
                "视角：右键拖动画面，滚轮缩放，鼠标靠近战场边缘自动平移。开局：进入战斗场景后点击地图开始。Esc：取消当前选择或施工指令。",
                16,
                FontStyle.Bold,
                TextAnchor.MiddleCenter);
            controlsText.color = new Color(0.84f, 0.94f, 1f, 1f);
            controlsText.resizeTextForBestFit = true;
            controlsText.resizeTextMinSize = 12;
            controlsText.resizeTextMaxSize = 16;
            controlsText.rectTransform.anchorMin = new Vector2(0.04f, 0.08f);
            controlsText.rectTransform.anchorMax = new Vector2(0.96f, 0.92f);
            controlsText.rectTransform.offsetMin = Vector2.zero;
            controlsText.rectTransform.offsetMax = Vector2.zero;
        }

        private void ShowArchiveBrowser()
        {
            ClearContent();
            SetContentBounds(0.06f, 0.11f, 0.94f, 0.78f);

            selectedArchiveEraIndex = Mathf.Clamp(selectedArchiveEraIndex, 0, ArchiveEras.Length - 1);
            CreateScreenTitle("兵工图鉴");
            CreateTopBackButton();
            CreateArchiveEraButtons();
            CreateArchiveCategoryButtons();

            var era = ArchiveEras[selectedArchiveEraIndex];
            var summary = CreateText(
                contentRoot,
                "Archive Summary",
                selectedArchiveCategory == ArchiveCategory.Units
                    ? era.Name + "兵种：战斗角色负责推线与攻城，建筑兵负责施工和修复。"
                    : era.Name + "设施：基地决定胜负，防御塔压制路线，资源点提升经济和时代值。",
                17,
                FontStyle.Bold,
                TextAnchor.MiddleCenter);
            summary.color = new Color(0.84f, 0.94f, 1f, 1f);
            summary.resizeTextForBestFit = true;
            summary.resizeTextMinSize = 12;
            summary.resizeTextMaxSize = 17;
            summary.rectTransform.anchorMin = new Vector2(0.2f, 0.595f);
            summary.rectTransform.anchorMax = new Vector2(0.8f, 0.655f);
            summary.rectTransform.offsetMin = Vector2.zero;
            summary.rectTransform.offsetMax = Vector2.zero;

            var entries = selectedArchiveCategory == ArchiveCategory.Units ? era.Units : era.Facilities;
            for (var i = 0; i < entries.Length; i++)
            {
                CreateArchiveEntryCard(entries[i], i, entries.Length);
            }
        }

        private void CreateArchiveEraButtons()
        {
            for (var i = 0; i < ArchiveEras.Length; i++)
            {
                var eraIndex = i;
                var selected = eraIndex == selectedArchiveEraIndex;
                var button = CreateButton(
                    contentRoot,
                    "Archive Era " + eraIndex,
                    ArchiveEras[eraIndex].Name,
                    () =>
                    {
                        selectedArchiveEraIndex = eraIndex;
                        ShowArchiveBrowser();
                    },
                    selected ? SelectedButtonTint : ButtonNormalTint,
                    17,
                    false,
                    false);
                var rect = button.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.1f + eraIndex * 0.2f, 0.75f);
                rect.anchorMax = rect.anchorMin;
                rect.sizeDelta = new Vector2(178f, 42f);
                rect.anchoredPosition = Vector2.zero;
                SetButtonColor(button, selected ? SelectedButtonTint : ButtonNormalTint);
            }
        }

        private void CreateArchiveCategoryButtons()
        {
            CreateArchiveCategoryButton(ArchiveCategory.Units, "兵种", new Vector2(0.43f, 0.68f));
            CreateArchiveCategoryButton(ArchiveCategory.Facilities, "设施", new Vector2(0.57f, 0.68f));
        }

        private void CreateArchiveCategoryButton(ArchiveCategory category, string label, Vector2 anchor)
        {
            var selected = selectedArchiveCategory == category;
            var button = CreateButton(
                contentRoot,
                "Archive Category " + label,
                label,
                () =>
                {
                    selectedArchiveCategory = category;
                    ShowArchiveBrowser();
                },
                selected ? SelectedButtonTint : ButtonNormalTint,
                19,
                false,
                false);
            var rect = button.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.sizeDelta = new Vector2(150f, 42f);
            rect.anchoredPosition = Vector2.zero;
            SetButtonColor(button, selected ? SelectedButtonTint : ButtonNormalTint);
        }

        private void CreateTutorialCard(int index, string titleValue, string detail)
        {
            var column = index % 3;
            var row = index / 3;
            var cardWidth = 0.29f;
            var cardHeight = 0.285f;
            var gapX = 0.035f;
            var gapY = 0.055f;
            var xMin = 0.035f + column * (cardWidth + gapX);
            var yMax = 0.79f - row * (cardHeight + gapY);

            var card = CreateStyledPanel("Tutorial Card " + index, contentRoot, new Color(0.04f, 0.07f, 0.11f, 0.9f));
            card.anchorMin = new Vector2(xMin, yMax - cardHeight);
            card.anchorMax = new Vector2(xMin + cardWidth, yMax);
            card.offsetMin = Vector2.zero;
            card.offsetMax = Vector2.zero;

            var title = CreateText(card, "Tutorial Card Title", titleValue, 21, FontStyle.Bold, TextAnchor.MiddleCenter);
            title.color = TextCyan;
            title.resizeTextForBestFit = true;
            title.resizeTextMinSize = 14;
            title.resizeTextMaxSize = 21;
            title.rectTransform.anchorMin = new Vector2(0.08f, 0.72f);
            title.rectTransform.anchorMax = new Vector2(0.92f, 0.93f);
            title.rectTransform.offsetMin = Vector2.zero;
            title.rectTransform.offsetMax = Vector2.zero;

            var body = CreateText(card, "Tutorial Card Body", detail, 16, FontStyle.Bold, TextAnchor.UpperLeft);
            body.color = new Color(0.84f, 0.93f, 1f, 1f);
            body.resizeTextForBestFit = true;
            body.resizeTextMinSize = 11;
            body.resizeTextMaxSize = 16;
            body.rectTransform.anchorMin = new Vector2(0.08f, 0.09f);
            body.rectTransform.anchorMax = new Vector2(0.92f, 0.69f);
            body.rectTransform.offsetMin = Vector2.zero;
            body.rectTransform.offsetMax = Vector2.zero;
        }

        private void CreateArchiveEntryCard(ArchiveEntry entry, int index, int total)
        {
            var columns = 3;
            var column = index % columns;
            var row = index / columns;
            var cardWidth = 0.30f;
            var cardHeight = total <= 3 ? 0.31f : 0.225f;
            var gapX = 0.022f;
            var gapY = 0.035f;
            var xMin = 0.028f + column * (cardWidth + gapX);
            var yMax = 0.57f - row * (cardHeight + gapY);

            var card = CreateStyledPanel("Archive Entry " + index, contentRoot, new Color(0.04f, 0.07f, 0.11f, 0.92f));
            card.anchorMin = new Vector2(xMin, yMax - cardHeight);
            card.anchorMax = new Vector2(xMin + cardWidth, yMax);
            card.offsetMin = Vector2.zero;
            card.offsetMax = Vector2.zero;

            var artBackplate = CreatePanel("Archive Entry Art Backplate", card, new Color(0.02f, 0.035f, 0.055f, 0.78f));
            artBackplate.anchorMin = new Vector2(0.045f, 0.19f);
            artBackplate.anchorMax = new Vector2(0.29f, 0.81f);
            artBackplate.offsetMin = Vector2.zero;
            artBackplate.offsetMax = Vector2.zero;

            var art = artBackplate.GetComponent<Image>();
            art.sprite = string.IsNullOrEmpty(entry.FallbackSpritePath)
                ? LoadResourceSprite(entry.SpritePath)
                : LoadResourceSprite(entry.SpritePath, entry.FallbackSpritePath);
            art.preserveAspect = true;
            art.color = Color.white;
            art.raycastTarget = false;

            var name = CreateText(card, "Archive Entry Name", entry.Name, 18, FontStyle.Bold, TextAnchor.MiddleLeft);
            name.color = TextCyan;
            name.resizeTextForBestFit = true;
            name.resizeTextMinSize = 12;
            name.resizeTextMaxSize = 18;
            name.rectTransform.anchorMin = new Vector2(0.33f, 0.74f);
            name.rectTransform.anchorMax = new Vector2(0.96f, 0.94f);
            name.rectTransform.offsetMin = Vector2.zero;
            name.rectTransform.offsetMax = Vector2.zero;

            var detail = CreateText(card, "Archive Entry Detail", entry.Detail, 13, FontStyle.Bold, TextAnchor.UpperLeft);
            detail.color = new Color(0.84f, 0.93f, 1f, 1f);
            detail.resizeTextForBestFit = true;
            detail.resizeTextMinSize = 9;
            detail.resizeTextMaxSize = 13;
            detail.rectTransform.anchorMin = new Vector2(0.33f, 0.09f);
            detail.rectTransform.anchorMax = new Vector2(0.96f, 0.73f);
            detail.rectTransform.offsetMin = Vector2.zero;
            detail.rectTransform.offsetMax = Vector2.zero;
        }

        private void CreateTopBackButton()
        {
            var button = CreateButton(contentRoot, "Top Back Button", "返回", ShowHomeScreen, ButtonNormalTint, 18, false, false);
            var rect = button.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.02f, 0.875f);
            rect.anchorMax = new Vector2(0.13f, 0.965f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static ArchiveEntry Unit(
            string name,
            string eraKey,
            string folder,
            int visualSlot,
            int cost,
            float health,
            float damage,
            float speed,
            float range,
            float interval,
            string note)
        {
            var fallbackFolder = FallbackUnitFolders[Mathf.Clamp(visualSlot, 0, FallbackUnitFolders.Length - 1)];
            return new ArchiveEntry(
                name,
                eraKey + "/Units/" + folder + "/move_01",
                "Barbarian/Units/" + fallbackFolder + "/move_01",
                "类型：战斗士兵\n消耗：" + cost + " 金币  生命：" + FormatArchiveNumber(health)
                + "\n伤害：" + FormatArchiveNumber(damage) + "  射程：" + FormatArchiveNumber(range) + "  间隔：" + FormatArchiveNumber(interval) + "s"
                + "\n速度：" + FormatArchiveNumber(speed) + "\n定位：" + note);
        }

        private static ArchiveEntry Builder(string name, string eraKey, int cost, float health, float damage, float speed, string note)
        {
            return new ArchiveEntry(
                name,
                eraKey + "/Units/Builder/move_01",
                "Barbarian/Units/Builder/move_01",
                "类型：建筑兵\n消耗：" + cost + " 金币  生命：" + FormatArchiveNumber(health)
                + "\n伤害：" + FormatArchiveNumber(damage) + "  射程：0.58  间隔：1.15s"
                + "\n速度：" + FormatArchiveNumber(speed) + "\n能力：" + note);
        }

        private static ArchiveEntry Base(string name, string eraKey, int health, string note)
        {
            return new ArchiveEntry(
                name,
                eraKey + "/Base/Base",
                "Barbarian/Base/Base",
                "类型：核心设施\n消耗：无  生命：" + health
                + "\n位置：玩家左侧 / 敌军右侧"
                + "\n规则：基地被摧毁即判负或判胜。"
                + "\n说明：" + note);
        }

        private static ArchiveEntry Tower(
            string name,
            string eraKey,
            string folder,
            int visualSlot,
            int cost,
            float damage,
            float interval,
            float range,
            string note)
        {
            var fallbackFolder = FallbackTowerFolders[Mathf.Clamp(visualSlot, 0, FallbackTowerFolders.Length - 1)];
            var health = Mathf.CeilToInt(220f + damage * 6f + range * 20f);
            return new ArchiveEntry(
                name,
                eraKey + "/Towers/" + folder + "/attack_01",
                "Barbarian/Towers/" + fallbackFolder + "/attack_01",
                "类型：防御塔\n消耗：" + cost + " 金币  生命：" + health
                + "\n伤害：" + FormatArchiveNumber(damage) + "  射程：" + FormatArchiveNumber(range) + "  间隔：" + FormatArchiveNumber(interval) + "s"
                + "\n施工：建筑兵前往设施点建造。"
                + "\n说明：" + note);
        }

        private static ArchiveEntry ResourceWell()
        {
            return new ArchiveEntry(
                "资源点",
                "Battle/Facilities/ResourceWellBuilt",
                string.Empty,
                "类型：资源设施\n消耗：" + FormatArchiveNumber(ResourceWellCost) + " 金币  生命：" + FormatArchiveNumber(ResourceWellHealth)
                + "\n收益：+" + FormatArchiveNumber(ResourceWellIncomeBonus) + " 金币/s  时代值：+" + FormatArchiveNumber(ResourceWellEraValue)
                + "\n规则：双方共享点位，占用后提升经济。"
                + "\n施工：建筑兵前往资源点完成占领。");
        }

        private static string FormatArchiveNumber(float value)
        {
            return value.ToString("0.##");
        }

        private sealed class ArchiveEraData
        {
            public ArchiveEraData(string name, ArchiveEntry[] units, ArchiveEntry[] facilities)
            {
                Name = name;
                Units = units;
                Facilities = facilities;
            }

            public string Name { get; }
            public ArchiveEntry[] Units { get; }
            public ArchiveEntry[] Facilities { get; }
        }

        private sealed class ArchiveEntry
        {
            public ArchiveEntry(string name, string spritePath, string fallbackSpritePath, string detail)
            {
                Name = name;
                SpritePath = spritePath;
                FallbackSpritePath = fallbackSpritePath;
                Detail = detail;
            }

            public string Name { get; }
            public string SpritePath { get; }
            public string FallbackSpritePath { get; }
            public string Detail { get; }
        }
    }
}
