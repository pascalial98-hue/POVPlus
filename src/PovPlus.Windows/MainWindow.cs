using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;

namespace PovPlus.Windows;

public class MainWindow : Window, IDisposable
{
    private readonly Plugin plugin;
    private Configuration Config => plugin.Configuration;
    private bool recordingKey, keyRecorderArmed;
    private int pendingModifier;

    public MainWindow(Plugin plugin) : base(L.T("POV+ Enhanced 第一人称设置###PovPlus"), ImGuiWindowFlags.AlwaysVerticalScrollbar)
    {
        this.plugin = plugin;
        Size = new Vector2(660, 720);
        SizeCondition = ImGuiCond.FirstUseEver;
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(520, 340),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };
    }

    public void Dispose() { }
    public override void OnClose() => recordingKey = false;
    public override void PreDraw() => WindowName = L.T("POV+ Enhanced 第一人称设置###PovPlus");

    public override void Draw()
    {
        DrawAxes();
        var language = Enum.IsDefined(Config.Language) ? (int)Config.Language : 0;
        ImGui.SetNextItemWidth(260 * ImGuiHelpers.GlobalScale);
        if (ImGui.Combo("语言 / Language###language", ref language, "自动 / Auto\0简体中文\0English\0中英对照 / 中文 + English\0"))
        {
            Config.Language = (PluginLanguage)language;
            Config.Save();
            plugin.RefreshLanguage();
        }
        ImGui.TextWrapped(L.T("中英对照逐项显示中文和英文；自动模式跟随 Dalamud 语言。修改立即生效。"));
        Toggle("启用 POV+ Enhanced", ref Config.Setting_ModEnabled);
        ImGui.TextDisabled(L.T("/pov toggle 可切换启用状态"));
        if (plugin.RuntimeStatus.Length > 0) ImGui.TextWrapped(plugin.RuntimeStatus);
        ImGui.BeginDisabled(!plugin.CanStartCalibration);
        var calibrate = ImGui.Button(L.Label("视点校准引导"));
        ImGui.EndDisabled();
        if (calibrate && plugin.StartCalibration()) return;
        ImGui.TextWrapped(L.T("切到第三人称，用图示和实时标记调整两眼之间的视点。请在正常场景、非战斗状态下使用。"));
        ImGui.Separator();
        if (!ImGui.BeginTabBar("PovSettings")) return;
        if (ImGui.BeginTabItem(L.Label("常用"))) { Common(); ImGui.EndTabItem(); }
        if (ImGui.BeginTabItem(L.Label("头部与特效"))) { Appearance(); ImGui.EndTabItem(); }
        if (ImGui.BeginTabItem(L.Label("高级"))) { Advanced(); ImGui.EndTabItem(); }
        ImGui.EndTabBar();
    }

    private void Common()
    {
        MouseLookSettings();
        Heading("转头范围");
        Toggle("限制转头角度", ref Config.FirstPersonLimitLook);
        ImGui.BeginDisabled(!Config.FirstPersonLimitLook);
        Slider("左右各", ref Config.FirstPersonYawDegrees, 5, 85, 80, "%.0f°");
        Slider("向上", ref Config.FirstPersonLookUpDegrees, 5, 85, 60, "%.0f°");
        Slider("向下", ref Config.FirstPersonLookDownDegrees, 5, 85, 75, "%.0f°");
        Toggle("接近边界时柔和减速", ref Config.FirstPersonSoftLimits);
        if (Config.FirstPersonSoftLimits)
        {
            Slider("提前减速的角度", ref Config.FirstPersonSoftZoneDegrees, 5, 25, 12, "%.0f°");
            ImGui.TextWrapped(L.T("只在继续向外转头时减速，往回看不减速。角度上限仍然有效。"));
        }
        ImGui.EndDisabled();
        Heading("限位探头");
        Toggle("到达转头限制后，继续拖动来探头", ref Config.FirstPersonLookDownLean);
        if (Config.FirstPersonLookDownLean)
        {
            Slider("前伸幅度", ref Config.FirstPersonLeanForward, 0, LookDownLean.Maximum, LookDownLean.DefaultForward, "%.3f");
            Slider("侧伸幅度（左右共用）", ref Config.FirstPersonLeanSide, 0, LookDownLean.Maximum, LookDownLean.DefaultSide, "%.3f");
            var sensitivity = BoundaryLookAxis.SanitizeSensitivity(Config.FirstPersonLeanSensitivity) * 100;
            if (Slider("伸展灵敏度", ref sensitivity, 10, 100, BoundaryLookAxis.DefaultSensitivity * 100, "%.0f%%", save: false))
            {
                Config.FirstPersonLeanSensitivity = sensitivity / 100;
                Config.Save();
            }
            ImGui.TextWrapped(L.T("顶到向下或左右上限后，继续拖动才伸展。位移直接跟随鼠标，停下保持，反向拖动先收回。伸展灵敏度只影响伸展和回收，正常转头速度不变；35% 时约需额外 43° 的拖动量伸到最大。"));
            ImGui.TextWrapped(L.T("需开启转头角度限制。幅度为游戏距离单位，设为 0 可分别关闭。"));
            Slider("前伸时额外低头", ref Config.FirstPersonLeanExtraDownDegrees, 0, 20, 10, "%.0f°");
            ImGui.TextWrapped(L.T("到原低头限位后，前伸与额外低头同时进行，反向同时收回。最多越过垂直向下 5°（总计 95°）。设为 0 仅保留前伸；两者沿用伸展灵敏度。"));
            Slider("侧伸时额外转头（左右各）", ref Config.FirstPersonLeanExtraSideDegrees, 0, 20, 10, "%.0f°");
            ImGui.TextWrapped(L.T("到原左右限位后，侧伸距离与额外转角同步变化。默认每侧多转 10°，原限位 80° 时可到 90°；设为 0 仅保留侧伸。反向同时收回距离和额外转角。"));
            if (FirstPersonLimits.LeanStatus.Length > 0) ImGui.TextWrapped(FirstPersonLimits.LeanStatus);
        }
        Heading("画面");
        Slider("视野（FOV）", ref Config.Setting_FOV, 0, 5, 0.75f, "%.2f");
        Slider("近裁剪距离（NearPlane）", ref Config.CameraNearPlane, 0.001f, 1, NearPlanePatch.Default, "%.3f");
        if (ImGui.Button(L.Label("设为 0.001"))) { Config.CameraNearPlane = 0.001f; Config.Save(); }
        ImGui.TextWrapped(L.T("数值越小，近处衣服越完整；水雾等粒子可能穿过地形显示。此项只作用于第一人称。"));
        Toggle("保留近镜头的其他角色与物件（试验）", ref Config.FirstPersonKeepNearObjects);
        if (plugin.NearObjectsStatus.Length > 0) ImGui.TextWrapped(plugin.NearObjectsStatus);
        if (Config.FirstPersonKeepNearObjects)
            ImGui.TextWrapped(L.T("解除另一层按距离隐藏整个对象的判断，仅在第一人称生效。针对走该判断的角色与物件；部分地图静态模型、未加载物体及背面不可见不在此项覆盖范围内。"));
    }

    private void MouseLookSettings()
    {
        Heading("镜头操作");
        ImGui.TextWrapped(L.T("坐下、躺下及同类固定/循环动作期间，手柄右摇杆、普通右键和鼠标直控都只转镜头，避免转身打断动作。左摇杆和移动键仍可正常起身。"));
        if (plugin.StationaryCameraStatus.Length > 0) ImGui.TextWrapped(plugin.StationaryCameraStatus);
        if (plugin.MouseLookStatus.Length > 0) ImGui.TextWrapped(plugin.MouseLookStatus);
        Toggle("第一人称下，移动鼠标直接转动视角", ref Config.FirstPersonMouseLook);
        if (!Config.FirstPersonMouseLook) { recordingKey = false; return; }
        var mode = Config.FirstPersonCursorToggle ? 1 : 0;
        ImGui.TextWrapped(L.T("呼出鼠标方式"));
        ImGui.SetNextItemWidth(-1);
        if (ImGui.Combo("##cursorMode", ref mode, L.T("按住快捷键\0按一下切换\0")))
        {
            Config.FirstPersonCursorToggle = mode == 1;
            Config.Save();
        }
        ImGui.TextWrapped(L.T("呼出快捷键：", inline: true) + MouseLookKeys.Name(Config.FirstPersonCursorKey, Config.FirstPersonCursorModifiers));
        if (ImGui.Button(recordingKey ? L.Label("等待按键…（Esc 取消）") : L.Label("录制快捷键")))
        {
            recordingKey = true;
            keyRecorderArmed = false;
            pendingModifier = 0;
        }
        if (Config.Language != PluginLanguage.Bilingual) ImGui.SameLine();
        if (ImGui.Button(L.Label("重置为 Alt")))
        {
            recordingKey = false;
            Config.FirstPersonCursorKey = MouseLookKeys.DefaultKey;
            Config.FirstPersonCursorModifiers = 0;
            Config.Save();
        }
        if (recordingKey) RecordMouseLookKey();
        ImGui.TextWrapped(Config.FirstPersonCursorToggle
            ? L.T("默认锁定视角；按一次快捷键呼出鼠标，再按一次恢复转视角。")
            : L.T("默认锁定视角；按住快捷键呼出鼠标，松开后恢复转视角。"));
        ImGui.TextWrapped(L.T("直控时按住左键可自由观察，镜头转动不带动人物朝向；松开左键恢复右键式直控。自由观察仍遵守转头与探头范围。"));
        ImGui.TextWrapped(L.T("支持单键或 Ctrl / Alt / Shift 组合。录制单独修饰键时，按下再松开。请避开常用游戏快捷键；原游戏按键绑定仍生效。锁定时需先呼出鼠标再点击界面。"));
        ImGui.TextWrapped(L.T("设置窗口、文字输入或切出游戏时自动释放；关闭设置、结束输入或返回游戏后恢复。退出第一人称恢复普通操作。"));
    }

    private void RecordMouseLookKey()
    {
        if (!MouseLookKeys.Foreground || MouseLookKeys.Down(27)) { recordingKey = false; return; }
        var any = false;
        foreach (var key in MouseLookKeys.Allowed) any |= MouseLookKeys.Down(key);
        if (!keyRecorderArmed) { if (!any) keyRecorderArmed = true; return; }
        foreach (var key in MouseLookKeys.Allowed)
        {
            if (!MouseLookKeys.Down(key)) continue;
            if (MouseLookKeys.Modifier(key)) { pendingModifier = key; continue; }
            SaveKey(key, MouseLookKeys.Modifiers);
            return;
        }
        if (!any && pendingModifier != 0) SaveKey(pendingModifier, 0);
        void SaveKey(int key, int modifiers)
        {
            Config.FirstPersonCursorKey = key;
            Config.FirstPersonCursorModifiers = modifiers;
            Config.Save();
            recordingKey = false;
        }
    }

    private void Appearance()
    {
        Heading("头部显隐");
        Toggle("隐藏自己的头部", ref Config.FirstPersonHideHead);
        if (plugin.HeadHidingStatus.Length > 0) ImGui.TextWrapped(plugin.HeadHidingStatus);
        ImGui.BeginDisabled(!Config.FirstPersonHideHead);
        Toggle("保留完整头影（整块隐藏脸、头发和头饰，试验）", ref Config.FirstPersonFullHeadShadow);
        if (Config.FirstPersonFullHeadShadow)
        {
            ImGui.TextWrapped(L.T("保留正常骨骼，只在主画面隐藏对应模型。发梢和饰品不再局部保留，脸部模型内的颈部也可能消失。独立兔耳按资源识别；特殊改模可能仍有遮挡。游戏需开启自身阴影。"));
            if (plugin.HeadShadowStatus.Length > 0) ImGui.TextWrapped(plugin.HeadShadowStatus);
        }
        else ImGui.TextWrapped(L.T("原骨骼隐藏方式：保留颈部，头影会随头部隐藏。"));
        Toggle("进入时，镜头接近后再隐藏", ref Config.FirstPersonDelayHeadHide);
        if (Config.FirstPersonDelayHeadHide)
            Slider("进入隐藏距离", ref Config.FirstPersonHeadHideDistance, 0.05f, 0.75f, 0.25f, "%.2f");
        Slider("退出恢复距离", ref Config.FirstPersonHeadShowDistance, 0.2f, 1, 0.35f, "%.2f");
        ImGui.TextWrapped(L.T("进入距离越小，隐藏越晚；退出距离越大，恢复越晚。"));

        Heading("保留外观细节（试验）");
        ImGui.BeginDisabled(Config.FirstPersonFullHeadShadow);
        Toggle("保留远离镜头的发梢", ref Config.FirstPersonPreserveHair);
        Toggle("保留远离镜头的耳部及关联饰物", ref Config.FirstPersonPreserveEars);
        if (Config.FirstPersonPreserveHair || Config.FirstPersonPreserveEars)
        {
            Slider("镜头周围的隐藏范围", ref Config.FirstPersonDetailHideRadius, 0.15f, 0.6f, 0.25f, "%.2f");
            ImGui.TextWrapped(L.T("范围越大，遮挡越少、保留越少。仅支持能识别的头发和耳部骨骼；若出现发丝拉伸或耳部遮挡，请关闭对应选项。"));
        }
        ImGui.EndDisabled();
        ImGui.EndDisabled();

        Heading("近处自身特效（试验）");
        Toggle("减弱贴近镜头的自身特效", ref Config.FirstPersonReduceSelfVfx);
        if (plugin.VfxStatus.Length > 0) ImGui.TextWrapped(plugin.VfxStatus);
        if (Config.FirstPersonReduceSelfVfx)
        {
            Slider("减弱范围", ref Config.FirstPersonVfxRadius, 0.2f, 3, 1.2f, "%.2f");
            Slider("贴近时保留强度", ref Config.FirstPersonVfxOpacity, 0.05f, 1, 0.25f, "%.2f");
            ImGui.TextWrapped(L.T("0.25 表示最贴近时保留四分之一强度。仅处理施放者和目标均为自己的可识别效果，按整个效果的位置判断远近；部分武器光效可能不在此范围。"));
        }
    }

    private void Advanced()
    {
        if (ImGui.CollapsingHeader(L.Label("使用说明")))
            ImGui.TextWrapped(L.T("请在游戏的角色设置中，将第一人称视角自动调整（1st Person Camera Auto Adjustment）设为 Never。左键自由观察、右键回正沿用游戏操作。设置修改后自动保存。"));
        if (ImGui.CollapsingHeader(L.Label("视点偏移")))
        {
            Slider("左右偏移", ref Config.SETTINGS_CameraOffsetX, -1, 1, 0, "%.3f");
            Slider("上下偏移", ref Config.SETTINGS_CameraOffsetY, -1, 1, 0, "%.3f");
            Slider("前后偏移", ref Config.SETTINGS_CameraOffsetZ, -1, 1, 0, "%.3f");
            Slider("额外倾斜", ref Config.CameraTiltOffset, -3, 3, 0, "%.2f");
        }
        if (ImGui.CollapsingHeader(L.Label("跟随头部旋转")))
        {
            ImGui.TextWrapped(L.T("视点位置始终跟随头部。以下开关额外让画面方向跟随头部动画。"));
            ImGui.TextWrapped(L.T("关联度控制转动幅度，平滑控制跟随速度。50% 表示头部转动 60°，镜头跟随约 30°；仍受转头限位约束。鼠标灵敏度不受关联度影响。"));
            Toggle("跟随左右转动", ref Config.YawEnabled);
            if (Config.YawEnabled)
            {
                Slider("左右关联度", ref Config.HeadYawWeight, 0, 100, 100, "%.0f%%");
                Slider("左右平滑", ref Config.Settings_YawSmoothing, 0, 100, 60, "%.0f%%");
            }
            Toggle("跟随上下转动", ref Config.PitchEnabled);
            if (Config.PitchEnabled)
            {
                Slider("上下关联度", ref Config.HeadPitchWeight, 0, 100, 100, "%.0f%%");
                Slider("上下平滑", ref Config.Settings_PitchSmoothing, 0, 100, 60, "%.0f%%");
            }
            Toggle("跟随侧倾", ref Config.RollEnabled);
            if (Config.RollEnabled)
            {
                Slider("侧倾关联度", ref Config.HeadRollWeight, 0, 100, 100, "%.0f%%");
                Slider("侧倾平滑", ref Config.Settings_RollSmoothing, 0, 100, 60, "%.0f%%");
                ImGui.TextWrapped(L.T("部分动作可能产生较大的画面倾斜。"));
            }
        }
        if (ImGui.CollapsingHeader(L.Label("其他模式与调试")))
        {
            Toggle("第三人称也启用视点绑定", ref Config.Setting_FirstPersonOnly);
            Toggle("集体动作（GPose）中启用视点绑定", ref Config.Setting_GposeEnabled);
            Toggle("显示头部位置与方向辅助线", ref Config.SETTINGS_ShowCameraLocation);
            ImGui.TextWrapped(L.T("辅助线需同时启用第三人称视点绑定，并保持此设置窗口打开。"));
        }
    }

    private static void Heading(string text)
    {
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.TextUnformatted(L.T(text));
    }

    private void Toggle(string label, ref bool value)
    {
        ImGui.PushID(label);
        var changed = ImGui.Checkbox("##enabled", ref value);
        ImGui.SameLine();
        ImGui.TextWrapped(L.T(label));
        // A wrapped label remains clickable; disabled controls also disable this hit target.
        if (ImGui.IsItemClicked()) { value = !value; changed = true; }
        if (changed) Config.Save();
        ImGui.PopID();
    }

    private bool Slider(string label, ref float value, float min, float max, float reset, string format, bool save = true)
    {
        ImGui.PushID(label);
        ImGui.TextWrapped(L.T(label));
        var resetWidth = ImGui.CalcTextSize(L.T("重置", inline: true)).X + ImGui.GetStyle().FramePadding.X * 2;
        ImGui.SetNextItemWidth(-resetWidth - ImGui.GetStyle().ItemSpacing.X);
        var changed = ImGui.SliderFloat("##value", ref value, min, max, format, ImGuiSliderFlags.AlwaysClamp);
        ImGui.SameLine();
        if (ImGui.Button(L.Label("重置"))) { value = reset; changed = true; }
        if (changed)
        {
            value = float.IsFinite(value) ? Math.Clamp(value, min, max) : reset;
            if (save) Config.Save();
        }
        ImGui.PopID();
        return changed;
    }

    private void DrawAxes()
    {
        if (!Config.SETTINGS_ShowCameraLocation || !Config.Setting_FirstPersonOnly ||
            !Service.GameGui.WorldToScreen(GlobalVars.BoneLocation, out var origin)) return;
        var draw = ImGui.GetBackgroundDrawList();
        draw.AddCircleFilled(origin, 6, 0xffffffff);
        Arrow(GlobalVars.DEBUG_PitchCameraEnd, 4278190335u);
        Arrow(GlobalVars.DEBUG_RollCameraEnd, 4278255360u);
        Arrow(GlobalVars.DEBUG_YawCameraEnd, 4294901760u);
        void Arrow(Vector3 end, uint color)
        {
            if (!Service.GameGui.WorldToScreen(end, out var point) || Vector2.DistanceSquared(origin, point) < 0.01f) return;
            draw.AddLine(origin, point, color, 3);
            var direction = Vector2.Normalize(point - origin);
            var side = new Vector2(-direction.Y, direction.X) * 8;
            draw.AddTriangleFilled(point, point - direction * 12 + side, point - direction * 12 - side, color);
        }
    }
}
