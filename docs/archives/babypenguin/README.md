# 企鹅宝宝 · 对象档案

> 改写原版宠物「企鹅宝宝」，给它追加**召唤类攻击**（近战挥鱼 / 远程丢鱼）。
> **不新建任何内容**——召唤物、Buff、宠物本体全部复用原版，只通过 `Global*` 挂载追加逻辑。
> 企鹅宝宝这一个对象相关的全部部件（召唤物品 / 增益 / 本体 / 两个攻击弹幕）都收在本文件夹里。
> 「为什么这么改、踩过什么坑」见 [log/](../../log/README.md)，「这套架构能不能复用」见 [method/](../../method/README.md)。

## 本对象文件清单

```
babypenguin/
├── README.md        本文：对象总览 + 复用 ID + 源文件清单
├── summon-item.md   召唤物品「鱼」(ItemID.Fish 669)：GlobalItem 改 Tooltip
├── buff.md          宠物增益 (BuffID.BabyPenguin 41)：GlobalBuff 改说明
├── projectile.md    企鹅本体 (ProjectileID.Penguin 112)：调度器 + 三层状态机
├── fish-swing.md    挥鱼弹幕：近战挥砍 / 远程前摇共用
└── thrown-fish.md   投掷鱼弹幕：远程抛物线
```

## 复用的原版 ID

| 类型 | 原版常量 | ID | 档案 |
|---|---|---|---|
| 召唤物品 | `ItemID.Fish`（鱼） | 669 | [summon-item.md](summon-item.md) |
| 宠物增益 | `BuffID.BabyPenguin` | 41 | [buff.md](buff.md) |
| 宠物本体弹幕 | `ProjectileID.Penguin` | 112 | [projectile.md](projectile.md) |

挥鱼与投掷弹幕的贴图也直接复用 `Terraria/Images/Item_` + `ItemID.Fish`，无需新 png。
ID 对照表：https://github.com/tModLoader/tModLoader/wiki/Vanilla-Content-IDs

## 8 个源文件清单

均在 `Content/Pets/BabyPenguin/` 下：

| 文件 | 层 | 职责 | 档案 |
|---|---|---|---|
| `BabyPenguinGlobalItem.cs` | 文本 | `AppliesToEntity` 锁定「鱼」，`ModifyTooltips` 替换提示 | [summon-item.md](summon-item.md) |
| `BabyPenguinGlobalBuff.cs` | 文本 | `ModifyBuffText` 替换增益说明 | [buff.md](buff.md) |
| `BabyPenguinGlobalProjectile.cs` | 调度器 | `PostAI` 每帧只做「刷新 Context → 跑模式门 → 调动作包」 | [projectile.md](projectile.md) |
| `PenguinContext.cs` | 数据层 | 状态数据包 + 枚举，每企鹅一份 | [projectile.md](projectile.md) |
| `PenguinGates.cs` | 判断层 | 检测门：静态纯函数 + 所有判定阈值 | [projectile.md](projectile.md) |
| `PenguinActions.cs` | 行为层 | 5 个无状态动作 + 模式入口 + 弹幕生成工具 | [projectile.md](projectile.md) |
| `PenguinFishSwing.cs` | 弹幕 | 挥鱼动作，近战 / 远程前摇共用 | [fish-swing.md](fish-swing.md) |
| `ThrownFish.cs` | 弹幕 | 远程投掷，重力抛物线 | [thrown-fish.md](thrown-fish.md) |

配套系统：`Common/Systems/PetTargetMarkerSystem.cs` → [目标标记系统](../pettargetmarker/README.md)。

## 状态机与参数

企鹅本体的三层状态机、模式循环、完整参数表见 [projectile.md](projectile.md)。
攻击弹幕的数值见 [fish-swing.md](fish-swing.md) 与 [thrown-fish.md](thrown-fish.md)。
