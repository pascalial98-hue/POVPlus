using System;

namespace PovPlus;

internal sealed class ShoulderZoom
{
    internal const float SliderMin = 1, SliderMax = 10, DefaultMin = 1.5f, DefaultMax = 4;
    internal float Selected { get; private set; }
    private float original, lastWritten;
    private bool wheelUsed;

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

    internal float Update(float nativeDistance, float nativeMin, float nativeMax, float min, float max, float blend)
    {
        (min, max) = Range(min, max);
        Selected = Math.Clamp(Selected, min, max);
        var low = Math.Clamp(min, nativeMin, nativeMax);
        var high = Math.Clamp(max, nativeMin, nativeMax);
        var delta = Math.Clamp(nativeDistance, nativeMin, nativeMax) - Math.Clamp(lastWritten, nativeMin, nativeMax);
        if (MathF.Abs(delta) > 0.00001f)
        {
            wheelUsed = true;
            if (low <= max && high >= min)
                Selected = Math.Clamp(Math.Clamp(Selected, low, high) + delta, low, high);
        }
        var target = Math.Clamp(Selected, low, high);
        // Only the automatic approach can begin outside the shoulder interval.
        // A wheel step takes over from the remembered shoulder distance.
        lastWritten = wheelUsed || blend >= 1 ? target :
            Math.Clamp(float.Lerp(original, target, blend), nativeMin, nativeMax);
        return lastWritten;
    }
}
