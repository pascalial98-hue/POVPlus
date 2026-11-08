using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;
using Hypostasis.Dalamud;

namespace Hypostasis.Debug;

public static class DebugIPC
{
	public const string HypostasisTag = "_HYPOSTASISPLUGINS";

	private static readonly List<HypostasisMemberDebugInfo> debugInfos = new List<HypostasisMemberDebugInfo>();

	private static readonly Dictionary<int, (object, MemberInfo)> memberInfos = new Dictionary<int, (object, MemberInfo)>();

	private static readonly Dictionary<Type, object> injectedObjects = new Dictionary<Type, object>();

	public static bool DebugHypostasis { get; set; }

	public static ICallGateProvider<IDalamudPlugin> GetPluginProvider { get; private set; }

	public static ICallGateProvider<Hypostasis.PluginState> GetPluginStateProvider { get; private set; }

	public static ICallGateProvider<List<HypostasisMemberDebugInfo>> GetDebugInfosProvider { get; private set; }

	public static ICallGateProvider<Dictionary<int, (object, MemberInfo)>> GetMemberInfosProvider { get; private set; }

	[Conditional("DEBUG")]
	public static void Initialize(IDalamudPlugin plugin)
	{
		string pluginName = Hypostasis.PluginName;
		GetPluginProvider = DalamudApi.PluginInterface.GetIpcProvider<IDalamudPlugin>(pluginName + ".Hypostasis.GetPlugin");
		GetPluginProvider.RegisterFunc(() => plugin);
		GetPluginStateProvider = DalamudApi.PluginInterface.GetIpcProvider<Hypostasis.PluginState>(pluginName + ".Hypostasis.GetPluginState");
		GetPluginStateProvider.RegisterFunc(() => Hypostasis.State);
		GetDebugInfosProvider = DalamudApi.PluginInterface.GetIpcProvider<List<HypostasisMemberDebugInfo>>(pluginName + ".Hypostasis.GetDebugInfos");
		GetDebugInfosProvider.RegisterFunc(() => debugInfos);
		GetMemberInfosProvider = DalamudApi.PluginInterface.GetIpcProvider<Dictionary<int, (object, MemberInfo)>>(pluginName + ".Hypostasis.GetMemberInfos");
		GetMemberInfosProvider.RegisterFunc(() => memberInfos);
		DalamudApi.Framework.RunOnTick((Action)EnableDebugging, default(TimeSpan), 0, default(CancellationToken));
	}

	private static void EnableDebugging()
	{
		HashSet<string> orCreateData = DalamudApi.PluginInterface.GetOrCreateData("_HYPOSTASISPLUGINS", () => new HashSet<string>());
		lock (orCreateData)
		{
			orCreateData.Add(Hypostasis.PluginName);
		}
	}

	private static void DisableDebugging()
	{
		if (!DalamudApi.PluginInterface.TryGetData<HashSet<string>>("_HYPOSTASISPLUGINS", out HashSet<string> data))
		{
			return;
		}
		lock (data)
		{
			data.Remove(Hypostasis.PluginName);
		}
	}

	[Conditional("DEBUG")]
	public static void AddInjectedObject(object o)
	{
		injectedObjects[o.GetType()] = o;
	}

	[Conditional("DEBUG")]
	public static void SetupDebugMembers()
	{
		HashSet<Type> debuggableTypes = (from t in Util.Assembly.GetTypesWithAttribute<HypostasisDebuggableAttribute>()
			select t.Item1).ToHashSet();
		IEnumerable<Type> enumerable;
		if (!DebugHypostasis)
		{
			enumerable = Util.AssemblyTypes.Where((Type type) =>
			{
				string text = type.Namespace;
				return text != null && !text.Contains("Hypostasis");
			});
		}
		else
		{
			IEnumerable<Type> assemblyTypes = Util.AssemblyTypes;
			enumerable = assemblyTypes;
		}
		foreach (Type item in enumerable)
		{
			foreach (MemberInfo item2 in item.GetAllMembers().Where((MemberInfo memberInfo) =>
			{
				MemberTypes memberType = memberInfo.MemberType;
				int result;
				if ((memberType == MemberTypes.Field || memberType == MemberTypes.Property) ? true : false)
				{
					if (memberInfo.GetCustomAttribute<HypostasisMemberInjectionAttribute>() == null && memberInfo.GetCustomAttribute<HypostasisDebuggableAttribute>() == null)
					{
						Type objectType = memberInfo.GetObjectType();
						result = (((object)objectType != null && debuggableTypes.Contains((!objectType.IsGenericType) ? objectType : objectType.GetGenericTypeDefinition())) ? 1 : 0);
					}
					else
					{
						result = 1;
					}
				}
				else
				{
					result = 0;
				}
				return (byte)result != 0;
			}))
			{
				AddDebugMember(item2);
			}
		}
	}

	private static void AddDebugMember(MemberInfo memberInfo)
	{
		Type reflectedType = memberInfo.ReflectedType;
		if (!(reflectedType == null))
		{
			injectedObjects.TryGetValue(reflectedType, out object value);
			memberInfos.Add(debugInfos.Count, (value, memberInfo));
			debugInfos.Add(new HypostasisMemberDebugInfo(memberInfo));
		}
	}

	[Conditional("DEBUG")]
	public static void Dispose()
	{
		DisableDebugging();
		DalamudApi.PluginInterface.RelinquishData("_HYPOSTASISPLUGINS");
		GetPluginProvider?.UnregisterFunc();
		GetPluginStateProvider?.UnregisterFunc();
		GetDebugInfosProvider?.UnregisterFunc();
		GetMemberInfosProvider?.UnregisterFunc();
	}
}
