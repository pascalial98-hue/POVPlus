using System;
using RenderCamera = FFXIVClientStructs.FFXIV.Client.Graphics.Render.Camera;

namespace PovPlus;

// Only touch the live camera supplied by the caller; never dereference a saved address.
internal sealed unsafe class NearPlanePatch
{
    internal const float Minimum = 0.001f;
    internal const float Maximum = 1f;
    internal const float Default = 0.015f;
    private nint cameraAddress;
    private float original;
    private float applied;

    internal static float Sanitize(float value) =>
        float.IsFinite(value) ? Math.Clamp(value, Minimum, Maximum) : Default;

    internal void Apply(RenderCamera* camera, float value)
    {
        if (camera == null) return;
        var current = camera->NearPlane;
        if (!float.IsFinite(current) || current <= 0f) return;
        if (cameraAddress != (nint)camera || current != applied)
        {
            cameraAddress = (nint)camera;
            original = current;
        }
        applied = Sanitize(value);
        camera->NearPlane = applied;
    }

    internal void Restore(RenderCamera* liveCamera)
    {
        if (liveCamera != null && cameraAddress == (nint)liveCamera && liveCamera->NearPlane == applied)
            liveCamera->NearPlane = original;
        cameraAddress = 0;
    }
}
