using System;

namespace PovPlus;

// Beyond the ordinary angle limit, one input-driven progress controls both neck
// travel and the optional outward rotation. They extend and retract together.
internal sealed class BoundaryLookAxis
{
    internal const float ExtensionTravel = 15 * FirstPersonMath.DegreesToRadians;
    internal const float DefaultSensitivity = 0.35f;
    internal float Excess { get; private set; }
    internal float ExtraAngle => Excess < 0
        ? -extraLowerLimit * Math.Clamp(-Excess / ExtensionTravel, 0, 1)
        : extraUpperLimit * Math.Clamp(Excess / ExtensionTravel, 0, 1);
    internal float ExtraDown => -Math.Min(ExtraAngle, 0);
    private float extraLowerLimit, extraUpperLimit;
    private float lastLower, lastUpper;

    internal void Reset() => Excess = 0;

    internal void SetExtraDownLimit(float limit)
    {
        extraLowerLimit = SanitizeExtraLimit(limit);
        extraUpperLimit = 0;
    }

    internal void SetExtraSideLimit(float limit) => extraLowerLimit = extraUpperLimit = SanitizeExtraLimit(limit);

    private static float SanitizeExtraLimit(float radians) =>
        float.IsFinite(radians) ? Math.Clamp(radians, 0, 20 * FirstPersonMath.DegreesToRadians) : 0;

    internal static float ExtraSideRange(float degrees) =>
        Math.Clamp(float.IsFinite(degrees) ? degrees : 10, 0, 20) * FirstPersonMath.DegreesToRadians;

    internal static float ExtraDownRange(float degrees, float ordinaryDown) =>
        Math.Min(Math.Clamp(float.IsFinite(degrees) ? degrees : 10, 0, 20) * FirstPersonMath.DegreesToRadians,
            Math.Max(0, 95 * FirstPersonMath.DegreesToRadians - ordinaryDown));

    internal static float SanitizeSensitivity(float value) => Math.Clamp(float.IsFinite(value) ? value : DefaultSensitivity, 0.1f, 1f);

    internal void UpdateLimits(float lower, float upper)
    {
        // Animation, body yaw, soft limits and native interpolation can move the
        // observed angle even with a stationary mouse. They do not own the drag.
        if (lastLower != lower || lastUpper != upper) Reset();
        lastLower = lower;
        lastUpper = upper;
    }

    internal bool ReleaseAfterAutomaticReturn(float angle, float manualInput, float automaticInput,
        float lower, float upper, bool mouseLookHeld)
    {
        // An arbitrary observed-angle change is not enough: idle animation used
        // to release the neck by mistake. Require verified native auto input,
        // no mouse-look ownership, and a return inside the ordinary angle range.
        if (Excess == 0 || mouseLookHeld || manualInput != 0 || !float.IsFinite(angle) ||
            !float.IsFinite(automaticInput) || automaticInput * Excess >= 0 ||
            MathF.Abs(automaticInput) < 0.000001f) return false;
        const float margin = 2 * FirstPersonMath.DegreesToRadians;
        var inside = angle >= lower && angle <= upper &&
            (Excess < 0 ? angle >= lower + margin : angle <= upper - margin);
        if (!inside) return false;
        Reset();
        return true;
    }

    internal float Apply(float angle, float input, float lower, float upper, float band, bool extendLower, bool extendUpper, float sensitivity = 1)
    {
        UpdateLimits(lower, upper);
        sensitivity = SanitizeSensitivity(sensitivity);
        if (!extendLower && Excess < 0 || !extendUpper && Excess > 0) Reset();
        // Work on the base angle. Animation may move it, but cannot release the
        // user's held extension or additional rotation.
        angle = Math.Clamp(float.IsFinite(angle) ? angle - ExtraAngle : 0, lower, upper);
        if (!float.IsFinite(input) || input == 0) return angle + ExtraAngle;
        // Reverse travel retracts extension first; travel beyond its maximum is
        // discarded, so a long outward drag does not create hidden input debt.
        if (Excess > 0 && input < 0 || Excess < 0 && input > 0)
        {
            var needed = MathF.Abs(Excess) / sensitivity;
            if (MathF.Abs(input) <= needed)
            {
                Excess += input * sensitivity;
                if (MathF.Abs(Excess) < 0.0000001f) Excess = 0;
                return angle + ExtraAngle;
            }
            input -= MathF.CopySign(needed, input);
            Excess = 0;
        }
        if (Excess != 0)
        {
            // While extended, the drag owns displacement. Keep the observed
            // angle (even after an automatic recenter), never snap it to an edge.
            Accumulate(input * sensitivity);
            return angle + ExtraAngle;
        }
        angle = SoftLookLimit.Step(angle, input, lower, upper, band, out var extra);
        if ((extra < 0 && extendLower) || (extra > 0 && extendUpper))
            Accumulate(extra * sensitivity);
        return angle + ExtraAngle;
    }

    private void Accumulate(float travel)
    {
        Excess = Math.Clamp(Excess + travel, -ExtensionTravel, ExtensionTravel);
    }
}
