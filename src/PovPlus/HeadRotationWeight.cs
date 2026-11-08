using System;

namespace PovPlus;

// Yaw/pitch are additive to native mouse input. Track the weighted animation
// contribution, so changing its strength replaces that contribution instead of
// scaling only future deltas and leaving a permanent offset after the head returns.
internal sealed class HeadRotationWeight
{
    private float previous;
    internal void Reset() => previous = 0;

    internal static float Scale(float radians, float percent, bool enabled)
    {
        if (!enabled || !float.IsFinite(radians)) return 0;
        var weight = float.IsFinite(percent) ? Math.Clamp(percent, 0, 100) / 100 : 1;
        return radians * weight;
    }

    internal float Update(float smoothedRadians, float percent, bool enabled)
    {
        var next = Scale(smoothedRadians, percent, enabled);
        var delta = next - previous;
        previous = next;
        return delta;
    }
}
