# 召唤物品「和风鱼」

> 和风鱼的召唤物品，复用原版 `ItemID.ZephyrFish`。属于 [和风鱼对象](README.md)。

## 基本信息

| 项 | 值 |
|---|---|
| 原版常量 | `ItemID.ZephyrFish` |
| 实现文件 | `Content/Pets/ZephyrFish/ZephyrFishGlobalItem.cs` |
| 类型 | `GlobalItem` |

## 实现逻辑

- **锁定目标**：`AppliesToEntity` 只认 `ItemID.ZephyrFish`，不影响其他物品。
- **改文本不改属性**：`ModifyTooltips` 优先替换已有的 `Tooltip` 行，没有才新增；只改提示文字，不改物品属性、不改物品名称。
- 文本取自本地化文件（`ZephyrFishGlobalItem.Tooltip`），未硬编码。

## 相关（本对象内）

- 对象总览：[README.md](README.md)
- 增益：[buff.md](buff.md)
- 宠物本体：[ram-projectile.md](ram-projectile.md)
