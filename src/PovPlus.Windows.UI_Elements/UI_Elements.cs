using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;

namespace PovPlus.Windows.UI_Elements;

internal class UI_Elements
{
	public static bool ResetSliderFloat(string id, ref float val, float min, float max, float reset, string format)
	{
		bool flag = false;
		ImGui.PushFont(UiBuilder.IconFont);
		ImU8String label = new ImU8String(2, 2);
		label.AppendFormatted(FontAwesomeIcon.UndoAlt.ToIconString());
		label.AppendLiteral("##");
		label.AppendFormatted(id);
		if (ImGui.Button(label))
		{
			val = reset;
			flag = true;
		}
		ImGui.PopFont();
		ImGui.SameLine();
		ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - 160f * ImGuiHelpers.GlobalScale);
		return flag | ImGui.SliderFloat(id, ref val, min, max, format);
	}

	public static bool ResetSliderInt(string id, ref int val, int min, int max, int reset, string format)
	{
		bool flag = false;
		ImGui.PushFont(UiBuilder.IconFont);
		ImU8String label = new ImU8String(2, 2);
		label.AppendFormatted(FontAwesomeIcon.UndoAlt.ToIconString());
		label.AppendLiteral("##");
		label.AppendFormatted(id);
		if (ImGui.Button(label))
		{
			val = reset;
			flag = true;
		}
		ImGui.PopFont();
		ImGui.SameLine();
		ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - 160f * ImGuiHelpers.GlobalScale);
		return flag | ImGui.SliderInt(id, ref val, min, max, format);
	}

	public static void ControlledSliderInt(string id, ref int val, int min, int max, int reset)
	{
		bool flag = true;
		if (ImGui.Button("-") && val != min)
		{
			val--;
		}
		ImGui.SameLine();
		if (ImGui.Button("+") && val != max)
		{
			val++;
		}
		ImGui.SameLine();
		ImGui.SliderInt(id, ref val, min, max);
	}
}
