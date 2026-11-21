# 和风鱼 · 对象档案

> 改写原版宠物「和风鱼」：给它追加一套「接近 → 蓄力 → 冲刺 → 回气」的冲撞攻击，只有冲刺阶段才造成接触伤害。
> **不新建召唤物品 / 增益 / 宠物本体**——三者全部复用原版，用 `GlobalProjectile` 挂载攻击逻辑。
> 「为什么这么改、踩过什么坑」见 [log/](../../log/README.md)，「这套架构能不能复用」见 [method/](../../method/README.md)。

## 本对象文件清单

```
zephyrfish/
├── README.md           本文：对象总览 + 复用 ID + 源文件清单
├── summon-item.md      召唤物品「和风鱼」(ItemID.ZephyrFish)
├── buff.md             宠物增益 (BuffID.ZephyrFish)
└── ram-projectile.md   宠物本体：四段冲撞状态机（GlobalProjectile）
```

## 复用的原版 ID

| 类型 | 原版常量 | 档案 |
|---|---|---|
| 召唤物品 | `ItemID.ZephyrFish` | [summon-item.md](summon-item.md) |
| 宠物增益 | `BuffID.ZephyrFish` | [buff.md](buff.md) |
| 宠物本体弹幕 | `ProjectileID.ZephyrFish` | [ram-projectile.md](ram-projectile.md) |

ID 对照表：https://github.com/tModLoader/tModLoader/wiki/Vanilla-Content-IDs

## 3 个源文件清单

均在 `Content/Pets/ZephyrFish/` 下：

| 文件 | 层 | 职责 | 档案 |
|---|---|---|---|
| `ZephyrFishGlobalItem.cs` | 文本 | `AppliesToEntity` 锁定「和风鱼」，`ModifyTooltips` 改提示 | [summon-item.md](summon-item.md) |
| `ZephyrFishGlobalBuff.cs` | 文本 | `ModifyBuffText` 改增益说明 | [buff.md](buff.md) |
| `ZephyrFishRamGlobalProjectile.cs` | 攻击调度 | 锁定本体弹幕，`PostAI` 追加四段冲撞 + 网络同步 | [ram-projectile.md](ram-projectile.md) |

## 攻击逻辑速览

- **四段状态机**（写在 `PostAI`，原版游动 AI 照常运行）：`Approach`（接近）→ `Windup`（14 帧蓄力）→ `Dash`（26 帧冲刺）→ `Recover`（30 帧回气）→ 循环。
- 只有 `Dash` 阶段把 `friendly` 置 true 造成接触伤害（25 / 击退 6）；命中后沿反方向弹开并立即转入 `Recover`，形成「撞一下→弹开→再来」的节奏。
- 索敌半径 600px，进入 280px 内开始蓄力；标记目标优先。无目标时完全交回原版游动 AI。
