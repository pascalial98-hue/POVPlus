using System;
using System.Numerics;

namespace PovPlus;

// X is forward and Y is the tangent toward increasing body-relative yaw.
// Keep these amounts in body space so turning the character cannot leave the
// camera displaced along an old world-space direction.
internal static class LookDownLean
{
    internal const float DefaultForward = 0.08f;
    internal const float DefaultSide = 0.06f;
    internal const float Maximum = 0.3f;

    internal static float Sanitize(float value, float fallback) =>
        Math.Clamp(float.IsFinite(value) ? value : fallback, 0, Maximum);

    internal static Vector2 Target(Vector2 pressure, float forward, float side)
    {
        var front = float.IsFinite(pressure.X) ? Math.Clamp(pressure.X, 0, 1) : 0;
        var lateral = float.IsFinite(pressure.Y) ? Math.Clamp(pressure.Y, -1, 1) : 0;
        // A direct linear mapping: no spring, easing curve, frame latch or
        // time-dependent chase that keeps moving after the mouse stops.
        return new Vector2(Sanitize(forward, DefaultForward) * front,
            Sanitize(side, DefaultSide) * lateral);
    }

    internal static Vector3 ToWorld(Vector2 amount, float bodyYaw)
    {
        if (!float.IsFinite(bodyYaw)) return Vector3.Zero;
        var yaw = FirstPersonMath.Wrap(bodyYaw);
        var forward = new Vector3(MathF.Sin(yaw), 0, MathF.Cos(yaw));
        var sideways = new Vector3(MathF.Cos(yaw), 0, -MathF.Sin(yaw));
        return forward * amount.X + sideways * amount.Y;
    }
}
