# 召唤物品「鱼」

> 企鹅宝宝的召唤物品，复用原版 `ItemID.Fish`。属于 [企鹅宝宝对象](README.md)。

## 基本信息

| 项 | 值 |
|---|---|
| 原版常量 | `ItemID.Fish` |
| ID | 669 |
| 实现文件 | `Content/Pets/BabyPenguin/BabyPenguinGlobalItem.cs` |
| 类型 | `GlobalItem` |

## 实现逻辑

- **锁定目标**：`AppliesToEntity` 只认 `ItemID.Fish`，不影响其他物品。
- **改文本不改属性**：`ModifyTooltips` 只替换 Tooltip 文字，不修改物品任何属性（召唤出的宠物仍是原版企鹅宝宝）。
- 文本取自本地化文件，未硬编码。

## 相关（本对象内）

- 对象总览：[README.md](README.md)
- 宠物本体：[projectile.md](projectile.md)
- 增益：[buff.md](buff.md)
- 本地化：`Localization/zh-Hans_Mods.TutorialMod.hjson`
