using System;

namespace PovPlus;

internal sealed class ShoulderZoom
{
    internal const float SliderMin = 0.25f, SliderMax = 10, DefaultMin = 1.5f, DefaultMax = 4;
    internal float Selected { get; private set; }
    private float original, lastWritten;
    internal float LastWritten => lastWritten;
    private bool wheelUsed;

    // Keep the native input and collision solvers working in physical distance,
    // including when the shoulder slider goes below the ordinary orbit minimum.
    internal static void ApplyFloor(ref CameraValueOverride owned, ref float nativeMin, float userMin)
    {
        var ordinaryMin = owned.Underlying(nativeMin);
        if (userMin < ordinaryMin) owned.Apply(ref nativeMin, userMin);
        else owned.Restore(ref nativeMin);
    }

    internal static (float Min, float Max) Range(float min, float max)
    {
        min = ShoulderRig.Bound(min, SliderMin, SliderMax, DefaultMin);
        max = ShoulderRig.Bound(max, SliderMin, SliderMax, DefaultMax);
        return (Math.Min(min, max), Math.Max(min, max));
    }

    internal void Begin(float thirdPerson, float remembered)
    {
        original = lastWritten = thirdPerson;
        Selected = ShoulderRig.Bound(remembered, SliderMin, SliderMax, ShoulderRig.DefaultDistance);
        wheelUsed = false;
    }

    internal static float PhysicalDistance(float baseDistance, float offset) => Math.Max(SliderMin, baseDistance + offset);

    internal float Update(float nativeDistance, float nativeMin, float nativeMax, float min, float max, float blend,
        float offset = 0)
    {
        (min, max) = Range(min, max);
        Selected = Math.Clamp(Selected, min, max);
        var low = Math.Clamp(PhysicalDistance(min, offset), nativeMin, nativeMax);
        var high = Math.Clamp(PhysicalDistance(max, offset), nativeMin, nativeMax);
        var target = Math.Clamp(Selected + offset, low, high);
        var delta = Math.Clamp(nativeDistance, nativeMin, nativeMax) - Math.Clamp(lastWritten, nativeMin, nativeMax);
        if (MathF.Abs(delta) > 0.00001f)
        {
            wheelUsed = true;
            var scrolled = Math.Clamp(target + delta, low, high);
            if (scrolled != target)
            {
                // Convert actual wheel movement back into the unshifted profile.
                Selected = Math.Clamp(scrolled - offset, min, max);
                target = scrolled;
            }
        }
        // Only the automatic approach can begin outside the shoulder interval.
        // A wheel step takes over from the remembered shoulder distance.
        lastWritten = wheelUsed || blend >= 1 ? target :
            Math.Clamp(float.Lerp(original, target, blend), nativeMin, nativeMax);
        return lastWritten;
    }
}
