# Changelog / 更新记录

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
