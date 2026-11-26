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

- `ai[0]` = 企鹅弹幕索引；`ai[1]` = 挥砍基准角度（朝目标）；`ai[2]` = 本次动画总帧数。蓄力帧数**不单独占 ai 槽**，由总帧数反推（`ai[2] >= 28` → 12 帧蓄力，否则 0）。原因：当前 tModLoader 的 `Projectile.ai` 只有 3 槽，写 `ai[3]` 会数组越界（见 [log/06](../../log/06-pet-teleport-on-attack.md)）。
- 每帧围绕企鹅中心做圆弧运动（`SwingStart=-1.2` → `SwingEnd=1.2` 弧度），同时旋转贴图形成「挥鱼」视觉。
- `PreDraw` 以鱼的「内侧一端」为旋转支点绘制，让挥动圆心落在企鹅身上；朝左挥砍时水平镜像并调整支点/旋转。
- `friendly = true`，靠接触判定造成召唤伤害（`DamageClass.Summon`）；`penetrate = -1`。

## 近战两段式动画（蓄力放大）

近战挥砍分两段，`scale` 只作用于绘制（局部 `drawScale` 变量），不改变 64×64 碰撞框：

1. **蓄力段**（前 12 帧）：角度固定在起手位，鱼从 1 倍平滑放大到 2 倍（`WindupScale = 2f`）。
2. **挥砍段**（后 16 帧）：`scale` 从 2 回落到 1，角度从起手扫到结束。缩放起点取"蓄力结束时的大小"——有蓄力段（近战）才是 2，无蓄力（远程）则退化为 1，避免远程鱼一出现就被放大（见 [log/07](../../log/07-ranged-swing-oversized.md)）。

缩放每帧由 `timeLeft` 现算，蓄力帧数由 `ai[2]` 反推，**不引入额外时间戳，也不依赖跨端同步字段**。放大不改变命中范围与判定时机。

## 两种用途

| 用途 | 时长常量 | 值 | 说明 |
|---|---|---|---|
| 近战挥砍 | `MeleeTotalLifetime = MeleeWindup(12) + MeleeLifetime(16)` | 28 帧 | 先蓄力放大 12 帧，再挥砍 16 帧 |
| 远程前摇 | `RangedLifetime` | 12 帧 | 反推出 `windup=0` 不带蓄力、鱼保持 1 倍；只播到「鱼正对目标」（6 帧）就被投掷逻辑 Kill，不播完整段 |

## 命中与加成

- `MinionShot = true` + `SummonTagDamageMultiplier = 1f`：吃鞭子标记（tag）加成。
- `usesLocalNPCImmunity = true`，`localNPCHitCooldown = HitCooldown = MeleeTotalLifetime + 4 = 32`：冷却覆盖整个生命周期（蓄力+挥砍 28 帧），**一砍对同一目标只中一次**（重复判定 bug 的修复，见 [log/05](../../log/05-melee-duplicate-hits.md)）。蓄力段延长时此值必须同步提高，否则重现重复命中。
- `CanCutTiles = false`：不割草/砸罐。

## 生命周期要点

进入挥砍动作前、退出时都要 Kill 上一发并清索引，保证任意时刻最多一个弹幕实例——否则索引被覆盖会留下孤儿弹幕并发。这套模式见 [method/00](../../method/00-projectile-lifecycle.md)。
