# CorruptEaterShot · 腐化追踪弹

> 婴儿吞噬怪平时周期发射的追踪弹，命中一个目标后消失。属于 [婴儿吞噬怪对象](README.md)。

## 基本信息

| 项 | 值 |
|---|---|
| 类名 | `CorruptEaterShot`（`ModProjectile`，新增内容） |
| 文件 | `Content/Pets/BabyEater/CorruptEaterShot.cs` |
| 生成方 | `BabyEaterGlobalProjectile.Spawn`（见 [projectile.md](projectile.md)） |
| 贴图 | 复用原版 `ProjectileID.TinyEater`，无需新 png |

## 机制

- **轻微追踪**：朝最近敌人用 `Vector2.Lerp` 平滑转向，索敌半径 700px、追踪速度 14px/帧、`HomingLerp = 0.08`（越小转向越慢）。
- **外观对齐原版 Tiny Eater**：朝向 `velocity.ToRotation() - PiOver2`；`alpha` 从 255 每帧减 50 淡入；腐化尘拖尾（`Dust 184`，沿速度等距放 3 个）。
- 动画每 6 帧切一帧（沿用原版帧数）；`CanCutTiles = false`。

## 数值与属性

| 项 | 值 |
|---|---|
| 伤害 / 击退 | 20 / 2（生成时按召唤加成缩放） |
| `penetrate` | 1（命中一个目标消失） |
| `tileCollide` / `ignoreWater` | false / true |
| `timeLeft` | 180 帧 |
| 尺寸 | 16 × 16 |

## 命中与加成

- `MinionShot = true` + `SummonTagDamageMultiplier = 1f`：吃鞭子标记（tag）加成。
- 索敌优先 `OwnerMinionAttackTargetNPC`（标记目标）。
