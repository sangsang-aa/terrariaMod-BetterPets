# Bug 2 · 贴边砍一段时间后莫名再索敌、冲进中心

> 第二轮近战修复。状态：仍未根治，引出 Bug 3 的分段测试。

- **现象**：上一轮修复后，贴边砍一阵子会突然重新索敌，冲进敌人中心挥砍。
- **根因（两个叠加）**：
  1. `FindNearestTarget` 每帧重选最近敌人 → 目标在相邻敌人间跳变。
  2. 朝 `target.Center` 加速 + hitbox 相交是离散帧检测，冲过头那帧仍判相交 → 把「敌人中心」记成 `meleeHoldPosition` 钉住。
- **尝试 / 修正**：
  - 目标锁定：新增 `lockedTarget`，`ResolveTarget` 优先用玩家标记目标，否则沿用锁定目标直到失效才重选。
  - 几何贴边：新增 `GetContactPoint`（slab 法算接触点）+ `PinToContact` 精确钉位；近战追接触点而非中心。
  - `UpdateMode` 的切换判定从 hitbox 相交改为「到接触点距离 ≤14px」（因为贴边后在框外 4px，不再相交，旧判定会失效）。
  - 结果：编译通过，但 bug **仍复现**。
- **教训**：目标锁定与几何贴边本身正确（后来被三层重构沿用，见 [method/02](../method/02-target-lock.md) / [method/03](../method/03-slab-contact-point.md)），但当时没解决「远程也一路冲中心」和「状态与位置判定耦合」这两个更深层问题。
- **后续**：见 [03-segmented-test.md](03-segmented-test.md)。
