# Bug 1 · 近战冲进敌人中心，模型重合

> 第一轮近战修复。状态：未根治，引出 Bug 2。

- **现象**：近战企鹅直接冲到敌人中心，模型与敌人重叠。
- **根因**：`Approach` 里 `SteerTo(target.Center)` 一路朝中心加速，用固定 `MeleeRange=70` 判定停手，没有「贴边」概念。
- **尝试 / 修正**：
  - 停手判定改为 `projectile.Hitbox.Intersects(target.Hitbox)`（相交即停）。
  - Ranged→Melee 切换条件同步从「10 格距离」改为 hitbox 相交。
  - 删除 `MeleeRecover`（回气）与 `Retreat`（后撤）两个状态及相关常量/方法。
  - 结果：编译通过，但 bug **仍复现**。
- **教训**：单纯把「固定距离」换成「hitbox 相交」是离散帧检测，冲过头那帧仍判相交，会留下钉在中心的隐患。
- **后续**：见 [02-retarget.md](02-retarget.md)。
