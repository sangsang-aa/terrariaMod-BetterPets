# 蜜蜂宝宝本体 · 毒刺 + 蜂巢（GlobalProjectile）

> 蜜蜂宝宝本体，复用原版 `ProjectileID.BabyHornet`。保留原版跟随 AI，用 `PostAI` 追加「索敌 → 毒刺 → 蜂巢」。属于 [花蜜蜜蜂对象](README.md)。

## 基本信息

| 项 | 值 |
|---|---|
| 原版常量 | `ProjectileID.BabyHornet`（ID 198，aiStyle=26） |
| 实现文件 | `Content/Pets/Nectar/NectarGlobalProjectile.cs` |
| 类型 | `GlobalProjectile`（`InstancePerEntity => true`） |

## 为什么用 PostAI + GlobalProjectile

`AppliesToEntity` 锁定 `ProjectileID.BabyHornet`；`PostAI` 在原版 AI 之后追加攻击，保留原版漂浮/传送/动画。`InstancePerEntity => true` 让每只宠物单独计时。注解：不要写 `class Nectar : ModItem` 想覆盖原版——1.4.4 里那只会新建物品，原版 1170 不受影响，必须用 Global 类。

## 逻辑（PostAI）

1. 主人检查：不存在/死亡 → 返回；无 `BuffID.BabyHornet` → 返回。
2. 索敌：`OwnerMinionAttackTargetNPC`（标记）优先，否则 `Main.ActiveNPCs` 里最近的 `CanBeChasedBy` 敌人，带视线检查（`Collision.CanHitLine`，100px 内可穿墙锁定）。
3. 无目标：计时归零返回；非物主客户端：返回（避免多人重复生成）。
4. 冷却到：发一枚毒刺（`ProjectileID.HornetStinger`，友方、自带 `MinionShot`）。
5. 毒刺计数到 20：掷一枚蜂巢（`NectarHive`，抛物线）。

**注意**：这只宠物仍是原版 `Main.projPet`，不要设 `minion = true` / `minionSlots`，否则占召唤栏并改变原版行为。`MinionTargettingFeature` 只让右键标记生效。

## 抛物线投掷

`CalculateLob`：已知重力 `g` 与飞行时间 `t`，反推初速度让弹幕在 t 帧后到达目标点：

```
vx = Δx / t
vy = (Δy − 0.5·g·t²) / t
```

这里 `HiveGravity = NectarHive.Gravity = 0.15f`——**两处必须一致**，否则蜂巢落在目标前/后方。

## 参数表

| 参数 | 值 | 含义 |
|---|---|---|
| `StingerCooldown` | 30 帧 | 毒刺间隔（0.5 秒） |
| `ShotsPerHive` | 20 发 | 每多少发毒刺掷一次蜂巢 |
| `StingerDamage` / `StingerKnockback` / `StingerSpeed` | 12 / 2 / 12px/帧 | 毒刺数值 |
| `StingerSpawnOffset` | 20px | 毒刺从宠物中心前移多少生成 |
| `DetectRange` | 700px | 索敌半径 |
| `HiveDamage` / `HiveKnockback` | 18 / 3 | 蜂巢撞击数值 |
| `HiveGravity` / `HiveFlightTime` | 0.15 / 45 帧 | 抛物线重力 / 预计落点帧数 |

完整参数表与调参建议见源码手册 `Content/Pets/Nectar/README.md` 第 9 节。

## 多人

`PostAI` 各端执行，但 `NewProjectile` 只在 `Main.myPlayer == projectile.owner` 时调用；`stingerTimer` / `stingerCount` 是普通字段不同步（只影响物主何时生成弹幕）。

## 相关（本对象内）

- 蜂巢弹幕：[hive.md](hive.md)
- 对象总览：[README.md](README.md)
