using System;
using Hypostasis.Dalamud;

namespace Hypostasis.Game;

public sealed class VirtualFunction<T> : GameFunction<T> where T : Delegate
{
	public unsafe nint* VTable { get; }

	public int VFuncIndex { get; }

	public unsafe VirtualFunction(nint* vtbl, int i, string sig = null)
	{
		VTable = vtbl;
		VFuncIndex = i;
		Signature = sig;
		SetupAddress(required: true);
	}

	protected unsafe override nint ScanAddress()
	{
		nint result;
		if (VTable == null)
		{
			result = IntPtr.Zero;
		}
		else
		{
			result = (string.IsNullOrEmpty(Signature) ? VTable[VFuncIndex] : DalamudApi.SigScanner.Scan(VTable[VFuncIndex], Signature.Length / 2, Signature));
		}
		return result;
	}
}
