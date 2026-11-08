using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using PovPlus.Windows.Styles;

namespace PovPlus.Windows.Main_Options;

internal class UI_CameraLocation
{
	public static void CameraLocation(Plugin plugin)
	{
		PovPlusStyle.HeaderTitle("Camera Location", "Camera Offset", 100f);
		ImGui.Spacing();
		ImGui.Spacing();
		ImGui.Spacing();
		if (ImGui.Checkbox("Display Camera/Head Location for Fine Tuning the Offset (Tick Enable in Third Person)", ref Plugin.P.Configuration.SETTINGS_ShowCameraLocation))
		{
			plugin.Configuration.Save();
		}
		ImGui.Text("X Camera Location Offset (Left Right)");
		ImGui.PushFont(UiBuilder.IconFont);
		ImU8String label = new ImU8String(8, 1);
		label.AppendFormatted(FontAwesomeIcon.UndoAlt.ToIconString());
		label.AppendLiteral("##resetX");
		if (ImGui.Button(label))
		{
			Plugin.P.Configuration.SETTINGS_CameraOffsetX = 0f;
		}
		ImGui.PopFont();
		ImGui.SameLine();
		if (ImGui.DragFloat("##X Offset (Left Right)", ref Plugin.P.Configuration.SETTINGS_CameraOffsetX, 0.001f, -1f, 1f, "%.3f"))
		{
			plugin.Configuration.Save();
		}
		ImGui.Spacing();
		ImGui.Text("Y Camera Location Offset (Up Down)");
		ImGui.PushFont(UiBuilder.IconFont);
		ImU8String label2 = new ImU8String(8, 1);
		label2.AppendFormatted(FontAwesomeIcon.UndoAlt.ToIconString());
		label2.AppendLiteral("##resetY");
		if (ImGui.Button(label2))
		{
			Plugin.P.Configuration.SETTINGS_CameraOffsetY = 0f;
		}
		ImGui.PopFont();
		ImGui.SameLine();
		if (ImGui.DragFloat("##Y Offset (Up Down)", ref Plugin.P.Configuration.SETTINGS_CameraOffsetY, 0.001f, -1f, 1f, "%.3f"))
		{
			plugin.Configuration.Save();
		}
		ImGui.Spacing();
		ImGui.Text("Z Camera Location Offset (Forwards / Backwards)");
		ImGui.PushFont(UiBuilder.IconFont);
		ImU8String label3 = new ImU8String(8, 1);
		label3.AppendFormatted(FontAwesomeIcon.UndoAlt.ToIconString());
		label3.AppendLiteral("##resetZ");
		if (ImGui.Button(label3))
		{
			Plugin.P.Configuration.SETTINGS_CameraOffsetZ = 0f;
		}
		ImGui.PopFont();
		ImGui.SameLine();
		if (ImGui.DragFloat("##Z Offset (Forwards / Backwards)", ref Plugin.P.Configuration.SETTINGS_CameraOffsetZ, 0.001f, -1f, 1f, "%.3f"))
		{
			plugin.Configuration.Save();
		}
		ImGui.Spacing();
	}
}
