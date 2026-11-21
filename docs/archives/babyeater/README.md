# 婴儿吞噬怪 · 对象档案

> 改写原版宠物「婴儿吞噬怪」：平时发射腐化追踪弹，主人低血量时追加高速冲撞的「小吞噬怪」。
> **不新建召唤物品 / 增益 / 宠物本体**——三者全部复用原版，只通过 `Global*` 挂载攻击逻辑。
> 两个攻击弹幕（`CorruptEaterShot` / `BabyEaterMinion`）是新增内容，但贴图复用原版、无需新 png。
> 「为什么这么改、踩过什么坑」见 [log/](../../log/README.md)，「这套架构能不能复用」见 [method/](../../method/README.md)。

## 本对象文件清单

```
babyeater/
├── README.md           本文：对象总览 + 复用 ID + 源文件清单
├── summon-item.md      召唤物品「吞噬怪骨头」(ItemID.EatersBone)
├── buff.md             宠物增益 (BuffID.BabyEater)
├── projectile.md       宠物本体：平时腐化弹 + 低血冲撞（GlobalProjectile）
├── corrupt-shot.md     CorruptEaterShot：平时腐化追踪弹
└── enraged-minion.md   BabyEaterMinion：低血量冲撞弹
```

## 复用的原版 ID

| 类型 | 原版常量 | 档案 |
|---|---|---|
| 召唤物品 | `ItemID.EatersBone`（吞噬怪骨头） | [summon-item.md](summon-item.md) |
| 宠物增益 | `BuffID.BabyEater` | [buff.md](buff.md) |
| 宠物本体弹幕 | `ProjectileID.BabyEater` | [projectile.md](projectile.md) |
| 腐化弹贴图 | `ProjectileID.TinyEater`（小吞噬怪） | [corrupt-shot.md](corrupt-shot.md) |
| 冲撞弹贴图 | `ProjectileID.BabyEater`（复用本体贴图） | [enraged-minion.md](enraged-minion.md) |

ID 对照表：https://github.com/tModLoader/tModLoader/wiki/Vanilla-Content-IDs

## 5 个源文件清单

均在 `Content/Pets/BabyEater/` 下：

| 文件 | 层 | 职责 | 档案 |
|---|---|---|---|
| `BabyEaterGlobalItem.cs` | 文本 | `AppliesToEntity` 锁定「吞噬怪骨头」，`ModifyTooltips` 改提示 | [summon-item.md](summon-item.md) |
| `BabyEaterGlobalBuff.cs` | 文本 | `ModifyBuffText` 改增益说明 | [buff.md](buff.md) |
| `BabyEaterGlobalProjectile.cs` | 攻击调度 | 锁定本体弹幕，`PostAI` 追加两套攻击 + 低血状态切换 | [projectile.md](projectile.md) |
| `CorruptEaterShot.cs` | 弹幕 | 平时腐化追踪弹，命中一个目标消失 | [corrupt-shot.md](corrupt-shot.md) |
| `BabyEaterMinion.cs` | 弹幕 | 低血量冲撞弹，接触后反复冲刺 | [enraged-minion.md](enraged-minion.md) |

## 攻击逻辑速览

- **平时**：每 45 帧（0.75 秒）朝最近敌人发一枚腐化追踪弹（伤害 20 / 击退 2）。
- **低血量**（主人血量 < 50%）：每 300 帧（5 秒）额外生成一只冲撞小吞噬怪（伤害 10 / 击退 3）；切换状态时喷宝石尘 + 文字提示（紫=暴怒 / 蓝=平静）。
- 索敌半径 700px，标记目标（`OwnerMinionAttackTargetNPC`）优先；只在物主客户端生成弹幕。
