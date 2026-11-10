# Validation / 验证记录

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
