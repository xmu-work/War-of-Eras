# War-of-Eras

《战线进化：纪元之战》课程项目。

这是一个 Unity 2D 俯视式多线路自动战斗策略游戏。玩家通过金币出兵、建造炮塔、争夺资源井、选择时代进化方向和释放技能来推进战线，最终摧毁敌方基地。

## 打开项目

- Unity 版本：`6000.3.15f1`
- 打开方式：用 Unity Hub 打开仓库根目录
- 主入口场景：[Assets/Scenes/Battle.unity](Assets/Scenes/Battle.unity)

## 当前开发框架

项目采用“核心框架 + 时代内容”的协作方式：

- 核心框架放在 `Assets/Scripts/Battle/`
- 数值与规则配置放在 `Assets/Data/`
- 单位、炮塔、基地、特效等预制体放在 `Assets/Prefabs/`
- 美术资源放在 `Assets/Art/`
- 旧的 Platformer 模板资源和脚本已清理，当前入口聚焦新的战线玩法框架

团队成员做不同时代内容时，尽量只改自己时代的 `Data`、`Prefabs` 和 `Art` 文件，少改核心脚本，减少合并冲突。

更详细的框架说明见：[开发框架说明.md](开发框架说明.md) 和 [项目结构说明.md](项目结构说明.md)。
