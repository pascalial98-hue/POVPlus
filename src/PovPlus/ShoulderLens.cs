using System;

namespace PovPlus;

internal static class ShoulderLens
{
    internal const float DefaultGlideDegrees = 8, MaxGlideDegrees = 20;
    private const float MaxWidenedFov = 120 * (MathF.PI / 180);

    // IDs and slots in the game's ActionTimeline sheet.
    internal const int MountGlideSlot = 0, RiderGlideSlot = 2;
    private const ushort MountGlide = 4049, RiderGlide = 6703;

    internal static bool IsGlideTimeline(ushort? mountTimeline, ushort riderTimeline) =>
        mountTimeline.HasValue ? mountTimeline == MountGlide : riderTimeline == RiderGlide;

    internal static bool Valid(float current, float min, float max) =>
        float.IsFinite(current) && float.IsFinite(min) && max > 0 && max < MathF.PI && min <= max;

    internal static float Target(float normalFov, bool gliding, float glideDegrees)
    {
        if (!gliding) return normalFov;
        var boost = ShoulderRig.Bound(glideDegrees, 0, MaxGlideDegrees, DefaultGlideDegrees) * (MathF.PI / 180);
        return normalFov + Math.Min(boost, Math.Max(0, MaxWidenedFov - normalFov));
    }
}
