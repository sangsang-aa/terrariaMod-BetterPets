# slab 法求贴边接触点（GetContactPoint）

从敌人中心朝宠物方向作射线，用 slab 法分别算射线到「敌人矩形边界」和「宠物矩形边界」的距离，取两轴较小者相加，再加间隙 `ContactGap`，得到宠物中心应落的位置。

```
dir = normalize(pet.Center - target.Center)
ax, ay = abs(dir.X), abs(dir.Y)
enemyReach = min(target.width/2 / ax, target.height/2 / ay)
petReach   = min(pet.width/2   / ax, pet.height/2   / ay)
contact    = target.Center + dir * (enemyReach + petReach + ContactGap)
```

## 适用条件

需要 A 精确贴在 B 边框外而不重合（近战贴边、绕障碍悬停）。

## 关键收益 / 坑

- 对细长敌人 / BOSS 也能贴准，比「用最大边长当半径的圆近似」更准。
- 坑：贴边后两者不再 hitbox 相交——所有依赖「相交」的判定要同步改成「到接触点距离」判定，否则会失效（见 [log/02](../log/02-retarget.md) 的 `UpdateMode` 修正）。

## 出处

由 [log/02](../log/02-retarget.md) 验证得出；实现见 `PenguinGates.GetContactPoint`。
