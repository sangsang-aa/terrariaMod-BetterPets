# 和风鱼本体 · 四段冲撞状态机

> 和风鱼本体，复用原版 `ProjectileID.ZephyrFish`。保留原版游动 AI，用 `PostAI` 追加冲撞攻击。属于 [和风鱼对象](README.md)。

## 基本信息

| 项 | 值 |
|---|---|
| 原版常量 | `ProjectileID.ZephyrFish` |
| 实现文件 | `Content/Pets/ZephyrFish/ZephyrFishRamGlobalProjectile.cs` |
| 类型 | `GlobalProjectile`（`InstancePerEntity => true`） |

## 为什么用 GlobalProjectile

用 `AppliesToEntity` 精确锁定 `ProjectileID.ZephyrFish`，不拦截 `PreAI`（原版游动 AI 照常运行），只在 `PostAI` 覆盖速度实现冲撞——保留原版跟随、不新增贴图。`InstancePerEntity => true` 让每只宠物单独保存状态。

## 四段状态机（写在 PostAI）

```
Approach ──[距离 < 280px]──> Windup ──[14 帧]──> Dash ──[26 帧]──> Recover ──[30 帧]──> Approach
   ▲                                                                    │
   └────────────────────────[命中弹开 / 回气结束]────────────────────────┘
```

| 状态 | `friendly` | 每帧行为 | 转出条件 |
|---|---|---|---|
| `Approach` | false | 惯性平滑贴近（8px/帧，惯性 12） | 距离 < 280px → `Windup` |
| `Windup` | false | 速度 ×0.85 减速、转向目标 | 14 帧到点 → `Dash` |
| `Dash` | **true** | 速度拉满 17px/帧冲撞 + 紫尘尾迹 | 26 帧到点 → `Recover` |
| `Recover` | false | 不干预速度，交给原版 AI 游动 | 30 帧到点 → `Approach` |

- **只有 Dash 阶段有接触伤害**；`MinionContactDamage()` 恒返回 true 允许接触判定。
- `OnHitNPC`：命中后沿「远离敌人 + 略向上」方向弹开（`DashSpeed × 0.7 - UnitY × 3`），立即转 `Recover`。
- 无目标：`friendly=false`、`ResetState` 回 `Approach`，完全交回原版游动 AI。

## 网络同步

- `PostAI` 各端执行；自定义 `state` / `stateTimer` 不在原版字段里，必须用 `SendExtraAI` / `ReceiveExtraAI` 同步（`byte state` + `short stateTimer`，顺序两端一致）。
- `EnterState` 切换状态时设 `netUpdate = true` 触发同步。

## 数值与属性

| 参数 | 值 | 含义 |
|---|---|---|
| `RamDamage` / `RamKnockback` | 25 / 6 | 冲撞伤害 / 击退（每帧按召唤加成写回） |
| `DetectRange` / `DashRange` | 600 / 280px | 索敌半径 / 开始蓄力距离 |
| `ApproachSpeed` / `Inertia` | 8 / 12 | 接近速度 / 惯性 |
| `DashSpeed` | 17px/帧 | 冲刺速度 |
| `Windup/Dash/RecoverTime` | 14 / 26 / 30 帧 | 各阶段时长 |
| `HitCooldown` | 10 帧 | 局部无敌帧 |
| `penetrate` / `tileCollide` | -1（无限穿透）/ false（由 AI 覆盖） | 属性 |

- `SetStaticDefaults`：`MinionShot`、`SummonTagDamageMultiplier=1f`、`MinionTargettingFeature[ZephyrFish]=true`——吃鞭子标记加成并允许右键指定目标。
- 面朝目标：和风鱼贴图天然头朝左，朝右翻转 `spriteDirection=-1`、朝左不翻转并减 `Pi` 校正旋转。
- 伤害可能被生成流程清零，故 `PostAI` 每帧按 `GetTotalDamage(Summon)` / `GetKnockback(Summon)` 写回。

## 相关（本对象内）

- 对象总览：[README.md](README.md)
