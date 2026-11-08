using System;
using System.Collections.Generic;
using System.Reflection;
using Dalamud.Game.Command;

namespace Hypostasis.Dalamud;

public sealed class PluginCommandManager : IDisposable
{
	private readonly HashSet<string> pluginCommands = new HashSet<string>();

	public PluginCommandManager(object o)
	{
		MethodInfo[] allMethods = o.GetType().GetAllMethods();
		foreach (MethodInfo method in allMethods)
		{
			AddPluginCommandMethod(o, method);
		}
	}

	private void AddPluginCommandMethod(object o, MethodInfo method)
	{
		PluginCommandAttribute customAttribute = method.GetCustomAttribute<PluginCommandAttribute>();
		if (customAttribute == null)
		{
			return;
		}
		IReadOnlyCommandInfo.HandlerDelegate handler = (IReadOnlyCommandInfo.HandlerDelegate)Delegate.CreateDelegate(typeof(IReadOnlyCommandInfo.HandlerDelegate), o, method);
		CommandInfo info = new CommandInfo(handler)
		{
			HelpMessage = customAttribute.HelpMessage,
			ShowInHelp = customAttribute.ShowInHelp
		};
		string[] commands = customAttribute.Commands;
		foreach (string text in commands)
		{
			if (DalamudApi.CommandManager.AddHandler(text, info))
			{
				pluginCommands.Add(text);
			}
		}
	}

	public void Dispose()
	{
		foreach (string pluginCommand in pluginCommands)
		{
			DalamudApi.CommandManager.RemoveHandler(pluginCommand);
		}
	}
}
