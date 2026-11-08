using Dalamud.Bindings.ImGui;
using PovPlus.Windows.Styles;

namespace PovPlus.Windows.UI_Tabs;

internal static class FirstPersonOptions
{
    internal static void Draw(Plugin plugin)
    {
        PovPlusStyle.HeaderTitle("First Person Comfort", "FirstPersonComfort", 100f);
        var config = plugin.Configuration;
        var changed = ImGui.Checkbox("Hide head, hair and head attachments (keep neck)", ref config.FirstPersonHideHead);
        if (config.FirstPersonHideHead)
        {
            changed |= ImGui.Checkbox("Keep head visible until the camera is close", ref config.FirstPersonDelayHeadHide);
            if (config.FirstPersonDelayHeadHide)
            {
                changed |= ImGui.SliderFloat("Hide when remaining distance is below", ref config.FirstPersonHeadHideDistance,
                    0.05f, 0.75f, "%.2f");
                ImGui.TextWrapped("Smaller = later; larger = earlier. Default: 0.25.");
            }
            changed |= ImGui.SliderFloat("Show head after camera moves away", ref config.FirstPersonHeadShowDistance,
                0.2f, 1f, "%.2f");
            ImGui.TextWrapped("On return to third person, keep the head hidden until the camera clears it. Larger = later reappearance. Default: 0.35.");
        }
        changed |= ImGui.Checkbox("Limit first-person look angles", ref config.FirstPersonLimitLook);
        if (config.FirstPersonLimitLook)
        {
            changed |= ImGui.SliderFloat("Left / right limit", ref config.FirstPersonYawDegrees, 5f, 85f, "%.0f deg");
            changed |= ImGui.SliderFloat("Look up limit", ref config.FirstPersonLookUpDegrees, 5f, 85f, "%.0f deg");
            changed |= ImGui.SliderFloat("Look down limit", ref config.FirstPersonLookDownDegrees, 5f, 85f, "%.0f deg");
        }
        ImGui.TextWrapped("Only affects your own character. Head hiding continues briefly while leaving first person. Normal third-person appearance returns once the camera is clear; GPose is unaffected. Look limits only apply in first person.");
        if (plugin.HeadHidingStatus.Length != 0) ImGui.TextWrapped(plugin.HeadHidingStatus);
        if (changed) config.Save();
    }
}
