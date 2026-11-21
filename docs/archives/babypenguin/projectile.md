# 企鹅本体与三层状态机

> 企鹅宝宝宠物本体，复用原版 `ProjectileID.Penguin`。这是整套攻击逻辑的核心。属于 [企鹅宝宝对象](README.md)。

## 基本信息

| 项 | 值 |
|---|---|
| 原版常量 | `ProjectileID.Penguin` |
| ID | 112 |
| 核心文件 | `Content/Pets/BabyPenguin/BabyPenguinGlobalProjectile.cs`（调度器） |
| 配套文件 | `PenguinContext.cs` / `PenguinGates.cs` / `PenguinActions.cs` |

## 为什么用 GlobalProjectile

原版宠物由 `ProjectileID.Penguin` 弹幕实现。本实现**不替换原版宠物**，而是用 `GlobalProjectile.AppliesToEntity` 精确锁定该弹幕 ID，保留原版游动/跟随 AI，只在 `PostAI`（原版 AI 之后）追加「追踪敌人 + 攻击状态机」——不用新物品/新贴图。

`InstancePerEntity => true` 让每个企鹅弹幕实例有独立状态。

## 三层架构

| 层 | 文件 | 职责 |
|---|---|---|
| 调度器 | `BabyPenguinGlobalProjectile.cs` | `PostAI` 只做「刷新 Context → 跑模式门决定是否切换 → 调 `Actions.Run`」 |
| 数据层 | `PenguinContext.cs` | 状态数据包 + `PenguinMode` / `PenguinAction` 枚举，每企鹅一份，唯一真相源 |
| 判断层 | `PenguinGates.cs` | 检测门：静态纯函数，只读 Context，所有判定阈值集中在这里 |
| 行为层 | `PenguinActions.cs` | 5 个无状态动作 + 模式入口 + 弹幕生成/投掷/几何工具 |

**关键解耦**：模式切换只读计时门（`ModeTimeUp` / `MeleeChaseTimedOut`），攻击触发只读位置门（`AtStandOff` / `AtContact` / `HasLineOfSight`）——两类判定互不干扰。

这套架构的复用方法见 [method/01](../../method/01-three-layer-state-machine.md)。

## 状态机

### 第一层 · 模式层（计时门控制）

```
Ranged ──[ModeTimeUp: 5秒]──> Melee
  ▲                            │
  └──[ModeTimeUp 5秒 或 MeleeChaseTimedOut 90帧]──┘
```

- `Ranged` → `Melee`：纯计时，5 秒到点即切，**位置不参与**。
- `Melee` → `Ranged`：5 秒到点切回；或近战接近 90 帧仍未贴边（目标太快）提前切回，避免真空期。

### 第二层 · 动作层（5 个动作）

| 模式 | 动作 | 每帧行为 | 转出条件 |
|---|---|---|---|
| Ranged | `RangedApproach` | 维持 8~10 格站位（太远靠近 / 太近后退 / 区间内减速悬停） | 进入站位区间 + 视线通畅 → `RangedWindup` |
| Ranged | `RangedWindup` | 维持站位，6 帧前摇 | 到点：主机生成投掷物 → `RangedRecover` |
| Ranged | `RangedRecover` | 维持站位，24 帧冷却 | 到点 → `RangedApproach` |
| Melee | `MeleeApproach` | `ChaseTimer++`，每帧重算贴边接触点并追击 | 贴边（`AtContact`）→ `MeleeSwing`；超时门在调度器层拦截 |
| Melee | `MeleeSwing` | 钉在接触点、速度清零，**固定挥满 16 帧（忽略目标移动）** | 挥满 → Kill 挥砍弹幕 → `MeleeApproach` |

挥砍期间忽略目标移动的理由：命中造成的击退会让目标位移，若据此提前转出，会与再次贴边生成的新弹幕叠加成自激循环（详见 [log/05](../../log/05-melee-duplicate-hits.md)）。

### 第三层 · 检测门（PenguinGates，只读）

- 存在性：`OwnerAlive` / `TargetValid` / `TooFarFromOwner`
- 模式层：`ModeTimeUp` / `MeleeChaseTimedOut`
- 走位层：`AtStandOff` / `TooClose` / `TooFar` / `AtContact` / `HasLineOfSight`
- 索敌：`ResolveTarget`（目标锁定，见 [method/02](../../method/02-target-lock.md)）
- 几何：`GetContactPoint`（slab 法贴边点，见 [method/03](../../method/03-slab-contact-point.md)）

## 参数表

判定阈值（`PenguinGates.cs`）：

| 参数 | 值 | 含义 |
|---|---|---|
| `DetectRange` | 600px | 索敌半径 |
| `LeashRange` | 1200px | 离主人超过则放弃追击，交回原版跟随 |
| `StandOffMin` / `StandOffMax` | 8 格 / 10 格（128 / 160px） | 远程站位区间 |
| `ContactGap` | 4px | 近战贴边与敌人框的间隙 |
| `ContactArriveDist` | 10px | 到贴边点多近算到位 |
| `RangedModeTime` / `MeleeModeTime` | 300 帧（5 秒） | 模式时长 |
| `MeleeChaseTimeout` | 90 帧（1.5 秒） | 近战追击超时阀 |

行为数值（`PenguinActions.cs`）：

| 参数 | 值 |
|---|---|
| `ChaseSpeed` / `ChaseInertia` | 15f / 15f |
| 近战伤害 / 击退 | 20 / 4 |
| 远程伤害 / 击退 | 15 / 3 |
| 挥砍时长（近战 / 远程前摇） | 16 帧 / 12 帧（前摇播 6 帧即丢鱼） |
| 投掷冷却 | 24 帧 |
| 抛物线重力 / 预计飞行帧 | 0.3 / 35 帧 |
| 伤害类别 | 召唤（`DamageClass.Summon`），经 `GetTotalDamage` / `GetKnockback` 计算 |

## 其它设定

- `SetStaticDefaults`：`MinionTargettingFeature[ProjectileID.Penguin] = true`，允许右键指定攻击目标。
- `SetDefaults`：本体 `friendly = false`（伤害全由挥砍/投掷弹幕结算），`penetrate = -1`，局部无敌帧 10。
- 面朝目标：贴图天然朝左，目标在右翻转 -1、在左不翻转 1；飞行帧段 3 起循环，带原版 AI_026 尾焰（75% Torch / 25% Smoke）。

## 本对象内相关

- 挥鱼弹幕：[fish-swing.md](fish-swing.md)
- 投掷鱼弹幕：[thrown-fish.md](thrown-fish.md)
