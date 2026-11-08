# Build / 构建

## 简体中文

需要 Windows x64、.NET SDK 10、Python 3，以及已安装的国服 Dalamud **15.0.3.8 / API 15** 依赖目录。依赖从自己的 Dalamud 安装取得，不包含在源码包内。发布基线的游戏版本为 `2026.10.06.0000.0000`。

在仓库根目录运行 PowerShell：

```powershell
.\scripts\build.ps1 -DalamudLibPath "$env:APPDATA\XIVLauncherCN\addon\Hooks\15.0.3.8"
```

如 `dotnet` 或 `python` 不在 PATH，可传入 `-DotNet '完整路径\dotnet.exe' -Python '完整路径\python.exe'`。

脚本会校验 102 条英文翻译、编译 `src/PovPlus.csproj`、运行离线回归，并生成：

- `artifacts/PovPlus-Enhanced-1.6.0.0.zip`：安装包。
- `artifacts/PovPlus-Enhanced-1.6.0.0-source.zip`：完整对应源码、测试和构建说明。
- `artifacts/SHA256SUMS.txt`：下载校验值。
- 根目录 `PovPlus.Enhanced.zip` 和 `repo.json`：自定义仓库安装/更新入口。

当前构建入口为 `src`，不是上游留下的 `SamplePlugin.sln`。源码由 Beta 程序集恢复，因此仍有未使用字段和不可达旧辅助代码的编译警告；本次不以大规模清理改变已验证行为。

发布新版本时同时更新 `src/Properties/AssemblyInfo.cs`、`PovPlus.json`、说明与更新记录，重新验证游戏接口，再运行构建。发布源码、ZIP、校验文件及 Git 标签；不要分发 Dalamud 或游戏的依赖 DLL。变更游戏版本限制前应核对原生 hook，不能只修改清单绕过兼容检查。

## English

Requires Windows x64, .NET SDK 10, Python 3, and the reference DLL directory from your own **Dalamud CN 15.0.3.8 / API 15** installation. Dependency DLLs are not bundled. The release baseline uses game build `2026.10.06.0000.0000`.

Run the PowerShell command above from the repository root. Supply `-DotNet` and `-Python` with executable paths if they are not in PATH. The script checks the 102 English translations, builds `src/PovPlus.csproj`, runs offline regressions, and writes binary/source ZIPs plus SHA-256 checksums to `artifacts/`. It also updates the root installer ZIP and custom-repository manifest.

Build `src`, not the inherited `SamplePlugin.sln`. Recovered Beta source retains warnings for unused members and old unreachable helper code; broad cleanup is deliberately outside this release's scope.

For a new release, update the assembly version, plugin manifest, documentation and changelog together. Revalidate native hooks against the target game build before changing the compatibility restriction. Publish the corresponding source, binary package, checksums and Git tag. Do not bundle the game's or Dalamud's dependency DLLs.
