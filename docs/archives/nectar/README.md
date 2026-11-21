# 花蜜蜜蜂 · 对象档案

> 改写原版宠物「蜜蜂宝宝」（花蜜召唤）：定期发射毒刺，每 20 发掷出一枚蜂巢，蜂巢爆炸散出一群小蜜蜂。
> **不新建召唤物品 / 增益 / 宠物本体**——三者全部复用原版，用 `Global*` 挂载攻击逻辑；只有蜂巢是新增弹幕（贴图复用原版）。
> 「为什么这么做、完整参数与踩坑清单」见源码目录内详尽的改写手册 `Content/Pets/Nectar/README.md`（本档案总览为精简提炼版）。
> 「为什么这么改、踩过什么坑」见 [log/](../../log/README.md)，「这套架构能不能复用」见 [method/](../../method/README.md)。

## 本对象文件清单

```
nectar/
├── README.md           本文：对象总览 + 原版链条 + 源文件清单
├── summon-item.md      召唤物品「花蜜」(ItemID.Nectar)
├── buff.md             宠物增益 (BuffID.BabyHornet)
├── projectile.md       宠物本体：毒刺 + 蜂巢（GlobalProjectile）
└── hive.md             NectarHive：蜂巢弹幕（爆炸生成小蜜蜂）
```

> 完整改写手册（含每段代码骨架、16 条常见坑、调参建议）：`Content/Pets/Nectar/README.md`。

## 原版链条（核心）

花蜜物品本身没有 AI，宠物 AI 在弹幕上：

```
ItemID.Nectar（花蜜，1170）
    └─使用→ BuffID.BabyHornet（宠物增益，51）
                └─维持→ ProjectileID.BabyHornet（宠物本体，198，aiStyle=26 宠物 AI）
                            ├─追加→ ProjectileID.HornetStinger（友方毒刺，374）
                            └─追加→ NectarHive（自定义蜂巢）
                                        └─爆炸→ ProjectileID.Bee（小蜜蜂，181）× 3~5
```

## 复用的原版 ID

| 类型 | 原版常量 | ID | 档案 |
|---|---|---|---|
| 召唤物品 | `ItemID.Nectar` | 1170 | [summon-item.md](summon-item.md) |
| 宠物增益 | `BuffID.BabyHornet` | 51 | [buff.md](buff.md) |
| 宠物本体弹幕 | `ProjectileID.BabyHornet` | 198 | [projectile.md](projectile.md) |
| 友方毒刺 | `ProjectileID.HornetStinger` | 374 | [projectile.md](projectile.md) |
| 小蜜蜂 | `ProjectileID.Bee` | 181 | [hive.md](hive.md) |
| 蜂巢贴图 | `ProjectileID.BeeHive` | 655 | [hive.md](hive.md) |

⚠️ 毒刺必须用友方 `HornetStinger`（374），不要用敌对的 `QueenBeeStinger`。
ID 对照表：https://github.com/tModLoader/tModLoader/wiki/Vanilla-Content-IDs

## 4 个源文件清单

均在 `Content/Pets/Nectar/` 下：

| 文件 | 层 | 职责 | 档案 |
|---|---|---|---|
| `NectarGlobalItem.cs` | 文本 | `AppliesToEntity` 锁定「花蜜」，`ModifyTooltips` 改提示 | [summon-item.md](summon-item.md) |
| `NectarGlobalBuff.cs` | 文本 | `ModifyBuffText` 改增益说明 | [buff.md](buff.md) |
| `NectarGlobalProjectile.cs` | 攻击调度 | `PostAI` 追加毒刺 + 每 20 发掷蜂巢 | [projectile.md](projectile.md) |
| `NectarHive.cs` | 弹幕 | 蜂巢飞行 + 三种爆炸 + 生成小蜜蜂 | [hive.md](hive.md) |

## 攻击逻辑速览

- 每 30 帧发一枚毒刺（伤害 12 / 击退 2 / 速度 12px/帧）。
- 每 20 发毒刺掷一枚蜂巢（抛物线，伤害 18 / 击退 3，重力 0.15、预计 45 帧）。
- 蜂巢碰敌人 / 碰方块 / 满 5 秒都会爆炸（`exploded` 守卫保证只爆一次），散出 3~5 只小蜜蜂（伤害 4 / 击退 0）。
- 所有伤害按玩家**召唤加成**缩放；索敌半径 700px 带视线检查（100px 内可穿墙锁定）；标记目标优先。
