using System;
using System.Numerics;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Hooking;
using Dalamud.Game.ClientState.Objects.Types;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.Graphics.Render;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using FFXIVClientStructs.FFXIV.Common.Math;
using FFXIVClientStructs.Havok.Animation.Rig;
using FFXIVClientStructs.Havok.Common.Base.Math.QsTransform;
using FFXIVClientStructs.Havok.Common.Base.Math.Quaternion;
using Hypostasis.Game;
using Hypostasis.Game.Structures;

namespace PovPlus;

public static class POVCameraFunctionality
{
	private static float CachedDefaultLookAtHeightOffset;

	private unsafe static GameObject* PrevCameraTarget;

	private static System.Numerics.Vector3 PrevCameraTargetPosition;

	private static float InterpolatedHeight;

	private static float OffsetCalculatedX;

	private static float OffsetCalculatedZ;

	private static bool POVEnabled;
    private static Hook<GameCamera.GameCameraVTable.GetCameraPositionDelegate>? positionHook;
    private static nint boundCamera, boundVTable;
    private static bool cameraSettingsCaptured;
    internal static bool IsEnabled => POVEnabled;

	private static float PreviousBoneYaw;

	private static float PreviousBonePitch;

	private static float PrevRoll;

	private static float PitchRollAddition;

	private static float RollRollAddition;

	private static float PreviousHeadYaw;

	private static float TARGET_CameraYaw;

	private static float PreviousHeadRotationYaw;

	private static float PreviousHeadRotationPitch;

	private static float PreviousHeadRotationRoll;

	private static float TargetCameraYaw;

	private static float AccumulatedYaw;

	private static float YawChaser;

	private static float AccumulatedRoll;

	private static float RollChaser;

	private static System.Numerics.Vector3 CurrentCameraLocation;

	public static System.Numerics.Quaternion PreviousQuarternion;

	public static bool HasPreviousQuarternion;

	public static System.Numerics.Vector3 relForward;

	public static System.Numerics.Vector3 relUp;

	public static System.Numerics.Vector3 relRight;

	public static System.Numerics.Quaternion DeltaQuarternion;

	private static float PreviousZRot;

	private static bool HasPreviousZRot;

	public static bool PreviousHemisphere;

	private static bool HasPreviousHemisphere;

	private static float AccumulatedPitch;

	private static float PitchChaser;

	private static float PreviousPitch;

	private static bool HasPreviousPitch;

	private static bool HasPreviousHeadRoll;
    private static nint headSkeleton;
    private static int headIndex = -1;
    private static readonly NearPlanePatch nearPlanePatch = new();
    private static System.Numerics.Vector3 appliedLean;
    private static nint leanActor, leanModel, leanSkeleton, leanCamera;
    private static readonly HeadRotationWeight yawWeight = new();
    private static readonly HeadRotationWeight pitchWeight = new();

	private unsafe static void GetCameraPositionDetour(GameCamera* HypostasisCamera, GameObject* target, System.Numerics.Vector3* position, Bool swapPerson)
	{
        FirstPersonLimits.Apply(HypostasisCamera);
		IPlayerCharacter localPlayer = Service.ObjectTable.LocalPlayer;
		if (localPlayer == null || target == null)
		{
            positionHook!.Original(HypostasisCamera, target, position, swapPerson);
			return;
		}
		CharacterBase* characterBase = localPlayer.GetCharacterBase();
		if (characterBase == null || characterBase->Skeleton == null)
		{
            positionHook!.Original(HypostasisCamera, target, position, swapPerson);
			return;
		}
		Skeleton* skeleton = characterBase->Skeleton;
		PartialSkeleton* partialSkeletons = skeleton->PartialSkeletons;
		if (skeleton->PartialSkeletonCount == 0 || partialSkeletons == null || partialSkeletons->GetHavokPose(0) == null)
		{
            positionHook!.Original(HypostasisCamera, target, position, swapPerson);
			return;
		}
		hkaPose* havokPose = partialSkeletons->GetHavokPose(0);
        if (!TryFindHead(havokPose, out var boneIndex))
        {
            positionHook!.Original(HypostasisCamera, target, position, swapPerson);
            return;
        }
		float num = 0.1f;
		float num2 = 0f;
		float num3 = 0f;
		float num4 = 0f;
        hkQsTransformf headTransform = Plugin.P.CameraHeadTransform(havokPose, boneIndex);
        hkQsTransformf* ptr = &headTransform;
		FFXIVClientStructs.FFXIV.Common.Math.Quaternion rotation = skeleton->Transform.Rotation;
		System.Numerics.Quaternion quaternion = (PreviousQuarternion = System.Numerics.Quaternion.Normalize(new System.Numerics.Quaternion(ptr->Rotation.X, ptr->Rotation.Y, ptr->Rotation.Z, ptr->Rotation.W)));
		HasPreviousQuarternion = true;
		num2 = MathF.Atan2(relForward.Z, relForward.X);
        UpdateNearPlane();
		HypostasisCamera->minFoV = Plugin.P.Configuration.Setting_FOV;
		System.Numerics.Quaternion quaternion2 = System.Numerics.Quaternion.Normalize(System.Numerics.Quaternion.Normalize(rotation) * quaternion);
		System.Numerics.Vector3 position2 = new System.Numerics.Vector3(ptr->Translation.X, ptr->Translation.Y, ptr->Translation.Z);
		FFXIVClientStructs.FFXIV.Common.Math.Vector3 position3 = skeleton->Transform.Position;
		float num5 = ((float*)characterBase)[169];
		System.Numerics.Vector3 scales = new System.Numerics.Vector3(characterBase->Scale.X * num5, characterBase->Scale.Y * num5, characterBase->Scale.Z * num5);
		System.Numerics.Matrix4x4 matrix = System.Numerics.Matrix4x4.CreateScale(scales) * System.Numerics.Matrix4x4.CreateFromQuaternion(System.Numerics.Quaternion.Normalize(rotation)) * System.Numerics.Matrix4x4.CreateTranslation(position3);
		System.Numerics.Vector3 vector = System.Numerics.Vector3.Transform(System.Numerics.Vector3.UnitX, quaternion);
		float num6 = 0.5f;
		System.Numerics.Vector3 normal = System.Numerics.Vector3.Transform(System.Numerics.Vector3.UnitX, quaternion);
		System.Numerics.Vector3 normal2 = System.Numerics.Vector3.Transform(System.Numerics.Vector3.UnitY, quaternion);
		System.Numerics.Vector3 normal3 = System.Numerics.Vector3.Transform(System.Numerics.Vector3.UnitZ, quaternion);
		System.Numerics.Vector3 vector2 = System.Numerics.Vector3.TransformNormal(normal, matrix);
		System.Numerics.Vector3 vector3 = System.Numerics.Vector3.TransformNormal(normal2, matrix);
		System.Numerics.Vector3 vector4 = System.Numerics.Vector3.TransformNormal(normal3, matrix);
		System.Numerics.Vector3 vector5 = new System.Numerics.Vector3(vector5.X = Plugin.P.Configuration.SETTINGS_CameraOffsetX, vector5.Y = Plugin.P.Configuration.SETTINGS_CameraOffsetY, vector5.Z = Plugin.P.Configuration.SETTINGS_CameraOffsetZ);
		System.Numerics.Vector3 vector6 = vector4 * vector5.X + vector3 * vector5.Y + vector2 * vector5.Z;
		GlobalVars.BoneLocation = FirstPersonMath.HeadCameraPosition(position2, quaternion, matrix, vector5);
		float num7 = MathF.Max(MathF.Pow(1f - Plugin.P.Configuration.Settings_PitchSmoothing / 100f, 3f), 0.01f);
		float num8 = MathF.Max(MathF.Pow(1f - Plugin.P.Configuration.Settings_YawSmoothing / 100f, 3f), 0.01f);
		float num9 = MathF.Max(MathF.Pow(1f - Plugin.P.Configuration.Settings_RollSmoothing / 100f, 3f), 0.01f);
		System.Numerics.Vector3 vector7 = System.Numerics.Vector3.Transform(System.Numerics.Vector3.UnitZ, quaternion);
		float num10 = MathF.Atan2(vector7.X, vector7.Z);
		{
			float num11 = num10 - PreviousBoneYaw;
			if (num11 > (float)Math.PI)
			{
				num11 -= (float)Math.PI * 2f;
			}
			if (num11 < -(float)Math.PI)
			{
				num11 += (float)Math.PI * 2f;
			}
			PreviousBoneYaw = num10;
			AccumulatedYaw -= num11;
			float num12 = AccumulatedYaw - YawChaser;
			YawChaser += num12 * num8;
			HypostasisCamera->currentHRotation -= yawWeight.Update(YawChaser,
                Plugin.P.Configuration.HeadYawWeight, Plugin.P.Configuration.YawEnabled);
		}
		System.Numerics.Quaternion quaternion3 = quaternion;
		float num13 = MathF.Atan2(2f * (quaternion3.X * quaternion3.Y + quaternion3.Z * quaternion3.W), 1f - 2f * (quaternion3.X * quaternion3.X + quaternion3.Z * quaternion3.Z));
		float num14 = (0f - num13 + (float)Math.PI) * (180f / (float)Math.PI);
		if (num14 > 180f)
		{
			num14 -= 360f;
		}
		if (num14 < -180f)
		{
			num14 += 360f;
		}
		if (HasPreviousPitch)
		{
			float num15 = num13 - PreviousPitch;
			if (num15 > (float)Math.PI)
			{
				num15 -= (float)Math.PI * 2f;
			}
			if (num15 < -(float)Math.PI)
			{
				num15 += (float)Math.PI * 2f;
			}
			AccumulatedPitch -= num15;
			float num16 = AccumulatedPitch - PitchChaser;
			PitchChaser += num16 * num7;
		}
        HypostasisCamera->currentVRotation += pitchWeight.Update(PitchChaser,
            Plugin.P.Configuration.HeadPitchWeight, Plugin.P.Configuration.PitchEnabled);
		PreviousPitch = num13;
		HasPreviousPitch = true;
		HypostasisCamera->minVRotation = -90f;
		HypostasisCamera->maxVRotation = 90f;
        // Clamp again after animation-driven yaw/pitch, before computing camera tilt.
        FirstPersonLimits.Apply(HypostasisCamera);
		GlobalVars.BoneLocation += UpdateLookDownLean(HypostasisCamera, characterBase, localPlayer);
		GlobalVars.DEBUG_YawCameraEnd = GlobalVars.BoneLocation + vector2 * num6;
		GlobalVars.DEBUG_PitchCameraEnd = GlobalVars.BoneLocation + vector4 * num6;
		GlobalVars.DEBUG_RollCameraEnd = GlobalVars.BoneLocation + vector3 * num6;
		System.Numerics.Vector3 vector8 = System.Numerics.Vector3.Transform(System.Numerics.Vector3.UnitZ, quaternion);
		float x = MathF.Atan2(vector8.X, vector8.Y);
		x = MathF.Abs(x);
		float num17 = 0f;
		{
			if (!HasPreviousHeadRoll)
			{
				PreviousHeadRotationRoll = (float)Math.PI / 2f;
				HasPreviousHeadRoll = true;
			}
			float num18;
			for (num18 = x - PreviousHeadRotationRoll; num18 > (float)Math.PI; num18 -= (float)Math.PI * 2f)
			{
			}
			for (; num18 < -(float)Math.PI; num18 += (float)Math.PI * 2f)
			{
			}
			PreviousHeadRotationRoll = x;
			AccumulatedRoll += num18;
			float num19 = AccumulatedRoll - RollChaser;
			RollChaser += num19 * num9;
			num17 = HeadRotationWeight.Scale(RollChaser,
                Plugin.P.Configuration.HeadRollWeight, Plugin.P.Configuration.RollEnabled);
		}
		bool flag = (int)MathF.Floor((HypostasisCamera->currentVRotation + (float)Math.PI / 2f) / (float)Math.PI) % 2 == 0;
		float num20 = (flag ? 0f : ((float)Math.PI));
		if (flag)
		{
			HypostasisCamera->tilt = num20 + Plugin.P.Configuration.CameraTiltOffset - num17;
		}
		else
		{
			HypostasisCamera->tilt = num20 + Plugin.P.Configuration.CameraTiltOffset - num17;
		}
		PreviousHemisphere = flag;
		HasPreviousHemisphere = true;
		System.Numerics.Vector3 vector9 = GlobalVars.BoneLocation + ((System.Numerics.Vector3)target->Position - PrevCameraTargetPosition);
		System.Numerics.Vector3 vector10 = *position;
		System.Numerics.Vector3 vector11 = vector9 - CurrentCameraLocation;
		*position = vector9;
		PrevCameraTargetPosition = target->Position;
		CurrentCameraLocation += vector11 * 0.5f;
	}

    public unsafe static void Initialize()
    {
        var manager = Common.CameraManager;
        var camera = manager == null ? null : manager->worldCamera;
        if (camera == null || camera->vtbl == null || camera->vtbl[16] == 0)
            throw new InvalidOperationException("World camera is not ready.");
        // Bind the live world-camera entry directly. The recovered wrapper caches
        // the first vtable it sees, which may be observed before world login.
        positionHook = Service.GameInteropProvider.HookFromAddress<GameCamera.GameCameraVTable.GetCameraPositionDelegate>(
            camera->vtbl[16], GetCameraPositionDetour);
        boundCamera = (nint)camera;
        boundVTable = (nint)camera->vtbl;
        GlobalVars.PreviousMaxVRotation = camera->maxVRotation;
        GlobalVars.PreviousMinVRotation = camera->minVRotation;
        GlobalVars.PreviousCurrentFoV = camera->currentFoV;
        GlobalVars.PreviousTilt = camera->tilt;
        GlobalVars.PreviousMinFOV = camera->minFoV;
        cameraSettingsCaptured = true;
        POVEnabled = false;
        headSkeleton = 0;
        headIndex = -1;
    }

    internal unsafe static bool RuntimeCameraMatches()
    {
        var manager = Common.CameraManager;
        var camera = manager == null ? null : manager->worldCamera;
        return camera != null && (nint)camera == boundCamera && (nint)camera->vtbl == boundVTable;
    }

    public static void Dispose()
    {
        DisablePOVCamera();
        positionHook?.Dispose();
        positionHook = null;
        cameraSettingsCaptured = false;
        boundCamera = boundVTable = 0;
        headSkeleton = 0;
        headIndex = -1;
    }

    public static void EnablePOVCamera()
    {
        var bodyHook = Plugin.P.ExecuteEmoteHook2;
        if (positionHook == null || bodyHook == null) return;
        // Reconcile actual hook state every tick. Commit POVEnabled only after
        // BOTH camera positioning and local-body visibility are enabled.
        if (!positionHook.IsEnabled) positionHook.Enable();
        if (!bodyHook.IsEnabled) bodyHook.Enable();
        if (POVEnabled) return;
        ResetLookDownLean();
        PreviousBoneYaw = 0f;
        AccumulatedYaw = YawChaser = AccumulatedPitch = PitchChaser = AccumulatedRoll = RollChaser = 0f;
        yawWeight.Reset();
        pitchWeight.Reset();
        HasPreviousPitch = HasPreviousHeadRoll = false;
        var player = Service.ObjectTable.LocalPlayer;
        if (player != null) PrevCameraTargetPosition = player.Position;
        POVEnabled = true;
        Service.Log.Information("===Enabled POV===");
    }

    public static void TurnOffPOVCamera() => DisablePOVCamera();

    public unsafe static void DisablePOVCamera()
    {
        var wasEnabled = POVEnabled;
        POVEnabled = false;
        RestoreNearPlane();
        ResetLookDownLean();
        FirstPersonLimits.ResetLean();
        if (Plugin.P.ExecuteEmoteHook2?.IsEnabled == true) Plugin.P.ExecuteEmoteHook2.Disable();
        if (positionHook?.IsEnabled == true) positionHook.Disable();
        // Never restore title-screen settings or dereference a saved camera
        // pointer after the game has replaced that camera.
        if (wasEnabled && cameraSettingsCaptured && RuntimeCameraMatches())
        {
            var camera = Common.CameraManager->worldCamera;
            camera->maxVRotation = GlobalVars.PreviousMaxVRotation;
            camera->minVRotation = GlobalVars.PreviousMinVRotation;
            camera->tilt = GlobalVars.PreviousTilt;
            camera->minFoV = GlobalVars.PreviousMinFOV;
            camera->currentFoV = GlobalVars.PreviousCurrentFoV;
        }
        GlobalVars.HideOwnBody = false;
        if (wasEnabled) Service.Log.Information("===Disabled POV===");
    }

    private static unsafe FFXIVClientStructs.FFXIV.Client.Graphics.Render.Camera* WorldRenderCamera()
    {
        if (Common.CameraManager == null || Common.CameraManager->worldCamera == null) return null;
        var camera = (FFXIVClientStructs.FFXIV.Client.Game.CameraBase*)Common.CameraManager->worldCamera;
        return camera->SceneCamera.RenderCamera;
    }

    internal static unsafe bool FirstPersonInputActive() => POVEnabled && Common.CameraManager != null &&
        FirstPersonLimits.IsActive(Common.CameraManager->worldCamera);

    internal static unsafe bool FirstPersonViewActive()
    {
        if (!POVEnabled || Common.CameraManager == null ||
            !FirstPersonLimits.IsActive(Common.CameraManager->worldCamera)) return false;
        var sceneManager = FFXIVClientStructs.FFXIV.Client.Graphics.Scene.CameraManager.Instance();
        var camera = (FFXIVClientStructs.FFXIV.Client.Game.CameraBase*)Common.CameraManager->worldCamera;
        return sceneManager != null && sceneManager->CurrentCamera == &camera->SceneCamera;
    }

    private static unsafe void UpdateNearPlane()
    {
        var camera = WorldRenderCamera();
        if (FirstPersonViewActive()) nearPlanePatch.Apply(camera, Plugin.P.Configuration.CameraNearPlane);
        else nearPlanePatch.Restore(camera);
    }

    // Restore even when third-person POV stays enabled and the position hook does not run.
    internal static unsafe void RestoreNearPlaneIfInactive()
    {
        if (!FirstPersonViewActive()) RestoreNearPlane();
    }

    private static unsafe void RestoreNearPlane() => nearPlanePatch.Restore(WorldRenderCamera());

    private static void ResetLookDownLean()
    {
        appliedLean = System.Numerics.Vector3.Zero;
        leanActor = leanModel = leanSkeleton = leanCamera = 0;
    }

    internal static unsafe void ResetLookDownLeanIfInactive()
    {
        if (!Plugin.P.Configuration.FirstPersonLookDownLean || !Plugin.P.Configuration.FirstPersonLimitLook || !POVEnabled ||
            Common.CameraManager == null || !FirstPersonLimits.IsActive(Common.CameraManager->worldCamera))
        {
            ResetLookDownLean();
            FirstPersonLimits.ResetLean();
        }
    }

    private static unsafe System.Numerics.Vector3 UpdateLookDownLean(GameCamera* camera, CharacterBase* model, IPlayerCharacter player)
    {
        // Another camera can call this shared virtual function between world
        // camera updates; it must not reset the world's smoothing state.
        if (Common.CameraManager == null || camera != Common.CameraManager->worldCamera) return System.Numerics.Vector3.Zero;
        var config = Plugin.P.Configuration;
        if (!config.FirstPersonLookDownLean || !config.FirstPersonLimitLook || !FirstPersonViewActive())
        {
            // A temporary scene-camera switch must not erase a held mouse drag.
            ResetLookDownLeanIfInactive();
            return System.Numerics.Vector3.Zero;
        }
        if (leanActor != player.Address || leanModel != (nint)model || leanSkeleton != (nint)model->Skeleton || leanCamera != (nint)camera)
        {
            ResetLookDownLean();
            FirstPersonLimits.ResetLean();
            leanActor = player.Address;
            leanModel = (nint)model;
            leanSkeleton = (nint)model->Skeleton;
            leanCamera = (nint)camera;
        }
        var amounts = LookDownLean.Target(FirstPersonLimits.LeanPressure, config.FirstPersonLeanForward, config.FirstPersonLeanSide);
        appliedLean = LookDownLean.ToWorld(amounts, player.Rotation);
        return appliedLean;
    }

    // Hide timing reads the exact offset applied to the camera, without reading
    // or consuming mouse input for a second time.
    internal static unsafe System.Numerics.Vector3 AppliedLookDownLean(GameCamera* camera, nint actor, nint model, nint skeleton) =>
        Plugin.P.Configuration.FirstPersonLookDownLean && Plugin.P.Configuration.FirstPersonLimitLook && FirstPersonViewActive() &&
        leanCamera == (nint)camera && leanActor == actor && leanModel == model && leanSkeleton == skeleton
            ? appliedLean : System.Numerics.Vector3.Zero;

	public unsafe static T* GetDrawObject<T>(this IGameObject go) where T : unmanaged
	{
		return (T*)((GameObject*)go.Address)->DrawObject;
	}

    internal static unsafe bool TryFindHead(hkaPose* pose, out int index)
    {
        index = -1;
        if (pose == null || pose->Skeleton == null || pose->Skeleton->Bones.Data == null ||
            pose->Skeleton->Bones.Length is <= 0 or > 2048) return false;
        var bones = pose->Skeleton->Bones;
        if (headSkeleton != (nint)pose->Skeleton || headIndex < 0 || headIndex >= bones.Length ||
            bones[headIndex].Name.String != "j_kao")
        {
            headSkeleton = (nint)pose->Skeleton;
            headIndex = -1;
            for (var i = 0; i < bones.Length; i++)
                if (bones[i].Name.String == "j_kao") { headIndex = i; break; }
        }
        index = headIndex;
        return index >= 0;
    }

	public unsafe static CharacterBase* GetCharacterBase(this ICharacter go)
	{
		return go.GetDrawObject<CharacterBase>();
	}

	public static hkQuaternionf MultiplyQuaternions(hkQuaternionf a, hkQuaternionf b)
	{
		return new hkQuaternionf
		{
			W = a.W * b.W - a.X * b.X - a.Y * b.Y - a.Z * b.Z,
			X = a.W * b.X + a.X * b.W + a.Y * b.Z - a.Z * b.Y,
			Y = a.W * b.Y - a.X * b.Z + a.Y * b.W + a.Z * b.X,
			Z = a.W * b.Z + a.X * b.Y - a.Y * b.X + a.Z * b.W
		};
	}

	public static void ToAxisAngle(this System.Numerics.Quaternion q, out System.Numerics.Vector3 axis, out float angle)
	{
		q = System.Numerics.Quaternion.Normalize(q);
		angle = 2f * MathF.Acos(Math.Clamp(q.W, -1f, 1f));
		float num = MathF.Sqrt(MathF.Max(0f, 1f - q.W * q.W));
		if (num < 0.0001f)
		{
			axis = System.Numerics.Vector3.UnitX;
		}
		else
		{
			axis = new System.Numerics.Vector3(q.X / num, q.Y / num, q.Z / num);
		}
	}
}
