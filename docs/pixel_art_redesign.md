# 像素风战场与时代美术重做说明

本轮继续沿用现有像素风，但把角色形态从单一的“三件套”扩展为更接近横版进化战争游戏的完整时代阵容。设计参考的是“原始近战/远程/重装一路进化到机械、电力、核能、星海”的形态节奏，不复刻具体商业素材。

## 地图结构

- 默认兼容地图：`Assets/Resources/Battle/Maps/PixelFrontlineThreeLanes.png`
- 每个时代都有同布局、不同主题的战场图：`Assets/Resources/Battle/Maps/PixelFrontline_*.png`
- 标注校准图：`Assets/Art/Generated/PixelRedesign/PixelFrontlineThreeLanes_annotated.png`
- 点位数据：`docs/pixel_frontline_layout.json`
- 结构规则：左右两个基地，上中下三条主路都连接双方基地，中段有 6 个支路节点供单位转线。

## 角色与设施

- 蛮荒部落：5 个兵种 `Hunter / Thrower / BoneArcher / TuskRider / Champion`；3 个炮塔 `BoneTower / SlingNest / MammothTotem`；地图 `PixelFrontline_Barbarian`。
- 机械工坊：5 个兵种 `GearSoldier / SteamCrossbow / BoilerGrenadier / SiegeRoller / ClockworkGuard`；3 个炮塔 `GearTower / SteamCannonTower / RivetMortar`；地图 `PixelFrontline_Machine`。
- 电力时代：5 个兵种 `VoltGuard / ArcRunner / CoilShooter / CrawlerTank / ThunderMech`；3 个炮塔 `TeslaTower / ArcPylon / RailgunTower`；地图 `PixelFrontline_Electric`。
- 核能纪元：5 个兵种 `RadTrooper / IsotopeScout / FissionLancer / ReactorWalker / NuclearTank`；3 个炮塔 `ParticleGunTower / ReactorMortar / FalloutObelisk`；地图 `PixelFrontline_Nuclear`。
- 星海文明：5 个兵种 `LaserTrooper / PhotonBlade / SkimmerMech / GravityDrone / AntimatterColossus`；3 个炮塔 `TitaniumRayTower / PlasmaSpire / SingularityBeacon`；地图 `PixelFrontline_Starsea`。

## 设计原则

- 兵种按“轻装、快速、远程、机械/载具、精英重装”形成五档选择，方便战斗节奏逐步升级。
- 炮塔按“基础速射、范围/连发、重炮/高伤”形成三档选择，避免单一塔贯穿全局。
- 地图不改变路线和点位，只通过地表色调、装饰物、能量线、污染标记、星海晶体等时代元素改变观感。
- 所有 PNG 使用 point filter、无 mipmap、Sprite 单图导入，保持清晰像素边缘。

## 比例

- `unitVisualScale`: 0.72
- `baseVisualScale`: 0.28
- `towerVisualScale`: 0.22
- `resourceWellVisualScale`: 0.22
