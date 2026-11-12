# Changelog / 更新记录

## 1.6.1.4 — 2026-11-12

- 新增可独立开启的“视角与限位跟随身体姿态”，默认关闭。以躯干作为正视基准，弯腰 90° 时正视地面，抬头 90° 可看向前方；转头范围和探头方向随身体姿态变化。<br>
  Add an independent, default-off Body-relative view and look limits option. The torso defines the neutral view: a 90° bow looks at the ground, while looking up 90° reaches the horizon. Look limits and leaning directions follow body posture.

- **使用建议：此选项适合静止姿态观察，不适合移动时使用；即使有平滑处理，跑动时仍可能明显晃动。**<br>
  **Usage note: This option is intended for viewing stationary poses, not for use while moving. Running may still cause noticeable camera sway even with smoothing.**

- 抬头上限放宽到 90°，头颈跟随只叠加相对躯干的变化，避免重复计算弯腰；移动时加入旋转平滑，停下后逐渐恢复。鼠标转头及原有视点位置不经过这层平滑。<br>
  Raise the upward look limit to 90°. Head follow adds only motion relative to the torso to avoid counting bends twice. Smooth animation rotation while moving and restore the stationary response gradually after stopping; mouse input and viewpoint position bypass this smoothing.

- 已添加本分支仓库的用户直接在 Dalamud 安装器更新，无需更换库链接或重装；保留 `PovPlus` 内部标识、API 15 和现有配置。<br>
  Existing users of this fork can update directly in the Dalamud installer, without changing the repository URL or reinstalling. Keep the `PovPlus` identity, API 15 and saved settings.

- 本地测试版已获得游戏内确认；146,981 项离线断言、129 条英文翻译覆盖及发布编译通过。移动使用的限制如上，不代表已验证所有种族、动作模组或国际服客户端。<br>
  The local test build received in-game confirmation. Release compilation, 146,981 offline assertions and 129 English translation entries pass. The movement limitation above remains; this does not verify every race, animation mod or international client.

## 1.6.1.3 — 2026-11-12

- 修复第一人称下，手柄右摇杆转镜头会让坐下、躺下及同类固定循环动作中断的问题。固定姿势期间改用游戏原生的只转镜头分支，保留原有摇杆灵敏度和镜头角度限制。
  Fix gamepad right-stick camera input interrupting sitting, dozing and similar stationary loops in first person. Use the native camera-only rotation path during those poses, retaining stick sensitivity and camera limits.
- 左摇杆和移动键仍交由游戏正常处理；保护无需开启鼠标直控。离开固定姿势或第一人称后恢复正常转身行为。
  Leave left-stick and movement-key input to the game. Protection does not require direct mouse look; normal turning resumes outside fixed poses or first person.
- 新增独立的镜头转身判断保护与故障提示，原有鼠标保护保留。原库地址、配置和内部名称不变。
  Add independent camera-turn protection with a failure status while retaining the existing mouse protection. Keep the repository URL, saved settings and internal plugin name.
- 编译、79,446 项离线断言及 26 项当前客户端原生控制流检查通过；尚待游戏内手柄验证。
  Compilation, 79,446 offline assertions and 26 native control-flow checks pass. Controller behavior still needs in-game verification.

## 1.6.1.2 — 2026-11-12

- 新增逐项中英对照界面：说明为中文后接英文，按钮和短标签并排显示。设置、状态和校准引导均支持；新配置默认使用对照模式，已有语言选择保留，可在 /pov 顶部切换。
  Add paired Chinese/English throughout settings, status messages and calibration. New configurations default to bilingual mode; existing language choices are retained and can be changed at the top of /pov.
- 新配置默认开启隐藏自己的头部与完整头影；保留旧配置中的开关选择，完整头影仍可关闭，游戏需开启自身阴影。
  Enable head hiding and full head shadows by default for new configurations. Preserve saved choices; full shadows can still be disabled and require the game's self-shadow option.
- XMA 正文及 GitHub 首页改为逐段中英对照；插件商店描述也按同样方式排列。
  Pair each Chinese paragraph with English in the XMA description, GitHub main page and plugin-store description.
- 旧清单使用的 logo2.png 同步换成新图标，继续使用现有库地址更新。如果安装器本次会话已缓存旧图，刷新仓库后重新打开；仍未变化时重启游戏。
  Update the legacy logo2.png URL to the new icon, keeping the existing update repository. Refresh the repository and reopen the installer; restart the game if its current session still holds the old image.

## 1.6.1.1 — 2026-11-11

- 设置窗口、校准窗口、启用开关与命令帮助统一使用 **POV+ Enhanced** 名称，商店清单沿用相同名称。
- 新增深蓝/青绿/金色插件图标与发布预览图；左侧为低头身体可见，右侧为第三人称参照和第一人称互动。商店分别使用 512×512 图标与 730×380 预览图。
- 补充适合 XMA 的中英发布介绍、安装说明、原作者署名及 AI 使用说明。国际服仍未验证。
- 仅更新名称、图像和文档，现有相机功能、配置、内部名称 `PovPlus` 与原库更新地址保持不变。

- Use **POV+ Enhanced** consistently in settings, calibration, enable controls and command help, matching the store listing.
- Add a navy/teal/gold icon and release preview. The large panel shows the downward body view; smaller panels compare third-person and first-person interaction. Store assets use a 512×512 icon and 730×380 preview.
- Include bilingual XMA-ready descriptions, installation steps, upstream credits and AI-use information. International clients remain unverified.
- Branding, artwork and documentation only; camera behavior, saved settings, internal name `PovPlus` and the existing update repository remain unchanged.

## 1.6.1.0 — 2026-11-11

- 新增首次使用视点校准：新配置在角色与相机就绪后自动进入第三人称，打开独立浮窗并显示实时视点标记；`/pov` 顶部提供重新进入按钮。
- 用参考 FF14 官方中原之民形象重绘的正面、侧面插图，说明将视点放在两眼之间的鼻梁表面。支持三轴拖动、直接输入和 0.001 微调，所有引导文字提供中英版本。
- 校准期间暂停相机绑定、鼠标直控与头部隐藏。偏移先存为草稿，保存才生效，取消/关闭或离开正常游戏场景放弃未保存的调整；结束后保持第三人称。
- 新配置及近裁剪重置值为 **0.015**，仍允许手动设为 0.001。已有用户的偏移、NearPlane 和其他设置保留，升级不自动弹出引导。
- 对 Dalamud CN 15.0.4.0 / API 15 编译。沿用原安装仓库、内部名称和更新地址；新增流程尚待游戏内验证。

- Add first-use viewpoint calibration: new configurations enter third person once the character and camera are ready, with a separate window and live viewpoint marker. Reopen from the top of `/pov`.
- Add front/side illustrations redrawn from the official FFXIV Midlander character reference, showing the nose-bridge surface between the eyes. Three-axis dragging, typed values and 0.001 fine steps; fully bilingual instructions.
- Pause camera binding, direct mouse look and head hiding during calibration. Offsets remain a draft until saved; cancel, close or leaving normal gameplay discards unsaved edits. Stay in third person afterward.
- Default/reset NearPlane is **0.015**; 0.001 remains available. Preserve existing offsets, NearPlane and other settings, without automatically interrupting existing users on upgrade.
- Build against Dalamud CN 15.0.4.0 / API 15. Keep the original repository URL, internal name and update links. The new workflow still needs in-game verification.

## 1.6.0.1 — 2026-11-10

- 修复国服更新后因旧游戏日期限制而无法加载的问题。`ApplicableVersion` 改为 `any`，保留 Dalamud API 15 要求和现有原生接口检查，后续普通小更新不再仅因日期变化而被拦截。
- 对国服 `2026.11.03.0000.0000` / Dalamud `15.0.3.9` 重新编译与验证；19 处签名、头影绘制路径、固定姿势鼠标输入、近对象距离判断及参考类型布局通过静态核对。
- 原有第一人称功能和配置保持一致，未新增功能。78,834 项离线断言通过；尚未进行本次更新后的游戏内复测。
- 发布脚本从插件清单读取 API 级别和更新说明，避免新版本仍显示旧版本说明。

- Fix loading after the CN game update by changing `ApplicableVersion` to `any`. Keep Dalamud API 15 and existing native-interface checks; ordinary updates are no longer blocked solely by a changed game date.
- Rebuild and validate against CN `2026.11.03.0000.0000` / Dalamud `15.0.3.9`: 19 signatures, head-shadow paths, stationary mouse input, near-object distance behavior and reference layouts checked offline.
- Preserve existing first-person behavior and configuration. All 78,834 offline assertions passed; no in-game retest after this update has been performed.
- Read release API level and changelog from the plugin manifest during packaging.

## 1.6.0.0 — 2026-11-08

首个公开增强版，包含此前个人版已验证的功能，并添加语言选择与公开发布流程。

- 设置支持自动、简体中文、英文，包含帮助和运行状态，切换即时生效。
- 保留头部显隐过渡、可选完整头影、软转头限位、限位后前伸/侧伸及同步额外转角。
- 保留鼠标直控、左键自由观察、快捷键呼出鼠标，以及普通右键和直控的固定姿势保护。
- 保留三轴动画跟随关联度、登录初始化修复、NearPlane 0.001 和可选近处物体/外观/自身特效处理。
- 添加中英安装与构建说明、完整可构建源码、回归检查、安装器仓库清单与发布包。

First public Enhanced release, carrying forward the established personal-build features and adding localization and public distribution.

- Immediate Auto / Simplified Chinese / English selection for settings, help and runtime status.
- Head transitions, optional complete head shadows, soft look limits, forward/side leaning with blended extra rotation.
- Direct mouse look, LMB free look, cursor-release hotkey, and stationary-pose protection for both ordinary RMB and direct input.
- Independent head-animation weights, login initialization fixes, NearPlane 0.001 and optional close-up object/detail/self-effect handling.
- Bilingual installation/build docs, complete buildable source, regressions, custom repository manifest and release packages.

Compatibility / 兼容基线: CN `2026.10.06.0000.0000`, Dalamud API 15 / 15.0.3.8, Windows x64 / .NET 10. Offline validation is recorded in `docs/VALIDATION.md`; it does not replace in-game compatibility testing.
