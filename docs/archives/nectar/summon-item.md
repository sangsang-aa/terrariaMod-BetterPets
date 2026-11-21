# 召唤物品「花蜜」

> 蜜蜂宝宝的召唤物品，复用原版 `ItemID.Nectar`。属于 [花蜜蜜蜂对象](README.md)。

## 基本信息

| 项 | 值 |
|---|---|
| 原版常量 | `ItemID.Nectar`（ID 1170，女王蜂掉落） |
| 实现文件 | `Content/Pets/Nectar/NectarGlobalItem.cs` |
| 类型 | `GlobalItem` |

## 实现逻辑

- **锁定目标**：`AppliesToEntity` 只认 `ItemID.Nectar`，不影响其他物品。
- **改文本不改属性**：`ModifyTooltips` 优先替换已有的 `Tooltip` 行，没有才新增；只改提示文字，不改 `shoot` / `buffType` 等属性。
- 原版花蜜 `Item.damage = 0`，伤害全部来自宠物生成的毒刺/蜂巢/蜜蜂。
- 文本取自本地化文件（`NectarGlobalItem.Tooltip`），位于 hjson **根级**（不是 `Items:` 下）。

## 相关（本对象内）

- 对象总览：[README.md](README.md)
- 增益：[buff.md](buff.md)
- 宠物本体：[projectile.md](projectile.md)
