# 目标标记系统 · PetTargetMarkerSystem

> 全局系统：玩家用右键 / 鞭子标记目标时，在目标头顶补一圈金色脉冲尘，强化「已标记」的视觉反馈。
> 这是一个跨宠物的独立对象（全局系统），档案单独成夹，与四只宠物对象平级。

## 基本信息

| 项 | 值 |
|---|---|
| 类名 | `PetTargetMarkerSystem` |
| 文件 | `Common/Systems/PetTargetMarkerSystem.cs` |
| 类型 | `ModSystem`（全局系统） |

## 机制（PostUpdateEverything）

- **只在本地客户端**：跳过专用服务器 / 主菜单 / 玩家无效或死亡。
- **识别四种本模组改写的宠物在场**（`HasTutorialPet`，见下表）才显示，避免影响纯原版/其它模组。
- 玩家标记目标后 `player.MinionAttackTargetNPC` 指向存活、可追击的敌人，系统每 8 帧在其头顶补一圈金色脉冲尘（`DustID.GemTopaz`）。
- 呼吸式半径（13±2px）+ 缓慢旋转，8 点组成「锁定圈」；尘不落、不参与光照。
- 纯视觉反馈，不参与索敌判定；真正的索敌优先逻辑在各宠物的 `OwnerMinionAttackTargetNPC` 读取处。

## 识别的四种宠物

`HasTutorialPet` 检查玩家弹幕里是否有以下四种本项目改写的宠物本体（缺一则标记尘不显示）：

| 宠物 | 本体弹幕 ID | 对象档案 |
|---|---|---|
| 和风鱼 | `ProjectileID.ZephyrFish`（380） | [zephyrfish](../zephyrfish/README.md) |
| 婴儿吞噬怪 | `ProjectileID.BabyEater`（175） | [babyeater](../babyeater/README.md) |
| 企鹅宝宝 | `ProjectileID.Penguin`（112） | [babypenguin](../babypenguin/README.md) |
| 蜜蜂宝宝（花蜜） | `ProjectileID.BabyHornet` | [nectar](../nectar/README.md) |

> 注：只覆盖这四只**改写原版**的宠物；新建的 [ExamplePet](../examplepet/README.md) 不在此列。

## 相关

- 目标锁定方法：[method/02](../../method/02-target-lock.md)
- 企鹅宝宝索敌中的检测门 `ResolveTarget`：`archives/babypenguin/projectile.md`
