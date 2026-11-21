# 宠物增益「婴儿吞噬怪」

> 召唤后玩家获得的增益，复用原版 `BuffID.BabyEater`。属于 [婴儿吞噬怪对象](README.md)。

## 基本信息

| 项 | 值 |
|---|---|
| 原版常量 | `BuffID.BabyEater` |
| 实现文件 | `Content/Pets/BabyEater/BabyEaterGlobalBuff.cs` |
| 类型 | `GlobalBuff` |

## 实现逻辑

- `ModifyBuffText` 里按 `type == BuffID.BabyEater` 替换增益说明的 `tip`。
- 只改 `tip`，名称 `buffName` 与稀有度 `rare` 保持原版；不改增益效果本身。
- 文本取自本地化文件（`BabyEaterGlobalBuff.Description`），未硬编码。

## 相关（本对象内）

- 对象总览：[README.md](README.md)
- 召唤物品：[summon-item.md](summon-item.md)
- 宠物本体：[projectile.md](projectile.md)
