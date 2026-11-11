using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Graphics.Render;

namespace PovPlus;

public class Service
{
	[PluginService]
	public static IDalamudPluginInterface PluginInterface { get; private set; }

	[PluginService]
	public static ITextureProvider TextureProvider { get; private set; }

	[PluginService]
	public static ICommandManager CommandManager { get; private set; }

	[PluginService]
	public static IClientState ClientState { get; private set; }

	[PluginService]
	public static IDataManager DataManager { get; private set; }

	[PluginService]
	public static IPluginLog Log { get; private set; }

	[PluginService]
	public static IGameInteropProvider GameInteropProvider { get; private set; }

	[PluginService]
	public static IFramework Framework { get; private set; }

	[PluginService]
	public static ISigScanner SigScanner { get; private set; }

	[PluginService]
	public static IPlayerState MyPlayerState { get; private set; }

	[PluginService]
	public static IObjectTable ObjectTable { get; private set; }

	[PluginService]
	internal static IGameGui GameGui { get; private set; }

    [PluginService]
    internal static ICondition Condition { get; private set; }

	public unsafe Skeleton* GameSkeleton { get; init; }
}
