using System;
using Dalamud.Hooking;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Common.Math;
using Hypostasis.Game;

namespace PovPlus;

internal sealed unsafe class NearObjectVisibility : IDisposable
{
    // CameraBase's near-object visibility routine compares this squared distance
    // with a whole-object hiding radius. Verified on the installed CN executable.
    internal const string DistanceSignature = "48 8B C4 48 89 58 ?? 57 48 81 EC ?? ?? ?? ?? 0F 29 70 ?? 48 8B FA F3 41 0F 10 70 04";
    private delegate float DistanceDelegate(CameraBase* camera, GameObject* target, Vector3* eye, byte useOrigin);
    private readonly Hook<DistanceDelegate> hook;

    internal NearObjectVisibility()
    {
        hook = Service.GameInteropProvider.HookFromAddress<DistanceDelegate>(Service.SigScanner.ScanText(DistanceSignature), Distance);
        try { hook.Enable(); }
        catch { hook.Dispose(); throw; }
    }

    internal bool Draw(CameraBase.Delegates.ShouldDrawGameObject original, CameraBase* camera, GameObject* target,
        Vector3* position, Vector3* lookAt)
    {
        var enabled = Plugin.P.Configuration.FirstPersonKeepNearObjects && POVCameraFunctionality.FirstPersonInputActive() &&
            Common.CameraManager != null && camera == (CameraBase*)Common.CameraManager->worldCamera;
        using var scope = new NearObjectDistanceScope(enabled, (nint)camera, (nint)position);
        // Keep native early exclusions and unrelated culling intact. Only its
        // inner proximity-distance test sees the temporary classification.
        return original(camera, target, position, lookAt);
    }

    private float Distance(CameraBase* camera, GameObject* target, Vector3* position, byte useOrigin) =>
        NearObjectDistanceScope.Filter(hook.Original(camera, target, position, useOrigin), (nint)camera, (nint)position);

    public void Dispose() => hook.Dispose();
}
