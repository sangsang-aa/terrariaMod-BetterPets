# ExamplePetBuff · 宠物增益

> 示例宠物的增益，本对象**新建**。属于 [ExamplePet 对象](README.md)。

## 基本信息

| 项 | 值 |
|---|---|
| 类名 | `ExamplePetBuff`（`ModBuff`） |
| 文件 | `Content/Pets/ExamplePet/ExamplePetBuff.cs` |
| 贴图 | `Content/Pets/ExamplePet/ExamplePetBuff.png` |

## 实现逻辑

- `SetStaticDefaults`：
  - `Main.buffNoTimeDisplay[Type] = true`：不显示剩余时间。
  - `Main.vanityPet[Type] = true`：标记为装饰性宠物（但弹幕本身仍可攻击）。
- `Update(Player, ref int buffIndex)`：增益在玩家身上时每帧调用，用 `BuffHandle_SpawnPetIfNeededAndSetTime` 在宠物弹幕不存在时生成一个，并刷新持续时间。

## 相关（本对象内）

- 对象总览：[README.md](README.md)
- 召唤物品：[item.md](item.md)
- 宠物本体：[projectile.md](projectile.md)
