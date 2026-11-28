# 海龟召唤物品 · 海草

> 改写原版物品 `ItemID.Seaweed`（海草）的提示文本，使其与改写后的攻击能力一致。属于 [海龟对象](README.md)。

## 基本信息

| 项 | 值 |
|---|---|
| 原版常量 | `ItemID.Seaweed`（海草） |
| 实现文件 | `Content/Pets/SeaTurtle/SeaTurtleGlobalItem.cs` |
| 类型 | `GlobalItem` |

## 机制

用 `GlobalItem` 挂到所有物品上，再用 `AppliesToEntity` 精确锁定海草：

- `SetStaticDefaults`：注册阶段用 `Mod.GetLocalization($"{nameof(SeaTurtleGlobalItem)}.Tooltip")` 读取本地化文本。
- `AppliesToEntity(Item entity, bool lateInstantiation)`：`entity.type == ItemID.Seaweed` 才生效，其它物品不受影响。
- `ModifyTooltips(Item item, List<TooltipLine> tooltips)`：优先替换已有的 `Tooltip` 行，没有才新增一行。
  只改说明文字，**不改物品名称、不改物品属性**。

## 本地化 key

| 语言 | key | 文本 |
|---|---|---|
| 简体中文 | `Mods.TutorialMod.SeaTurtleGlobalItem.Tooltip` | 召唤宠物海龟 / 它会把自己发射出去（见 hjson 原文） |
| 英文 | `Mods.TutorialMod.SeaTurtleGlobalItem.Tooltip` | 见 `en-US_Mods.TutorialMod.hjson` |

> 文本写在 `Localization/*.hjson`，不在代码里硬编码。

## 相关

- [Localization 指南](https://github.com/tModLoader/tModLoader/wiki/Localization)
- [Basic Item 指南](https://github.com/tModLoader/tModLoader/wiki/Basic-Item)
