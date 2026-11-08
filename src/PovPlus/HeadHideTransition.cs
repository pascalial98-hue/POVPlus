using System;

namespace PovPlus;

// Covers both entering first person and retreating from it. Distance-driven.
internal sealed class HeadHideTransition
{
    internal const float DefaultDistance = 0.25f;
    internal const float DefaultShowDistance = 0.35f;
    private bool hidden;
    internal bool IsHidden => hidden;

    internal static float SanitizeDistance(float distance) =>
        float.IsFinite(distance) ? Math.Clamp(distance, 0.05f, 0.75f) : DefaultDistance;

    internal static float SanitizeShowDistance(float distance) =>
        float.IsFinite(distance) ? Math.Clamp(distance, 0.2f, 1f) : DefaultShowDistance;

    internal bool Update(bool firstPerson, bool delay, float targetDistance, float headDistance,
        float hideDistance, float headClearance, float showDistance = DefaultShowDistance)
    {
        if (!firstPerson)
        {
            // A third-person mode flag changes before the camera has left the
            // head. Preserve the existing latch until the actual eye is clear.
            // Use distance to the head, not the configurable first-person target.
            var clearance = float.IsFinite(headClearance) ? MathF.Max(0f, headClearance) : 0.18f;
            var releaseDistance = MathF.Max(SanitizeShowDistance(showDistance), clearance + 0.05f);
            if (hidden && float.IsFinite(headDistance) && headDistance >= releaseDistance)
                hidden = false;
            return hidden;
        }
        if (hidden) return true;
        // If a position cannot be measured, keep the established first-person
        // protection rather than leaving the camera inside a visible head.
        hidden = !delay || !float.IsFinite(targetDistance) || !float.IsFinite(headDistance) ||
            targetDistance <= SanitizeDistance(hideDistance) || headDistance <= headClearance;
        return hidden;
    }

    internal void Reset() => hidden = false;
}
