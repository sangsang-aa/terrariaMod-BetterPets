# 环境问题 · TML003 打包失败

> 非代码 bug，环境占用。

- **现象**：`dotnet build` 时 C# 编译通过（`TutorialMod.dll` 生成，0 警告），但最后打包 `.tmod` 报 TML003 "Please close tModLoader"。
- **根因**：tModLoader 游戏正在运行，占用 `Mods/TutorialMod.tmod`，打包器无法写入。
- **修正**：非代码问题。关闭游戏（或游戏内禁用 mod）后重新 `dotnet build` 即可生成新包。
- **教训**：判断构建是否成功，看 `TutorialMod.dll` 是否新生成 + 是否 0 警告，不要只看最终 `.tmod` 是否产出。编译与打包是两个阶段，TML003 只影响后者。
