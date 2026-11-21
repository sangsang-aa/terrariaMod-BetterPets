# ExamplePetItem · 召唤物品

> 示例宠物的召唤物品，本对象**新建**（非改写原版）。属于 [ExamplePet 对象](README.md)。

## 基本信息

| 项 | 值 |
|---|---|
| 类名 | `ExamplePetItem`（`ModItem`） |
| 文件 | `Content/Pets/ExamplePet/ExamplePetItem.cs` |
| 贴图 | `Content/Pets/ExamplePet/ExamplePetItem.png` |

## 实现逻辑

- `CloneDefaults(ItemID.ZephyrFish)`：复制原版和风鱼的默认属性作为基础。
- `Item.shoot = ModContent.ProjectileType<ExamplePetProjectile>()`：使用时发射宠物弹幕。
- `Item.buffType = ModContent.BuffType<ExamplePetBuff>()`：使用时附加宠物增益。
- `UseItem`：仅本地玩家（`whoAmI == Main.myPlayer`）添加 3600 帧增益，避免多人重复添加。
- `AddRecipes`：10 个泥土 + 工作台。

## 相关（本对象内）

- 对象总览：[README.md](README.md)
- 增益：[buff.md](buff.md)
- 宠物本体：[projectile.md](projectile.md)
