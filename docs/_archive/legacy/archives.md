# 对象档案（archives）

> 按游戏对象归档**完整技术细节**：复用了哪些原版 ID、每个文件干什么、状态机、参数表。
> 这里只描述「它是什么、现在怎么运作」；「为什么这么改、踩过什么坑」见 [log.md](log.md)，
> 「这套架构能不能复用」见 [method.md](method.md)。

---

## 对象：企鹅宝宝（BabyPenguin）

### 1. 概述

改写原版宠物「企鹅宝宝」，给它追加**召唤类攻击**（近战挥鱼 / 远程丢鱼）。
**不新建任何内容**——召唤物、Buff、宠物本体全部复用原版，只通过 `Global*` 挂载追加逻辑。

### 2. 复用的原版 ID

| 类型 | 原版常量 | ID |
|---|---|---|
| 召唤物品 | `ItemID.Fish`（鱼） | 669 |
| 宠物增益 | `BuffID.BabyPenguin` | 41 |
| 宠物本体弹幕 | `ProjectileID.Penguin` | 112 |

挥鱼与投掷弹幕的贴图也直接复用 `Terraria/Images/Item_` + `ItemID.Fish`，无需新 png。
ID 对照表：https://github.com/tModLoader/tModLoader/wiki/Vanilla-Content-IDs

### 3. 文件清单与职责

均在 `Content/Pets/BabyPenguin/` 下，共 8 个 `.cs`：

| 文件 | 层 | 职责 |
|---|---|---|
| `BabyPenguinGlobalItem.cs` | 文本 | `AppliesToEntity` 锁定「鱼」，`ModifyTooltips` 替换提示文字（只改文本不改属性） |
| `BabyPenguinGlobalBuff.cs` | 文本 | `ModifyBuffText` 里按 `type == BuffID.BabyPenguin` 替换增益说明 |
| `BabyPenguinGlobalProjectile.cs` | 调度器 | 挂到企鹅本体（`AppliesToEntity`）；`PostAI` 每帧只做「刷新 Context → 跑模式门 → 调动作包」 |
| `PenguinContext.cs` | 数据层 | 状态数据包 + `PenguinMode` / `PenguinAction` 枚举；每企鹅一份，存全部可变状态 |
| `PenguinGates.cs` | 判断层 | 检测门：静态纯函数 + 所有判定阈值；只读 Context |
| `PenguinActions.cs` | 行为层 | 5 个无状态动作 + 模式入口 + 弹幕生成/投掷/几何工具 |
| `PenguinFishSwing.cs` | 弹幕 | 挥鱼动作（`ModProjectile`），近战挥砍与远程前摇共用，绕企鹅做圆弧 |
| `ThrownFish.cs` | 弹幕 | 远程投出的鱼，带重力抛物线、旋转、命中一个目标后消失 |

配套：`Common/Systems/PetTargetMarkerSystem.cs`（全局系统）在玩家标记目标时给目标头顶补金色脉冲尘，企鹅是它识别的四种宠物之一。

### 4. 状态机（三层架构）

**第一层 · 模式层（`mode`，调度器里的计时门控制）**

```
Ranged ──[ModeTimeUp: 5秒]──> Melee
  ▲                            │
  └──[ModeTimeUp 5秒 或 MeleeChaseTimedOut 90帧]──┘
```

- `Ranged` → `Melee`：纯计时，5 秒到点即切，**位置不参与**。
- `Melee` → `Ranged`：5 秒到点切回；或近战接近 90 帧仍未贴边（目标太快）提前切回，避免真空期。

**第二层 · 动作层（`action`，5 个动作）**

| 模式 | 动作 | 每帧行为 | 转出条件 |
|---|---|---|---|
| Ranged | `RangedApproach` | 维持 8~10 格站位（太远靠近 / 太近后退 / 区间内减速悬停） | 进入站位区间 + 视线通畅 → `RangedWindup` |
| Ranged | `RangedWindup` | 维持站位，6 帧前摇 | 到点：主机生成投掷物 → `RangedRecover` |
| Ranged | `RangedRecover` | 维持站位，24 帧冷却 | 到点 → `RangedApproach` |
| Melee | `MeleeApproach` | `ChaseTimer++`，每帧重算贴边接触点并 `SteerTo` | 贴边（`AtContact`）→ `MeleeSwing`；超时门在调度器层拦截 |
| Melee | `MeleeSwing` | 钉在接触点、速度清零，固定挥满 16 帧（忽略目标移动） | 挥满 16 帧 → Kill 挥砍弹幕 → `MeleeApproach` |

**第三层 · 检测门（`PenguinGates`，只读）**

- 存在性：`OwnerAlive` / `TargetValid` / `TooFarFromOwner`
- 模式层：`ModeTimeUp` / `MeleeChaseTimedOut`
- 走位层：`AtStandOff` / `TooClose` / `TooFar` / `AtContact` / `HasLineOfSight`
- 索敌：`ResolveTarget`（目标锁定）
- 几何：`GetContactPoint`（slab 法算贴边点）

**关键解耦**：模式切换只读计时门，攻击触发只读位置门——两者互不干扰。

### 5. 参数表

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

行为数值（`PenguinActions.cs` / 弹幕）：

| 参数 | 值 |
|---|---|
| `ChaseSpeed` / `ChaseInertia` | 15f / 15f |
| 近战伤害 / 击退 | 20 / 4 |
| 远程伤害 / 击退 | 15 / 3 |
| 挥砍时长（近战 / 远程前摇） | 16 帧 / 12 帧（前摇播 6 帧即丢鱼） |
| 挥砍命中冷却 `HitCooldown` | 20 帧（= 挥砍 16 + 4，一砍只中一次） |
| 投掷冷却 | 24 帧 |
| 抛物线重力 / 预计飞行帧 | 0.3 / 35 帧 |
| 挥鱼环绕半径 | 30px |
| 伤害类别 | 召唤（`DamageClass.Summon`），经 `GetTotalDamage` / `GetKnockback` 计算 |

### 6. 其它设定

- `SetStaticDefaults`：`MinionTargettingFeature[ProjectileID.Penguin] = true`，允许右键指定攻击目标。
- `SetDefaults`：本体 `friendly = false`（伤害全由挥砍/投掷弹幕结算），`penetrate = -1`，局部无敌帧 10。
- 挥砍与投掷弹幕均 `MinionShot = true` + `SummonTagDamageMultiplier = 1f`，吃鞭子标记加成。
- 本地化文本（`Tooltip` / buff 说明）放在 `Localization/zh-Hans_Mods.TutorialMod.hjson`，未硬编码。
