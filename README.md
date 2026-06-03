# 战线进化：纪元之战

《战线进化：纪元之战》是一个 Unity 2D 俯视角三路自动战斗策略项目。玩家通过出兵、派遣建筑兵、修建防御塔和资源点、释放时代技能、选择进攻或防守进化路线来推进战线，最终摧毁敌方基地获胜。

## 运行环境

- Unity 版本：`6000.3.15f1`
- 渲染管线：Universal Render Pipeline，包版本见 `Packages/manifest.json`
- 输入系统：Unity Input System，同时保留旧输入系统的条件编译兼容
- 推荐入口场景：`Assets/Scenes/MainMenu.unity`

在 Unity Hub 中打开仓库根目录后，先打开 `MainMenu` 场景运行。菜单会记录地图和难度选择，再进入 `Battle` 场景。

## 当前玩法

1. 进入主菜单，选择地图和难度。
2. 在战斗场景中点击道路附近切换默认出兵路线。
3. 点击兵种按钮消耗金币出兵，单位会沿当前路线自动推进。
4. 拖拽框选己方单位后，点击道路附近可以单独下达移动目标。
5. 点击防御塔或资源点按钮后，再点击地图上的可建造标记，会派出建筑兵前往施工。
6. 建筑兵到达设施点一定范围内后，才会真正完成防御塔或资源点建造。
7. 防御塔和资源点被攻击掉血后，附近建筑兵会自动修复友方设施。
8. 时代值达到门槛后，可以选择进攻进化或防守进化，进入下一个时代。
9. 摧毁敌方基地获胜，己方基地被摧毁则失败。

## 建筑兵设计

建筑兵是当前项目新增的非纯战斗单位，职责集中在设施系统上：

- 角色标记：`UnitRole.Builder`
- 任务类型：`BuilderTaskKind.Tower` 和 `BuilderTaskKind.ResourceWell`
- 建造流程：点击建造按钮 -> 选择设施槽位 -> 扣除金币并记录待建任务 -> 派出建筑兵 -> 到达施工范围 -> 生成设施
- 修复流程：建筑兵没有待建任务时，会检查附近受损友方设施并逐帧修复
- 视觉标识：建筑兵沿用当前时代兵种帧，并叠加小工具标识，方便在战场上识别

相关实现主要在：

- `Assets/Scripts/Battle/Core/BattleGameController.cs`
- `Assets/Scripts/Battle/Core/BattleRuntimeActors.cs`

## 项目结构

```text
War-of-Eras/
  Assets/
    Art/                 美术草图和候选资源
    Audio/               旧音频资源与外部音频素材
    Data/                预留的数据配置目录
    Editor/              编辑器辅助脚本
    Prefabs/             预留的 prefab 目录
    Rendering/           URP 和后处理配置
    Resources/           当前实际运行时加载的地图、单位、塔、基地、音频、菜单资源
    Scenes/              MainMenu 和 Battle 场景
    Scripts/
      Battle/Core/       战斗主逻辑、运行时单位、布局和会话配置
      MainMenu/          主菜单运行时 UI
    Settings/            Unity 设置资源
    TextMesh Pro/        TextMesh Pro 默认资源
  Packages/              Unity 包依赖
  ProjectSettings/       Unity 项目设置
```

当前战斗内容主要通过 `Resources.Load` 加载 `Assets/Resources` 下的资源。五个时代资源目录分别是：

- `Barbarian`：蛮荒部落
- `Machine`：机械工坊
- `Electric`：电力时代
- `Nuclear`：核能纪元
- `Starsea`：星海文明

每个时代目录下通常包含：

```text
Base/       基地图片
Units/      单位序列帧
Towers/     防御塔攻击帧
```

## 关键脚本

- `BattleGameController.cs`：战斗主控制器。负责战场生成、经济、时代升级、AI、建造、HUD、胜负结算。
- `BattleRuntimeActors.cs`：运行时单位和设施类。包含士兵移动攻击、建筑兵施工修复、防御塔攻击、资源点生命值。
- `BattleGameController.Sprites.cs`：程序化 UI 和设施标记 sprite，以及资源点默认图形。
- `BattleGameController.Types.cs`：战斗控制器内部绑定类型、路线目标、路线候选和建造预览结构。
- `BattleLayout.cs`：可选场景标记布局。如果场景中放置 Layout 标记点，战斗控制器会优先读取这些坐标。
- `GameSetup.cs`：主菜单与战斗之间共享的地图、难度和初始经济配置。
- `MainMenuController.cs`：运行时生成主菜单 UI，处理地图、难度、开始游戏和辅助页面。
- `MainMenuController.Helpers.cs`：主菜单程序化 sprite、字体和按钮 hover 效果。
- `AutoPlayFromMainMenu.cs`：编辑器辅助入口，用于从主菜单场景开始播放。
- `BattleArtImportSettings.cs`：编辑器辅助工具，用于批量修正蛮荒时代图片导入设置。

## 场景说明

- `Assets/Scenes/MainMenu.unity`：主菜单场景，挂载 `MainMenuController`。
- `Assets/Scenes/Battle.unity`：正式战斗场景，挂载 `BattleGameController`。

两个控制器都支持运行时自动补齐自身，因此空场景调试时也能尽量正常生成 UI 或战场对象。不过正式运行仍建议从 `MainMenu` 进入。

## 扩展指南

新增时代资源时，优先保持现有目录和命名方式：

- 地图：`Assets/Resources/Battle/Maps/PixelFrontline_时代名.png`
- 基地：`Assets/Resources/时代目录/Base/Base.png`
- 单位：`Assets/Resources/时代目录/Units/单位名/move_01.png` 这类序列帧
- 防御塔：`Assets/Resources/时代目录/Towers/塔名/attack_01.png` 这类攻击帧
- 音乐：`Assets/Resources/Audio/Ambience/编号_描述_loop.ogg`

新增兵种、防御塔或时代技能时，先检查 `BattleGameController.BuildDefinitions()` 中当前的定义方式，再补对应资源路径。现在项目仍以代码内定义为主，`Assets/Data` 更像后续 ScriptableObject 化的预留空间。

## 校验建议

日常改动后建议至少做三步检查：

1. 打开 Unity，确认没有 C# 编译错误。
2. 从 `MainMenu` 运行到 `Battle`，检查地图、UI、出兵、建造和时代升级是否正常。
3. 在命令行运行 `git diff --check`，确认没有明显格式错误。

本项目当前没有独立的自动化测试工程，完整验证仍以 Unity 编辑器内编译和试玩为准。
