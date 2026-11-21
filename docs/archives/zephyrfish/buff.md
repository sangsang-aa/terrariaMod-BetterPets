# 宠物增益「和风鱼」

> 召唤后玩家获得的增益，复用原版 `BuffID.ZephyrFish`。属于 [和风鱼对象](README.md)。

## 基本信息

| 项 | 值 |
|---|---|
| 原版常量 | `BuffID.ZephyrFish` |
| 实现文件 | `Content/Pets/ZephyrFish/ZephyrFishGlobalBuff.cs` |
| 类型 | `GlobalBuff` |

## 实现逻辑

- `ModifyBuffText` 里按 `type == BuffID.ZephyrFish` 替换增益说明的 `tip`。
- 只改 `tip`，名称与稀有度保持原版；不改增益效果本身。
- 文本取自本地化文件（`ZephyrFishGlobalBuff.Description`），未硬编码。

## 相关（本对象内）

- 对象总览：[README.md](README.md)
- 召唤物品：[summon-item.md](summon-item.md)
- 宠物本体：[ram-projectile.md](ram-projectile.md)
