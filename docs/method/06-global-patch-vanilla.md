# 用 Global* 挂载改写原版内容

想给原版宠物/物品加行为，但不新建内容时：

- `GlobalProjectile.AppliesToEntity` 精确锁定目标弹幕 ID（如 `ProjectileID.Penguin`）。
- `InstancePerEntity => true` 让每个弹幕实例有独立状态。
- 保留原版跟随 AI，只在 `PostAI`（原版 AI 之后）追加自己的逻辑。

配套的还有 `GlobalItem` / `GlobalBuff`：用 `AppliesToEntity` 或 `type ==` 锁定，只替换 Tooltip / buff 文本（`ModifyTooltips` / `ModifyBuffText`），不改属性。

## 适用条件

在原版内容上做「增强版」而非全新内容。

## 关键坑

`PostAI` 里本体要 `friendly = false`，伤害全由自定义弹幕结算，避免接触伤害与弹幕伤害重复。

## 出处 / 实例

本项目的企鹅宝宝全套均用此法，见 [archives/README.md](../archives/README.md)。
