# 宠物增益「企鹅宝宝」

> 企鹅宝宝召唤后玩家获得的增益，复用原版 `BuffID.BabyPenguin`。属于 [企鹅宝宝对象](README.md)。

## 基本信息

| 项 | 值 |
|---|---|
| 原版常量 | `BuffID.BabyPenguin` |
| ID | 41 |
| 实现文件 | `Content/Pets/BabyPenguin/BabyPenguinGlobalBuff.cs` |
| 类型 | `GlobalBuff` |

## 实现逻辑

- `ModifyBuffText` 里按 `type == BuffID.BabyPenguin` 替换增益说明的 `tip`。
- 只改文本，不改增益效果（增益本身由原版处理）。
- 文本取自本地化文件，未硬编码。

## 相关（本对象内）

- 对象总览：[README.md](README.md)
- 召唤物品：[summon-item.md](summon-item.md)
- 宠物本体：[projectile.md](projectile.md)
- 本地化：`Localization/zh-Hans_Mods.TutorialMod.hjson`
