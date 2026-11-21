# 宠物本体 · 攻击调度（GlobalProjectile）

> 婴儿吞噬怪本体，复用原版 `ProjectileID.BabyEater`。保留原版跟随 AI，用 `PostAI` 追加两套攻击。属于 [婴儿吞噬怪对象](README.md)。

## 基本信息

| 项 | 值 |
|---|---|
| 原版常量 | `ProjectileID.BabyEater` |
| 实现文件 | `Content/Pets/BabyEater/BabyEaterGlobalProjectile.cs` |
| 类型 | `GlobalProjectile`（`InstancePerEntity => true`） |

## 为什么用 GlobalProjectile

要改的是原版已有弹幕，用 `GlobalProjectile.AppliesToEntity` 精确锁定 `ProjectileID.BabyEater`，保留原版游动/跟随 AI，只在 `PostAI`（原版 AI 之后）追加攻击——不替换原版宠物、不新增贴图。`InstancePerEntity => true` 让每只宠物单独保存计时器。

## 两套攻击

| 模式 | 触发 | 弹幕 | 伤害 / 击退 | 间隔 |
|---|---|---|---|---|
| 平时 | 有目标 | `CorruptEaterShot`（腐化追踪弹） | 20 / 2 | 45 帧（0.75 秒） |
| 低血量 | 主人血量 < 50% 且有目标 | `BabyEaterMinion`（冲撞弹） | 10 / 3 | 300 帧（5 秒） |

- 低血量判定：`owner.statLife < owner.statLifeMax2 * 0.5f`。
- 回血后 `lowHpTimer` 归零，重新计时。
- 伤害/击退都按玩家的**召唤加成**缩放（`GetTotalDamage(Summon)` / `GetKnockback(Summon)` 的 `ApplyTo`）。

## 低血状态反馈（纯本地表现）

- 进入/离开低血阈值时切换 `enraged`，喷一圈宝石尘（紫 `GemAmethyst`=暴怒 / 蓝 `GemSapphire`=平静），并在物主客户端 `Main.NewText` 提示。
- 尘按圆周 24 点、每帧 4 点、半径 56px 持续喷 `ringTimer=45` 帧。
- 有目标时宠物头部朝向目标；贴图天然「头朝下」，故 `rotation = 方向角 - PiOver2`。

## 索敌与多人

- 索敌半径 700px；`OwnerMinionAttackTargetNPC`（鞭子/右键标记）优先，否则取最近的可追击敌人（`CanBeChasedBy`）。
- `PostAI` 各端都会执行，但**生成弹幕只在物主客户端**（`Main.myPlayer == projectile.owner`），避免多人重复生成。
- `SetStaticDefaults` 里设 `MinionTargettingFeature[ProjectileID.BabyEater] = true`，允许右键指定目标。

## 参数表

| 参数 | 值 | 含义 |
|---|---|---|
| `BasicCooldown` / `BasicDamage` / `BasicKnockback` | 45 帧 / 20 / 2 | 平时攻击 |
| `LowHpCooldown` / `LowHpDamage` / `LowHpKnockback` | 300 帧 / 10 / 3 | 低血量攻击 |
| `LowHpThreshold` | 0.5 | 触发低血攻击的血量比例 |
| `DetectRange` / `ShotSpeed` | 700px / 12px/帧 | 索敌半径 / 射出速度 |

## 相关（本对象内）

- 腐化追踪弹：[corrupt-shot.md](corrupt-shot.md)
- 低血冲撞弹：[enraged-minion.md](enraged-minion.md)
