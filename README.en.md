# POV+ Enhanced

[简体中文](README.md) · **English**

See your body, turn and lean naturally, and keep your head's shadow.

A community fork of [Tiraesias's POV+](https://github.com/TiraesiascenGraven/POVPlus) Beta for FFXIV CN / Dalamud. Current version: **1.6.0.0**. Original attribution, Git history and the AGPL-3.0-or-later license are retained.

## Installation

1. Open Dalamud Settings → **Experimental** → **Custom Plugin Repositories**. Add, enable and save:

   ```text
   https://raw.githubusercontent.com/pascalial98-hue/POVPlus/main/repo.json
   ```

2. Refresh the plugin installer, search for **POV+ Enhanced**, and install it. Future updates use the same installer; the testing channel is not required.
3. In Character Configuration → Control Settings → General, set **1st Person Camera Auto Adjustment** to **Never**.
4. Open settings with `/pov`, then enter first person using the game's normal controls.

**Migrating from upstream or a personal test build:** back up your `PovPlus` configuration, disable the old plugin, and remove its development-plugin entry if present. Disable the original POV+ custom repository before installing this fork. The internal name remains `PovPlus` to preserve settings. Do not load two copies at once; deleting your configuration is normally unnecessary.

Manual packages are available in [Releases](https://github.com/pascalial98-hue/POVPlus/releases/latest). The custom repository is recommended for normal installation. `PovPlus.Enhanced.zip` in the repository root is the current installer package; the other old ZIPs and `SamplePlugin` are inherited upstream files, not current fork releases.

## Features

| Feature | Behavior |
| --- | --- |
| Head hiding and camera transitions | Hides the head as the camera approaches, and restores it only after the camera has moved away. The default bone method retains the neck bone. |
| Complete head shadow, optional | Excludes face, hair, headgear and related models from the main view while keeping their native shadow rendering. No duplicate actor is created. |
| Natural look limits | Separate horizontal, upward and downward limits with gentle slowdown near the boundary. |
| Lean beyond the boundary | Continue moving the mouse at a limit to lean forward or sideways. Adjustable distance, sensitivity and extra angle; translation and rotation blend together. Holds when input stops, retracts with reverse input. |
| Direct mouse look, optional | Turn without holding RMB; hold LMB to look around without turning the character. Release the cursor with a customizable held or toggled key/chord. |
| Stationary-pose protection | Ordinary RMB and direct mouse look preserve sitting, dozing and similar looped poses. Movement keys still end the pose. |
| Head-animation weights | Independent 0–100% yaw, pitch and roll weights, separate from smoothing. |
| Close-up visibility | NearPlane down to 0.001; optional near-object visibility, selected hair/ear details and attenuation of nearby self-targeted effects. |
| Bilingual settings | Choose **Auto / 简体中文 / English** at the top of settings. Changes apply immediately. Auto follows Dalamud's UI language, using English for non-Chinese languages. |

Use `/pov toggle` to enable or disable the plugin. Settings save automatically. Full head shadows, near-object visibility, appearance details and self-effect attenuation are experimental and off by default.

## Compatibility and limitations

- Verified baseline: **CN client `2026.10.06.0000.0000`, Dalamud API 15 (15.0.3.8), Windows x64 / .NET 10**. The manifest is restricted to this game version; native interfaces need review after game updates. **International clients and other builds have not been verified.**
- NearPlane 0.001 retains more close-up clothing but can make mist and other particles appear through terrain. This tradeoff remains configurable; increasing NearPlane can improve particle occlusion.
- Complete head shadows require the game's self-shadow option. This mode hides entire head-related models, so hair tips/ear accessories cannot be selectively retained; neck geometry belonging to the face model may also disappear. Unusual mods may use different model slots.
- Near-object visibility affects only the relevant distance-based visibility check, not every static map object, backface-culling case or unloaded resource.
- Pose protection recognizes fixed/looped game states; it does not promise to protect every short emote, cast, mount state or custom animation. Cursor hotkeys still reach the game, so avoid binding conflicts.
- Core features received in-game feedback during development. This public packaging/localization update passed compilation and offline checks; not every race, outfit, animation or camera-plugin combination has been retested.

## Source and feedback

Buildable current source is in [`src/`](src), with offline regressions in [`tests/`](tests). This fork starts from source recovered from the author's published Beta binary, with subsequent modifications. The older upstream `SamplePlugin` folder is retained for historical reference only.

See the [bilingual build guide](docs/BUILD.md) and [changelog](CHANGELOG.md). Report problems in [this fork's Issues](https://github.com/pascalial98-hue/POVPlus/issues), including game/Dalamud/plugin versions, relevant options, reproduction steps and necessary logs. Remove personal information from logs first.

Thanks to **Tiraesias / TiraesiascenGraven**, and to the Hypostasis, Dalamud and FFXIVClientStructs projects used by the original plugin. See [NOTICE](NOTICE.md) and [LICENSE](LICENSE.md). This fork is independently maintained.
