# Validation / 验证记录

## 1.6.1.3 — 2026-11-12

- 原因：此前只将鼠标右拖转换为左拖，手柄右摇杆仍经原生相机分支发出人物转身请求。用户确认仅推右摇杆就会起身。
  Cause: the earlier protection converted right-mouse drag to free look, while native analog camera input could still request character turning. The user confirmed that right-stick input alone ended the pose.
- 在当前国服客户端 `2026.11.03.0000.0000` 中，独立定位原生相机旋转模式函数及调用者，核对 26 项控制流/指令：模式 1 与 3 使用相同的水平角更新和垂直角限制，仅模式 1 的额外分支请求人物转身。
  Independently locate the camera rotation-mode function and caller in CN `2026.11.03.0000.0000`. Twenty-six checks confirm identical yaw integration and pitch clamping for modes 1 and 3, with the additional character-turn request restricted to mode 1.
- 仅在插件启用、当前世界相机为第一人称、角色处于既有固定循环模式 3/11 时，将模式 1 改为只转镜头的模式 3。第三人称、GPose、校准、事件相机、其他动作及其他原生模式不变。接口返回值按原生 EAX 使用 32 位整数。
  Override mode 1 with camera-only mode 3 only for the enabled first-person world camera during stationary character modes 3/11. Preserve third person, GPose, calibration, event cameras, unrelated poses and other native modes. Match the native 32-bit EAX return ABI.
- 不改写摇杆、人物动作、移动标志或角色朝向；判断每次调用即时生效，没有需要跨帧恢复的输入快照。镜头保护单独初始化，失败时保留游戏决定并显示状态，不影响原有鼠标保护。
  Do not write stick values, character modes, movement flags or character rotation. The decision is per-call with no retained input snapshot. Initialize independently; failure retains the native decision and reports status without disabling existing mouse protection.
- 发布编译通过，79,446 项离线断言与 124 条英文翻译覆盖通过。尚未做游戏内手柄实测；需要确认右摇杆左右/上下转镜头维持坐姿和躺姿，左摇杆正常起身，站立及第三人称操作正常。
  Release compilation, 79,446 offline assertions and coverage of 124 English entries pass. No in-game controller test has been performed. Verify right-stick yaw/pitch while sitting/dozing, left-stick stand-up movement, and normal standing/third-person controls.

## 1.6.1.2 — 2026-11-12

- 新增逐项中英对照，新配置默认隐藏头部并保留完整头影；使用实际发布程序集验证默认值及旧 JSON 中的手动关闭、自动语言和 NearPlane 0.001 均被保留，共 6 项通过。
  Add paired Chinese/English and enable head hiding/full head shadows for new configurations. Six checks against the actual release assembly confirm defaults and preserve explicit saved off values, Auto language and NearPlane 0.001.
- 编译无错误；79,215 项离线断言及 123 条英文翻译覆盖检查通过。新增检查覆盖逐项组合框、窗口稳定 ID、语言编号兼容与按条对照。
  Compilation succeeds; 79,215 offline assertions and coverage of 123 English entries pass, including paired combo options, stable window IDs, saved language enum compatibility and per-item translation.
- 本机旧安装清单仍为 1.6.1.0，图标地址为 logo2.png。该地址已同步新图标，当前清单继续使用版本化图标文件；两个文件均为相同的 512×512 PNG。
  The inspected local installation was 1.6.1.0, pointing to logo2.png. The legacy URL now serves the new icon; current manifests retain the versioned icon URL. Both files contain the same 512×512 PNG.
- 说明文本按中文后接英文排列。调整复选框文字换行、滑块按钮宽度及校准图示标题高度；本次尚未进行实际游戏界面排版或商店缓存刷新验证。
  Documentation pairs Chinese with English. UI changes wrap checkbox labels, size reset buttons and reserve bilingual diagram headings. Live-game layout and installer cache refresh have not been verified for this release.

## 1.6.1.1 — 2026-11-11

- Branding/artwork/documentation release: window titles, enable label, command help and assembly display metadata use `POV+ Enhanced`. Store `Name` matches. Internal assembly/config identity remains `PovPlus`; the existing custom-repository URL and installer/update URLs remain unchanged.
- 本版仅统一名称、更新图像和文档；保留 `PovPlus` 内部标识、现有配置与原库更新路径，没有修改相机功能。
- Icon and preview dimensions checked against the installed Dalamud CN 15.0.4.0 `PluginImageCache`: 512×512 square icon and 730×380 preview. The full 1672×941 cover is retained separately; installer export preserves its proportions. The icon was also visually inspected at 64×64.
- 与本机安装器核对图标/预览尺寸，分别使用 512×512、730×380；完整横版保留 1672×941，另检查了 64×64 图标可读性。
- Release compilation, 78,962 offline assertions and 123 English translation entries pass. There is no new in-game visual test of the installer, renamed windows or calibration flow in this release.
- 编译、78,962 项离线断言及 123 条英文翻译检查通过；本次未在游戏中实测商店图像、新名称或校准流程。

## 1.6.1.0 — 2026-11-11

| Check / 检查 | Result / 结果 |
| --- | --- |
| Build references / 编译依赖 | .NET SDK 10.0.100; Dalamud CN 15.0.4.0 / API 15. |
| Offline regressions / 离线回归 | 78,962 assertions passed, including calibration migration, pending first use across restarts, draft save/cancel, invalid offset handling and native camera transition. / 78,962 项通过，包含引导迁移、重启待办、偏移草稿保存/取消及相机切换。 |
| Camera fields / 相机字段 | Eight camera fields checked against the installed reference DLL. Transition tests verify that only zoom mode, distance and interpolated distance change; saved distance and unrelated bytes stay intact. Invalid/event/special cameras are unchanged. / 核对八个字段偏移，相机切换只改视角模式与当前/插值距离，其他字段及特殊相机保持不变。 |
| Default and migration / 默认值与迁移 | New/reset/invalid NearPlane uses 0.015; valid saved values including 0.001 remain supported. Existing configurations skip automatic onboarding; a previously pending first use remains pending. / 新配置、重置与无效值回退到 0.015，旧有效值保留，已有用户不被自动引导打断。 |
| Localization / 语言 | 123 English entries; main settings, calibration and status text fully covered. / 123 条英文翻译，主设置、校准及状态文字完整覆盖。 |
| Artwork / 插图 | Embedded PNG and independent localized labels/target markers. Offline diagram composition inspected; no network fetch at runtime. / 内嵌 PNG，标签与标记独立绘制；离线检查图示排版，使用时无需联网。 |
| Reference game / 游戏基线 | CN `2026.11.03.0000.0000`; executable SHA-256 remains `52042b60631cf2c48851b3aaee36e9ec4fedd6d7f2c68f0e9a8c3befb0af9f98`, identical to the native checks recorded for 1.6.0.1. |

No in-game first-use, manual reentry, camera orbit or visual alignment test has been performed for this release. Offline checks do not establish the actual appearance of the popup or alignment on each race/modded face. Existing users can verify the flow with the calibration button at the top of `/pov`, without deleting settings. For a fresh configuration, verify that the guide opens once after login, can save/cancel normally, and does not reopen on the next login. Combat, cutscene, logout and camera changes should close an active draft without saving it.

本版尚未进行游戏内首次引导、手动重进、绕看与视点对齐实测。离线检查不代表已确认弹窗实际外观或所有种族、改模脸型的对齐。已有用户可通过 `/pov` 顶部按钮验证，无需删配置；新配置需确认登录后仅自动引导一次，保存/取消正常，下次登录不再弹出。战斗、过场、退出登录或相机更换应关闭引导并放弃未保存的草稿。

## 1.6.0.1 — 2026-11-10

| Check / 检查 | Result / 结果 |
| --- | --- |
| Loading failure / 加载失败原因 | Dalamud rejected 1.6.0.0 because its applicable game date was older than the running client. / 旧版清单的游戏日期早于当前客户端，导致加载被拒绝。 |
| Version policy / 版本策略 | `ApplicableVersion: any`; API 15 and existing native-interface checks retained. Original repository URL and `PovPlus` identity retained for installer updates. / 取消日期限制，保留 API 15 和现有接口检查，沿用仓库地址与插件标识。 |
| Build references / 编译依赖 | .NET SDK 10.0.100; Dalamud CN 15.0.3.9 / API 15. |
| Offline regressions / 离线回归 | 78,834 assertions passed with the updated reference DLLs. / 新版依赖下全部通过。 |
| Signatures / 签名 | 19 native signatures each resolve uniquely in the new executable. / 19 处原生签名均唯一匹配。 |
| Head-shadow paths / 头影绘制路径 | 46 instruction/branch checks passed after rebasing from independently scanned renderer anchors; stack-local main/shadow masks remain separate. / 46 处指令与分支检查通过，主画面和阴影标志保持分离。 |
| Stationary input / 固定姿势输入 | 14 native posture/input checks passed; modes 3/11, held-button and cursor-rectangle fields retain their semantics. / 14 处姿势和鼠标输入检查通过。 |
| Camera entry / 相机入口 | 84 instructions compared with the previous slot-16 entry; register operations and member offsets match after relocation. / 相机虚表第 16 项入口的 84 条指令对照通过。 |
| Near objects / 近对象 | Three distance-helper calls, their pointer arguments and the squared-radius visibility branch revalidated. / 三处距离函数调用及参数、可见性判断重新核对通过。 |
| Reference layouts / 参考类型布局 | 79 field declarations/offsets in Camera, CameraBase, InputManager and ThreadLocals match the previous references. / 四类参考结构共 79 处字段声明和偏移一致。 |
| Localization / 语言 | 102 English entries and active UI/status coverage checked. |
| Reference game / 游戏基线 | CN `2026.11.03.0000.0000`, executable SHA-256 `52042b60631cf2c48851b3aaee36e9ec4fedd6d7f2c68f0e9a8c3befb0af9f98`. |

These are offline checks. No new in-game visual/input test was performed for this compatibility release. Removing the date restriction permits ordinary updates to load; it does not prove compatibility with all future clients or detect every possible native layout change.

以上为离线验证。本兼容更新尚未进行新的游戏内画面和输入测试。取消日期限制允许普通更新继续加载，不代表已证明所有未来客户端兼容，也不代表现有检查能检测全部原生布局变化。

## 1.6.0.0 — 2026-11-08

| Check / 检查 | Result / 结果 |
| --- | --- |
| Release compilation / 发布编译 | .NET SDK 10.0.100; API 15 references from Dalamud CN 15.0.3.8. |
| Offline regressions / 离线回归 | 78,834 assertions passed, including localization, ordinary/direct stationary input, weighted head rotation, head shadow masks, lifecycle, near-object scope, leaning, limits, VFX and camera transitions. |
| Localization / 语言 | 102 English entries; active Chinese UI/status text coverage checked; locale override, automatic selection, stable ImGui IDs and NUL-separated combo options checked. |
| Reference game / 游戏基线 | CN `2026.10.06.0000.0000`, executable SHA-256 `958b78e278ad896493c484eb313ff605ab41edefef74f92b0ee078e69b281bf9`. |

Earlier feature iterations received in-game feedback, including full head shadows and stationary-pose protection. The public localization/package revision is checked offline; no claim is made that all models, animations, clients or third-party combinations have been tested. Interface layout and language switching should also be checked in game.

此前功能迭代已获得游戏内反馈，包含完整头影和固定姿势保护。本次公开版的语言与打包修改进行了离线验证；没有宣称已覆盖全部模型、动作、客户端或插件组合。语言切换和界面布局仍应在实际游戏中检查。
