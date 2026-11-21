# 远程站位区间控制（MaintainStandOff）

远程攻击不要一路 `SteerTo(目标中心)`，而是维持一个距离区间 `[Min, Max]`：

- 太远 → 朝目标靠近
- 太近 → 朝远离目标方向后退
- 区间内 → 减速悬停

## 适用条件

远程单位需要拉开距离输出（丢投射物、放法术）。

## 关键坑

没有站位概念的单位会一路挤到目标中心，既失去远程的意义，又和「贴边才能切近战」之类的判定打架（见 [log/03](../log/03-segmented-test.md)）。区间值要考虑攻击射程与前摇期间目标位移。

本项目取值：8~10 格（128~160px），见 [archives/babypenguin/projectile.md](../archives/babypenguin/projectile.md) 参数表。

## 出处

由 [log/03](../log/03-segmented-test.md) 的分段测试结论驱动得出。
