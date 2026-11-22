using System;
using Camera = FFXIVClientStructs.FFXIV.Client.Game.Camera;

namespace PovPlus;

// At minimum orbit distance the native wheel controls FOV. For a shoulder
// input update, give that branch a single lens value. Restore the bounds
// before rendering and before other plugins next apply their own preset.
internal readonly unsafe struct ShoulderWheelScope
{
    private readonly bool active;
    private readonly float min, max, lens, minDistance, inputMinDistance;

    internal ShoulderWheelScope(Camera* camera)
    {
        if (camera == null || !float.IsFinite(camera->FoV) || !float.IsFinite(camera->MinFoV) ||
            !float.IsFinite(camera->MaxFoV) || camera->MinFoV > camera->MaxFoV ||
            !float.IsFinite(camera->MinDistance) || camera->MinDistance <= 0) return;
        min = camera->MinFoV;
        max = camera->MaxFoV;
        lens = Math.Clamp(camera->FoV, min, max);
        minDistance = camera->MinDistance;
        // Native scroll-to-first-person checks the orbit floor before zooming.
        // Keep that threshold just below the actual floor during this call,
        // including when another plugin replaces the perspective permission.
        inputMinDistance = MathF.BitDecrement(minDistance);
        camera->MinDistance = inputMinDistance;
        camera->MinFoV = camera->MaxFoV = lens;
        camera->FoV = lens;
        active = true;
    }

    internal void Restore(Camera* camera)
    {
        if (!active) return;
        if (camera->MinFoV == lens) camera->MinFoV = min;
        if (camera->MaxFoV == lens) camera->MaxFoV = max;
        if (camera->MinDistance == inputMinDistance) camera->MinDistance = minDistance;
    }
}
