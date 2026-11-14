# 国服主库审核材料 / CN repository review notes

申请将 **POV+ Enhanced 1.6.1.4** 收录到 `testing/live/PovPlus`。这是一份 POV+ 增强分支的首次收录申请，目标为国服/API 15；国际服兼容性尚未验证，也尚未向国际服主库提交。<br>
Initial submission of **POV+ Enhanced 1.6.1.4** to `testing/live/PovPlus`. This enhanced POV+ fork targets FFXIV CN/API 15. International-client compatibility is unverified and it has not been submitted to the international repository.

## 源码与构建 / Source and build

- 源码入口为 `src/PovPlus.csproj`，内部名称为 `PovPlus`，维护者为 `pascalial98-hue`。<br>
  Build `src/PovPlus.csproj`; internal name `PovPlus`; maintainer `pascalial98-hue`.
- 原项目为 [TiraesiascenGraven/POVPlus](https://github.com/TiraesiascenGraven/POVPlus)。当前源码基线由其公开发布的 Beta 1.5.2.2 程序集恢复，不是原作者未公开的源代码；保留原署名、Git 历史和 AGPL-3.0-or-later 声明，详见 [NOTICE](../NOTICE.md)。<br>
  Based on the publicly distributed upstream Beta 1.5.2.2 assembly recovered into source, not the author's unpublished source. Attribution, Git history and the upstream AGPL-3.0-or-later declaration are retained; see [NOTICE](../NOTICE.md).
- 使用 .NET SDK 10、`DalamudPackager` 15.0.0、已提交的 `src/packages.lock.json` 和 `$(DalamudLibPath)`。`IsPlogonBuild=True` 启用锁定还原；Release 自动生成 `PovPlus/latest.zip`。无需密钥、自定义构建容器或运行时下载。<br>
  Uses .NET SDK 10, DalamudPackager 15.0.0, the committed lockfile and `$(DalamudLibPath)`. `IsPlogonBuild=True` enables locked restore; Release automatically emits `PovPlus/latest.zip`. No secrets, custom build container or runtime downloads are required.
- 标准安装包仅包含 `PovPlus.dll`、`PovPlus.json`、`LICENSE.md` 和 `NOTICE.md`，不包含 Dalamud 或游戏依赖 DLL。构建和验证命令见 [BUILD.md](BUILD.md)。<br>
  The standard installer contains only the plugin DLL, manifest, license and notice, without Dalamud/game dependency DLLs. Commands are documented in [BUILD.md](BUILD.md).

## 功能与原生改动 / Features and native changes

插件将第一人称视点绑定到角色头部，保留可见身体，并提供头部隐藏与完整头影、视角软限位、边界探头、可选鼠标直控、固定姿势观察保护、分轴跟随权重、视点校准及中英界面。使用角色现有动作，不替换动作资源。<br>
The camera follows the character's head while keeping the body visible. Features include head hiding with full shadows, soft look limits, boundary leaning, optional direct mouse look, stationary-pose protection, weighted head follow, viewpoint calibration and Chinese/English UI. Existing character animations are used without replacing animation assets.

审核应注意以下客户端原生改动；它们不应被当作单纯的界面插件：<br>
The following client-side native changes deserve review; this is more than a UI-only plugin:

- 相机更新和渲染方向、头部骨骼/模型隐藏，以及主画面与阴影绘制的区分。<br>
  Camera update/render orientation, head bone/model visibility, and separate main-view/shadow rendering.
- 鼠标观察输入、手柄相机旋转模式，以及固定循环姿势时对人物转身请求的限制。<br>
  Mouse-look input, controller camera-rotation mode, and suppression of character-turn requests during supported stationary loop states.
- 可调 FOV、NearPlane，以及默认关闭的近对象距离剔除修正、自身近镜头特效减弱。<br>
  Adjustable FOV/NearPlane, optional near-object distance-culling changes and nearby self-effect attenuation, with the latter two off by default.
- 默认关闭的身体姿态关联视角/限位；它改变镜头参考方向，不修改角色动作。<br>
  Optional body-relative view/limits, off by default; this changes the camera reference rather than character animations.

主要实现入口为 `src/PovPlus/POVCameraFunctionality.cs`、`FirstPersonHeadShadow.cs`、`FirstPersonMouseLook.cs`，以及同目录下的 `HeadShadow*`、`Stationary*`、`MouseLook*`、`NearObject*` 和 `Body*` 文件。保留原生接口检查及功能失败状态；游戏更新后仍需要核对 hook、字段布局与实际行为。`ApplicableVersion: any` 不代表未来版本兼容保证。<br>
Primary implementation is in `src/PovPlus/POVCameraFunctionality.cs`, `FirstPersonHeadShadow.cs`, `FirstPersonMouseLook.cs` and the neighboring `HeadShadow*`, `Stationary*`, `MouseLook*`, `NearObject*` and `Body*` files. Native checks and failure status remain in place; updates still require checking hooks, layouts and behavior. `ApplicableVersion: any` is not a compatibility guarantee.

## 测试范围与限制 / Validation and limitations

游戏内反馈基线为国服 `2026.11.03.0000.0000`；编译依赖为 Dalamud CN `15.0.4.0 / API 15`。此前多轮游戏内反馈覆盖第一人称核心行为、头影和身体姿态视角，但不是所有种族、动作、改模和输入组合的完整测试。各版本具体记录见 [VALIDATION.md](VALIDATION.md)。<br>
The in-game feedback baseline is CN `2026.11.03.0000.0000`, with Dalamud CN `15.0.4.0 / API 15` build references. Earlier in-game iterations covered core POV behavior, shadows and body-relative view, but not every race, animation, mod or input combination. See the per-version [validation record](VALIDATION.md).

身体姿态关联选项适合静止观察，不适合移动时使用；平滑后跑动仍可能明显晃动。NearPlane 默认 0.015，仍允许 0.001，但过低会让部分粒子透过场景显示。完整头影隐藏整块头部相关模型，可能同时隐藏属于脸部模型的部分颈部。近对象保留不覆盖所有场景模型，也不显示未加载对象。<br>
Body-relative view is intended for stationary poses, not movement; running can still sway noticeably after smoothing. NearPlane defaults to 0.015 and allows 0.001, which can cause particle depth artifacts. Full-shadow mode hides whole head-related models and may hide neck geometry belonging to the face model. Near-object visibility does not cover all scenery or reveal unloaded objects.

本次仅适配构建与准备审核材料，没有修改功能源码。不会替换已有自建库链接，也不会要求现有用户同时加载两份插件。主库通过审核之前，不能宣称已经收录。<br>
This submission preparation changes build integration and documentation only, not feature source. The existing custom-repository URL is retained; users should not load duplicate copies. Inclusion must not be claimed before repository approval.

## AI 参与 / AI involvement

新增功能实现、测试、文档和图片均有大量 AI 参与；维护者提供具体需求、修正和游戏实测反馈。没有声称完成全部代码的逐行人工审查。PR 本文也由 AI 按明确授权协助准备和提交。完整说明见 [AI_USAGE.md](AI_USAGE.md)。<br>
AI contributed substantially to added code, tests, documentation and artwork, guided by the maintainer's requirements, corrections and in-game feedback. A complete line-by-line human audit is not claimed. The PR is also AI-assisted and explicitly authorized; see [AI_USAGE.md](AI_USAGE.md).
