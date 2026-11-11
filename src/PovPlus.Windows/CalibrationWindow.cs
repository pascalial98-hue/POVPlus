using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Windowing;

namespace PovPlus.Windows;

internal sealed class CalibrationWindow : Window
{
    private readonly Plugin plugin;
    private readonly CalibrationState state = new();
    private ISharedImmediateTexture? artwork;
    private bool artworkFailed;
    private bool frameValid, pointVisible;
    private CalibrationFrame frame;
    private nint actor;
    private nint camera;
    internal bool Active => state.Active;

    internal CalibrationWindow(Plugin plugin) : base(L.T("POV+ 视点校准###PovCalibration"),
        ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.AlwaysVerticalScrollbar)
    {
        this.plugin = plugin;
        Size = new Vector2(560, 800);
        SizeCondition = ImGuiCond.FirstUseEver;
        Position = new Vector2(30, 80);
        PositionCondition = ImGuiCond.FirstUseEver;
        SizeConstraints = new WindowSizeConstraints { MinimumSize = new Vector2(460, 400), MaximumSize = new Vector2(900, 1000) };
    }

    internal void Begin(nint actorAddress, nint cameraAddress)
    {
        var c = plugin.Configuration;
        if (!state.Begin(new Vector3(c.SETTINGS_CameraOffsetX, c.SETTINGS_CameraOffsetY, c.SETTINGS_CameraOffsetZ))) return;
        actor = actorAddress;
        camera = cameraAddress;
        frameValid = pointVisible = false;
        IsOpen = true;
    }

    internal bool Matches(nint actorAddress, nint cameraAddress) => actor == actorAddress && camera == cameraAddress;
    internal void Cancel() => End(false);
    public override void OnClose() => Cancel();
    public override void PreDraw() => WindowName = L.T("POV+ 视点校准###PovCalibration");

    private void End(bool save)
    {
        var result = state.End(save);
        IsOpen = false;
        frameValid = pointVisible = false;
        actor = camera = 0;
        if (result is not { } value) return;
        var config = plugin.Configuration;
        config.SETTINGS_CameraOffsetX = value.X;
        config.SETTINGS_CameraOffsetY = value.Y;
        config.SETTINGS_CameraOffsetZ = value.Z;
        config.Save();
    }

    // Independent of the old third-person binding/debug settings: read the
    // actual head pose each draw, using exactly the first-person offset math.
    internal void DrawMarker()
    {
        frameValid = pointVisible = false;
        if (!Active || !plugin.CalibrationEnvironmentReady) return;
        frameValid = CalibrationViewpoint.TryRead(state.Draft, out frame);
        if (!frameValid || !Service.GameGui.WorldToScreen(frame.Position, out var point)) return;
        pointVisible = true;
        var draw = ImGui.GetBackgroundDrawList();
        var scale = ImGuiHelpers.GlobalScale;
        Axis(frame.Right, 0xff7e7eff);
        Axis(frame.Up, 0xff8ae4a2);
        Axis(frame.Forward, 0xffffba7e);
        draw.AddCircle(point, 9 * scale, 0xcc101010, 32, 5 * scale);
        draw.AddCircle(point, 9 * scale, 0xffffdf65, 32, 2 * scale);
        draw.AddLine(point - new Vector2(14 * scale, 0), point + new Vector2(14 * scale, 0), 0xffffdf65, 2 * scale);
        draw.AddLine(point - new Vector2(0, 14 * scale), point + new Vector2(0, 14 * scale), 0xffffdf65, 2 * scale);
        var label = L.T("视点");
        var textPos = point + new Vector2(18, -9) * scale;
        draw.AddText(textPos + Vector2.One * scale, 0xff101010, label);
        draw.AddText(textPos, 0xffffdf65, label);
        void Axis(Vector3 direction, uint colour)
        {
            if (Service.GameGui.WorldToScreen(frame.Position + direction, out var end))
                draw.AddLine(point, end, colour, 2 * scale);
        }
    }

    public override void Draw()
    {
        var language = Enum.IsDefined(plugin.Configuration.Language) ? (int)plugin.Configuration.Language : 0;
        ImGui.SetNextItemWidth(220 * ImGuiHelpers.GlobalScale);
        if (ImGui.Combo("Language / 语言###language", ref language, "自动 / Auto\0简体中文\0English\0"))
        {
            plugin.Configuration.Language = (PluginLanguage)language;
            plugin.Configuration.Save();
            plugin.RefreshLanguage();
        }
        ImGui.TextWrapped(L.T("把青色十字移到两眼之间的鼻梁处。"));
        ImGui.TextWrapped(L.T("左键拖动绕看角色，滚轮缩放。可把这个窗口拖到一旁，露出脸部。"));
        DrawDiagram();
        ImGui.TextWrapped(L.T("先从正面调左右和上下，再从侧面调前后：视点贴近鼻梁表面，不要放在鼻尖或头部深处。"));
        ImGui.TextWrapped(L.T("青色十字是实际视点，可透过模型显示；请从正面和侧面都确认一次。"));
        ImGui.Separator();
        Adjust("左右偏移", ref state.Draft.X);
        Adjust("上下偏移", ref state.Draft.Y);
        Adjust("前后偏移", ref state.Draft.Z);
        ImGui.TextWrapped(L.T("拖动数值可调整，Ctrl+点击可直接输入；－/＋ 每次微调 0.001。"));
        if (ImGui.Button(L.Label("偏移归零"))) state.Draft = Vector3.Zero;
        if (!frameValid) ImGui.TextWrapped(L.T("正在等待角色头部加载，暂时无法显示视点。"));
        else if (!pointVisible) ImGui.TextWrapped(L.T("视点不在画面内，请转动或拉远镜头，或将偏移归零。"));
        ImGui.Separator();
        ImGui.TextWrapped(L.T("保存后，用游戏原有方式切入第一人称。之后可在 /pov 顶部重新进入校准。"));
        ImGui.BeginDisabled(!frameValid);
        if (ImGui.Button(L.Label("保存并完成"))) End(true);
        ImGui.EndDisabled();
        ImGui.SameLine();
        if (ImGui.Button(L.Label("取消本次调整"))) Cancel();
        ImGui.TextDisabled(L.T("关闭窗口会放弃本次未保存的偏移。"));
    }

    private static void Adjust(string label, ref float value)
    {
        ImGui.PushID(label);
        ImGui.TextUnformatted(L.T(label));
        var scale = ImGuiHelpers.GlobalScale;
        ImGui.SetNextItemWidth(Math.Max(120 * scale, ImGui.GetContentRegionAvail().X - 84 * scale));
        var changed = ImGui.DragFloat("##offset", ref value, 0.001f, -1, 1, "%.3f", ImGuiSliderFlags.AlwaysClamp);
        ImGui.SameLine();
        if (ImGui.Button("-##minus", new Vector2(30 * scale, 0))) { value -= 0.001f; changed = true; }
        ImGui.SameLine();
        if (ImGui.Button("+##plus", new Vector2(30 * scale, 0))) { value += 0.001f; changed = true; }
        if (changed) value = CalibrationState.Offset(value);
        ImGui.PopID();
    }

    private void DrawDiagram()
    {
        // Shared, assembly-embedded artwork: no network or per-frame decoding.
        // Dalamud owns this shared texture and releases unused resources.
        if (artwork == null && !artworkFailed)
        {
            try { artwork = Service.TextureProvider.GetFromManifestResource(typeof(Plugin).Assembly, "PovPlus.Assets.calibration-heads.png"); }
            catch (Exception ex) { artworkFailed = true; Service.Log.Warning(ex, "POV+ calibration artwork unavailable."); }
        }
        // Obtain only a frame-scoped wrap; do not retain or dispose a shared wrap.
        var texture = artwork?.GetWrapOrDefault();
        if (texture == null)
        {
            ImGui.TextWrapped(L.T("示意图正在加载或暂不可用；仍可按文字说明调整实时视点。"));
            return;
        }
        var s = ImGuiHelpers.GlobalScale;
        var origin = ImGui.GetCursorScreenPos();
        var gap = 10 * s;
        var width = (ImGui.GetContentRegionAvail().X - gap) / 2;
        var imageSide = width - 12 * s;
        var height = imageSide + 60 * s;
        ImGui.InvisibleButton("##calibrationDiagram", new Vector2(width * 2 + gap, height));
        var draw = ImGui.GetWindowDrawList();
        const uint accent = 0xffffdf65;
        for (var panel = 0; panel < 2; panel++)
        {
            var left = origin + new Vector2(panel * (width + gap), 0);
            draw.AddRectFilled(left, left + new Vector2(width, height), 0xff302722, 8 * s);
            draw.AddRect(left, left + new Vector2(width, height), 0xff58483a, 8 * s);
            var title = L.T(panel == 0 ? "正面：两眼之间" : "侧面：鼻梁表面");
            draw.AddText(left + new Vector2(10, 9) * s, 0xfff4ebe0, title);
            var imageMin = left + new Vector2(6, 32) * s;
            draw.AddImage(texture.Handle, imageMin, imageMin + new Vector2(imageSide),
                new Vector2(panel * 0.5f, 0), new Vector2((panel + 1) * 0.5f, 1));
            // Coordinates refer to the nose ROOT in each half of the atlas.
            var target = imageMin + new Vector2(panel == 0 ? 0.523f : 0.797f, 0.433f) * imageSide;
            var eyeY = target.Y;
            draw.AddLine(new Vector2(imageMin.X + 10 * s, eyeY), new Vector2(imageMin.X + imageSide - 10 * s, eyeY), 0x55ffdf65, s);
            draw.AddCircleFilled(target, 10 * s, 0x90201b16, 32);
            draw.AddCircle(target, 7 * s, accent, 32, 1.6f * s);
            draw.AddCircleFilled(target, 2 * s, 0xfff3ffdf);
            foreach (var direction in new[] { Vector2.UnitX, -Vector2.UnitX, Vector2.UnitY, -Vector2.UnitY })
                draw.AddLine(target + direction * 9 * s, target + direction * 13 * s, accent, 1.6f * s);
            var caption = L.T(panel == 0 ? "与双眼同高" : "贴近鼻梁，不是鼻尖");
            draw.AddText(left + new Vector2((width - ImGui.CalcTextSize(caption).X) / 2, height - 23 * s), accent, caption);
        }
    }
}
