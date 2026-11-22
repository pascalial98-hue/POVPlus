# Build / 构建

## 主库标准构建 / Standard repository build

当前构建入口是 `src/PovPlus.csproj`，使用 .NET SDK 10；运行插件需要 Windows x64 和国服 Dalamud API 15。原生接口基线为国服 `2026.11.03.0000.0000`，本地验证依赖为 Dalamud CN `15.0.4.0`。<br>
Build `src/PovPlus.csproj` with .NET SDK 10. Running the plugin requires Windows x64 and Dalamud CN API 15. Native interfaces target CN `2026.11.03.0000.0000`; local validation uses Dalamud CN `15.0.4.0` references.

在仓库根目录运行以下命令，将 `<Dalamud reference directory>` 替换为实际依赖路径。主库 Plogon 同样通过 `DalamudLibPath` 提供依赖。首次联网还原后，构建可使用本地 NuGet 包缓存；构建期间不执行额外下载脚本。<br>
Run from the repository root, replacing `<Dalamud reference directory>` with the actual reference directory. Plogon supplies references through the same property. After an initial online restore, builds can use the local NuGet cache; no additional download scripts run during compilation.

```text
dotnet restore src/PovPlus.csproj --locked-mode -p:DalamudLibPath="<Dalamud reference directory>"
dotnet build src/PovPlus.csproj -c Release -o artifacts/dip17 --no-restore -p:DalamudLibPath="<Dalamud reference directory>" -p:IsPlogonBuild=True
python scripts/verify_dip17.py artifacts/dip17 --baseline PovPlus.Enhanced.zip
```

Release 构建自动调用固定版本的 `DalamudPackager` 15.0.0，输出 `artifacts/dip17/PovPlus/latest.zip` 及旁边的 `PovPlus.json`。安装包只包含插件 DLL、清单、LICENSE 与 NOTICE，不包含 Dalamud 或游戏依赖。根目录 `PovPlus.json` 是两种分发方式共用的清单来源；版本由 `src/Properties/AssemblyInfo.cs` 固定。<br>
Release builds invoke pinned DalamudPackager 15.0.0 and emit `artifacts/dip17/PovPlus/latest.zip` with its sibling manifest. The installer includes only the plugin DLL, manifest, LICENSE and NOTICE, with no Dalamud/game dependencies. Both channels use the root `PovPlus.json`; assembly versions are fixed in `src/Properties/AssemblyInfo.cs`.

`src/packages.lock.json` 必须随依赖变更提交。`IsPlogonBuild=True` 自动开启锁定还原。修改依赖时重新生成锁文件，再运行上述命令。`--baseline` 只适用于本次保持 DLL 不变的构建适配；功能版本升级后应省略或更换为对应已测试版本。<br>
Commit `src/packages.lock.json` with dependency changes. `IsPlogonBuild=True` enables locked restore. Regenerate the lockfile when changing dependencies and rerun the checks above. `--baseline` is for this build-only revision with an unchanged DLL; omit it or supply the matching tested release after feature changes.

离线功能与语言检查：<br>
Offline regression and localization checks:

```text
dotnet run --project tests/PovPlus.Tests.csproj -c Release -p:DalamudLibPath="<Dalamud reference directory>"
python scripts/verify_localization.py
```

国服提交材料及 AI 使用范围见 [CN_SUBMISSION.md](CN_SUBMISSION.md) 和 [AI_USAGE.md](AI_USAGE.md)。<br>
See [CN_SUBMISSION.md](CN_SUBMISSION.md) and [AI_USAGE.md](AI_USAGE.md) for the CN review notes and AI disclosure.

## 简体中文

需要 Windows x64、.NET SDK 10、Python 3，以及已安装的国服 Dalamud **15.0.4.0 / API 15** 依赖目录。依赖从自己的 Dalamud 安装取得，不包含在源码包内。发布基线的游戏版本为 `2026.11.03.0000.0000`。

在仓库根目录运行 PowerShell：

```powershell
.\scripts\build.ps1 -DalamudLibPath "$env:APPDATA\XIVLauncherCN\addon\Hooks\15.0.4.0"
```

如 `dotnet` 或 `python` 不在 PATH，可传入 `-DotNet '完整路径\dotnet.exe' -Python '完整路径\python.exe'`。

脚本会校验 129 条英文翻译、编译 `src/PovPlus.csproj`、运行离线回归，并生成：

- `artifacts/PovPlus-Enhanced-1.8.1.0.zip`：安装包。
- `artifacts/PovPlus-Enhanced-1.8.1.0-source.zip`：完整对应源码、测试和构建说明。
- `artifacts/SHA256SUMS.txt`：下载校验值。
- `artifacts/POVPlus_Enhanced_XMA_Release_Kit.zip`：逐段中英对照的发布正文、单语备用正文、发布字段及预览图标。
- 根目录 `PovPlus.Enhanced.zip` 和 `repo.json`：自定义仓库安装/更新入口。

当前构建入口为 `src`，不是上游留下的 `SamplePlugin.sln`。源码由 Beta 程序集恢复，因此仍有未使用字段和不可达旧辅助代码的编译警告；本次不以大规模清理改变已验证行为。

发布新版本时同时更新 `src/Properties/AssemblyInfo.cs`、`PovPlus.json`、说明与更新记录，重新验证游戏接口，再运行构建。发布源码、ZIP、校验文件及 Git 标签；不要分发 Dalamud 或游戏的依赖 DLL。游戏版本策略为 `ApplicableVersion: any`，API 级别及现有接口检查仍然保留。维护时应核对目标客户端的原生 hook；不要仅因签名仍能匹配就假定所有字段和调用约定均未变化。

## English

Requires Windows x64, .NET SDK 10, Python 3, and the reference DLL directory from your own **Dalamud CN 15.0.4.0 / API 15** installation. Dependency DLLs are not bundled. The release baseline uses game build `2026.11.03.0000.0000`.

Run the PowerShell command above from the repository root. Supply `-DotNet` and `-Python` with executable paths if they are not in PATH. The script checks the 129 English translations, builds `src/PovPlus.csproj`, runs offline regressions, and writes binary/source ZIPs plus SHA-256 checksums to `artifacts/`. It also updates the root installer ZIP and custom-repository manifest.

Build `src`, not the inherited `SamplePlugin.sln`. Recovered Beta source retains warnings for unused members and old unreachable helper code; broad cleanup is deliberately outside this release's scope.

For a new release, update the assembly version, plugin manifest, documentation and changelog together. The manifest uses `ApplicableVersion: any` while retaining API requirements and existing interface checks. Revalidate native hooks against the target client when maintaining the plugin; a matching signature alone does not prove that field layouts and calling conventions are unchanged. Publish the corresponding source, binary package, checksums and Git tag. Do not bundle the game's or Dalamud's dependency DLLs.
