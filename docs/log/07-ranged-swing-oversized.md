# Bug 7 · 远程挥舞显示为放大过后的鱼

> 已修复（C# 编译通过，待实测）。

- **现象**：修完 Bug 6 后，远程前摇挥鱼时，鱼一出现就是放大约 2 倍，随后才缩回 1 倍。近战动画正常。
- **根因**：这是 Bug 6 修复时引入的连带问题。`windup` 改为由总帧数反推后，远程 `total=12 < MeleeTotalLifetime=28` → `windup=0`，于是**永远走 else 分支（挥砍段）**。而挥砍段的缩放起点写死为 `WindupScale=2`：

  ```csharp
  drawScale = MathHelper.Lerp(WindupScale, 1f, swingProgress);
  ```

  远程没有蓄力段把 1→2 铺好，第一帧 `swingProgress=0` 直接取到 `WindupScale=2`，于是显示放大的鱼，再随进度缩回 1。近战有前 12 帧蓄力段衔接，所以不受影响。

- **修正**：挥砍段的缩放起点改为"取决于是否有蓄力段"：

  ```csharp
  float startScale = windup > 0f ? WindupScale : 1f;
  drawScale = MathHelper.Lerp(startScale, 1f, swingProgress);
  ```

  近战（windup=12）起点仍为 2，动画不变；远程（windup=0）起点为 1，全程正常大小、只做角度扫动。

- **澄清**：上一轮曾提示"`PenguinFishSwing` 缺 `InstancePerEntity => true` 可能导致 `drawScale` 串扰"——此说法**不成立**。`ModProjectile` 本来就是每个弹幕实体独立实例（需要 `InstancePerEntity` 的是 `GlobalProjectile`，它默认单例）。不是本 bug 原因，无需修改。

- **教训**：把动画参数由"显式传参"改为"反推"时，要检查所有下游分支在参数缺省值（这里是 `windup=0`）下的表现。两段式动画的"第二段起点"必须与第一段的终点一致；当第一段不存在时，起点应退化为默认值（1 倍），而不是硬编码段末值。
