using System.Numerics;
using Dalamud.Bindings.ImGui;

namespace PovPlus.Windows.Styles;

public static class PovPlusStyle
{
	public static void LoadStyles()
	{
		ImGuiStylePtr style = ImGui.GetStyle();
		style.WindowRounding = 12f;
		style.ChildRounding = 10f;
		style.FrameRounding = 8f;
		style.WindowBorderSize = 1f;
		style.ChildBorderSize = 1f;
		style.FrameBorderSize = 1f;
	}

	public static void Background()
	{
		ImGui.PushStyleVar(ImGuiStyleVar.ChildRounding, 12f);
		ImGui.BeginChild("Header", new Vector2(0f, 90f), border: true);
		ImGui.SetCursorPos(new Vector2(12f, 12f));
		ImGui.SameLine();
		ImGui.BeginGroup();
		ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 8f);
		ImGui.Text("POV Plus");
		ImGui.TextDisabled("Advanced first-person camera controls");
		ImGui.EndGroup();
		ImGui.EndChild();
		ImGui.PopStyleVar();
	}

	public static void HeaderTitle(string Title, string SubText, float BoxSize)
	{
		ImGui.Separator();
		ImGui.Text(Title);
		ImGui.TextDisabled(SubText);
	}
}
