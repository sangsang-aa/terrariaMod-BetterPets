# Bug 5 · 一次近战出现多次判定 + 多次重复动画

> 2026-11-21。状态：已修复（C# 编译通过，待游戏内实测）。

- **现象**：一次近战攻击，同一时间（小时间差）出现多次攻击判定和多次挥鱼动画——一次挥砍被结算成好几下。
- **根因（挥砍弹幕生命周期漏洞 + 击退自激）**：
  1. `MeleeSwing` 转出（`TargetMovedAway` 或挥满 16 帧）→ `EnterMeleeApproach` 时**没有 Kill 挥砍弹幕**。
  2. `MeleeApproach` 下一帧 `AtContact` 又成立 → `EnterMeleeSwing` → `SpawnSwing` 生成新弹幕、**覆盖 `MeleeSwingIndex`**，旧弹幕成孤儿，活到 `timeLeft` 自然结束 → 多个 `PenguinFishSwing` 并存，各自判定、各自绘制。
  3. **自激**：弹幕越多 → 目标被击退越狠 → `TargetMovedAway` 越容易成立 → 转出越快 → 又生成更多弹幕。
  4. 叠加因素：`PenguinFishSwing.localNPCHitCooldown = 10`（< 16 帧挥砍），单个弹幕对同一目标也可能命中 2 次。
- **注意区分**：`EnterRangedMode` / `EnterMeleeMode` / `ResetState` 都有 Kill，是干净的；只有动作层内部 `MeleeSwing → Approach → Swing` 这条路径漏了。
- **用户确认方案**：
  - 挥砍期间**忽略目标移动**（固定挥满 16 帧）——击退是自己造成的，不该被当成「目标逃走」打断自己。
  - 单次挥砍对同一目标**只命中一次**。
- **修正**（3 个源文件 + 死代码清理）：
  - `PenguinActions.MeleeSwing`：去掉 `TargetMovedAway` 判断，固定挥满 16 帧；转出前 `KillMeleeSwing`。
  - `PenguinActions.EnterMeleeSwing`：生成新弹幕前先 `KillMeleeSwing`（双保险，保证任意时刻最多一个挥砍弹幕）。
  - `PenguinFishSwing`：新增 `HitCooldown = MeleeLifetime + 4 = 20`，`localNPCHitCooldown` 由 10 改为 20，一砍只中一次。
  - 死代码清理：删除 `PenguinGates.TargetMovedAway` / `TargetMovedThreshold` 与 `PenguinContext.SwingTargetSnapshot`。
- **验证**：C# 编译通过（`TutorialMod.dll` 11:53:37 生成，0 警告）；打包仍因游戏占用 .tmod 报 TML003（见 [env-tml003.md](env-tml003.md)）。
- **教训**：状态机里凡是「生成短命弹幕」的动作，切换出去时必须 Kill 上一发并清索引，否则索引被覆盖就会留下孤儿弹幕并发。击退类攻击尤其要小心「自己的击退触发自己的提前转出」这种自激。方法见 [method/00](../method/00-projectile-lifecycle.md)。
