using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Common.Math;

namespace PovPlus;

internal class HidePlayerBody
{
	public static void Initialize()
	{
		Service.Log.Information("===HidePlayerBody Initialized===");
	}

	public unsafe bool ShouldDrawGameObjectDetour(CameraBase* thisPtr, GameObject* gameObject, Vector3* sceneCameraPos, Vector3* lookAtVector)
	{
		return true;
	}

	public void EnableDrawGameObject()
	{
	}

	public void DisableDrawGameObject()
	{
	}

	public static void Dispose()
	{
	}
}
