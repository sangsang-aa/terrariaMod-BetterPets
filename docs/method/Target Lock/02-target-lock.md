# 目标锁定（ResolveTarget）

## 做法

- 玩家标记目标（`OwnerMinionAttackTargetNPC`）最高优先，可随时强制切换。
- 否则沿用已锁定目标（`whoAmI` 缓存），只要它还活着、可追击、在索敌范围内。
- 只有锁定目标失效才重新找最近的。

## 适用条件

多敌人场景，需要攻击连贯、不来回跳目标。

## 关键坑

不要在每帧用「最近距离」重选目标——目标会在相邻敌人间跳变（见 [log/02](../log/02-retarget.md)）。锁定要在目标失效时才解除。

## 出处

由 [log/02](../log/02-retarget.md) 验证得出；实现见 `PenguinGates.ResolveTarget`，档案见 [archives/babypenguin/projectile.md](../archives/babypenguin/projectile.md)。
