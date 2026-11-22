using System;

namespace PovPlus;

internal static class ShoulderLens
{
    internal const float DefaultGlideDegrees = 8, MaxGlideDegrees = 60;
    private const float MaxWidenedFov = 170 * (MathF.PI / 180);

    // IDs and slots in the game's ActionTimeline sheet.
    internal const int MountGlideSlot = 0, RiderGlideSlot = 2;
    private const ushort MountGlide = 4049, RiderGlide = 6703;

    internal static bool IsGlideTimeline(ushort? mountTimeline, ushort riderTimeline) =>
        mountTimeline.HasValue ? mountTimeline == MountGlide : riderTimeline == RiderGlide;

    internal static bool Valid(float current, float min, float max) =>
        float.IsFinite(current) && float.IsFinite(min) && max > 0 && max < MathF.PI && min <= max;

    internal static float Widen(float normalFov, float weight, float glideDegrees)
    {
        if (!(normalFov > 0 && normalFov < MaxWidenedFov)) return normalFov;
        var boost = ShoulderRig.Bound(glideDegrees, 0, MaxGlideDegrees, DefaultGlideDegrees) * (MathF.PI / 180);
        return normalFov + weight * Math.Min(boost, MaxWidenedFov - normalFov);
    }
}

internal struct ShoulderGlideTransition
{
    internal const float MaxSeconds = 3, DefaultEnterSeconds = 0.25f, DefaultExitSeconds = 0.45f;

    private bool gliding;
    private float start, elapsed, delay, duration;
    internal float Weight { get; private set; }

    internal void Update(bool active, float dt, float delaySeconds, float transitionSeconds)
    {
        var target = active ? 1f : 0f;
        if (active != gliding)
        {
            gliding = active;
            start = Weight;
            elapsed = 0;
            delay = ShoulderRig.Bound(delaySeconds, 0, MaxSeconds, 0);
            duration = ShoulderRig.Bound(transitionSeconds, 0, MaxSeconds, active ? DefaultEnterSeconds : DefaultExitSeconds);
            // Time before the first sample of this edge belongs to the old state.
            dt = 0;
        }
        if (Weight == target) return;

        elapsed += dt;
        if (elapsed < delay) return;
        var t = duration == 0 ? 1 : Math.Clamp((elapsed - delay) / duration, 0, 1);
        Weight = float.Lerp(start, target, t * t * (3 - 2 * t));
    }
}
