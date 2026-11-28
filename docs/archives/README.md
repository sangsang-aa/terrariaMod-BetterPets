# 对象档案（archives）

> 按**实际游戏对象**归档完整技术细节——一个对象一个文件夹，该对象的所有部件（物品 / 增益 / 弹幕 / 配套系统）都收在它自己的文件夹里。
> 这里只描述「它是什么、现在怎么运作」；「为什么这么改、踩过什么坑」见 [log/](../log/README.md)，「这套架构能不能复用」见 [method/](../method/README.md)。

## 目录结构

```
archives/
├── README.md              本文：对象索引
├── babypenguin/           企鹅宝宝（改写原版·近战挥鱼 / 远程丢鱼）
│   ├── README.md          对象总览：复用 ID / 文件清单 / 状态机入口
│   ├── summon-item.md     召唤物品「鱼」(ItemID.Fish 669)
│   ├── buff.md            宠物增益 (BuffID.BabyPenguin 41)
│   ├── projectile.md      企鹅本体 (ProjectileID.Penguin 112)：三层状态机 + 参数表
│   ├── fish-swing.md      挥鱼弹幕：近战 / 远程前摇共用
│   └── thrown-fish.md     投掷鱼弹幕：远程抛物线
├── babyeater/             婴儿吞噬怪（改写原版·腐化弹 / 低血冲撞）
│   ├── README.md          对象总览
│   ├── summon-item.md     召唤物品「吞噬怪骨头」(ItemID.EatersBone)
│   ├── buff.md            宠物增益 (BuffID.BabyEater)
│   ├── projectile.md      宠物本体：两套攻击调度
│   ├── corrupt-shot.md    腐化追踪弹
│   └── enraged-minion.md  低血冲撞弹
├── zephyrfish/            和风鱼（改写原版·四段冲撞）
│   ├── README.md          对象总览
│   ├── summon-item.md     召唤物品「和风鱼」(ItemID.ZephyrFish)
│   ├── buff.md            宠物增益 (BuffID.ZephyrFish)
│   └── ram-projectile.md  宠物本体：四段冲撞状态机 + 网络同步
├── nectar/                花蜜蜜蜂（改写原版·毒刺 / 蜂巢 / 小蜜蜂）
│   ├── README.md          对象总览 + 原版链条（指向源码内详尽手册）
│   ├── summon-item.md     召唤物品「花蜜」(ItemID.Nectar 1170)
│   ├── buff.md            宠物增益 (BuffID.BabyHornet 51)
│   ├── projectile.md      宠物本体：毒刺 + 每 20 发掷蜂巢
│   └── hive.md            NectarHive 蜂巢弹幕
├── examplepet/            示例宠物（新建内容·近战冲撞 / 远程星尘龙）
│   ├── README.md          对象总览
│   ├── item.md            ExamplePetItem：召唤物品
│   ├── buff.md            ExamplePetBuff：宠物增益
│   ├── projectile.md      ExamplePetProjectile：宠物本体
│   └── stardust-dragon.md 配套全局系统：星尘龙寿命管理
├── seaturtle/             海龟（改写原版·三态冲撞；设计稿见对象夹内 HTML）
│   ├── README.md          对象总览
│   ├── summon-item.md     召唤物品「海草」(ItemID.Seaweed)
│   ├── buff.md            宠物增益 (BuffID.PetTurtle)
│   └── projectile.md      宠物本体：三态冲撞状态机 + 网络同步
└── pettargetmarker/       目标标记系统（跨宠物全局系统）
    └── README.md          右键标记时的金色脉冲尘 + 识别的四种宠物
```

## 对象索引

| 对象 | 类型 | 文件夹 | 说明 |
|---|---|---|---|
| 企鹅宝宝 | 改写原版 | [babypenguin/](babypenguin/README.md) | 召唤类近战挥鱼 / 远程丢鱼，三层状态机；含 5 个部件档案 |
| 婴儿吞噬怪 | 改写原版 | [babyeater/](babyeater/README.md) | 平时腐化追踪弹 + 低血量冲撞弹；含 5 个部件档案 |
| 和风鱼 | 改写原版 | [zephyrfish/](zephyrfish/README.md) | 接近→蓄力→冲刺→回气四段冲撞；含 3 个部件档案 |
| 花蜜蜜蜂 | 改写原版 | [nectar/](nectar/README.md) | 毒刺 + 每 20 发掷蜂巢炸出小蜜蜂；含 4 个部件档案 |
| 示例宠物 | 新建内容 | [examplepet/](examplepet/README.md) | 近战接触伤害 / 远程召唤限时星尘龙；含 4 个部件档案 |
| 海龟 | 改写原版 | [seaturtle/](seaturtle/README.md) | 三态冲撞（蓄力 / 冲刺 / 回气），地面平射设计；含 3 个部件档案 |
| 目标标记系统 | 全局系统 | [pettargetmarker/](pettargetmarker/README.md) | 四只改写宠物的右键标记尘反馈 |

> 归档约定：新增对象时，在 `archives/` 下建一个以对象命名的文件夹，把该对象相关的全部档案（含配套系统）放进去；跨对象关联用相对链接互指。
