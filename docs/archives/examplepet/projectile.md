# ExamplePetProjectile · 宠物本体

> 示例宠物本体，本对象**新建**。近距离接触冲撞、远距离召唤限时星尘龙。属于 [ExamplePet 对象](README.md)。

## 基本信息

| 项 | 值 |
|---|---|
| 类名 | `ExamplePetProjectile`（`ModProjectile`） |
| 文件 | `Content/Pets/ExamplePet/ExamplePetProjectile.cs` |
| 贴图 | `Content/Pets/ExamplePet/ExamplePetProjectile.png`（4 帧竖向动画） |

## 基础设定

- `Main.projPet[Type] = true`：宠物弹幕，不占召唤栏；`minion = false` / `minionSlots = 0`。
- `CloneDefaults(ProjectileID.ZephyrFish)` 后覆盖：`aiStyle=0`（自定义 AI）、`tileCollide=false`（穿墙）、`DamageType=Summon`、`penetrate=-1`、`netImportant=true`。
- `friendly` 初始 false，由 AI 按攻击方式开关（避免待机误伤小动物）。
- `MinionContactDamage() = true` 允许接触伤害；`CanCutTiles() = false`。
- 角色选择界面动画：`CharacterPreviewAnimations` + `SimpleLoop(0, 4, 6)`。

## AI 三分支

| 距离 | 行为 | 关键方法 |
|---|---|---|
| 无目标 | 待机跟随 | `IdleMovement`：>2000px 传送回玩家、>600px 加速 12、否则减速 4；惯性平滑 |
| < 100px（`MeleeRange`） | 近战冲撞 | `ChaseMovement`：>40px 才加速，避免贴身抖动；`friendly=true` |
| ≥ 100px | 远程召唤 | `RangedAttack`：保持 `RangedHoverDistance=320px` 悬停，每 `DragonCooldown=120` 帧召唤星尘龙 |

- 伤害可能被 buff 生成流程清零，AI 里持续写回 `Projectile.damage = ContactDamage`。
- 索敌半径 700px，`CanBeChasedBy` + 视线检查（100px 内可穿墙锁定）。
- 动画每 5 帧切一帧，`spriteDirection` 随水平速度，`rotation = velocity.X × 0.05`。

## 数值表

| 参数 | 值 | 含义 |
|---|---|---|
| `ContactDamage` / `RangedDamage` | 10000 / 5000 | 近战接触 / 星尘龙伤害（示例值） |
| `MeleeRange` / `RangedHoverDistance` | 100 / 320px | 近战切换距离 / 远程悬停距离 |
| `DragonCooldown` / `DragonLifetime` | 120 / 300 帧 | 召唤间隔（2 秒）/ 星尘龙初始寿命 |
| `knockBack` | 4 | 击退 |

## 星尘龙召唤（FireStardustDragon）

- 只在物主客户端生成；场上已有本宠物发射的星尘龙（`minion==false`）则不再重复生成。
- 按原版法杖方式生成 4 段（头 `StardustDragon1` + 身体 2/3 + 尾 4），用 `ai[0]` / `localAI[1]` 链接。
- 四段统一设 `minion=false` / `minionSlots=0`、`timeLeft=300`、`originalDamage`、`netUpdate`。
- 实际寿命由配套系统 [stardust-dragon.md](stardust-dragon.md) 接管。

## 相关（本对象内）

- 对象总览：[README.md](README.md)
- 星尘龙寿命系统：[stardust-dragon.md](stardust-dragon.md)
