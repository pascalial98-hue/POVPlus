using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;

namespace PovPlus.Windows;

public class ConfigWindow : Window, IDisposable
{
	private readonly Configuration configuration;

	public ConfigWindow(Plugin plugin)
		: base("Settings Window")
	{
		Size = new Vector2(232f, 90f);
		SizeCondition = ImGuiCond.Always;
		configuration = plugin.Configuration;
	}

	public void Dispose()
	{
	}

	public override void PreDraw()
	{
		if (configuration.IsConfigWindowMovable)
		{
			Flags &= ~ImGuiWindowFlags.NoMove;
		}
		else
		{
			Flags |= ImGuiWindowFlags.NoMove;
		}
	}

	public override void Draw()
	{
		ImU8String text = new ImU8String(24, 0);
		text.AppendLiteral("All settings are in Open");
		ImGui.TextUnformatted(text);
	}
}
