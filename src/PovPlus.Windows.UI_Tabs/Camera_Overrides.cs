using Dalamud.Bindings.ImGui;
using PovPlus.Windows.Styles;
using PovPlus.Windows.UI_Elements;

namespace PovPlus.Windows.UI_Tabs;

internal class Camera_Overrides
{
	public static void Camera_Overrides_Main(Plugin plugin)
	{
		PovPlusStyle.HeaderTitle("Camera Overrides", "Camera Location + Settings", 100f);
		ImGui.Spacing();
		ImGui.Spacing();
		ImGui.Spacing();
		ImGui.Text("Field of View (FOV) ");
		ImGui.SameLine();
		ImGui.TextDisabled("");
		if (PovPlus.Windows.UI_Elements.UI_Elements.ResetSliderFloat("##FOV", ref Plugin.P.Configuration.Setting_FOV, 0f, 5f, 0.75f, "%.2f"))
		{
			plugin.Configuration.Save();
		}
		ImGui.Spacing();
		ImGui.Spacing();
		ImGui.Text("Additional Tilt Offset ");
		ImGui.SameLine();
		ImGui.TextDisabled("");
		if (PovPlus.Windows.UI_Elements.UI_Elements.ResetSliderFloat("##Tilt Offset", ref Plugin.P.Configuration.CameraTiltOffset, -3f, 3f, 0f, "%1f"))
		{
			plugin.Configuration.Save();
		}
		ImGui.Spacing();
        ImGui.Text("Near Plane (first person)");
        if (PovPlus.Windows.UI_Elements.UI_Elements.ResetSliderFloat("##Near Plane",
            ref plugin.Configuration.CameraNearPlane, NearPlanePatch.Minimum, NearPlanePatch.Maximum,
            NearPlanePatch.Default, "%.3f"))
        {
            plugin.Configuration.CameraNearPlane = NearPlanePatch.Sanitize(plugin.Configuration.CameraNearPlane);
            plugin.Configuration.Save();
        }
        if (ImGui.Button("Set to 0.001##NearPlane"))
        {
            plugin.Configuration.CameraNearPlane = NearPlanePatch.Minimum;
            plugin.Configuration.Save();
        }
        ImGui.TextWrapped("Smaller values keep nearby clothing visible, but may make particles show through terrain. Reset to 0.100 if needed.");
	}
}
