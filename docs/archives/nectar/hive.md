# NectarHive · 蜂巢弹幕

> 蜜蜂宝宝掷出的蜂巢，飞行中碰敌人 / 碰方块 / 满 5 秒都会爆炸，散出一群小蜜蜂。属于 [花蜜蜜蜂对象](README.md)。

## 基本信息

| 项 | 值 |
|---|---|
| 类名 | `NectarHive`（`ModProjectile`，新增内容） |
| 文件 | `Content/Pets/Nectar/NectarHive.cs` |
| 生成方 | `NectarGlobalProjectile.FireHive`（见 [projectile.md](projectile.md)） |
| 贴图 | 复用原版 `ProjectileID.BeeHive`（31×31），无需新 png |

## 机制

- **CloneDefaults 陷阱**：`CloneDefaults(ProjectileID.BeeHive)` 会把 `hostile=true`、`trap=true`、`penetrate=-1` 一起复制过来，必须逐项改掉（`friendly=true` / `hostile=false` / `trap=false` / `penetrate=1` / `aiStyle=0`）。只改 `friendly` 不改 `hostile`，蜂巢会同时伤害玩家和敌人。
- **飞行**：每帧 `velocity.Y += Gravity`（0.15 抛物线），`rotation += velocity.X × 0.02` 轻微翻滚。
- **三种爆炸**：`OnHitNPC`（命中敌人）、`OnTileCollide`（碰方块）、`OnKill`（5 秒寿命归零）都调用 `Explode()`；`bool exploded` 守卫保证只爆一次。
- **`OnTileCollide` 返回 true**：交给引擎销毁弹幕。（注意：`return false` 不会销毁，只跳过原版碰撞处理。）

## 爆炸（Explode）

- 表现层（所有客户端）：`SoundID.Item14` 音效 + 16 颗蜂蜜/蜜蜂尘（`DustID.Honey` / `DustID.Bee` 交替）。
- 生成小蜜蜂（仅物主客户端）：`Main.rand.Next(BeeMin, BeeMax+1)` 只 `ProjectileID.Bee`，随机散布生成，由原版 AI 自动追踪敌人。
- 小蜜蜂伤害按召唤加成缩放。⚠️ 原版 `ProjectileID.Bee` 不在 `MinionShot` 集合里，所以**小蜜蜂吃不到鞭子标记加成**（若要吃到需换自定义 `ModProjectile`）。

## 数值与属性

| 项 | 值 |
|---|---|
| `Gravity` / `Lifetime` | 0.15 / 300 帧（5 秒） |
| `BeeMin` / `BeeMax` | 3 / 5 只 |
| `BeeDamage` / `BeeKnockback` | 4 / 0 |
| `DamageType` / `MinionShot` | Summon / true（蜂巢本身吃标记加成） |

## 调参提示

`NectarGlobalProjectile.HiveGravity` 用于反推初速度、这里 `Gravity` 是实际每帧重力，两者必须一致。想做「悬浮蜂巢」就把两处都设为 0，靠 5 秒寿命结束。完整坑位清单见源码手册 `Content/Pets/Nectar/README.md` 第 12 节。
