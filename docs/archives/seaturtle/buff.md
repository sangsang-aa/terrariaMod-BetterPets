# 海龟宠物增益 · Pet Turtle

> 改写原版增益 `BuffID.PetTurtle` 的说明文字，使其与改写后的攻击能力一致。属于 [海龟对象](README.md)。

## 基本信息

| 项 | 值 |
|---|---|
| 原版常量 | `BuffID.PetTurtle` |
| 实现文件 | `Content/Pets/SeaTurtle/SeaTurtleGlobalBuff.cs` |
| 类型 | `GlobalBuff` |

## 机制

- `SetStaticDefaults`：注册阶段用 `Mod.GetLocalization($"{nameof(SeaTurtleGlobalBuff)}.Description")` 读取本地化文本。
- `ModifyBuffText(int type, ref string buffName, ref string tip, ref int rare)`：构造增益提示时调用；
  当 `type == BuffID.PetTurtle` 时把 `tip` 替换为本地化文本。三个参数都是 `ref`，可原地替换。
  只改说明文字，**不改增益名称、不改增益效果**。

## 本地化 key

| 语言 | key | 文本 |
|---|---|---|
| 简体中文 | `Mods.TutorialMod.SeaTurtleGlobalBuff.Description` | 它会把自己发射出去。？核弹？ |
| 英文 | `Mods.TutorialMod.SeaTurtleGlobalBuff.Description` | 见 `en-US_Mods.TutorialMod.hjson` |

> 文本写在 `Localization/*.hjson`，不在代码里硬编码。

## 相关

- [GlobalBuff API](https://docs.tmodloader.net/docs/1.4-stable/class_terraria_1_1_mod_loader_1_1_global_buff.html)
- [Localization 指南](https://github.com/tModLoader/tModLoader/wiki/Localization)
