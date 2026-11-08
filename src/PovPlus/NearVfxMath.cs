using System;

namespace PovPlus;

internal static class NearVfxMath
{
    // Only effects both created by and targeting the local character qualify.
    internal static bool IsSelf(nint player, nint caster, nint target) =>
        player != 0 && caster == player && target == player;

    internal static float Factor(float distance, float radius, float minimum)
    {
        if (!float.IsFinite(distance) || distance < 0) return 1;
        radius = Math.Clamp(float.IsFinite(radius) ? radius : 1.2f, 0.2f, 3f);
        minimum = Math.Clamp(float.IsFinite(minimum) ? minimum : 0.25f, 0.05f, 1f);
        var t = Math.Clamp(distance / radius, 0, 1);
        return minimum + (1 - minimum) * t * t * (3 - 2 * t);
    }
}
