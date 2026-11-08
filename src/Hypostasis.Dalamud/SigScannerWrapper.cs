#define DEBUG
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Dalamud.Game;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using Hypostasis.Debug;

namespace Hypostasis.Dalamud;

public class SigScannerWrapper : IDisposable
{
	private struct RUNTIME_FUNCTION
	{
		public uint StartOffset;

		public uint EndOffset;

		public uint UnwindData;
	}

	private readonly Dictionary<string, nint> sigCache = new Dictionary<string, nint>();

	private readonly Dictionary<string, nint> staticSigCache = new Dictionary<string, nint>();

	private readonly List<IDisposable> disposableHooks = new List<IDisposable>();

	public ISigScanner DalamudSigScanner { get; init; }

	public ProcessModule Module { get; init; }

	public nint BaseAddress { get; init; }

	public nint BaseTextAddress { get; init; }

	public nint BaseRDataAddress { get; init; }

	public nint BaseDataAddress { get; init; }

	public int PDataSectionSize { get; init; }

	public long PDataSectionOffset { get; init; }

	public nint PDataSectionBase { get; init; }

	public nint BasePDataAddress { get; init; }

	public int TotalPDataFunctions { get; init; }

	public unsafe SigScannerWrapper(ISigScanner s)
	{
		DalamudSigScanner = s;
		Module = s.Module;
		BaseAddress = Module.BaseAddress;
		BaseTextAddress = (nint)(BaseAddress + s.TextSectionOffset);
		BaseRDataAddress = (nint)(BaseAddress + s.RDataSectionOffset);
		BaseDataAddress = (nint)(BaseAddress + s.DataSectionOffset);
		nint num = Scan(BaseAddress, 4096, string.Join(' ', from b in ".pdata"u8.ToArray()
			select b.ToString("X2")));
		PDataSectionSize = *(int*)(num + 8);
		PDataSectionOffset = *(int*)(num + 12);
		PDataSectionBase = (nint)(s.SearchBase + PDataSectionOffset);
		BasePDataAddress = (nint)(BaseAddress + PDataSectionOffset);
		TotalPDataFunctions = PDataSectionSize / sizeof(RUNTIME_FUNCTION);
	}

	public nint Scan(nint address, int size, string signature)
	{
		bool flag = address >= BaseAddress && address < BaseRDataAddress;
		if (flag)
		{
			address = DalamudSigScanner.SearchBase + (address - BaseAddress);
		}
		nint num = SigScanner.Scan(address, size, signature);
		if (flag && num >= DalamudSigScanner.SearchBase)
		{
			num = BaseAddress + (num - DalamudSigScanner.SearchBase);
		}
		return num;
	}

	public nint Scan(nint address, nint endAddress, string signature)
	{
		return Scan(address, (int)(endAddress - address), signature);
	}

	public bool TryScan(nint address, int size, string signature, out nint result)
	{
		bool flag = address >= BaseAddress && address < BaseRDataAddress;
		if (flag)
		{
			address = DalamudSigScanner.SearchBase + (address - BaseAddress);
		}
		bool result2 = SigScanner.TryScan(address, size, signature, out result);
		if (flag && result >= DalamudSigScanner.SearchBase)
		{
			result = BaseAddress + (result - DalamudSigScanner.SearchBase);
		}
		return result2;
	}

	public bool TryScan(nint address, nint endAddress, string signature, out nint result)
	{
		return TryScan(address, (int)(endAddress - address), signature, out result);
	}

	public nint ScanText(string signature)
	{
		if (sigCache.TryGetValue(signature, out var value))
		{
			return value;
		}
		value = DalamudSigScanner.ScanText(signature);
		AddSignatureInfo(signature, value, 0, stc: false);
		return value;
	}

	public bool TryScanText(string signature, out nint result)
	{
		if (sigCache.TryGetValue(signature, out result))
		{
			return true;
		}
		bool result2 = DalamudSigScanner.TryScanText(signature, out result);
		AddSignatureInfo(signature, result, 0, stc: false);
		return result2;
	}

	public nint ScanData(string signature)
	{
		if (sigCache.TryGetValue(signature, out var value))
		{
			return value;
		}
		value = DalamudSigScanner.ScanData(signature);
		AddSignatureInfo(signature, value, 0, stc: false);
		return value;
	}

	public bool TryScanData(string signature, out nint result)
	{
		if (sigCache.TryGetValue(signature, out result))
		{
			return true;
		}
		bool result2 = DalamudSigScanner.TryScanData(signature, out result);
		AddSignatureInfo(signature, result, 0, stc: false);
		return result2;
	}

	public nint ScanModule(string signature)
	{
		if (sigCache.TryGetValue(signature, out var value))
		{
			return value;
		}
		value = DalamudSigScanner.ScanModule(signature);
		AddSignatureInfo(signature, value, 0, stc: false);
		return value;
	}

	public bool TryScanModule(string signature, out nint result)
	{
		if (sigCache.TryGetValue(signature, out result))
		{
			return true;
		}
		bool result2 = DalamudSigScanner.TryScanModule(signature, out result);
		AddSignatureInfo(signature, result, 0, stc: false);
		return result2;
	}

	public nint ScanStaticAddress(string signature, int offset = 0)
	{
		if (offset == 0 && staticSigCache.TryGetValue(signature, out var value))
		{
			return value;
		}
		value = DalamudSigScanner.GetStaticAddressFromSig(signature, offset);
		AddSignatureInfo(signature, value, offset, stc: true);
		return value;
	}

	public bool TryScanStaticAddress(string signature, out nint result, int offset = 0)
	{
		if (offset == 0 && staticSigCache.TryGetValue(signature, out result))
		{
			return true;
		}
		bool result2 = DalamudSigScanner.TryGetStaticAddressFromSig(signature, out result, offset);
		AddSignatureInfo(signature, result, offset, stc: true);
		return result2;
	}

	public nint[] ScanAllText(string signature)
	{
		return DalamudSigScanner.ScanAllText(signature);
	}

	private Hook<T> HookAddress<T>(nint address, T detour, bool startEnabled = true, bool autoDispose = true, IGameInteropProvider.HookBackend backend = IGameInteropProvider.HookBackend.Automatic) where T : Delegate
	{
		Hook<T> hook = DalamudApi.GameInteropProvider.HookFromAddress(address, detour, backend);
		AddHook(hook, startEnabled, autoDispose);
		return hook;
	}

	private Hook<T> HookSignature<T>(string signature, T detour, bool scanModule = false, bool startEnabled = true, bool autoDispose = true, IGameInteropProvider.HookBackend backend = IGameInteropProvider.HookBackend.Automatic) where T : Delegate
	{
		nint num = ((!scanModule) ? DalamudSigScanner.ScanText(signature) : DalamudSigScanner.ScanModule(signature));
		Hook<T> hook = DalamudApi.GameInteropProvider.HookFromAddress(num, detour, backend);
		AddSignatureInfo(signature, num, 0, stc: false);
		AddHook(hook, startEnabled, autoDispose);
		return hook;
	}

	private void AddSignatureInfo(string signature, nint ptr, int offset, bool stc)
	{
		if (!stc)
		{
			sigCache[signature] = ptr;
		}
		else
		{
			staticSigCache[signature] = ptr;
		}
	}

	public void InjectSignatures()
	{
		foreach (var item2 in Util.Assembly.GetTypesWithAttribute<HypostasisInjectionAttribute>())
		{
			Type item = item2.Item1;
			Inject(item);
		}
	}

	public void Inject(Type type, object o = null)
	{
		if (o != null)
		{
			DebugIPC.AddInjectedObject(o);
		}
		foreach (MemberInfo item in type.GetAllMembers().Where((MemberInfo memberInfo) =>
		{
			MemberTypes memberType = memberInfo.MemberType;
			return (memberType == MemberTypes.Field || memberType == MemberTypes.Property) ? true : false;
		}))
		{
			InjectMember(o, item);
		}
	}

	public void Inject(object o)
	{
		Inject(o.GetType(), o);
	}

	public void InjectMember(object o, MemberInfo memberInfo)
	{
		HypostasisMemberInjectionAttribute customAttribute = memberInfo.GetCustomAttribute<HypostasisMemberInjectionAttribute>();
		if (customAttribute == null)
		{
			return;
		}
		HypostasisMemberInjectionAttribute hypostasisMemberInjectionAttribute = customAttribute;
		HypostasisMemberInjectionAttribute hypostasisMemberInjectionAttribute2 = hypostasisMemberInjectionAttribute;
		if (!(hypostasisMemberInjectionAttribute2 is HypostasisSignatureInjectionAttribute sigAttribute))
		{
			if (hypostasisMemberInjectionAttribute2 is HypostasisClientStructsInjectionAttribute csAttribute)
			{
				InjectClientStructs(o, memberInfo, csAttribute);
			}
		}
		else
		{
			InjectSignature(o, memberInfo, sigAttribute);
		}
	}

	private void InjectSignature(object o, MemberInfo memberInfo, HypostasisSignatureInjectionAttribute sigAttribute)
	{
		Util.AssignableInfo assignableInfo = new Util.AssignableInfo(o, memberInfo);
		string signature = sigAttribute.Signature;
		bool flag = sigAttribute.Static;
		if ((!flag) ? (!DalamudSigScanner.TryScanText(signature, out var result)) : (!DalamudSigScanner.TryGetStaticAddressFromSig(signature, out result)))
		{
			LogInjectError(memberInfo, $"Failed to find signature: \"{signature}\" (Static: {flag})", sigAttribute.Required);
		}
		else
		{
			InjectAddress(assignableInfo, result, sigAttribute);
		}
	}

	private void InjectClientStructs(object o, MemberInfo memberInfo, HypostasisClientStructsInjectionAttribute csAttribute)
	{
		string name = (memberInfo.Name.EndsWith("Hook") ? memberInfo.Name.Replace("Hook", string.Empty) : csAttribute.MemberName);
		MemberInfo memberInfo2 = csAttribute.ClientStructsType.GetMember(name)[0];
		Util.AssignableInfo assignableInfo = new Util.AssignableInfo(o, memberInfo);
		if (1 == 0)
		{
		}
		object obj;
		if (!(memberInfo2 is FieldInfo fieldInfo))
		{
			if (!(memberInfo2 is PropertyInfo propertyInfo))
			{
				if (!(memberInfo2 is MethodInfo methodInfo))
				{
					throw new ApplicationException("Member type is unsupported");
				}
				obj = methodInfo.Invoke(null, Array.Empty<object>());
			}
			else
			{
				obj = propertyInfo.GetValue(null);
			}
		}
		else
		{
			obj = fieldInfo.GetValue(null);
		}
		if (1 == 0)
		{
		}
		object o2 = obj;
		InjectAddress(assignableInfo, Util.ConvertObjectToIntPtr(o2), csAttribute);
	}

	private void InjectAddress(Util.AssignableInfo assignableInfo, nint address, HypostasisMemberInjectionAttribute attribute)
	{
		address += attribute.Offset;
		Type type = assignableInfo.Type;
		if (type == typeof(nint) || type.IsPointer || type.IsFunctionPointer)
		{
			assignableInfo.SetValue(address);
		}
		else if (type.IsAssignableTo(typeof(Delegate)))
		{
			assignableInfo.SetValue(Marshal.GetDelegateForFunctionPointer(address, type));
		}
		else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Hook<>))
		{
			InjectHook(assignableInfo, address, attribute);
		}
		else if (type.IsPrimitive)
		{
			assignableInfo.SetValue(Marshal.PtrToStructure(address, type));
		}
		else
		{
			LogInjectError(assignableInfo.MemberInfo, "Failed to determine how to inject member", attribute.Required);
		}
	}

	private void InjectHook(Util.AssignableInfo assignableInfo, nint address, HypostasisMemberInjectionAttribute attribute)
	{
		Type reflectedType = assignableInfo.MemberInfo.ReflectedType;
		object o = assignableInfo.Object;
		Type type = assignableInfo.Type;
		Type type2 = type.GenericTypeArguments[0];
		if (!IsValidHookAddress(address))
		{
			LogInjectError(assignableInfo.MemberInfo, $"Attempted to place hook on invalid location {address:X}", attribute.Required);
			return;
		}
		Delegate obj = GetMethodDelegate(reflectedType, type2, o, assignableInfo.Name.Replace("Hook", "Detour"));
		if ((object)obj == null)
		{
			string detourName = attribute.DetourName;
			if (detourName != null)
			{
				obj = GetMethodDelegate(reflectedType, type2, o, detourName);
				if ((object)obj == null)
				{
					LogInjectError(assignableInfo.MemberInfo, "Detour not found or was incompatible with delegate \"" + detourName + "\" " + type2.Name, attribute.Required);
					return;
				}
			}
			else
			{
				Delegate[] methodDelegates = GetMethodDelegates(reflectedType, type2, o);
				if (methodDelegates.Length != 1)
				{
					LogInjectError(assignableInfo.MemberInfo, $"Found {methodDelegates.Length} matching detours: specify a detour name", attribute.Required);
					return;
				}
				obj = methodDelegates[0];
			}
		}
		object obj2 = type.GetMethod("FromAddress", BindingFlags.Static | BindingFlags.NonPublic)?.Invoke(null, new object[4] { address, obj, false, null });
		assignableInfo.SetValue(obj2);
		if (attribute.EnableHook)
		{
			type.GetMethod("Enable")?.Invoke(obj2, null);
		}
		if (attribute.DisposeHook)
		{
			disposableHooks.Add(obj2 as IDisposable);
		}
	}

	private static Delegate GetMethodDelegate(Type ownerType, Type delegateType, object o, string methodName)
	{
		MethodInfo method = ownerType.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
		return CreateDelegate(delegateType, o, method);
	}

	private static Delegate[] GetMethodDelegates(IReflect ownerType, Type delegateType, object o)
	{
		return (from methodInfo in ownerType.GetAllMethods()
			select CreateDelegate(delegateType, o, methodInfo) into del
			where (object)del != null
			select del).ToArray();
	}

	private static Delegate CreateDelegate(Type delegateType, object o, MethodInfo delegateMethod)
	{
		if (delegateType == null)
		{
			return null;
		}
		return delegateMethod.IsStatic ? Delegate.CreateDelegate(delegateType, delegateMethod, throwOnBindFailure: false) : Delegate.CreateDelegate(delegateType, o, delegateMethod, throwOnBindFailure: false);
	}

	public void AddHook<T>(Hook<T> hook, bool enable = true, bool dispose = true) where T : Delegate
	{
		if (enable)
		{
			hook.Enable();
		}
		if (dispose)
		{
			disposableHooks.Add(hook);
		}
	}

	public void InjectMember(Type type, object o, string member)
	{
		InjectMember(o, type.GetMember(member, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)[0]);
	}

	private static void LogInjectError(MemberInfo memberInfo, string message, bool required)
	{
		string message2 = $"Error injecting {memberInfo.ReflectedType?.FullName}.{memberInfo.Name}:\n{message}";
		if (required)
		{
			throw new ApplicationException(message2);
		}
		DalamudApi.LogWarning(message2);
	}

	public unsafe bool IsValidHookAddress(nint address)
	{
		return address == BaseTextAddress || (address > BaseTextAddress && address < BaseRDataAddress && *(byte*)address != 204 && *(byte*)(address - 1) == 204);
	}

	private unsafe RUNTIME_FUNCTION* FindRuntimeFunction(nint target)
	{
		RUNTIME_FUNCTION* basePDataAddress = (RUNTIME_FUNCTION*)BasePDataAddress;
		if (basePDataAddress == null)
		{
			return null;
		}
		nint num = target - BaseAddress;
		if (num < 4096)
		{
			return null;
		}
		int num2 = 0;
		int num3 = TotalPDataFunctions / 2;
		while (true)
		{
			RUNTIME_FUNCTION* ptr = basePDataAddress + num2 + num3;
			uint startOffset = ptr->StartOffset;
			if (num == startOffset)
			{
				return ptr;
			}
			if (num3 <= 0)
			{
				break;
			}
			if (num > startOffset)
			{
				num2 += num3;
				if (num3 >= 2)
				{
					num3 /= 2;
				}
			}
			else
			{
				num3 /= 2;
			}
		}
		return null;
	}

	private unsafe (nint start, int length, nint next) GetRuntimeFunctionInfo(RUNTIME_FUNCTION* ptr)
	{
		if (ptr == null)
		{
			return default;
		}
		uint startOffset = ptr->StartOffset;
		nint item = (nint)(BaseAddress + startOffset);
		while (*(byte*)(BaseAddress + ptr->EndOffset) != 204)
		{
			ptr++;
			if (ptr->EndOffset == 0)
			{
				return default;
			}
		}
		return (start: item, length: (int)(ptr->EndOffset - startOffset), next: (nint)(ptr + 1));
	}

	private unsafe (nint start, int length, nint next) GetRuntimeFunctionInfo(nint ptr)
	{
		return GetRuntimeFunctionInfo((RUNTIME_FUNCTION*)ptr);
	}

	private unsafe nint FindRuntimeFunctionAddress(nint startingFunction) => (nint)FindRuntimeFunction(startingFunction);

	public IEnumerable<(nint start, int length)> GetFunctions(nint startingFunction)
	{
		nint a = FindRuntimeFunctionAddress(startingFunction);
		var (address, length, next) = GetRuntimeFunctionInfo(a);
		while (address != 0)
		{
			yield return (start: address, length: length);
			(address, length, next) = GetRuntimeFunctionInfo(next);
		}
	}

	public void Dispose()
	{
		foreach (IDisposable disposableHook in disposableHooks)
		{
			disposableHook?.Dispose();
		}
		GC.SuppressFinalize(this);
	}
}
