# PetStardustDragonLifetime · 星尘龙寿命管理（配套系统）

> 全局 `GlobalProjectile`：统一管理「由 ExamplePet 发射的星尘龙」的寿命，并确保它们永不占召唤栏。属于 [ExamplePet 对象](README.md)。

## 基本信息

| 项 | 值 |
|---|---|
| 类名 | `PetStardustDragonLifetime`（`GlobalProjectile`） |
| 文件 | `Common/GlobalProjectiles/PetStardustDragonLifetime.cs` |
| 类型 | 全局系统（`InstancePerEntity => true`） |

## 机制（写在 AI 里）

- **只处理宠物发射的星尘龙**：`minion == false` 的 `StardustDragon1~4` 四段。玩家自己召唤的星尘龙 `minion == true`，完全不受影响。
- **首次遇到初始化**：`initialized` 标志 + `life = Lifetime`（300 帧 = 5 秒）。
- **持续写回不占召唤栏**：每帧 `projectile.minion = false`、`minionSlots = 0`，防止其他逻辑改回去。
- **接管存活时间**：每帧 `life--` 并强制 `timeLeft = 2`（无论玩家身上有没有星尘龙 buff），到 0 时 `projectile.Kill()`。

## 参数

| 项 | 值 |
|---|---|
| `Lifetime` | 300 帧（5 秒） |

## 为什么需要它

原版星尘龙的寿命与「占召唤栏」由玩家 buff 驱动；宠物发射的这几段必须**独立于玩家 buff** 按时消失，且不消耗召唤栏——这套逻辑集中在这个全局系统里，而不是散在召唤代码里。召唤方见 [projectile.md](projectile.md) 的 `FireStardustDragon`。

## 相关（本对象内）

- 对象总览：[README.md](README.md)
- 召唤方：[projectile.md](projectile.md)
