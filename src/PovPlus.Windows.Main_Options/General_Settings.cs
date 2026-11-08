using Dalamud.Bindings.ImGui;
using PovPlus.Windows.Styles;

namespace PovPlus.Windows.Main_Options;

internal class General_Settings
{
	public static void General_Settings_Main(Plugin plugin)
	{
		PovPlusStyle.HeaderTitle("General Settings", "Overrides", 100f);
		ImGui.Spacing();
		ImGui.Spacing();
		ImGui.Spacing();
		if (ImGui.Checkbox("Mod Enabled?        /Pov Toggle", ref Plugin.P.Configuration.Setting_ModEnabled))
		{
			plugin.Configuration.Save();
		}
		if (ImGui.Checkbox("Enable in Third Person", ref Plugin.P.Configuration.Setting_FirstPersonOnly))
		{
			plugin.Configuration.Save();
		}
		if (ImGui.Checkbox("Enable in Gpose", ref Plugin.P.Configuration.Setting_GposeEnabled))
		{
			plugin.Configuration.Save();
		}
	}
}
