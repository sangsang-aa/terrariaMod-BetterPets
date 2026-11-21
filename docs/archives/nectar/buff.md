# 宠物增益「蜜蜂宝宝」

> 召唤后玩家获得的增益，复用原版 `BuffID.BabyHornet`。属于 [花蜜蜜蜂对象](README.md)。

## 基本信息

| 项 | 值 |
|---|---|
| 原版常量 | `BuffID.BabyHornet`（ID 51） |
| 实现文件 | `Content/Pets/Nectar/NectarGlobalBuff.cs` |
| 类型 | `GlobalBuff` |

## 实现逻辑

- `ModifyBuffText` 里按 `type == BuffID.BabyHornet` 替换增益说明的 `tip`。
- 只改 `tip`，名称与稀有度保持原版；**不改增益逻辑**——原版 `BuffID.BabyHornet` 的 `Update` 负责生成/维持宠物。
- 文本取自本地化文件（`NectarGlobalBuff.Description`，hjson 根级），未硬编码。

## 相关（本对象内）

- 对象总览：[README.md](README.md)
- 召唤物品：[summon-item.md](summon-item.md)
- 宠物本体：[projectile.md](projectile.md)
