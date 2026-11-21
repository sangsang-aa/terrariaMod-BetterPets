# 方法库（method）

> 存放**已经验证、后续可学习复用**的架构与设计。每个模式一个文件，含「做法 / 适用条件 / 关键坑」。
> 只收**通过编译或实测**的方案；失败的尝试留在 [log/](../log/README.md)，不进这里。
> 本项目所有条目的验证状态：**C# 编译通过（0 警告 0 错误）**；游戏内实测待做。

## 方法清单

| 文件 | 方法 | 适用场景 |
|---|---|---|
| [00-projectile-lifecycle.md](00-projectile-lifecycle.md) | 短命攻击弹幕的生命周期管理 | 一次动作 = 一个弹幕的挥砍/前摇/特效 |
| [01-three-layer-state-machine.md](01-three-layer-state-machine.md) | 三层状态机：动作包 / 状态数据包 / 检测门 | 状态多、判定多、易互相干扰的 AI |
| [02-target-lock.md](02-target-lock.md) | 目标锁定（ResolveTarget） | 多敌人场景，攻击要连贯不跳目标 |
| [03-slab-contact-point.md](03-slab-contact-point.md) | slab 法求贴边接触点 | A 精确贴 B 边框外而不重合 |
| [04-standoff-range.md](04-standoff-range.md) | 远程站位区间控制 | 远程单位拉开距离输出 |
| [05-chase-timeout.md](05-chase-timeout.md) | 追击超时安全阀 | 目标速度可能 ≥ 己方的模式切换 |
| [06-global-patch-vanilla.md](06-global-patch-vanilla.md) | 用 Global* 挂载改写原版内容 | 在原版内容上做增强版 |
| [07-summon-checklist.md](07-summon-checklist.md) | 召唤类实现铁律 | 宠物 / 随从 / 鞭子 |
| [08-comment-style.md](08-comment-style.md) | 代码注释规范 | 面向看不到原版源码的读者 |
