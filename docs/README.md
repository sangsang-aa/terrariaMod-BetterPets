# TutorialMod 开发文档

> 本项目文档体系的主入口。四个文件夹分工如下：
>
> | 文件夹 | 职责 | 写什么 |
> |---|---|---|
> | **docs/**（本文为 README） | 主记录 | 项目总览、里程碑时间线、当前状态、文档导航 |
> | [archives/](archives/README.md) | 对象档案 | 各游戏对象的完整技术细节，按实际对象分文件夹（一个对象一个夹，含其全部部件） |
> | [log/](log/README.md) | 问题日志 | 修 bug 时遇到的错误、根因、尝试与修正方案，每个问题一个文件 |
> | [method/](method/README.md) | 方法库 | 已经验证、可复用的架构与设计（含适用条件与坑），每个模式一个文件 |
>
> 维护约定：
> - **对象细节**写 archives，**踩坑与修正**写 log，**验证过的通用套路**写 method，本文只留索引与当前状态。
> - method 只收**已通过编译/实测**的方案；失败尝试留在 log，不进 method。
> - 原始每日日志归档于 [_archive/2026-11-20.md](_archive/2026-11-20.md)；重组前的平铺文档归档于 [_archive/legacy/](_archive/legacy/)。

---

## 一、项目总览

- **项目**：`TutorialMod`，tModLoader 1.4.4 / .NET 8 模组。
- **显示名**：`更好的宠物`（`build.txt` 的 `displayName`）；内部标识符（namespace / 类名 / .csproj）保持 `TutorialMod` 不变。
- **玩法主题**：改写原版宠物，给它们追加召唤类攻击逻辑（目前实现的是「企鹅宝宝」）。
- **远端仓库**：`https://github.com/sangsang-aa/terrariaMod-`（公开，注意仓库名因 App token 权限所限未改成中文）。
- **本地 git**：分支 `main`，已入库 39 个源文件；`user.name=NewbieModder` / `user.email=newbiemodder@users.noreply.github.com`。
- **.gitignore**：排除 `bin/`、`obj/`、`.workbuddy/`、`*.tmod`、`.vs/`、`.vscode/`。
  - 注意：`docs/` **未被忽略**，会随源码一起进版本库。

## 二、开发铁律（来自 tmod-modding skill）

1. **任何改动前先说明逻辑，等用户确认再动手。**
2. 目录结构仿照 ExampleMod。
3. 召唤类实现必须完整：鞭子索敌 + tag 加成 + 饰品/药水/栏位加成。
4. 注释面向「看不到原版源码」的读者，附已验证的文档链接。
   → 详见 [method/07](method/07-summon-checklist.md) 与 [method/08](method/08-comment-style.md)。

## 三、里程碑时间线

| 日期 | 事件 | 结果 |
|---|---|---|
| 2026-11-20 | 将 `tmod-modding-skill` 转为 WorkBuddy Skill | `tmod-modding`（22 文件）+ `tmod-code-comments`（1 文件）安装到用户级 skills，均通过校验与打包 |
| 2026-11-20 | 准备并上传 GitHub | 本地改名「更好的宠物」+ git init + 首次提交；App token 403 受阻，改用 PAT + REST Git Data API 上传成功 |
| 2026-11-20 | 修改企鹅宝宝近战逻辑 | 冲中心 → 贴边即停；删回气与后撤；编译通过 |
| 2026-11-20 | 修复「冲进敌人中心」bug（第 1、2 次） | 目标锁定 + 几何贴边接触点；编译通过但 bug 仍复现 |
| 2026-11-21 | 临时注释近战，分段测试 | 定位到真正病根：远程不保持距离 + 近战钉死 |
| 2026-11-21 | 状态机三层重构 | 拆成 Context / Gates / Actions + 瘦调度器；C# 编译通过 0 警告 |
| 2026-11-21 | 修复「一次近战多次判定 + 重复动画」 | 弹幕生命周期清理 + 挥砍期忽略目标移动 + 一砍只中一次 |
| 2026-11-21 | 文档体系重组为四文件夹结构 | 平铺文档迁移归档，建立四文件夹体系 |
| 2026-11-21 | archives 改按实际对象归档 | 企鹅全部部件归入 `babypenguin/`，目标标记系统独立为 `pettargetmarker/`，112 个链接全有效 |
| 2026-11-21 | 其余四只宠物全部归档 | 新增 babyeater / zephyrfish / nectar / examplepet 四个对象夹，补全目标标记系统识别的四种宠物 |

## 四、当前状态（2026-11-21）

- **代码状态**：三层架构重构 + 近战重复攻击修复完成，`TutorialMod.dll` 编译通过（0 警告 0 错误）。
- **打包状态**：`.tmod` 打包受 TML003 阻塞——tModLoader 游戏运行时占用 `Mods/TutorialMod.tmod`。**非代码问题**，关闭游戏（或游戏内禁用 mod）后重新 `dotnet build` 即可生成新包。
- **待验证**：所有重构与修复的**游戏内实测**（尚未实测）。重点观察远程站位 8~10 格、近战贴边走位、追击超时阀、一砍一次的判定节奏。
- **待办**：GitHub 仓库名仍为 `terrariaMod-`（未改中文，受权限所限）；本地 git 未建立 `origin/main` 跟踪引用（纯 cosmetic）。

## 五、快速索引

- 想了解「企鹅宝宝怎么实现的」→ [archives/README.md](archives/README.md)
- 想回顾「这个 bug 是怎么修的 / 为什么改了好几遍」→ [log/README.md](log/README.md)
- 想复用「三层状态机 / 贴边算法 / 目标锁定」→ [method/README.md](method/README.md)
