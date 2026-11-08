# Validation / 验证记录

Release / 版本: **1.6.0.0**, 2026-11-08.

| Check / 检查 | Result / 结果 |
| --- | --- |
| Release compilation / 发布编译 | .NET SDK 10.0.100; API 15 references from Dalamud CN 15.0.3.8. |
| Offline regressions / 离线回归 | 78,834 assertions passed, including localization, ordinary/direct stationary input, weighted head rotation, head shadow masks, lifecycle, near-object scope, leaning, limits, VFX and camera transitions. |
| Localization / 语言 | 102 English entries; active Chinese UI/status text coverage checked; locale override, automatic selection, stable ImGui IDs and NUL-separated combo options checked. |
| Reference game / 游戏基线 | CN `2026.10.06.0000.0000`, executable SHA-256 `958b78e278ad896493c484eb313ff605ab41edefef74f92b0ee078e69b281bf9`. |

Earlier feature iterations received in-game feedback, including full head shadows and stationary-pose protection. The public localization/package revision is checked offline; no claim is made that all models, animations, clients or third-party combinations have been tested. Interface layout and language switching should also be checked in game.

此前功能迭代已获得游戏内反馈，包含完整头影和固定姿势保护。本次公开版的语言与打包修改进行了离线验证；没有宣称已覆盖全部模型、动作、客户端或插件组合。语言切换和界面布局仍应在实际游戏中检查。
