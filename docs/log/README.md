# 问题日志（log）

> 记录修 bug 时**面对的错误、走过的弯路、最终修正方案**。每个问题一个文件。
> 每条按「现象 → 根因 → 尝试 → 修正 → 教训」组织。
> 已验证可复用的套路提炼到 [method/](../method/README.md)；对象的最终实现见 [archives/](../archives/README.md)。

## 问题清单

| 文件 | 问题 | 状态 |
|---|---|---|
| [01-melee-charge-center.md](01-melee-charge-center.md) | Bug 1 · 近战冲进敌人中心、模型重合 | 第一轮修复（未根治，引出 Bug 2） |
| [02-retarget.md](02-retarget.md) | Bug 2 · 贴边砍一阵后莫名再索敌、冲进中心 | 第二轮修复（仍未根治） |
| [03-segmented-test.md](03-segmented-test.md) | Bug 3 · 分段测试定位真正病根 | 定位完成，引出三层重构 |
| [04-combat-vacuum.md](04-combat-vacuum.md) | Bug 4 · 5 秒切换的战斗真空期边界推演 | 加追击超时阀解决 |
| [05-melee-duplicate-hits.md](05-melee-duplicate-hits.md) | Bug 5 · 一次近战多次判定 + 重复动画 | 已修复（编译通过，待实测） |
| [06-pet-teleport-on-attack.md](06-pet-teleport-on-attack.md) | Bug 6 · 进入攻击后企鹅被"传送"回玩家身边 | 已修复（真因 `ai[3]` 越界，编译通过待实测） |
| [07-ranged-swing-oversized.md](07-ranged-swing-oversized.md) | Bug 7 · 远程挥舞显示为放大过后的鱼 | 已修复（编译通过，待实测） |
| [env-tml003.md](env-tml003.md) | 环境问题 · TML003 打包失败 | 非代码问题，关游戏再 build |
| [env-github-token-403.md](env-github-token-403.md) | 环境限制 · GitHub App token 403 | 改用 PAT + REST API |

> 说明：Bug 1~4 是同一条「近战冲中心」问题的逐步定位过程；Bug 3 的结论（状态机设计耦合）直接导致了三层重构，Bug 4 是重构时的边界加固，Bug 5 是重构后暴露的弹幕生命周期问题。
