# 花蜜宠物改写手册（毒刺 + 蜂巢 + 小蜜蜂）

> 目标：保留原版“花蜜”物品、宠物增益和蜜蜂宠物，只给宠物追加攻击行为。
> 全部复用原版贴图，不新增 png。本文只给**类结构、钩子签名、算法逻辑和参数表**，代码由你自己手写。

**建议动手顺序**

1. 先建 4 个 `.cs` 文件（内容先空着，确认能编译）。
2. 写 `NectarGlobalItem` / `NectarGlobalBuff`，Build + Reload，先确认 Tooltip 和增益说明能变。
3. 写 `NectarGlobalProjectile` 的“索敌 + 毒刺”，确认宠物会发毒刺。
4. 写 `NectarHive` 的飞行和三种爆炸，先用 `Main.NewText` 或尘确认触发。
5. 最后把“每 20 发毒刺 → 蜂巢”接上。
6. 按第 9 节调参，按第 11 节测试。

> 如果你已经照抄了骨架、但不知道怎么填方法体，直接跳到第 15 节「对照现有实现」。

---

## 1. 最终效果

- 使用原版“花蜜”召唤出的蜜蜂宠物，会主动攻击附近敌人。
- 每 `StingerCooldown` 帧发射一枚毒刺（`ProjectileID.HornetStinger`）。
- 每发射 `ShotsPerHive` 枚毒刺后，朝目标抛出一个蜂巢。
- 蜂巢在以下任一情况爆炸：
  1. 碰到敌人；
  2. 碰到物块；
  3. 存在时间达到 `NectarHive.Lifetime`（默认 5 秒）。
- 蜂巢爆炸后散出 `BeeMin`～`BeeMax` 只小蜜蜂（`ProjectileID.Bee`），由原版蜜蜂 AI 自动追踪敌人。
- 所有伤害按玩家的**召唤伤害/召唤击退**加成缩放。

---

## 2. 先搞清楚原版链条

这是整个方案的核心。**花蜜物品本身没有 AI**，宠物 AI 在弹幕上：

```text
ItemID.Nectar（花蜜，1170）
    │ 使用物品
    ▼
BuffID.BabyHornet（宠物增益，51）
    │ 增益的 Update 每帧维持宠物
    ▼
ProjectileID.BabyHornet（宠物本体，198，aiStyle = 26 宠物 AI）
    ├─ 我们追加：ProjectileID.HornetStinger（毒刺，374）
    └─ 我们追加：NectarHive（自定义蜂巢弹幕）
             └─ 爆炸生成 ProjectileID.Bee（小蜜蜂，181）× 3～5
```

### 已核实的原版 ID（tModLoader 1.4.4）

| 内容 | 常量 | ID | 说明 |
| --- | --- | ---: | --- |
| 花蜜 | `ItemID.Nectar` | 1170 | 女王蜂掉落，召唤宠物 |
| 宠物增益 | `BuffID.BabyHornet` | 51 | 原版增益，`Update` 会生成宠物 |
| 宠物本体 | `ProjectileID.BabyHornet` | 198 | `26×26`，`aiStyle = 26` |
| 毒刺 | `ProjectileID.HornetStinger` | 374 | 黄蜂随从的**友方**毒刺，`MinionShot` |
| 女王蜂毒刺 | `ProjectileID.QueenBeeStinger` | — | **敌对**，不要用错 |
| 小蜜蜂 | `ProjectileID.Bee` | 181 | 养蜂人/蜂枪同款，自动追踪 |
| 蜂巢贴图 | `ProjectileID.BeeHive` | 655 | 原版蜂巢陷阱弹幕，`31×31` |
| 蜂蜜尘 | `DustID.Honey` | 152 | 爆炸尘 |
| 蜜蜂尘 | `DustID.Bee` | 150 | 爆炸尘 |
| 爆炸音 | `SoundID.Item14` | — | `SoundStyle`，用 `SoundEngine.PlaySound` |

> 代码里请始终使用常量名（`ItemID.Nectar`、`ProjectileID.BabyHornet`），不要写数字 ID。
> 数字只用于对照和排查。ID 查询：<https://github.com/tModLoader/tModLoader/wiki/Vanilla-Content-IDs>

---

## 3. 为什么不能写 `class Nectar : ModItem`

很多教程会说“把类名写成原版物品名就能覆盖”，但在 **tModLoader 1.4.4** 里不能这样做：

- `ModItem` 注册时，`ItemLoader.Register(ModItem)` 总是分配一个**新 ID**（内部是 `ItemCount++`）。
- `ItemLoader.GetItem(int type)` 对 `type < ItemID.Count` 直接返回 `null`。
- 所以你写 `class Nectar : ModItem` 只会得到一件**新的**物品，原版 1170 号花蜜完全不受影响。

正确的“覆盖原版”方式是 **Global 类**：

| 想改什么 | 用什么 | 限定方式 |
| --- | --- | --- |
| 原版物品的提示/属性 | `GlobalItem` | `AppliesToEntity` → `ItemID.Nectar` |
| 原版增益的说明文字 | `GlobalBuff` | `ModifyBuffText` 里判断 `BuffID.BabyHornet` |
| 原版宠物弹幕的 AI | `GlobalProjectile` | `AppliesToEntity` → `ProjectileID.BabyHornet` |
| 新增蜂巢弹幕 | `ModProjectile` | 新类型，自己实现 AI |
| 新增小蜜蜂 | 直接用 `ProjectileID.Bee` | 原版 AI 已经会追踪 |

这也正是你现有 `BabyEater`、`BabyPenguin`、`ZephyrFish` 目录的做法，本手册保持一致。

---

## 4. 目录与文件清单

```text
TutorialMod/Content/Pets/Nectar/
├── NectarGlobalItem.cs        // 改花蜜的 Tooltip
├── NectarGlobalBuff.cs        // 改“蜜蜂宝宝”增益的说明
├── NectarGlobalProjectile.cs  // 给 ProjectileID.BabyHornet 追加攻击
├── NectarHive.cs              // 自定义蜂巢弹幕
└── README.md                  // 本手册
```

命名空间统一为：

```cs
namespace TutorialMod.Content.Pets.Nectar
{
    // 你的类
}
```

不需要新建 `Content/Projectiles/`，蜂巢和宠物放在一起，和 `BabyEater` 的组织方式一致。
现有代码用 Tab 缩进，手写时保持一致即可。

---

## 5. `NectarGlobalItem.cs`：只改提示

### 职责

只替换花蜜的物品说明，不改物品属性、不改 `shoot`、不改 `buffType`。

### 类骨架

```cs
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace TutorialMod.Content.Pets.Nectar
{
    public class NectarGlobalItem : GlobalItem
    {
        public static LocalizedText TooltipText { get; private set; }

        // 注册阶段调用一次：缓存本地化文本。
        public override void SetStaticDefaults() {
            TooltipText = Mod.GetLocalization($"{nameof(NectarGlobalItem)}.Tooltip");
        }

        // 物品初始化时调用：只认原版花蜜。
        public override bool AppliesToEntity(Item entity, bool lateInstantiation) {
            return entity.type == ItemID.Nectar;
        }

        // 构造物品提示时调用：替换已有的 Tooltip 行，没有就新增。
        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips) {
            TooltipLine line = tooltips.Find(x => x.Name.StartsWith("Tooltip"));
            if (line != null) {
                line.Text = TooltipText.Value;
            }
            else {
                tooltips.Add(new TooltipLine(Mod, "NectarTooltip", TooltipText.Value));
            }
        }
    }
}
```

### 要点

- `AppliesToEntity` 的 `lateInstantiation` 参数在“后注册”阶段会再调用一次；判断 `entity.type` 即可，不需要区分阶段。
- 本地化 key 是 `NectarGlobalItem.Tooltip`，对应 hjson 里的**根级 key**（不是 `Items:` 下面），见第 10 节。
- 如果原版花蜜有多行 Tooltip，这里只替换第一行；需要全部替换时，可先 `tooltips.RemoveAll(x => x.Name.StartsWith("Tooltip"))` 再 `Add`。
- 物品名称“花蜜”仍然来自原版，不需要改。

参考：<https://docs.tmodloader.net/docs/1.4-stable/class_terraria_1_1_mod_loader_1_1_global_item.html>

---

## 6. `NectarGlobalBuff.cs`：只改增益说明

### 职责

只改增益栏里的说明文字。**不要**改增益逻辑：原版 `BuffID.BabyHornet` 的 `Update` 负责生成/维持宠物。

### 类骨架

```cs
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace TutorialMod.Content.Pets.Nectar
{
    public class NectarGlobalBuff : GlobalBuff
    {
        public static LocalizedText Description { get; private set; }

        public override void SetStaticDefaults() {
            Description = Mod.GetLocalization($"{nameof(NectarGlobalBuff)}.Description");
        }

        // 构造增益提示时调用；buffName/tip/rare 都是 ref，可原地替换。
        public override void ModifyBuffText(int type, ref string buffName, ref string tip, ref int rare) {
            if (type == BuffID.BabyHornet) {
                tip = Description.Value;
            }
        }
    }
}
```

### 要点

- 只改 `tip`，名称 `buffName` 和稀有度 `rare` 保持原版。
- hjson key 是 `NectarGlobalBuff.Description`。

参考：<https://docs.tmodloader.net/docs/1.4-stable/class_terraria_1_1_mod_loader_1_1_global_buff.html>

---

## 7. `NectarGlobalProjectile.cs`：给宠物追加攻击

### 职责

在 `ProjectileID.BabyHornet` 的原版跟随 AI 之后，追加“索敌 → 毒刺 → 蜂巢”的逻辑。

### 为什么用 `PostAI`

- `PostAI` 在原版 AI 每帧执行完之后调用；原版宠物的跟随/漂浮/传送/动画全部保留。
- 如果你以后想**完全替换**移动逻辑，用 `PreAI` 并 `return false` 拦截原版 AI（见第 13.1 节）。
- `PostAI` 在本地客户端、服务器、远程客户端都会执行；生成弹幕必须限定在物主客户端。

### 类骨架

```cs
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace TutorialMod.Content.Pets.Nectar
{
    public class NectarGlobalProjectile : GlobalProjectile
    {
        // 关键：每只宠物一份 Global 实例，计时器/计数才不会互相干扰。
        public override bool InstancePerEntity => true;

        // 右键 / 鞭子标记：让 OwnerMinionAttackTargetNPC 对这只宠物生效。
        // 原版宠物默认没有登记这个 Set，不写的话右键标记会被忽略。
        public override void SetStaticDefaults() {
            ProjectileID.Sets.MinionTargettingFeature[ProjectileID.BabyHornet] = true;
        }

        // 参数见第 9 节；先写成常量，后面统一调。
        private const int StingerCooldown = 0;
        private const int ShotsPerHive = 0;
        private const int StingerDamage = 0;
        private const float StingerKnockback = 0f;
        private const float StingerSpeed = 0f;
        private const float DetectRange = 0f;
        private const int HiveDamage = 0;
        private const float HiveKnockback = 0f;
        private const float HiveGravity = 0f;
        private const int HiveFlightTime = 0;
        private const float StingerSpawnOffset = 0f;

        // 只在物主客户端使用，不需要额外网络同步。
        private int stingerTimer;
        private int stingerCount;

        public override bool AppliesToEntity(Projectile entity, bool lateInstantiation) {
            return entity.type == ProjectileID.BabyHornet;
        }

        public override void PostAI(Projectile projectile) {
            // 1) 主人检查
            // 2) 无增益/死亡直接返回
            // 3) 索敌（OwnerMinionAttackTargetNPC 优先，然后 Main.ActiveNPCs）
            // 4) 没目标：计时归零并返回
            // 5) 非物主客户端：直接返回（避免多人重复生成）
            // 6) 冷却到：发射毒刺
            // 7) 计数到 ShotsPerHive：发射蜂巢并把计数归零
        }

        private static NPC FindNearestTarget(Projectile projectile, float range) {
            // 见下方“关键片段”
            return null;
        }

        private static void FireStinger(Projectile pet, Player owner, NPC target) {
            // 见下方“关键片段”
        }

        private static void FireHive(Projectile pet, Player owner, NPC target) {
            // 见下方“关键片段”
        }

        private static Vector2 CalculateLob(Vector2 start, Vector2 end, float gravity, int flightTime) {
            // 见下方“关键片段”
            return Vector2.Zero;
        }
    }
}
```

> **注意**：上面常量后面的 `0` / `0f` 只是占位，不是让你照抄运行。
> 先按第 9 节的参数表填好，否则会出现 `HiveFlightTime = 0` 除零、`StingerCooldown = 0` 每帧发射、`DetectRange = 0` 永远没目标等问题。

### `PostAI` 的算法逻辑

```text
PostAI(projectile):
    owner = Main.player[projectile.owner]

    if !owner.active || owner.dead:
        return

    if !owner.HasBuff(BuffID.BabyHornet):
        return

    target = FindNearestTarget(projectile, DetectRange)

    if target == null:
        stingerTimer = 0
        return

    if Main.myPlayer != projectile.owner:
        return                      // 只有物主客户端生成弹幕

    if ++stingerTimer < StingerCooldown:
        return

    stingerTimer = 0
    FireStinger(projectile, owner, target)

    if ++stingerCount >= ShotsPerHive:
        stingerCount = 0
        FireHive(projectile, owner, target)
```

注意：第 20 发是“毒刺 + 蜂巢”同时发出，然后计数归零；如果你想让第 20 发只发蜂巢，把 `FireStinger` 放到判断之后即可。

> 这只宠物仍然是原版 `Main.projPet`，**不要**设 `Projectile.minion = true` / `minionSlots`；否则会占召唤栏并改变原版宠物行为。`MinionTargettingFeature` 只负责让右键标记生效。

对照实现：`TutorialMod/Content/Pets/BabyEater/BabyEaterGlobalProjectile.cs` 的 `PostAI`（83–142 行）和 `Spawn`（165–173 行）就是同一套结构，可以直接对着改。

### 关键片段 1：索敌

```cs
private static NPC FindNearestTarget(Projectile projectile, float range) {
    // 鞭子标记 / 右键指定的目标优先
    NPC marked = projectile.OwnerMinionAttackTargetNPC;
    if (marked != null && marked.CanBeChasedBy()) {
        return marked;
    }

    NPC result = null;
    float bestDistance = range;

    foreach (NPC npc in Main.ActiveNPCs) {
        if (!npc.CanBeChasedBy()) {
            continue; // 排除友方、无敌、已死亡目标
        }

        float distance = Vector2.Distance(npc.Center, projectile.Center);
        if (distance >= bestDistance) {
            continue;
        }

        // 视线检查：很近的目标允许隔墙锁定，避免宠物贴着墙不攻击
        bool lineOfSight = Collision.CanHitLine(
            projectile.position, projectile.width, projectile.height,
            npc.position, npc.width, npc.height);

        if (!lineOfSight && distance > 100f) {
            continue;
        }

        bestDistance = distance;
        result = npc;
    }

    return result;
}
```

### 关键片段 2：发射毒刺

```cs
private static void FireStinger(Projectile pet, Player owner, NPC target) {
    Vector2 direction = (target.Center - pet.Center).SafeNormalize(-Vector2.UnitY);
    Vector2 spawnPosition = pet.Center + direction * StingerSpawnOffset;

    int damage = (int)owner.GetTotalDamage(DamageClass.Summon).ApplyTo(StingerDamage);
    float knockback = owner.GetKnockback(DamageClass.Summon).ApplyTo(StingerKnockback);

    Projectile.NewProjectile(
        pet.GetSource_FromAI(),
        spawnPosition,
        direction * StingerSpeed,
        ProjectileID.HornetStinger,
        damage,
        knockback,
        pet.owner);
}
```

要点：

- `ProjectileID.HornetStinger` 是黄蜂随从的友方毒刺；它的 `aiStyle = 1`，带一点下坠，命中一个目标后消失。
- 它在原版 `ProjectileID.Sets.MinionShot` 里，所以伤害类型是召唤、能吃鞭子标记。
- 不要用 `ProjectileID.QueenBeeStinger`，那个是女王蜂的敌对弹幕。
- `SafeNormalize(-Vector2.UnitY)` 在两点重合时退化为朝上，避免除零。

### 关键片段 3：发射蜂巢（抛物线）

```cs
private static void FireHive(Projectile pet, Player owner, NPC target) {
    Vector2 start = pet.Center;
    Vector2 velocity = CalculateLob(start, target.Center, HiveGravity, HiveFlightTime);

    int damage = (int)owner.GetTotalDamage(DamageClass.Summon).ApplyTo(HiveDamage);
    float knockback = owner.GetKnockback(DamageClass.Summon).ApplyTo(HiveKnockback);

    Projectile.NewProjectile(
        pet.GetSource_FromAI(),
        start,
        velocity,
        ModContent.ProjectileType<NectarHive>(),
        damage,
        knockback,
        pet.owner);
}

// 已知重力 g 和飞行时间 t，反推初速度，让弹幕在 t 帧后到达目标点。
// x = vx * t
// y = vy * t + 0.5 * g * t²
private static Vector2 CalculateLob(Vector2 start, Vector2 end, float gravity, int flightTime) {
    Vector2 delta = end - start;
    float vx = delta.X / flightTime;
    float vy = (delta.Y - 0.5f * gravity * flightTime * flightTime) / flightTime;
    return new Vector2(vx, vy);
}
```

要点：

- `HiveFlightTime` 是“预计落到目标点”的帧数；目标移动时会打偏，但蜂巢仍有 5 秒/碰墙/碰敌人三种爆炸兜底。
- 如果想让蜂巢“悬浮飞行”，把宠物侧的 `HiveGravity` 和蜂巢侧的 `Gravity` 都设为 0（两处必须一致），5 秒寿命就是主要的结束条件。
- `ModContent.ProjectileType<NectarHive>()` 取自定义弹幕的运行时 ID。

### 多人与同步

- `PostAI` 在所有端执行，所以索敌和计时可以各端跑；**生成弹幕只在 `Main.myPlayer == projectile.owner` 时调用**。
- `Projectile.NewProjectile` 在物主客户端调用时会自动同步给服务器和其他客户端，不要在服务器端再生成一次。
- `stingerTimer` / `stingerCount` 是普通字段，没有同步；它们只影响“物主什么时候生成弹幕”，远程客户端不需要一致。
- 如果以后要把“当前状态”显示给其他玩家（例如蜂巢层数），再用 `projectile.ai[]` 或 `SendExtraAI`/`ReceiveExtraAI`。

参考：

- <https://docs.tmodloader.net/docs/1.4-stable/class_terraria_1_1_mod_loader_1_1_global_projectile.html>
- <https://github.com/tModLoader/tModLoader/wiki/IEntitySource>
- <https://github.com/tModLoader/tModLoader/wiki/Basic-Netcode>

---

## 8. `NectarHive.cs`：蜂巢弹幕

### 职责

蜂巢的飞行、三种爆炸条件、爆炸表现和生成小蜜蜂。

### 类骨架

```cs
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace TutorialMod.Content.Pets.Nectar
{
    public class NectarHive : ModProjectile
    {
        // 复用原版蜂巢陷阱弹幕的贴图，无需新 png。
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.BeeHive;

        // 爆炸参数（具体数值见第 9 节）
        private const float Gravity = 0f;
        private const int Lifetime = 0;
        private const int BeeMin = 0;
        private const int BeeMax = 0;
        private const int BeeDamage = 0;
        private const float BeeKnockback = 0f;

        // 防止 OnKill / OnTileCollide / OnHitNPC 重复爆炸。
        private bool exploded;

        public override void SetStaticDefaults() {
            // 让蜂巢吃到鞭子的召唤标记加成。
            ProjectileID.Sets.MinionShot[Type] = true;
        }

        public override void SetDefaults() {
            // 见下方“关键片段 1”
        }

        public override void AI() {
            // 见下方“关键片段 2”
        }

        public override bool OnTileCollide(Vector2 oldVelocity) {
            // 见下方“关键片段 3”：false 不会自动销毁，必须自己 Kill
            Explode();
            Projectile.Kill();
            return false;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            // 见下方“关键片段 4”
        }

        public override void OnKill(int timeLeft) {
            // 见下方“关键片段 5”
        }

        public override bool? CanCutTiles() {
            return false; // 不割草、不砸罐
        }

        private void Explode() {
            // 见下方“关键片段 6”
        }
    }
}
```

### 关键片段 1：`SetDefaults`

```cs
public override void SetDefaults() {
    // 复用原版蜂巢陷阱的尺寸（31×31）等基础值。
    Projectile.CloneDefaults(ProjectileID.BeeHive);

    // 原版 655 是“陷阱”，会把 hostile/trap/penetrate 一起复制过来，必须逐项改掉。
    Projectile.aiStyle = 0;          // 0 = 不跑原版 AI，全部自己写
    Projectile.friendly = true;      // 只伤害敌人
    Projectile.hostile = false;      // 不伤害玩家
    Projectile.trap = false;
    Projectile.penetrate = 1;        // 命中一个目标就结束
    Projectile.tileCollide = true;   // 碰方块爆炸
    Projectile.ignoreWater = true;
    Projectile.timeLeft = Lifetime;  // 5 秒兜底（见参数表）
    Projectile.DamageType = DamageClass.Summon;
}
```

> `CloneDefaults` 会把 `hostile = true`、`trap = true`、`penetrate = -1` 一起复制过来。只改 `friendly` 不改 `hostile`，蜂巢会同时伤害玩家和敌人，这是最常见的翻车点。

### 关键片段 2：`AI`（飞行）

```cs
public override void AI() {
    // 抛物线：每帧给 Y 加速；把 Gravity 设为 0 就变成悬浮直线飞行。
    Projectile.velocity.Y += Gravity;

    // 蜂巢是方块状贴图，做一点翻滚即可；不需要像箭一样对准速度。
    Projectile.rotation += Projectile.velocity.X * 0.02f;
}
```

> 5 秒寿命由 `Projectile.timeLeft` 自动倒计时，归零时引擎调用 `OnKill`；不需要自己再写 `ai[0]` 计时器。

### 关键片段 3：`OnTileCollide`

```cs
public override bool OnTileCollide(Vector2 oldVelocity) {
    Explode();
    Projectile.Kill(); // 立即销毁蜂巢
    return false;      // false = 不走原版碰撞处理；上面已经手动 Kill 了
}
```

也可以写成“交给原版销毁”：

```cs
public override bool OnTileCollide(Vector2 oldVelocity) {
    Explode();
    return true; // true = 让原版碰撞代码处理（通常会销毁弹幕）
}
```

> 关键：`return false` **不是**“销毁”。它只是跳过原版碰撞处理，弹幕会继续存在，直到 `timeLeft` 归零。
> 想让蜂巢撞墙立刻消失，必须在 `false` 之前自己 `Projectile.Kill()`，或者直接 `return true`。

### 为什么 `OnTileCollide` 有返回值，而 `OnKill` / `OnHitNPC` 没有？

- `OnTileCollide` 是**决策钩子**：它先于原版碰撞处理调用，游戏在问“这次碰撞要不要按原版规则处理？”
  - `return true`：让原版处理（通常会销毁弹幕）；
  - `return false`：跳过原版处理，弹幕不会自动消失；要消失就自己 `Projectile.Kill()`。
- `OnHitNPC` / `OnKill` 是**通知钩子**：事情已经发生了（命中/死亡），游戏只是通知你，不需要你决定“要不要继续”。
  - `OnHitNPC` 在命中结算后调用，返回 `void`；要提前结束弹幕，自己调 `Projectile.Kill()`；
  - `OnKill` 在弹幕已经死亡时调用，返回 `void`；这里只做爆炸/掉落/音效。
- 同一个 `OnTileCollide` 两种写法都能让蜂巢消失，区别只是“谁来 Kill”：
  ```cs
  Explode();
  Projectile.Kill();
  return false; // 自己负责销毁

  // 或者
  Explode();
  return true;  // 交给原版销毁
  ```
  不要在 `Projectile.Kill()` 之后返回 `true`，那会让原版再处理一个已经死亡的弹幕，没必要。

### 关键片段 4：`OnHitNPC`

```cs
public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
    Explode();
    Projectile.Kill();
}
```

### 关键片段 5：`OnKill`

```cs
public override void OnKill(int timeLeft) {
    // 寿命到 0、被 OnHitNPC 杀死、OnTileCollide 里手动 Kill()，最终都会走到这里。
    // exploded 守卫保证只爆一次。
    Explode();
}
```

> **1.4.4 注意**：`ModProjectile.Kill(int)` 已标记为 obsolete，重命名为 `OnKill(int)`；写成 `Kill` 会直接编译失败。
> `Projectile.Kill()` 是销毁弹幕的普通方法，仍然可以调用，两者不要混淆。
> 现成参考：`ExampleMod/Content/Projectiles/ExampleExplosive.cs` 的 `OnKill`（134–202 行），里面也演示了 `Projectile.owner == Main.myPlayer` 时生成子弹幕。

### 关键片段 6：`Explode`

```cs
private void Explode() {
    if (exploded) {
        return;
    }

    exploded = true;

    // 表现层：所有客户端都播放
    SoundEngine.PlaySound(SoundID.Item14, Projectile.Center);

    for (int i = 0; i < 16; i++) {
        Vector2 velocity = Main.rand.NextVector2Circular(3f, 3f);
        Dust dust = Dust.NewDustPerfect(
            Projectile.Center,
            i % 2 == 0 ? DustID.Honey : DustID.Bee,
            velocity,
            100,
            default,
            1.2f);
        dust.noGravity = true;
    }

    // 生成弹幕：只在物主客户端
    if (Main.myPlayer != Projectile.owner) {
        return;
    }

    Player owner = Main.player[Projectile.owner];
    if (!owner.active || owner.dead) {
        return;
    }

    int count = Main.rand.Next(BeeMin, BeeMax + 1);
    int damage = (int)owner.GetTotalDamage(DamageClass.Summon).ApplyTo(BeeDamage);
    float knockback = owner.GetKnockback(DamageClass.Summon).ApplyTo(BeeKnockback);

    for (int i = 0; i < count; i++) {
        Vector2 spawnPosition = Projectile.Center + Main.rand.NextVector2Circular(12f, 12f);
        Vector2 velocity = Main.rand.NextVector2Circular(5f, 5f) - Vector2.UnitY * 1.5f;

        Projectile.NewProjectile(
            Projectile.GetSource_FromAI(),
            spawnPosition,
            velocity,
            ProjectileID.Bee,
            damage,
            knockback,
            Projectile.owner);
    }
}
```

### 要点

- `ProjectileID.Bee` 是养蜂人/蜂枪同款的友方蜜蜂，生成后由原版 AI 自动追踪敌人，不需要自己写追踪。
- 蜜蜂的伤害在生成时通过 `NewProjectile` 的 `damage` 参数传进去；`DamageType` 不是必须改。
- 原版蜜蜂不在 `MinionShot` 集合里，所以**小蜜蜂吃不到鞭子标记加成**；如果在意这一点，可以把蜜蜂换成自定义 `ModProjectile`，并设置 `ProjectileID.Sets.MinionShot[Type] = true`（见第 13.2 节）。
- `Explode` 在 `OnKill` 里也会被调用（5 秒寿命），所以不要只在 `OnHitNPC` 里写爆炸。
- `exploded` 是每个弹幕实例自己的字段，不参与网络同步；它只用于防止同端重复生成蜜蜂/尘。各端都会各自爆炸一次表现层，这是正常的。

参考：

- <https://docs.tmodloader.net/docs/1.4-stable/class_terraria_1_1_mod_loader_1_1_mod_projectile.html>
- <https://github.com/tModLoader/tModLoader/wiki/Basic-Projectile>
- <https://github.com/tModLoader/tModLoader/wiki/Basic-Minion-Guide>
- 爆炸 + 子弹幕参考：`ExampleMod/Content/Projectiles/ExampleExplosive.cs`（`OnKill` 134–202 行）

---

## 9. 参数表

代码里用常量名，不要把数字散落在方法里。单位：帧 = 1/60 秒，像素 = px。

| 常量 | 作用 | 单位 | 建议范围 | 本需求建议值 |
| --- | --- | --- | --- | ---: |
| `StingerCooldown` | 两发毒刺之间的间隔 | 帧 | 20～60 | 30（0.5 秒） |
| `ShotsPerHive` | 多少发毒刺后掷蜂巢 | 发 | 10～30 | **20** |
| `StingerDamage` | 毒刺基础伤害 | 点 | 8～20 | 12 |
| `StingerKnockback` | 毒刺基础击退 | — | 1～4 | 2 |
| `StingerSpeed` | 毒刺初速度 | px/帧 | 8～16 | 12 |
| `StingerSpawnOffset` | 毒刺从宠物中心往前多少生成 | px | 12～28 | 20 |
| `DetectRange` | 宠物索敌半径 | px | 500～900 | 700 |
| `HiveDamage` | 蜂巢撞击伤害 | 点 | 10～30 | 18 |
| `HiveKnockback` | 蜂巢撞击击退 | — | 2～5 | 3 |
| `HiveGravity` | 宠物侧反推抛物线用的重力（必须与 `NectarHive.Gravity` 相同） | px/帧² | 0～0.25 | 0.15 |
| `NectarHive.Gravity` | 蜂巢每帧实际受到的 Y 加速度 | px/帧² | 0～0.25 | 0.15 |
| `HiveFlightTime` | 抛物线预计落到目标的帧数 | 帧 | 30～60 | 45（0.75 秒） |
| `NectarHive.Lifetime` | 蜂巢最长存在时间 | 帧 | 180～420 | **300（5 秒）** |
| `BeeMin` / `BeeMax` | 爆炸生成蜜蜂数量范围 | 只 | 2～6 | 3 / 5 |
| `BeeDamage` | 每只蜜蜂基础伤害 | 点 | 6～15 | 10 |
| `BeeKnockback` | 蜜蜂基础击退 | — | 1～4 | 2 |

调参建议：

- 觉得宠物太吵/太强：先加 `StingerCooldown`，再降 `StingerDamage`。
- 觉得蜂巢太频繁：加 `ShotsPerHive`；想更频繁：减到 10～15。
- 蜂巢总是砸地：减 `HiveGravity` / `NectarHive.Gravity`、加 `HiveFlightTime`，或把两者都设为 0 做悬浮蜂巢。
- 蜜蜂太多卡顿：把 `BeeMax` 降到 4，或把 `BeeDamage` 提高、数量减少。

> **重力常量要一致**：`NectarGlobalProjectile.HiveGravity` 只用于反推抛物线初速度，
> `NectarHive.Gravity` 才是蜂巢每帧真正受到的重力。两处不一致，蜂巢就会落在目标前方或后方。
> 更稳妥的做法是把蜂巢参数集中到一个 `static class NectarBalance`，或者把 `NectarHive.Gravity`
> 改成 `public const`，让 `NectarGlobalProjectile` 直接引用它。

---

## 10. 本地化（hjson）

在 `TutorialMod/Localization/zh-Hans_Mods.TutorialMod.hjson` 和 `en-US_Mods.TutorialMod.hjson` 的**根级**添加（不要放进 `Items:`）：

```hjson
NectarGlobalItem.Tooltip:
    '''
    召唤宠物蜜蜂
    它会发射毒刺攻击附近的敌人
    每发射 20 枚毒刺后掷出蜂巢，蜂巢会炸出小蜜蜂
    '''
NectarGlobalBuff.Description: 它会发射毒刺，并定期掷出蜂巢
```

英文示例：

```hjson
NectarGlobalItem.Tooltip:
    '''
    Summons a pet Bee
    It fires stingers at nearby enemies
    Every 20 stingers it throws a hive that releases bees
    '''
NectarGlobalBuff.Description: It fires stingers and periodically throws a hive
```

注意：

- 代码里 `Mod.GetLocalization($"{nameof(NectarGlobalItem)}.Tooltip")` 生成的 key 是 `Mods.TutorialMod.NectarGlobalItem.Tooltip`；hjson 根级写 `NectarGlobalItem.Tooltip` 就能对上。
- 如果写进 `Items:` 里，key 会变成 `Items.NectarGlobalItem.Tooltip`，代码取不到，显示为原始 key 或空。
- 花蜜的物品名“花蜜”来自原版，不用改；这里只改 Tooltip 和增益说明。
- 如果 `en-US_Mods.TutorialMod.hjson` 里出现 `Mods.TutorialMod.NectarGlobalItem.Tooltip` 这种原始 key，说明还没翻译，进游戏会直接显示 key；补上英文即可。
- 修改 hjson 后重新 Build + Reload 才会生效。

参考：<https://github.com/tModLoader/tModLoader/wiki/Localization>

---

## 11. 构建与测试

### 构建

首选游戏内：**Workshop → Develop Mods → Build + Reload**，看是否有编译错误。

命令行（可选）：

```powershell
cd "C:\Users\zh180\Documents\My Games\Terraria\tModLoader\ModSources\TutorialMod"
dotnet build
```

### 测试清单

- [ ] 用花蜜召唤宠物，原版蜜蜂正常跟随、漂浮、传送。
- [ ] 物品 Tooltip 显示新说明；增益栏说明也变了。
- [ ] 附近放一个训练假人（Target Dummy），宠物会定期发射毒刺。
- [ ] 毒刺命中后造成伤害；换上召唤师装备后伤害变高。
- [ ] 第 20 发毒刺后出现蜂巢，蜂巢沿抛物线飞向目标。
- [ ] 蜂巢命中敌人会爆开并生成蜜蜂。
- [ ] 蜂巢砸到方块会爆开并生成蜜蜂。
- [ ] 蜂巢在空旷处飞满 5 秒会爆开并生成蜜蜂（可临时把 `NectarHive.Gravity` 和 `HiveGravity` 都设为 0 方便观察）。
- [ ] 小蜜蜂生成后会追踪敌人，约 10 秒后消失。
- [ ] 宠物没有增益/主人死亡时不再攻击。
- [ ] 多人：两个客户端只看到一套毒刺/蜂巢/蜜蜂，没有翻倍。

### 调试小技巧

- 临时在 `PostAI` 里 `Main.NewText($"timer={stingerTimer}, count={stingerCount}")`，确认计数节奏。
- 蜂巢不爆：检查 `timeLeft` 是否被 `CloneDefaults` 覆盖、`OnTileCollide` 里是否调用了 `Explode()`、`OnKill` 里是否调用了 `Explode`。
- 蜂巢撞墙后不消失：检查 `OnTileCollide` 是否只写了 `return false`；`false` 不会销毁，必须自己 `Projectile.Kill()` 或返回 `true`。
- 蜂巢不生成蜜蜂：检查 `Main.myPlayer == Projectile.owner`、`owner.active`，以及是否在 `Explode` 里提前 `return`。
- 毒刺打不到：检查 `DetectRange`、`CanBeChasedBy`、视线判断和 `StingerSpeed`。

---

## 12. 常见坑与排查

1. **写 `class Nectar : ModItem` 想覆盖原版**
   只会新增物品，原版 1170 不变。用 `GlobalItem`/`GlobalBuff`/`GlobalProjectile`。

2. **`GlobalProjectile` 忘了 `InstancePerEntity => true`**
   所有宠物共用一份实例，计时器和计数互相干扰，表现为“有的宠物不发炮/蜂巢乱出”。

3. **在 `PostAI` 里所有客户端都 `NewProjectile`**
   多人游戏会翻倍。生成前判断 `Main.myPlayer == projectile.owner`。

4. **`PostAI` 和 `PreAI` 搞混**
   `PostAI` 保留原版 AI；`PreAI` 返回 `false` 才会拦截原版 AI。只想追加攻击就用 `PostAI`。

5. **蜂巢 `CloneDefaults(ProjectileID.BeeHive)` 后没改 `hostile` / `trap`**
   原版 655 是陷阱，会伤害玩家。必须 `hostile = false; trap = false;`。

6. **蜂巢贴图路径写错**
   正确：`"Terraria/Images/Projectile_" + ProjectileID.BeeHive`。
   不要加 `.png`，不要写成 `Projectiles/...` 或 `Terraria/Projectile_655`。

7. **用了 `ProjectileID.QueenBeeStinger`**
   那是敌对毒刺，会打玩家。用 `ProjectileID.HornetStinger`（374）。

8. **以为花蜜物品有伤害**
   原版花蜜 `Item.damage = 0`。伤害来自我们生成的毒刺/蜂巢/蜜蜂，用固定基础值 × 召唤加成。

9. **忘了鞭子标记**
   `HornetStinger` 自带 `MinionShot`；自定义蜂巢要手动 `ProjectileID.Sets.MinionShot[Type] = true`；原版 `ProjectileID.Bee` 不是 `MinionShot`，所以小蜜蜂吃不到标记加成。
   另外，`NectarGlobalProjectile.SetStaticDefaults` 里要设 `ProjectileID.Sets.MinionTargettingFeature[ProjectileID.BabyHornet] = true`，否则右键标记不会作用到这只宠物。

10. **`OnTileCollide` 返回值理解反了**
    `true` = 交给原版碰撞代码处理（通常会销毁）；`false` = 跳过原版处理，弹幕不会自动消失。
    撞墙要立刻消失：`Explode(); Projectile.Kill(); return false;`，或者 `Explode(); return true;`。

11. **只在 `OnHitNPC` 里爆炸**
    5 秒寿命和碰方块不会爆炸。把爆炸放在 `OnKill`，并在 `OnTileCollide`/`OnHitNPC` 里也调用，配合 `exploded` 守卫。

12. **在 `Explode` 里没有守卫**
    `OnHitNPC`、`OnTileCollide`、`OnKill` 都可能触发爆炸；尤其 `OnTileCollide` 里手动 `Kill()` 后还会走一次 `OnKill`，可能爆两次、生成两批蜜蜂。用 `bool exploded`。

13. **本地化 key 放错层级**
    `NectarGlobalItem.Tooltip` 要放根级，不要放 `Items:` 下。

14. **在服务器端生成蜜蜂**
    `Main.myPlayer` 在服务器上不是物主；只在物主客户端生成，`NewProjectile` 会自动同步。

15. **`Projectile.timeLeft` 被 `CloneDefaults` 覆盖**
    `CloneDefaults` 之后再写 `Projectile.timeLeft = Lifetime;`，顺序不能反。

16. **小蜜蜂不追踪**
    检查是否真的生成了 `ProjectileID.Bee`，以及目标是否 `CanBeChasedBy()`；原版蜜蜂只追可攻击目标。

17. **用了 1.4.4 已废弃的 `Kill(int)`**
    `ModProjectile.Kill(int)` 已标记为 obsolete，重命名为 `OnKill(int)`；写 `Kill` 会编译失败。
    `Projectile.Kill()` 是销毁弹幕的普通方法，仍然可以调用，两者不要混淆。

---

## 13. 进阶方向

### 13.1 完全接管宠物移动（更像战斗宠物）

在 `NectarGlobalProjectile` 里改：

```cs
public override bool PreAI(Projectile projectile) {
    // 自己写移动/悬停/冲刺
    // 返回 false：拦截原版宠物 AI
    return false;
}
```

代价：原版的漂浮、传送、动画节奏都要自己处理；建议先跑通 `PostAI` 版本，再考虑替换。

### 13.2 自定义小蜜蜂（吃鞭子标记）

新建 `NectarBee : ModProjectile`：

```cs
public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.Bee;

public override void SetStaticDefaults() {
    Main.projFrames[Type] = Main.projFrames[ProjectileID.Bee];
    ProjectileID.Sets.MinionShot[Type] = true;
}
```

然后自己写追踪、动画、命中逻辑（可参考 `BabyEater/CorruptEaterShot.cs` 的写法），蜂巢爆炸时生成它而不是 `ProjectileID.Bee`。

### 13.3 悬浮蜂巢

把宠物侧的 `HiveGravity` 和蜂巢侧的 `Gravity` 都设为 0，`HiveFlightTime` 只用于反推直线速度；蜂巢会直线飞向目标，5 秒后爆炸。适合“空中雷”玩法。

### 13.4 自定义贴图

把 `NectarHive.png` 放到 `Content/Pets/Nectar/` 下，删掉 `Texture` 覆盖（或改成自己的路径），tModLoader 会按“命名空间/类名”加载。

### 13.5 参数配置

用 `ModConfig` 把冷却、伤害、蜜蜂数量做成游戏内配置，避免每次改代码重新编译。

---

## 14. 注释模板（建议每个文件都写）

你可以直接套用这个文件头：

```cs
// ============================================================================
// NectarGlobalProjectile —— 给原版“花蜜”宠物追加毒刺/蜂巢攻击
// ============================================================================
// 【这是什么】GlobalProjectile，挂到所有弹幕上，用 AppliesToEntity 限定 BabyHornet。
// 【为什么这么做】1.4.4 不能用 ModItem 顶掉原版物品；原版宠物 AI 在弹幕上。
// 【机制】PostAI：索敌 -> 毒刺 -> 每 20 发 -> 蜂巢。
// 【涉及的原版 ID】
//   ItemID.Nectar / BuffID.BabyHornet / ProjectileID.BabyHornet
//   ProjectileID.HornetStinger / ProjectileID.Bee / ProjectileID.BeeHive
//   https://github.com/tModLoader/tModLoader/wiki/Vanilla-Content-IDs
// 【相关文档】...
// ============================================================================
```

每个 override 上方至少回答三个问题：

1. 这个钩子什么时候调用？
2. 原版默认行为是什么？
3. 这里为什么重写/追加？

---

## 15. 对照现有实现（改写附录）

> 这一节是给“已经照抄骨架、但不会填方法体”的情况准备的。先看对照表，再按 15.2 逐处改。

### 15.1 零件对照表

行号按当前文件；如果文件以后改过，按方法名找即可。

| 你要写的零件 | 现成实现 | 重点看什么 |
| --- | --- | --- |
| 宠物 `GlobalProjectile` 骨架、`InstancePerEntity`、`PostAI` | `TutorialMod/Content/Pets/BabyEater/BabyEaterGlobalProjectile.cs` | 42 行、75–77 行、83–142 行 |
| 索敌：鞭子标记优先 + 最近敌人 + 视线 | 同上 | 176–193 行 |
| 生成射击、伤害/击退按召唤加成缩放 | 同上 | 165–173 行 |
| 自定义弹幕：复用贴图、`SetDefaults`、追踪 | `TutorialMod/Content/Pets/BabyEater/CorruptEaterShot.cs` | 40–64 行、68–91 行、132–149 行 |
| 抛物线投掷：反推初速度 | `TutorialMod/Content/Pets/BabyPenguin/BabyPenguinGlobalProjectile.cs` | 250–260 行（`ThrowFish`）、290–295 行（`CalculateLob`） |
| 投掷物的重力、旋转、寿命 | `TutorialMod/Content/Pets/BabyPenguin/ThrownFish.cs` | 25–63 行 |
| 爆炸、`OnKill` 里生成子弹幕、物主判断 | `ExampleMod/Content/Projectiles/ExampleExplosive.cs` | 30–43 行（`timeLeft = 300` 就是 5 秒）、55–82 行（`OnTileCollide`）、134–202 行（`OnKill`） |
| 右键标记生效 | `ExampleMod/Content/Projectiles/Minions/ExampleSimpleMinion.cs` | 102 行：`MinionTargettingFeature[Type] = true;` |
| 物品/增益文本 | `TutorialMod/Content/Pets/BabyEater/BabyEaterGlobalItem.cs`、`BabyEaterGlobalBuff.cs` | 你现在的 `NectarGlobalItem` / `NectarGlobalBuff` 已经基本正确 |

### 15.2 现有 `NectarGlobalProjectiles.cs` 的逐处修正

**① `Player.owner` 不存在**

```cs
// 错误
Player.owner = Main.player[projectile.owner];

// 正确（参考 BabyEaterGlobalProjectile.cs:84）
Player owner = Main.player[projectile.owner];
```

**② `FindNearestTarget` 少了返回和方法结尾**

`foreach` 结束后要补：

```cs
    return result;
}
```

否则 `FireStinger` 会被嵌进 `FindNearestTarget`，引发一连串编译错误。

**③ 毒刺 ID 用错**

```cs
// 错误：Stinger 是敌对毒刺
ProjectileID.Stinger

// 正确：HornetStinger 是黄蜂随从的友方毒刺，且是 MinionShot
ProjectileID.HornetStinger
```

**④ 常量不要留 0**

先按第 9 节的参数表填。起步可以用：

```cs
private const int StingerCooldown = 30;
private const int ShotsPerHive = 20;
private const int StingerDamage = 12;
private const float StingerKnockback = 2f;
private const float StingerSpeed = 12f;
private const float DetectRange = 700f;
private const int HiveDamage = 18;
private const float HiveKnockback = 3f;
private const float HiveGravity = 0.15f;
private const int HiveFlightTime = 45;
private const float StingerSpawnOffset = 20f;
```

蜂巢类里的 `Gravity` / `Lifetime` 也要用同一套值：`Gravity = 0.15f`、`Lifetime = 300`。`HiveGravity` 只负责反推初速度，`NectarHive.Gravity` 才是实际每帧重力，两者不一致蜂巢就会落偏。

**⑤ `FireHive` / `CalculateLob` 照抄 BabyPenguin**

看 `BabyPenguinGlobalProjectile.cs`：

- `ThrowFish`（250–260 行）：算出速度、生成弹幕；
- `CalculateLob`（290–295 行）：反推初速度。

你只需要把目标弹幕换成：

```cs
ModContent.ProjectileType<NectarHive>()
```

**⑥ 补右键标记**

在 `NectarGlobalProjectile` 里加：

```cs
public override void SetStaticDefaults() {
    // 让这只宠物可以被鞭子/右键标记，索敌里的 OwnerMinionAttackTargetNPC 才会生效
    ProjectileID.Sets.MinionTargettingFeature[ProjectileID.BabyHornet] = true;
}
```

**⑦ 文件名**

`NectarGlobalProjectiles.cs` 是复数，类名是单数。tModLoader 不会因此报错，但按现有代码习惯建议改成 `NectarGlobalProjectile.cs`。

### 15.3 新文件 `NectarHive.cs` 看两个文件就够

- **飞行**：照 `ThrownFish.cs`（贴图、`SetDefaults`、`AI` 里加重力/旋转）。
- **爆炸**：照 `ExampleExplosive.cs` 的 `OnKill`（134–202 行）。

关键差异：

```cs
// 1.4.4 的钩子叫 OnKill，不叫 Kill
public override void OnKill(int timeLeft) {
    Explode();
}
```

- `OnTileCollide` 里写 `Explode(); Projectile.Kill(); return false;`：`false` 只是跳过原版碰撞处理，**不会自动销毁**，所以必须自己 `Kill()`；或者写 `Explode(); return true;` 交给原版销毁。
- 生成蜜蜂前判断 `if (Projectile.owner == Main.myPlayer)`，参考 `ExampleExplosive.cs:136`（等价于 `Explode` 里的 `if (Main.myPlayer != Projectile.owner) return;`）。
- `Explode` 里用 `bool exploded` 防止 `OnHitNPC` / `OnTileCollide` / `OnKill` 重复生成。

### 15.4 推荐阅读顺序

1. `BabyEaterGlobalProjectile.cs`：看 `PostAI` 怎么在宠物原版 AI 后追加攻击。
2. `BabyPenguinGlobalProjectile.cs` + `ThrownFish.cs`：看抛物线怎么反推、投掷物怎么飞。
3. `ExampleExplosive.cs` 的 `OnKill`：看爆炸、音效、生成子弹幕、物主判断。
4. `ExampleSimpleMinion.cs:102`：看右键标记。
5. `CorruptEaterShot.cs`：如果以后想把小蜜蜂换成自定义追踪弹幕，照它写。

### 15.5 本地化与文件名的小问题

- 英文 hjson 里如果还是：

  ```hjson
  NectarGlobalItem.Tooltip: Mods.TutorialMod.NectarGlobalItem.Tooltip
  NectarGlobalBuff.Description: Mods.TutorialMod.NectarGlobalBuff.Description
  ```

  说明还是原始 key，进游戏会直接显示这串文字。中文可以保留你写的版本，英文按中文意思补上即可。

- `using Terraria.Localization;` 如果没用到可以删掉，不影响编译。

---

## 16. 参考链接（均已验证可访问）

**API 文档**

- GlobalItem：<https://docs.tmodloader.net/docs/1.4-stable/class_terraria_1_1_mod_loader_1_1_global_item.html>
- GlobalBuff：<https://docs.tmodloader.net/docs/1.4-stable/class_terraria_1_1_mod_loader_1_1_global_buff.html>
- GlobalProjectile：<https://docs.tmodloader.net/docs/1.4-stable/class_terraria_1_1_mod_loader_1_1_global_projectile.html>
- ModProjectile：<https://docs.tmodloader.net/docs/1.4-stable/class_terraria_1_1_mod_loader_1_1_mod_projectile.html>
- ModBuff：<https://docs.tmodloader.net/docs/1.4-stable/class_terraria_1_1_mod_loader_1_1_mod_buff.html>

**WIKI 指南**

- 基础物品：<https://github.com/tModLoader/tModLoader/wiki/Basic-Item>
- 基础弹幕：<https://github.com/tModLoader/tModLoader/wiki/Basic-Projectile>
- 随从/宠物：<https://github.com/tModLoader/tModLoader/wiki/Basic-Minion-Guide>
- 弹幕类文档：<https://github.com/tModLoader/tModLoader/wiki/Projectile-Class-Documentation>
- 物品类文档：<https://github.com/tModLoader/tModLoader/wiki/Item-Class-Documentation>
- IEntitySource：<https://github.com/tModLoader/tModLoader/wiki/IEntitySource>
- 网络同步：<https://github.com/tModLoader/tModLoader/wiki/Basic-Netcode>
- 本地化：<https://github.com/tModLoader/tModLoader/wiki/Localization>
- 原版 ID 总表：<https://github.com/tModLoader/tModLoader/wiki/Vanilla-Content-IDs>
