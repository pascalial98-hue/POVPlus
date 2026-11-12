using System;
using Dalamud.Configuration;

namespace PovPlus;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public PluginLanguage Language = PluginLanguage.Bilingual;
    // Null distinguishes pre-guide configurations from new/pending installations.
    public bool? HasSeenCalibrationGuide;
    public bool FirstPersonHideHead = true;
    public bool FirstPersonFullHeadShadow = true;
    public bool FirstPersonDelayHeadHide = true;
    public float FirstPersonHeadHideDistance = HeadHideTransition.DefaultDistance;
    public float FirstPersonHeadShowDistance = HeadHideTransition.DefaultShowDistance;
    public bool FirstPersonLimitLook = true;
    public bool FirstPersonBodyRelativeLook = false;
    public bool FirstPersonSoftLimits = true;
    public float FirstPersonSoftZoneDegrees = 12f;
    public bool FirstPersonPreserveHair = false;
    public bool FirstPersonPreserveEars = false;
    public float FirstPersonDetailHideRadius = 0.25f;
    public bool FirstPersonReduceSelfVfx = false;
    public float FirstPersonVfxRadius = 1.2f;
    public float FirstPersonVfxOpacity = 0.25f;
    public float FirstPersonYawDegrees = 80f;
    public float FirstPersonLookUpDegrees = 60f;
    public float FirstPersonLookDownDegrees = 75f;
    public bool FirstPersonLookDownLean = true;
    public float FirstPersonLeanForward = LookDownLean.DefaultForward;
    public float FirstPersonLeanSide = LookDownLean.DefaultSide;
    public float FirstPersonLeanSensitivity = BoundaryLookAxis.DefaultSensitivity;
    public float FirstPersonLeanExtraDownDegrees = 10f;
    public float FirstPersonLeanExtraSideDegrees = 10f;
    public bool FirstPersonMouseLook = false;
    public bool FirstPersonKeepNearObjects = false;
    public int FirstPersonCursorKey = MouseLookKeys.DefaultKey;
    public int FirstPersonCursorModifiers = 0;
    public bool FirstPersonCursorToggle = false;

	public float MinFovValue = 1f;

	public bool SETTINGS_ShowCameraLocation;

	public float SETTINGS_CameraOffsetX = 0f;

	public float SETTINGS_CameraOffsetY = 0f;

	public float SETTINGS_CameraOffsetZ = 0f;

	public float SETTINGS_CameraDeadZone = 0f;

	public float Setting_FOV = 0.75f;

	public bool Setting_ModEnabled = true;

	public bool Setting_FirstPersonOnly = false;

	public bool Setting_ShowBody = true;

	public bool Setting_RotateWithplayer = false;

	public bool Setting_GposeEnabled = false;

	public bool Settings_PitchEnabled = false;

	public bool Settings_RollEnabled = false;

	public bool RotationBindBoolYaw_X = false;

	public float MountOffsetYawRadians = (float)Math.PI / 2f;

	public float MountOffsetPitchRadians = 0f;

	public float MountOffsetRollRadians = 0f;

	public float Settings_YawSmoothing = 60f;

	public float Settings_PitchSmoothing = 60f;

	public float Settings_RollSmoothing = 60f;

    public float HeadYawWeight = 100f;
    public float HeadPitchWeight = 100f;
    public float HeadRollWeight = 100f;

	public int CameraRotationYawBind = 0;

	public int CameraRotationPitchBind = 1;

	public int CameraRotationRollBind = 2;

	public int BindBone_Rotation = 1;

	public int BindBone_Location = 1;

	public bool CameraRotationYawInvert = false;

	public bool CameraRotationPitchInvert = false;

	public bool CameraRotationRollInvert = false;

	public float BoneYawOffsetRadians = (float)Math.PI / 2f;

	public bool SwapMountPitchAndRoll = false;

	public float MountOffsetYawDegrees = 90f;

	public float MountOffsetPitchDegrees = 0f;

	public float MountOffsetRollDegrees = 0f;

	public bool EnableRotationSmoothing = true;

	public float RotationSmoothing = 0.92f;

	public float DeadzoneDegrees = 1.8f;

	public bool PitchEnabled;

	public bool YawEnabled;

	public bool RollEnabled;

	public float CameraTiltOffset;

    // Small values preserve nearby clothing but may break particle occlusion.
    public float CameraNearPlane = NearPlanePatch.Default;

	public int Version { get; set; } = 0;

	public bool IsConfigWindowMovable { get; set; } = true;

	public bool SomePropertyToBeSavedAndWithADefault { get; set; } = true;

	public bool TestVar1 { get; set; } = true;

	public float DeadzoneRadians => DeadzoneDegrees * ((float)Math.PI / 180f);

	public void Save()
	{
		Service.PluginInterface.SavePluginConfig(this);
	}
}
