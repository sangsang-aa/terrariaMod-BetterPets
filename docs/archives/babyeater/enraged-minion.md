# BabyEaterMinion · 低血冲撞弹

> 主人低血量时生成、接触目标后高速冲撞的「小吞噬怪」。属于 [婴儿吞噬怪对象](README.md)。

## 基本信息

| 项 | 值 |
|---|---|
| 类名 | `BabyEaterMinion`（`ModProjectile`，新增内容） |
| 文件 | `Content/Pets/BabyEater/BabyEaterMinion.cs` |
| 生成方 | `BabyEaterGlobalProjectile.Spawn`（见 [projectile.md](projectile.md)） |
| 贴图 | 复用原版 `ProjectileID.BabyEater`（含帧数），无需新 png |

## 机制：接近 → 冲撞状态机

模仿「魔焰眼随从」的冲撞：平时追踪敌人，**接触后才触发冲撞**，沿当前飞行方向高速穿过去，而不是贴着目标持续追踪。首次命中后不再回到慢速接近——每段冲撞结束会直接重新瞄准再冲，直到 1 秒生命周期结束。

`ai[]` 槽位（`Projectile.ai` 是原版 `float[3]`，随弹幕同步，无需 `SendExtraAI`）：

| 槽位 | 含义 |
|---|---|
| `ai[0]` | 0=接近 / 1=冲撞 |
| `ai[1]` | 冲撞计时 |
| `ai[2]` | 是否已首次命中 |

- **接近**：惯性平滑追踪（`ApproachSpeed=10` / `Inertia=10`）。
- **冲撞**：`OnHitNPC` 里沿当前速度方向拉满 `DashSpeed=20` 冲出，持续 `DashTime=16` 帧；之后若已命中则立即重新瞄准再冲，否则回到接近。
- 命中靠局部无敌帧每 10 帧结算一次；首次命中把 `timeLeft` 设为 `AttackDuration=60`（1 秒）。

## 数值与属性

| 项 | 值 |
|---|---|
| 伤害 / 击退 | 10 / 3（生成时按召唤加成缩放） |
| `HomingRange` | 1500px |
| `penetrate` / `tileCollide` | -1（无限穿透）/ false |
| `timeLeft` | 未命中 180 帧，命中后 60 帧 |
| 尺寸 | 20 × 20 |

## 命中与加成

- `MinionShot = true` + `SummonTagDamageMultiplier = 1f`：吃鞭子标记加成。
- `DamageType = Summon`；`CanCutTiles = false`。
- 贴图天然头朝下，`rotation = 速度角 - PiOver2`；动画每 5 帧切一帧。
