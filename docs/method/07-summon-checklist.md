# 召唤类实现铁律（来自 tmod-modding skill）

召唤类（宠物 / 随从 / 鞭子）要实现完整，不能只做一半：

- 鞭子索敌 + tag 加成（`MinionShot` + `SummonTagDamageMultiplier`）
- 饰品 / 药水 / 栏位加成（`GetTotalDamage` / `GetKnockback` 用 `DamageClass.Summon`）

## 关键坑

自定义攻击弹幕要登记为 `MinionShot`，否则鞭子标记加成不生效。

## 实例

本项目的 `PenguinFishSwing` / `ThrownFish` 都登记了 `MinionShot = true` + `SummonTagDamageMultiplier = 1f`，见 [archives/babypenguin/fish-swing.md](../archives/babypenguin/fish-swing.md)。
