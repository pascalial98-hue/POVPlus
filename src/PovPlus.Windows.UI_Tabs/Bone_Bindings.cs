using Dalamud.Bindings.ImGui;
using PovPlus.Windows.Styles;
using PovPlus.Windows.UI_Elements;

namespace PovPlus.Windows.UI_Tabs;

internal class Bone_Bindings
{
	public static string[] options = new string[3] { "Yaw", "Pitch", "Roll" };

	public static int selected = 0;

	public bool CameraYawInvert = false;

	public bool CameraPitchInvert = false;

	public bool CameraRollInvert = false;

	public static void Bone_Bindings_Main(Plugin plugin)
	{
		PovPlusStyle.HeaderTitle("Camera Bindings", "Rotation Bindings", 100f);
		ImGui.Spacing();
		ImGui.Spacing();
		ImGui.Spacing();
		string rotationBoneValueXName = GlobalVars.RotationBoneValueXName;
		bool flag = false;
		ImGui.Text("Yaw (Left Right)");
		if (ImGui.Checkbox("Yaw Enabled", ref Plugin.P.Configuration.YawEnabled))
		{
			plugin.Configuration.Save();
		}
		if (PovPlus.Windows.UI_Elements.UI_Elements.ResetSliderFloat("Camera Yaw Smoothing", ref Plugin.P.Configuration.Settings_YawSmoothing, 0f, 100f, 60f, "%.0f%%"))
		{
			plugin.Configuration.Save();
		}
		ImGui.Spacing();
		ImGui.Text("Pitch (Up/Down)");
		if (ImGui.Checkbox("Pitch Enabled", ref Plugin.P.Configuration.PitchEnabled))
		{
			plugin.Configuration.Save();
		}
		if (PovPlus.Windows.UI_Elements.UI_Elements.ResetSliderFloat("Camera Pitch Smoothing", ref Plugin.P.Configuration.Settings_PitchSmoothing, 0f, 100f, 60f, "%.0f%%"))
		{
			plugin.Configuration.Save();
		}
		ImGui.Spacing();
		ImGui.Text("Roll (Camera Tilt) - Bit glichy, if you find animations that MASSIVELY glitch out let me know");
		if (ImGui.Checkbox("Roll Enabled", ref Plugin.P.Configuration.RollEnabled))
		{
			plugin.Configuration.Save();
		}
		if (PovPlus.Windows.UI_Elements.UI_Elements.ResetSliderFloat("Camera Roll Smoothing ", ref Plugin.P.Configuration.Settings_RollSmoothing, 0f, 100f, 60f, "%.0f%%"))
		{
			plugin.Configuration.Save();
		}
		ImGui.Spacing();
	}
}
