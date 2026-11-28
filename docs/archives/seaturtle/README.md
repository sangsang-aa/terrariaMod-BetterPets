# 海龟 · 对象档案

> 改写原版宠物「海龟」(Turtle)：给它追加一套冲撞攻击，只有冲撞阶段才造成接触伤害。
> **不新建召唤物品 / 增益 / 宠物本体**——三者全部复用原版，用 `Global*` 挂载追加逻辑。
> 「为什么这么改、踩过什么坑」见 [log/](../../log/README.md)，「这套架构能不能复用」见 [method/](../../method/README.md)。

## 本对象文件清单

```
seaturtle/
├── README.md          本文：对象总览 + 复用 ID + 源文件清单
├── summon-item.md     召唤物品「海草」(ItemID.Seaweed)：GlobalItem 改 Tooltip
├── buff.md            宠物增益 (BuffID.PetTurtle)：GlobalBuff 改说明
└── projectile.md      宠物本体 (ProjectileID.Turtle)：三态冲撞状态机 + 网络同步
```

设计稿：`Content/Pets/SeaTurtle/海龟宠物攻击状态机.html`（六段流程的 SVG 状态机图，是本对象的目标设计参考）。

## 复用的原版 ID

| 类型 | 原版常量 | 档案 |
|---|---|---|
| 召唤物品 | `ItemID.Seaweed`（海草） | [summon-item.md](summon-item.md) |
| 宠物增益 | `BuffID.PetTurtle` | [buff.md](buff.md) |
| 宠物本体弹幕 | `ProjectileID.Turtle` | [projectile.md](projectile.md) |

ID 对照表：https://github.com/tModLoader/tModLoader/wiki/Vanilla-Content-IDs

> 注意：wiki 上的 "Sea Turtle" 条目是海洋生物 NPC（内部 NPC ID 625），不是宠物本体。
> 宠物的正确名称是 **Turtle**，由 **Seaweed**（海草）召唤，增益为 **Pet Turtle**，弹幕为 `ProjectileID.Turtle`。

## 源文件清单

均在 `Content/Pets/SeaTurtle/` 下：

| 文件 | 层 | 职责 | 档案 |
|---|---|---|---|
| `SeaTurtleGlobalItem.cs` | 文本 | `AppliesToEntity` 锁定「海草」，`ModifyTooltips` 改提示 | [summon-item.md](summon-item.md) |
| `SeaTurtleGlobalBuff.cs` | 文本 | `ModifyBuffText` 改增益说明 | [buff.md](buff.md) |
| `SeaTurtleGlobalProjectile.cs` | 攻击调度 | 锁定本体弹幕，`PostAI` 追加三态冲撞 + 网络同步 | [projectile.md](projectile.md) |
| `SeaTurtleContext.cs` | 数据层 | 当前为空文件（预留） | — |
| `SeaTurtleGates.cs` | 判断层 | 当前只有 namespace 声明（预留） | — |

## 状态机速览

当前代码实现的是**三态**循环：`Windup`（30 帧蓄力）→ `Dash`（60 帧冲撞）→ `Recover`（30 帧回气）→ 回到 `Windup`。
只有 `Dash` 阶段把 `friendly` 置 true 造成接触伤害（25 / 击退 32，按召唤加成写回），命中后沿反方向弹开并立即转入 `Recover`。
详见 [projectile.md](projectile.md)。
