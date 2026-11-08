using System;
using System.Runtime.InteropServices;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using Hypostasis.Dalamud;
using Lumina.Excel.Sheets;

namespace Hypostasis.Game.Structures;

[StructLayout(LayoutKind.Explicit)]
[GameStructure("45 33 C0 B8 ?? ?? ?? ?? 48 89 41 38")]
public struct ActionManager : IHypostasisStructure
{
	public delegate uint GetSpellIDForActionDelegate(uint actionType, uint actionID);

	public unsafe delegate Bool CanUseActionOnGameObjectDelegate(uint actionID, GameObject* o);

	public delegate uint GetAdjustedRecastTimeDelegate(uint actionType, uint actionID, Bool useStats);

	public unsafe delegate Bool CanQueueActionDelegate(ActionManager* actionManager, uint actionType, uint actionID);

	[FieldOffset(0)]
	public FFXIVClientStructs.FFXIV.Client.Game.ActionManager CS;

	[FieldOffset(8)]
	public float animationLock;

	[FieldOffset(40)]
	public bool isCasting;

	[FieldOffset(40)]
	public uint castActionType;

	[FieldOffset(44)]
	public uint castActionID;

	[FieldOffset(48)]
	public float elapsedCastTime;

	[FieldOffset(52)]
	public float castTime;

	[FieldOffset(56)]
	public ulong castTargetObjectID;

	[FieldOffset(96)]
	public float remainingComboTime;

	[FieldOffset(104)]
	public bool isQueued;

	[FieldOffset(108)]
	public uint queuedActionType;

	[FieldOffset(112)]
	public uint queuedActionID;

	[FieldOffset(120)]
	public ulong queuedTargetObjectID;

	[FieldOffset(160)]
	public ulong queuedGroundTargetObjectID;

	[FieldOffset(192)]
	public byte activateGroundTarget;

	[FieldOffset(288)]
	public ushort currentSequence;

	[FieldOffset(1532)]
	public bool isGCDRecastActive;

	[FieldOffset(1536)]
	public uint currentGCDAction;

	[FieldOffset(1540)]
	public float elapsedGCDRecastTime;

	[FieldOffset(1544)]
	public float gcdRecastTime;

	public static readonly GameFunction<GetSpellIDForActionDelegate> getSpellIDForAction = new GameFunction<GetSpellIDForActionDelegate>("E8 ?? ?? ?? ?? 33 D2 89 44 24 20");

	public static readonly GameFunction<CanUseActionOnGameObjectDelegate> canUseActionOnGameObject = new GameFunction<CanUseActionOnGameObjectDelegate>("48 89 5C 24 08 57 48 83 EC 20 48 8B DA 8B F9 E8 ?? ?? ?? ?? 4C 8B C3");

	public static readonly GameFunction<GetAdjustedRecastTimeDelegate> getAdjustedRecastTime = new GameFunction<GetAdjustedRecastTimeDelegate>("E8 ?? ?? ?? ?? 85 C0 7E 1C");

	public static readonly GameFunction<CanQueueActionDelegate> canQueueAction = new GameFunction<CanQueueActionDelegate>("E8 ?? ?? ?? ?? 84 C0 0F 84 ?? ?? ?? ?? F6 05 ?? ?? ?? ?? ?? 74 2A");

	public static uint GCDRecast => Math.Min(GetAdjustedRecastTime(1u, 9u, useStats: true), GetAdjustedRecastTime(1u, 14u, useStats: true));

	public static uint GetSpellIDForAction(uint actionType, uint actionID)
	{
		return getSpellIDForAction.Invoke(actionType, actionID);
	}

	public unsafe static bool CanUseActionOnGameObject(uint actionID, GameObject* o)
	{
		return (bool)canUseActionOnGameObject.Invoke(actionID, o) || (DalamudApi.DataManager.GetExcelSheet<Lumina.Excel.Sheets.Action>().GetRowOrDefault(actionID)?.TargetArea ?? false);
	}

	public static uint GetAdjustedRecastTime(uint actionType, uint actionID, bool useStats)
	{
		return getAdjustedRecastTime.Invoke(actionType, actionID, useStats);
	}

	public unsafe bool CanQueueAction(uint actionType, uint actionID)
	{
		fixed (ActionManager* actionManager = &this)
		{
			return canQueueAction.Invoke(actionManager, actionType, actionID);
		}
	}

	public bool Validate()
	{
		return true;
	}
}
