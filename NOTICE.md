# Attribution / 署名

- Original POV+ / 原项目: **Tiraesias (TiraesiascenGraven)** — https://github.com/TiraesiascenGraven/POVPlus
- Enhanced fork / 增强分支: **pascalial98-hue** — https://github.com/pascalial98-hue/POVPlus
- License / 许可: **AGPL-3.0-or-later**, as declared by the original project. The complete license is preserved in `LICENSE.md`.

The current `src/` baseline was recovered from the upstream publicly distributed POV+ Beta 1.5.2.2 assembly, then adapted for the CN/API 15 environment. It is not presented as the original author's unpublished source. Inherited Hypostasis utilities and ImGui helpers are retained with the original names. Dalamud, FFXIVClientStructs, Lumina, InteropGenerator and Newtonsoft.Json are external build/runtime dependencies; their binaries are not included in this release.

`src/` 当前基线由上游公开发布的 POV+ Beta 1.5.2.2 程序集恢复，再适配国服/API 15，并非原作者未公开的源代码。保留所继承 Hypostasis 与 ImGui 辅助代码的名称；外部依赖 DLL 不随发布包分发。

Changes dated 2026-11-08: head visibility and transitions, optional head-shadow preservation, soft look limits, blended boundary leaning, direct mouse control, stationary-pose protection, per-axis head-follow weights, lifecycle fixes, optional close-up visibility/effects, localization and public packaging. Full corresponding source and build instructions accompany this release.
