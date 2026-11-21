# 示例宠物 ExamplePet · 对象档案

> 本模组里**唯一「新建内容」的宠物**（其余四只都是用 `Global*` 改写原版宠物）：新建物品 / 增益 / 宠物本体弹幕，带自己的贴图。
> 战斗逻辑：近距离用接触伤害冲撞，远距离保持悬停并召唤一条限时的原版星尘龙（不占召唤栏）。
> 「为什么这么改、踩过什么坑」见 [log/](../../log/README.md)，「这套架构能不能复用」见 [method/](../../method/README.md)。

## 本对象文件清单

```
examplepet/
├── README.md               本文：对象总览 + 源文件清单 + 状态逻辑
├── item.md                 ExamplePetItem：召唤物品（新建）
├── buff.md                 ExamplePetBuff：宠物增益（新建）
├── projectile.md           ExamplePetProjectile：宠物本体（新建）
└── stardust-dragon.md      PetStardustDragonLifetime：配套全局系统（星尘龙寿命）
```

## 4 个源文件清单

| 文件 | 位置 | 层 | 职责 | 档案 |
|---|---|---|---|---|
| `ExamplePetItem.cs` | `Content/Pets/ExamplePet/` | 物品 | 新建召唤物品，配方 10 泥土 + 工作台 | [item.md](item.md) |
| `ExamplePetBuff.cs` | `Content/Pets/ExamplePet/` | 增益 | 新建宠物增益，维持宠物弹幕 | [buff.md](buff.md) |
| `ExamplePetProjectile.cs` | `Content/Pets/ExamplePet/` | 弹幕 | 宠物本体，近战冲撞 / 远程召唤星尘龙 | [projectile.md](projectile.md) |
| `PetStardustDragonLifetime.cs` | `Common/GlobalProjectiles/` | 全局系统 | 统一管理宠物发射的星尘龙寿命、强制不占召唤栏 | [stardust-dragon.md](stardust-dragon.md) |

贴图：`ExamplePetItem.png` / `ExamplePetBuff.png` / `ExamplePetProjectile.png`（本对象自带 png，不复用原版）。

## 战斗逻辑速览（AI 三分支）

| 目标距离 | 行为 | `friendly` |
|---|---|---|
| 无目标 | 待机跟随玩家（`IdleMovement`） | false |
| < 100px | 近战：冲向敌人，接触伤害（10000） | true |
| ≥ 100px | 远程：保持 320px 悬停，每 2 秒召唤星尘龙（5000） | false |

- 宠物保持 `Main.projPet`，不设 `minion` / `minionSlots`，**不占用召唤栏**。
- 近战/远程伤害数值极高（10000 / 5000）是示例用途，可直接改常量。
- 星尘龙的寿命与「不占召唤栏」由配套系统 [stardust-dragon.md](stardust-dragon.md) 统一管理。

## 与其他对象的区别

其余四只宠物（企鹅 / 婴儿吞噬怪 / 和风鱼 / 花蜜蜜蜂）都是 `Global*` 改写原版、复用原版贴图；ExamplePet 是标准 `ModItem + ModBuff + ModProjectile` 新建链路，可作为「从零写一只宠物」的模板参考。
