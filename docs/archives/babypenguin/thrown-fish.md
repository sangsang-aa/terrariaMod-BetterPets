# 投掷鱼弹幕

> 企鹅宝宝远程模式丢出的鱼，走抛物线。属于 [企鹅宝宝对象](README.md)。

## 基本信息

| 项 | 值 |
|---|---|
| 类名 | `ThrownFish`（`ModProjectile`） |
| 文件 | `Content/Pets/BabyPenguin/ThrownFish.cs` |
| 贴图 | 复用 `"Terraria/Images/Item_" + ItemID.Fish`（无需新 png） |
| 生成方 | `PenguinActions.ThrowFish`（见 [projectile.md](projectile.md)） |

## 机制

- 生成时初速度由 `CalculateLob` 按抛物线反推：`vx = Δx / t`，`vy = (Δy - 0.5·g·t²) / t`。
  - 重力 `g = 0.3`，预计飞行 `t = 35` 帧。
- 每帧 `velocity.Y += Gravity` 形成抛物线，同时按水平方向旋转贴图（像被丢出去的鱼）。
- `penetrate = 1`：命中一个目标后消失。
- `tileCollide = true`：会撞墙/撞地，不穿墙；`timeLeft = 180`。

## 数值

| 项 | 值 |
|---|---|
| 伤害 / 击退 | 15 / 3（远程，见 [projectile.md](projectile.md) 参数表） |
| 重力 `Gravity` | 0.3（与生成时反推用的一致） |
| 旋转速度 `SpinSpeed` | 0.25 弧度/帧 |
| 尺寸 | 16 × 16 |

## 命中与加成

- `MinionShot = true` + `SummonTagDamageMultiplier = 1f`：吃鞭子标记加成。
- `CanCutTiles = false`：不割草/砸罐。

## 调参提示

站位从「贴中心」改成 8~10 格后，落点预测更依赖 `ThrowFlightTime`（35 帧）。实测若鱼总丢偏，优先调 `PenguinActions.ThrowFlightTime`；改这里的值要同步改本类的 `Gravity`，两者必须一致。
