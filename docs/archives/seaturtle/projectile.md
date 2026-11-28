# 海龟本体 · 三态冲撞状态机

> 海龟本体，复用原版 `ProjectileID.Turtle`。保留原版游动 AI，用 `PostAI` 追加冲撞攻击。属于 [海龟对象](README.md)。
> 设计稿见 `Content/Pets/SeaTurtle/海龟宠物攻击状态机.html`（六段流程），本页描述**当前代码实际实现**。

## 基本信息

| 项 | 值 |
|---|---|
| 原版常量 | `ProjectileID.Turtle` |
| 实现文件 | `Content/Pets/SeaTurtle/SeaTurtleGlobalProjectile.cs` |
| 类型 | `GlobalProjectile`（`InstancePerEntity => true`） |

## 为什么用 GlobalProjectile

用 `AppliesToEntity` 精确锁定 `ProjectileID.Turtle`，不拦截 `PreAI`（原版游动 AI 照常运行），只在 `PostAI` 覆盖速度实现冲撞——保留原版跟随、不新增贴图。`InstancePerEntity => true` 让每只宠物单独保存状态。

## 三态状态机（写在 PostAI）

```
Windup ──[30 帧]──> Dash ──[60 帧]──> Recover ──[30 帧]──> Windup
   ▲                   │                    │
   └──────────[无目标 ResetState / 命中弹开]──┘
```

| 状态 | `friendly` | 每帧行为 | 转出条件 |
|---|---|---|---|
| `Windup` | false | 速度 ×0.85 减速、`FaceTarget` 转向目标 | 30 帧到点 → `Dash` |
| `Dash` | **true** | 首帧按地形决定速度，后续帧维持水平速度并施加重力 | 60 帧到点 → `Recover` |
| `Recover` | false | `FaceTarget`，速度 ×0.8，低于 0.5 px/帧归零 | 30 帧到点 → `Windup`（计时 0） |

- 只有 `Dash` 阶段有接触伤害；`MinionContactDamage()` 恒返回 true 允许接触判定。
- 无目标：主人死亡/离开或无索敌目标时 `friendly=false`、`ResetState` 回 `Windup`，完全交回原版游动 AI。
- `OnHitNPC`：命中后沿「远离敌人 + 略向上」方向弹开，立即转 `Recover`，形成「撞一下 → 弹开 → 再来」的节奏。

## 发射速度（Dash 首帧写入）

`Dash()` 在首帧（`stateTimer == DashTime`）判定地形：

- 目标在地面 + 海龟在地面 + 底部高度差 ≤ `GroundHeightDiff`(10px) → 地面平射：
  `savedVx = 朝向目标方向.X × GroundDashSpeed(16)`、`savedVy = 0`、`currentGravity = 0`。
- 其它情形：当前代码**不写入新速度**（空中抛物线分支处于注释状态），保留上一阶段速度。
- 后续帧：`velocity.Y = savedVy + currentGravity`、`velocity.X = savedVx`，并把 `rotation` 置 0。

## 索敌

`SearchForTargets` 返回最近的、可攻击的 `NPC`：

- 优先攻击玩家标记目标 `projectile.OwnerMinionAttackTargetNPC`（鞭子命中或右键指定）。
- 否则遍历 `Main.ActiveNPCs`，过滤 `CanBeChasedBy()`，取半径 `DetectRange`(600px) 内、有视线（`Collision.CanHitLine`）或距离 < 100px（贴身穿墙）的最近目标。
- 采样 `Main.ActiveNPCs`（只含活跃 NPC），`distanceFromTarget` 作为 out 参数回传。

## 网络同步

- `PostAI` 各端执行；自定义 `state` / `stateTimer` 不在原版字段里，必须用 `SendExtraAI` / `ReceiveExtraAI` 同步（`byte state` + `short stateTimer`，顺序两端一致）。
- `EnterState` 切换状态时设 `netUpdate = true` 触发同步。

## 属性与数值

| 参数 | 值 | 含义 |
|---|---|---|
| `RamDamage` / `RamKnockback` | 25 / 32 | 冲撞伤害 / 击退（每帧按召唤加成写回） |
| `DetectRange` | 600px | 索敌半径 |
| `GroundDashSpeed` | 16px/帧 | 地面冲刺速度 |
| `GroundHeightDiff` | 10px | 双方底部高度差阈值（在此范围内视为同平面） |
| `FlightTime` / `Gravity` | 60 帧 / 0.35 px/帧² | 空中抛物线参数（当前未启用） |
| `Windup/Dash/RecoverTime` | 30 / 60 / 30 帧 | 各阶段时长 |
| `HitCooldown` | 10 帧 | 局部无敌帧 |
| `penetrate` / `friendly` | -1（无限穿透）/ 初始 false | 属性 |

- `SetStaticDefaults`：`MinionShot[Turtle]=true`、`SummonTagDamageMultiplier[Turtle]=1.1f`、`MinionTargettingFeature[Turtle]=true`——吃鞭子标记加成并允许右键指定目标。
- `SetDefaults`：`DamageType = DamageClass.Summon`、`usesLocalNPCImmunity = true`、`localNPCHitCooldown = 10`、`CanCutTiles = false`。
- 伤害/击退每帧用 `owner.GetTotalDamage(Summon).ApplyTo(...)` / `GetKnockback(Summon).ApplyTo(...)` 写回，以吃到召唤加成。
- `FaceTarget`：海龟贴图天然头朝左，朝右翻转 `spriteDirection=-1`、朝左不翻转并减 `Pi` 校正旋转。

## 相关（本对象内）

- 对象总览：[README.md](README.md)
