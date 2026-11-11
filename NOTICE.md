# Attribution / 署名

- Original POV+ / 原项目: **Tiraesias (TiraesiascenGraven)** — https://github.com/TiraesiascenGraven/POVPlus
- Enhanced fork / 增强分支: **pascalial98-hue** — https://github.com/pascalial98-hue/POVPlus
- License / 许可: **AGPL-3.0-or-later**, as declared by the original project. The complete license is preserved in `LICENSE.md`.

The current `src/` baseline was recovered from the upstream publicly distributed POV+ Beta 1.5.2.2 assembly, then adapted for the CN/API 15 environment. It is not presented as the original author's unpublished source. Inherited Hypostasis utilities and ImGui helpers are retained with the original names. Dalamud, FFXIVClientStructs, Lumina, InteropGenerator and Newtonsoft.Json are external build/runtime dependencies; their binaries are not included in this release.

`src/` 当前基线由上游公开发布的 POV+ Beta 1.5.2.2 程序集恢复，再适配国服/API 15，并非原作者未公开的源代码。保留所继承 Hypostasis 与 ImGui 辅助代码的名称；外部依赖 DLL 不随发布包分发。

Changes dated 2026-11-08: head visibility and transitions, optional head-shadow preservation, soft look limits, blended boundary leaning, direct mouse control, stationary-pose protection, per-axis head-follow weights, lifecycle fixes, optional close-up visibility/effects, localization and public packaging. Full corresponding source and build instructions accompany this release.

Changes dated 2026-11-11: first-use viewpoint calibration, embedded character diagrams and default NearPlane 0.015. The calibration artwork is an instructional redraw based on the official FFXIV Hyur character reference, not an official illustration or game screenshot. FFXIV character designs belong to SQUARE ENIX; the source-code license does not grant rights to Square Enix's character designs or trademarks. Reference links and the image-generation prompt are documented in `docs/CALIBRATION_ART.md` in the source package.

2026-11-11 新增首次使用校准、内嵌角色图示及 NearPlane 0.015 默认值。图示参考 FF14 官方中原之民角色重绘，并非官方插图或游戏截图。FF14 角色设计归 SQUARE ENIX 所有；源码许可不授予其角色设计或商标权利。参考来源及生成提示词见源码包 `docs/CALIBRATION_ART.md`。
