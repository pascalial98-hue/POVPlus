using System;
using Dalamud.Hooking;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using Hypostasis.Game.Structures;
using NativeCharacter = FFXIVClientStructs.FFXIV.Client.Game.Character.Character;
using NativeCamera = FFXIVClientStructs.FFXIV.Client.Game.Camera;

namespace PovPlus;

// Protect the camera-to-character turn decision itself, including native
// gamepad input. Do not modify either stick, character mode or movement state.
internal sealed unsafe class StationaryCameraLook : IDisposable
{
    internal const string RotationModeSignature = "E8 ?? ?? ?? ?? 48 8B CB 85 C0 0F 84 ?? ?? ?? ?? 83 E8 01";

    // The caller tests EAX and subtracts 32-bit modes. The inherited helper's
    // byte return type is unsuitable for a detour returning a native mode.
    private delegate int RotationModeDelegate(GameCamera* camera, Framework* framework);
    private readonly Hook<RotationModeDelegate> hook;
    internal bool Failed { get; private set; }

    internal StationaryCameraLook()
    {
        hook = Service.GameInteropProvider.HookFromAddress<RotationModeDelegate>(
            Service.SigScanner.ScanText(RotationModeSignature), GetRotationMode);
        try { hook.Enable(); }
        catch { hook.Dispose(); throw; }
    }

    private int GetRotationMode(GameCamera* camera, Framework* framework)
    {
        var original = hook.Original(camera, framework);
        if (Failed || original != 1) return original;
        try
        {
            // Limit the override to the active first-person world camera.
            // Third person, GPose, calibration and special cameras
            // keep their original decision. No per-frame input snapshot lives
            // beyond this call, so disable/unload needs no game-state restore.
            if (!FirstPersonLimits.IsActive(camera) || !POVCameraFunctionality.FirstPersonViewActive() ||
                ((NativeCamera*)camera)->IsEventCameraAutoControl || !MouseLookKeys.Foreground) return original;
            var player = Service.ObjectTable.LocalPlayer;
            var protect = player != null && StationaryLook.Protects(((NativeCharacter*)player.Address)->Mode);
            return StationaryLook.CameraRotationMode(original, protect);
        }
        catch (Exception ex)
        {
            Failed = true;
            Service.Log.Error(ex, "POV+ stationary camera-turn protection stopped; native camera decisions retained.");
            return original;
        }
    }

    public void Dispose() => hook.Dispose();
}
