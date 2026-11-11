using Hypostasis.Game;

namespace PovPlus;

internal class TickManager
{
	public int CameraState_CURRENT = 0;

	public int CameraState_BEFORE = 0;

	public unsafe void TickUpdate()
	{
        if (Plugin.P.CalibrationActive)
        {
            CameraState_CURRENT = CameraState_BEFORE = 0;
            POVCameraFunctionality.DisablePOVCamera();
            return;
        }
		if (Plugin.P.Configuration.Setting_ModEnabled && Service.ObjectTable.LocalPlayer != null &&
            Common.CameraManager != null && Common.CameraManager->worldCamera != null)
		{
			if (Service.ClientState.IsGPosing && Plugin.P.Configuration.Setting_GposeEnabled)
			{
				CameraState_CURRENT = 1;
			}
			else if (Service.ClientState.IsGPosing && !Plugin.P.Configuration.Setting_GposeEnabled)
			{
				CameraState_CURRENT = 0;
			}
			else if (Common.CameraManager->worldCamera->mode == 0)
			{
				CameraState_CURRENT = 1;
			}
			else if (Common.CameraManager->worldCamera->mode == 1)
			{
				if (!Plugin.P.Configuration.Setting_FirstPersonOnly)
				{
					CameraState_CURRENT = 0;
				}
				else
				{
					CameraState_CURRENT = 2;
				}
			}
			else
			{
				CameraState_CURRENT = 0;
			}
		}
		else
		{
			CameraState_CURRENT = 0;
		}
		if (CameraState_CURRENT != CameraState_BEFORE)
		{
			if (CameraState_CURRENT == 0)
			{
				Service.Log.Information("=== POV DISABLED ===");
			}
			else if (CameraState_CURRENT == 1)
			{
				Service.Log.Information("=== POV ENABLED - First Person ===");
			}
			else if (CameraState_CURRENT == 2)
			{
				Service.Log.Information("=== POV ENABLED - Third Person ===");
			}
		}
        // A mode transition is not proof that both hooks are still enabled.
        // Idempotent reconciliation also repairs an incomplete first activation.
        if (CameraState_CURRENT == 0) POVCameraFunctionality.DisablePOVCamera();
        else POVCameraFunctionality.EnablePOVCamera();
		CameraState_BEFORE = CameraState_CURRENT;
	}
}
