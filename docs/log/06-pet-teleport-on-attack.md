# Bug 6 · 进入攻击后企鹅被传送回玩家身边

> 已修复（C# 编译通过，待实测）。真正的根因是 `IndexOutOfRangeException`，不是原版 AI。

- **现象**：宠物进入攻击逻辑后不攻击，直接"闪现"回玩家身边。用户实测：与主人距离未超 1200px、是传送而非消失重召唤、单人游戏、闪现起点正好是远程站位内界（距目标 128px）。同时游戏系统报 `index is out of the boundary of array`。

## 排查弯路（已推翻）

1. 排除自己的门：`TooFarFromOwner`（1200px）只触发 `ResetState`，而 `ResetState` 只 `new PenguinContext`，**从不改坐标**。
2. 清点全部改坐标代码：`EnterMeleeSwing` 的 `Pet.Center = contact`、`MeleeSwing` 的 `position = HoldPosition` 都钉在敌人边框外；`PenguinFishSwing.AI` 是弹幕跟随宠物（反向）。没有一处把宠物放回玩家身边。
3. 基于"传送 + 单人 + 非重召唤"推断为原版企鹅 AI（1.4.4.9 喷气背包）的"远离主人 + 静止 → 卡死回城"，据此把 `MaintainStandOff` 的 `velocity *= 0.82` 改成切向低速漂移。**用户实测 bug 未解决，且报错依旧** → 该推断不成立，改动已全部退回。

## 真正根因

游戏日志 `client.log` 堆栈给出确证：

```
System.IndexOutOfRangeException: Index was outside the bounds of the array.
   at PenguinActions.SpawnSwing(...) in PenguinActions.cs:line 282
```

- 越界点是 `Main.projectile[index].ai[3] = windup;`。当前 tModLoader 版本的 `Projectile.ai` 只有 **3 个槽位（ai[0..2]）**，写 `ai[3]` 必抛异常。
- 该异常在 22:54 的日志里就存在，**早于**切向漂移改动，所以一直才是真凶：每次攻击生成 `PenguinFishSwing` 都抛异常 → `PostAI` 中断 → 攻击失败 + 宠物状态异常，表现为"闪现回玩家身边且不攻击"。
- 这也解释了为什么之前 `NewProjectile` 传 4 个 ai 参数会编译失败——同样是 ai 数组只有 3 槽位的限制。
- 附带问题：远程的 `windup=0` 因异常从未真正生效，远程前摇动画实际一直在按近战蓄力逻辑跑。

## 修正

不依赖 `ai[3]`，改为**由 `duration`（ai[2]）反推 windup**：

- `PenguinActions.SpawnSwing`：删除 `ai[3]` 赋值；`windup` 参数仅留作调用方语义标注，不再写入。
- `PenguinFishSwing.AI`：`float windup = total >= MeleeTotalLifetime ? MeleeWindup : 0f;`（近战 28→12，远程 12→0）。

好处：完全绕开 ai[3]，省掉一个跨端同步字段，并顺带修正远程前摇误带蓄力的问题。

## 教训

- **看到"传送/闪现"先查日志堆栈**：行为层面的合理推测（原版 AI 回城）可以是弯路，`IndexOutOfRangeException` 这类异常打断 `PostAI` 同样会让宠物表现异常。
- 本项目 tModLoader 版本的 `Projectile.ai` 只有 3 槽（`ai[0..2]`），任何第 4 个 ai 字段都要改用别的载体（本地字段、由其它 ai 反推）。
- 用户"距离没超 1200px""是传送"这类限定有效排除了自家门，但**不能替代堆栈证据**。

## 相关

- [method/04](../method/04-standoff-range.md)（远程站位区间；已移除本次被推翻的"静止回城"坑）
- [archives/babypenguin/projectile.md](../archives/babypenguin/projectile.md)（企鹅弹幕参数）
