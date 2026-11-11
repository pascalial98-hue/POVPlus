using System;
using System.Numerics;
using Hypostasis.Game;
using Hypostasis.Game.Structures;
using Dalamud.Hooking;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using NativeCamera = FFXIVClientStructs.FFXIV.Client.Game.Camera;

namespace PovPlus;

internal static unsafe class FirstPersonLimits
{
    private static Hook<NativeCamera.Delegates.CalculateSceneCameraYaw>? yawHook;
    private static Hook<NativeCamera.Delegates.CalculateSceneCameraPitch>? pitchHook;
    private static Hook<NativeCamera.Delegates.ResetAngles>? resetAnglesHook;
    internal const string RotationSignature = "40 53 48 81 EC ?? ?? ?? ?? 8B 81 50 01 00 00 48 8B D9 44 0F 29 54 24 ?? 89 81 48 01 00 00 8B 89 80 01 00 00";
    private delegate void UpdateRotationDelegate(NativeCamera* camera);
    private static Hook<UpdateRotationDelegate>? rotationHook;
    private static Hook<NativeCamera.Delegates.GetInputDeltaHMultiplier>? horizontalInputHook;
    private static Hook<NativeCamera.Delegates.GetInputDeltaVMultiplier>? verticalInputHook;
    private static bool sharedInputMultiplier;
    private static readonly BoundaryLookAxis yawExtension = new();
    private static readonly BoundaryLookAxis pitchExtension = new();
    private static bool extensionFailed;
    [ThreadStatic] private static bool rotationUpdating;
    [ThreadStatic] private static nint inputCamera;
    [ThreadStatic] private static float manualH, manualV;
    [ThreadStatic] private static bool capturedH, capturedV;
    private static readonly SoftLookLimit softYaw = new();
    private static readonly SoftLookLimit softPitch = new();
    private static nint lastPlayer;

    internal static string LeanStatus => rotationHook == null || extensionFailed
        ? L.T("限位探头输入不可用；转头限制仍有效，请查看 Dalamud 日志。") : "";
    internal static Vector2 LeanPressure => rotationHook == null || extensionFailed ? Vector2.Zero :
        new Vector2(-pitchExtension.Excess, yawExtension.Excess) / BoundaryLookAxis.ExtensionTravel;

    internal static void ResetLean()
    {
        yawExtension.Reset();
        pitchExtension.Reset();
    }

    internal static void Reset()
    {
        softYaw.Reset();
        softPitch.Reset();
        lastPlayer = 0;
        ResetLean();
    }

    internal static void Initialize()
    {
        extensionFailed = false;
        Reset();
        var camera = (NativeCamera*)Common.CameraManager->worldCamera;
        try
        {
            yawHook = Service.GameInteropProvider.HookFromAddress<NativeCamera.Delegates.CalculateSceneCameraYaw>(
                (nint)camera->VirtualTable->CalculateSceneCameraYaw, CalculateYaw);
            pitchHook = Service.GameInteropProvider.HookFromAddress<NativeCamera.Delegates.CalculateSceneCameraPitch>(
                (nint)camera->VirtualTable->CalculateSceneCameraPitch, CalculatePitch);
            yawHook.Enable();
            pitchHook.Enable();
        }
        catch { Dispose(); throw; }
        // Optional: keep ordinary camera limits usable if this client's input
        // updater is unavailable. It is not safe to infer extra input from pose.
        try
        {
            horizontalInputHook = Service.GameInteropProvider.HookFromAddress<NativeCamera.Delegates.GetInputDeltaHMultiplier>(
                (nint)camera->VirtualTable->GetInputDeltaHMultiplier, CaptureHorizontalInput);
            sharedInputMultiplier = (nint)camera->VirtualTable->GetInputDeltaHMultiplier == (nint)camera->VirtualTable->GetInputDeltaVMultiplier;
            if (!sharedInputMultiplier)
                verticalInputHook = Service.GameInteropProvider.HookFromAddress<NativeCamera.Delegates.GetInputDeltaVMultiplier>(
                    (nint)camera->VirtualTable->GetInputDeltaVMultiplier, CaptureVerticalInput);
            rotationHook = Service.GameInteropProvider.HookFromAddress<UpdateRotationDelegate>(
                Service.SigScanner.ScanText(RotationSignature), UpdateRotation);
            horizontalInputHook.Enable();
            verticalInputHook?.Enable();
            rotationHook.Enable();
        }
        catch (Exception ex)
        {
            rotationHook?.Dispose();
            horizontalInputHook?.Dispose();
            verticalInputHook?.Dispose();
            rotationHook = null;
            horizontalInputHook = null;
            verticalInputHook = null;
            Service.Log.Error(ex, "POV+ could not initialize boundary lean input.");
        }
        // The native update also has an explicit angle-reset path before the
        // incremental rotation updater. Observe it without changing its result.
        try
        {
            resetAnglesHook = Service.GameInteropProvider.HookFromAddress<NativeCamera.Delegates.ResetAngles>(
                (nint)camera->VirtualTable->ResetAngles, ResetNativeAngles);
            resetAnglesHook.Enable();
        }
        catch (Exception ex)
        {
            resetAnglesHook?.Dispose();
            resetAnglesHook = null;
            Service.Log.Warning(ex, "POV+ explicit camera-reset observer unavailable; incremental auto-return remains supported.");
        }
    }

    private static bool CanExtend(GameCamera* camera) =>
        IsActive(camera) && Plugin.P.Configuration.FirstPersonLimitLook && Plugin.P.Configuration.FirstPersonLookDownLean &&
        POVCameraFunctionality.FirstPersonViewActive();

    private static bool MouseLookHeld()
    {
        var input = InputManager.StaticAddressPointers.pInstance;
        // The pointer is already resolved by FFXIVClientStructs. If unavailable,
        // preserve the held gesture rather than guessing from OS mouse deltas.
        return Plugin.P.MouseLookActive || input == null || input->HeldMouseButtons != InputManager.MouseButtonHoldState.None;
    }

    private static void ResetNativeAngles(NativeCamera* native)
    {
        var camera = (GameCamera*)native;
        var eligible = IsActive(camera) && !MouseLookHeld();
        var yaw = camera->GameObjectHRotation;
        var pitch = native->DirV;
        resetAnglesHook!.Original(native);
        if (!eligible || !IsActive(camera) || MouseLookHeld()) return;
        var config = Plugin.P.Configuration;
        var side = FirstPersonMath.Degrees(config.FirstPersonYawDegrees, 80, 85);
        var down = FirstPersonMath.Degrees(config.FirstPersonLookDownDegrees, 75, 85);
        var up = FirstPersonMath.Degrees(config.FirstPersonLookUpDegrees, 60, 85);
        var player = Service.ObjectTable.LocalPlayer;
        if (player == null) return;
        var afterYaw = camera->GameObjectHRotation;
        if (yawExtension.ReleaseAfterAutomaticReturn(FirstPersonMath.Wrap(afterYaw - player.Rotation), 0,
            FirstPersonMath.Wrap(afterYaw - yaw), -side, side, false)) softYaw.Sync(afterYaw);
        if (pitchExtension.ReleaseAfterAutomaticReturn(native->DirV, 0, native->DirV - pitch, -down, up, false))
            softPitch.Sync(native->DirV);
    }

    private static float CaptureHorizontalInput(NativeCamera* camera)
    {
        var factor = horizontalInputHook!.Original(camera);
        if (rotationUpdating && inputCamera == (nint)camera && !capturedH)
        {
            // This is the same multiplier call made by UpdateRotation. At this
            // point Adjusted still contains raw manual input; auto-follow has
            // not yet been added. Do not call the native getter an extra time.
            manualH = camera->InputDeltaHAdjusted * factor;
            if (!float.IsFinite(manualH)) manualH = 0;
            capturedH = true;
        }
        else if (rotationUpdating && inputCamera == (nint)camera && sharedInputMultiplier && !capturedV)
        {
            // Some camera implementations share one multiplier function. The
            // native updater calls horizontal first, then vertical.
            manualV = camera->InputDeltaVAdjusted * factor;
            if (!float.IsFinite(manualV)) manualV = 0;
            capturedV = true;
        }
        return factor;
    }

    private static float CaptureVerticalInput(NativeCamera* camera)
    {
        var factor = verticalInputHook!.Original(camera);
        if (rotationUpdating && inputCamera == (nint)camera && !capturedV)
        {
            // The native updater has already applied first-person inversion.
            manualV = camera->InputDeltaVAdjusted * factor;
            if (!float.IsFinite(manualV)) manualV = 0;
            capturedV = true;
        }
        return factor;
    }

    private static void UpdateRotation(NativeCamera* native)
    {
        if (rotationUpdating) { rotationHook!.Original(native); return; }
        var camera = (GameCamera*)native;
        var eligible = !extensionFailed && CanExtend(camera);
        var beforePitch = native->DirV;
        var beforeYaw = camera->GameObjectHRotation;
        var mouseLookHeld = eligible && MouseLookHeld();
        var player = Service.ObjectTable.LocalPlayer;
        if (eligible && player != null && player.Address != lastPlayer) { Reset(); lastPlayer = player.Address; }
        manualH = manualV = 0;
        capturedH = capturedV = false;
        inputCamera = eligible ? (nint)native : 0;
        // Capture the native manual-input stage, before auto-follow/recenter is
        // mixed in. The adjusted values at the end are NOT pure mouse deltas.
        rotationUpdating = true;
        try { rotationHook!.Original(native); }
        finally { rotationUpdating = false; inputCamera = 0; }
        if (!eligible || player == null || !CanExtend(camera) || Service.ObjectTable.LocalPlayer?.Address != player.Address)
        {
            // A render-stage scene-camera swap isn't a release gesture. Mode,
            // actor and feature shutdown still reset through Apply/Tick.
            return;
        }
        try
        {
            var config = Plugin.P.Configuration;
            var side = FirstPersonMath.Degrees(config.FirstPersonYawDegrees, 80, 85);
            var up = FirstPersonMath.Degrees(config.FirstPersonLookUpDegrees, 60, 85);
            var down = FirstPersonMath.Degrees(config.FirstPersonLookDownDegrees, 75, 85);
            var band = config.FirstPersonSoftLimits ? FirstPersonMath.Degrees(config.FirstPersonSoftZoneDegrees, 12, 25) : 0;
            var sensitivity = config.FirstPersonLeanSensitivity;
            yawExtension.SetExtraSideLimit(BoundaryLookAxis.ExtraSideRange(config.FirstPersonLeanExtraSideDegrees));
            pitchExtension.SetExtraDownLimit(BoundaryLookAxis.ExtraDownRange(config.FirstPersonLeanExtraDownDegrees, down));
            // Native auto-follow is added AFTER the captured manual-input stage.
            // Retire a released gesture once that auto-follow has brought the
            // camera back inside ordinary bounds, before the next right drag.
            if (capturedH && capturedV && manualH == 0 && manualV == 0)
            {
                var held = mouseLookHeld || MouseLookHeld();
                var yaw = camera->GameObjectHRotation;
                if (float.IsFinite(native->InputDeltaHAdjusted) &&
                    MathF.Abs(FirstPersonMath.Wrap(yaw - beforeYaw - native->InputDeltaHAdjusted)) <= 0.0001f &&
                    yawExtension.ReleaseAfterAutomaticReturn(FirstPersonMath.Wrap(yaw - player.Rotation), 0,
                        native->InputDeltaHAdjusted, -side, side, held))
                    softYaw.Sync(yaw);
                var expectedPitch = Math.Clamp(beforePitch + native->InputDeltaVAdjusted, native->DirVMin, native->DirVMax);
                if (float.IsFinite(native->InputDeltaVAdjusted) && MathF.Abs(native->DirV - expectedPitch) <= 0.0001f &&
                    pitchExtension.ReleaseAfterAutomaticReturn(native->DirV, 0, native->InputDeltaVAdjusted, -down, up, held))
                    softPitch.Sync(native->DirV);
            }
            if (capturedH && manualH != 0)
            {
                var delta = manualH;
                var facing = camera->GameObjectHRotation;
                // Automatic correction belongs to the base angle, never to the
                // extension input. Subtract only the captured manual component.
                var origin = FirstPersonMath.Wrap(facing - delta - player.Rotation);
                var enabled = LookDownLean.Sanitize(config.FirstPersonLeanSide, LookDownLean.DefaultSide) > 0;
                var limited = yawExtension.Apply(origin, delta, -side, side, band, enabled, enabled, sensitivity);
                var correction = FirstPersonMath.Wrap(player.Rotation + limited - facing);
                camera->currentHRotation = FirstPersonMath.Wrap(camera->currentHRotation + correction);
                if (MathF.Abs(correction) > 0.00001f) native->InputDeltaHAdjusted = 0;
                softYaw.Sync(camera->GameObjectHRotation);
            }
            if (capturedV && manualV != 0 && float.IsFinite(beforePitch) && float.IsFinite(native->InputDeltaVAdjusted))
            {
                var nativeDelta = native->InputDeltaVAdjusted;
                // Skip alternate input modes that rotated tilt instead of pitch.
                var expected = Math.Clamp(beforePitch + nativeDelta, native->DirVMin, native->DirVMax);
                if (MathF.Abs(native->DirV - expected) <= 0.0001f)
                {
                    var enabled = LookDownLean.Sanitize(config.FirstPersonLeanForward, LookDownLean.DefaultForward) > 0;
                    var origin = beforePitch + nativeDelta - manualV;
                    native->DirV = pitchExtension.Apply(origin, manualV, -down, up, band, enabled, false, sensitivity);
                    softPitch.Sync(native->DirV);
                }
            }
            // Rendering receives only the extra downward angle already earned
            // by input after full forward extension, never the full allowance.
            Apply(camera);
        }
        catch (Exception ex)
        {
            extensionFailed = true;
            ResetLean();
            Service.Log.Error(ex, "POV+ boundary leaning stopped after an input error.");
        }
    }

    private static float CalculateYaw(NativeCamera* camera)
    {
        Apply((GameCamera*)camera);
        return yawHook!.Original(camera);
    }

    private static float CalculatePitch(NativeCamera* camera)
    {
        Apply((GameCamera*)camera);
        return pitchHook!.Original(camera);
    }

    internal static void Dispose()
    {
        yawHook?.Dispose();
        pitchHook?.Dispose();
        resetAnglesHook?.Dispose();
        rotationHook?.Dispose();
        horizontalInputHook?.Dispose();
        verticalInputHook?.Dispose();
        yawHook = null;
        pitchHook = null;
        resetAnglesHook = null;
        rotationHook = null;
        horizontalInputHook = null;
        verticalInputHook = null;
        Reset();
    }

    internal static bool IsActive(GameCamera* camera) =>
        !Plugin.P.CalibrationActive && Plugin.P.Configuration.Setting_ModEnabled && camera != null && camera->mode == 0 &&
        Common.CameraManager != null && camera == Common.CameraManager->worldCamera &&
        !Service.ClientState.IsGPosing && Service.ObjectTable.LocalPlayer != null;

    internal static void Apply(GameCamera* camera)
    {
        if (rotationUpdating) return;
        // Other cameras can use these same virtual functions; do not let them
        // reset the world camera's state between its callbacks.
        if (Common.CameraManager == null || camera != Common.CameraManager->worldCamera) return;
        if (!IsActive(camera) || !Plugin.P.Configuration.FirstPersonLimitLook) { Reset(); return; }
        var config = Plugin.P.Configuration;
        var player = Service.ObjectTable.LocalPlayer;
        if (player == null) return;
        if (player.Address != lastPlayer) { Reset(); lastPlayer = player.Address; }
        var side = FirstPersonMath.Degrees(config.FirstPersonYawDegrees, 80f, 85f);
        var up = FirstPersonMath.Degrees(config.FirstPersonLookUpDegrees, 60f, 85f);
        var down = FirstPersonMath.Degrees(config.FirstPersonLookDownDegrees, 75f, 85f);
        var band = config.FirstPersonSoftLimits
            ? FirstPersonMath.Degrees(config.FirstPersonSoftZoneDegrees, 12f, 25f) : 0f;
        // Rendering/animation angles alone never release a gesture. Verified
        // native auto-return is handled at its input stage in UpdateRotation.
        yawExtension.UpdateLimits(-side, side);
        pitchExtension.UpdateLimits(-down, up);
        if (!config.FirstPersonLookDownLean) ResetLean();
        if (LookDownLean.Sanitize(config.FirstPersonLeanSide, LookDownLean.DefaultSide) == 0) yawExtension.Reset();
        if (LookDownLean.Sanitize(config.FirstPersonLeanForward, LookDownLean.DefaultForward) == 0) pitchExtension.Reset();
        yawExtension.SetExtraSideLimit(BoundaryLookAxis.ExtraSideRange(config.FirstPersonLeanExtraSideDegrees));
        pitchExtension.SetExtraDownLimit(BoundaryLookAxis.ExtraDownRange(config.FirstPersonLeanExtraDownDegrees, down));
        // Expand only the currently extended side, and only by its earned
        // rotation. Every rendering callback must preserve that same bound.
        var extraYaw = yawExtension.ExtraAngle;
        var facingYaw = camera->GameObjectHRotation;
        var limited = softYaw.Apply(facingYaw, player.Rotation, -side + Math.Min(0, extraYaw), side + Math.Max(0, extraYaw), band);
        var correction = FirstPersonMath.Wrap(limited - facingYaw);
        camera->currentHRotation = FirstPersonMath.Wrap(camera->currentHRotation + correction);
        if (MathF.Abs(correction) > 0.00001f) camera->hRotationDelta = 0f;
        var lower = -down - pitchExtension.ExtraDown;
        camera->minVRotation = lower;
        camera->maxVRotation = up;
        camera->currentVRotation = softPitch.Apply(camera->currentVRotation, 0, lower, up, band);
    }
}
