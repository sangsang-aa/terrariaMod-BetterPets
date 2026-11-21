# 问题日志（log）

> 记录修 bug 时**面对的错误、走过的弯路、最终修正方案**。
> 每条按「现象 → 根因 → 尝试 → 修正 → 教训」组织。
> 已验证可复用的套路提炼到 [method.md](method.md)；对象的最终实现见 [archives.md](archives.md)。

---

## Bug 5 · 一次近战出现多次判定 + 多次重复动画（2026-11-21）

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
  - 死代码清理：删除 `PenguinGates.TargetMovedAway` / `TargetMovedThreshold` 与 `PenguinContext.SwingTargetSnapshot`（已无人使用）。
- **验证**：C# 编译通过（`TutorialMod.dll` 11:53:37 生成，0 警告）；打包仍因游戏占用 .tmod 报 TML003（环境问题）。
- **教训**：状态机里凡是「生成短命弹幕」的动作，切换出去时必须 Kill 上一发并清索引，否则索引被覆盖就会留下孤儿弹幕并发。击退类攻击尤其要小心「自己的击退触发自己的提前转出」这种自激。

## Bug 1 · 近战冲进敌人中心，模型重合

- **现象**：近战企鹅直接冲到敌人中心，模型与敌人重叠。
- **根因**：`Approach` 里 `SteerTo(target.Center)` 一路朝中心加速，用固定 `MeleeRange=70` 判定停手，没有「贴边」概念。
- **尝试 / 修正（第一轮）**：
  - 停手判定改为 `projectile.Hitbox.Intersects(target.Hitbox)`（相交即停）。
  - Ranged→Melee 切换条件同步从「10 格距离」改为 hitbox 相交。
  - 删除 `MeleeRecover`（回气）与 `Retreat`（后撤）两个状态及相关常量/方法。
  - 结果：编译通过，但 bug **仍复现**。

## Bug 2 · 贴边砍一段时间后「莫名其妙再索敌」，冲进中心

- **现象**：上一轮修复后，贴边砍一阵子会突然重新索敌，冲进敌人中心挥砍。
- **根因（两个叠加）**：
  1. `FindNearestTarget` 每帧重选最近敌人 → 目标在相邻敌人间跳变。
  2. 朝 `target.Center` 加速 + hitbox 相交是离散帧检测，冲过头那帧仍判相交 → 把「敌人中心」记成 `meleeHoldPosition` 钉住。
- **尝试 / 修正（第二轮）**：
  - 目标锁定：新增 `lockedTarget`，`ResolveTarget` 优先用玩家标记目标，否则沿用锁定目标直到失效才重选。
  - 几何贴边：新增 `GetContactPoint`（slab 法算接触点）+ `PinToContact` 精确钉位；近战追接触点而非中心。
  - `UpdateMode` 的切换判定从 hitbox 相交改为「到接触点距离 ≤14px」（因为贴边后在框外 4px，不再相交，旧判定会失效）。
  - 结果：编译通过，但 bug **仍复现**。

## Bug 3 · 分段测试定位真正病根

- **背景**：两轮修复都失败，用户要求分开测试、先禁用近战。
- **操作**：注释掉 `PostAI` 里的 `UpdateMode(projectile, target);` → `mode` 恒为 Ranged，只跑远程；近战代码全保留不删。
- **发现**：
  - 远程三态全部 `SteerTo(target.Center)`，**没有「保持距离」概念**，一路挤到敌人中心。
  - 近战只有 `Approach` + `MeleeSwing`，挥砍后钉在接触点、仍贴边则立刻再钉住 → **原地不动，从不重新走位**。
  - 隐藏矛盾：Ranged→Melee 要求「距接触点 ≤14px」，但远程偏偏把企鹅推到中心，两个判定天然打架。
- **结论**：问题不在索敌，在**状态机本身的设计**——模式切换与位置判定耦合。

## Bug 4 · 5 秒切换会不会有战斗真空期（边界推演）

- **问题**：切近战时若离目标太远，5 秒内追不上就会空转。
- **推演**（速度 15f / 帧时，5 秒理论能跑 4500px，索敌半径 600px）：
  - 静止/普通敌人：0.2~0.5 秒贴边，无真空。
  - 快速敌人：约 0.6 秒贴边，无真空。
  - 冲刺型 BOSS（≥15f/帧）：同速或更快，永远追不上 → 整段 5 秒近战空转。
- **修正**：加「追击超时阀」`MeleeChaseTimeout = 90` 帧——切入近战 90 帧仍未贴边就提前切回远程，把最坏真空从 5 秒压到 1.5 秒；正常战斗触发不到。

## 环境问题 · TML003 打包失败（非代码 bug）

- **现象**：`dotnet build` 时 C# 编译通过（`TutorialMod.dll` 生成，0 警告），但最后打包 `.tmod` 报 TML003 "Please close tModLoader"。
- **根因**：tModLoader 游戏正在运行，占用 `Mods/TutorialMod.tmod`。
- **修正**：非代码问题。关闭游戏（或游戏内禁用 mod）后重新 `dotnet build` 即可生成新包。
- **教训**：判断构建是否成功，看 `TutorialMod.dll` 是否新生成 + 是否 0 警告，不要只看最终 `.tmod` 是否产出。

## 环境限制 · GitHub App token 403（非 bug，记录备查）

- **现象**：GitHub 连接器发的 App 令牌 `ghu_` 对建仓/改名/写文件/push 全部返回 403 "Resource not accessible by integration"；环境安全策略也拦截带 token 的 git push。
- **修正**：用户手动建空公开仓库 + 提供 fine-grained PAT，改用 REST Git Data API（blob/tree/commit/ref）以 base64 上传；临时凭据用完即删。
- **遗留**：仓库名仍是 `terrariaMod-`（App token 无改名权限），本地未建立 `origin/main` 跟踪引用（纯 cosmetic）。
