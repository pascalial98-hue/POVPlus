using System;

namespace Hypostasis.Dalamud;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public abstract class HypostasisMemberInjectionAttribute : HypostasisInjectionAttribute
{
	public string DetourName { get; init; } = null;

	public int Offset { get; init; } = 0;

	public bool Required { get; init; } = false;

	public bool EnableHook { get; init; } = true;

	public bool DisposeHook { get; init; } = true;
}
