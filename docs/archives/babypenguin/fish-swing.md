# 挥鱼弹幕

> 企鹅宝宝近战挥砍 / 远程前摇共用的短命弹幕。属于 [企鹅宝宝对象](README.md)。

## 基本信息

| 项 | 值 |
|---|---|
| 类名 | `PenguinFishSwing`（`ModProjectile`） |
| 文件 | `Content/Pets/BabyPenguin/PenguinFishSwing.cs` |
| 贴图 | 复用 `"Terraria/Images/Item_" + ItemID.Fish`（无需新 png） |
| 生成方 | `PenguinActions.SpawnSwing`（见 [projectile.md](projectile.md)） |

## 机制

- `ai[0]` = 企鹅弹幕索引；`ai[1]` = 挥砍基准角度（朝目标）；`ai[2]` = 本次动画总帧数。
- 每帧围绕企鹅中心做圆弧运动（`SwingStart=-1.2` → `SwingEnd=1.2` 弧度），同时旋转贴图形成「挥鱼」视觉。
- `PreDraw` 以鱼的「内侧一端」为旋转支点绘制，让挥动圆心落在企鹅身上；朝左挥砍时水平镜像并调整支点/旋转。
- `friendly = true`，靠接触判定造成召唤伤害（`DamageClass.Summon`）；`penetrate = -1`。

## 两种用途

| 用途 | 时长常量 | 值 | 说明 |
|---|---|---|---|
| 近战挥砍 | `MeleeLifetime` | 16 帧 | 完整挥完 |
| 远程前摇 | `RangedLifetime` | 12 帧 | 只播到「鱼正对目标」（6 帧）就被投掷逻辑 Kill，不播完整段 |

## 命中与加成

- `MinionShot = true` + `SummonTagDamageMultiplier = 1f`：吃鞭子标记（tag）加成。
- `usesLocalNPCImmunity = true`，`localNPCHitCooldown = HitCooldown = MeleeLifetime + 4 = 20`：冷却覆盖整个挥砍时长，**一砍对同一目标只中一次**（重复判定 bug 的修复，见 [log/05](../../log/05-melee-duplicate-hits.md)）。
- `CanCutTiles = false`：不割草/砸罐。

## 生命周期要点

进入挥砍动作前、退出时都要 Kill 上一发并清索引，保证任意时刻最多一个弹幕实例——否则索引被覆盖会留下孤儿弹幕并发。这套模式见 [method/00](../../method/00-projectile-lifecycle.md)。
