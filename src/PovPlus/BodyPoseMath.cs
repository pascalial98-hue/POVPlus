using System;
using System.Numerics;

namespace PovPlus;

// The input camera stays upright. Only its rendered basis is moved into the
// body's frame, so native recentering never mistakes a bow for mouse input.
internal static class BodyPoseMath
{
    internal static bool TryNormalize(Quaternion value, out Quaternion result)
    {
        var length = value.LengthSquared();
        result = Quaternion.Identity;
        if (!float.IsFinite(length) || length < 0.000001f) return false;
        result = Quaternion.Normalize(value);
        return true;
    }

    internal static bool Finite(Vector3 value) =>
        float.IsFinite(value.X) && float.IsFinite(value.Y) && float.IsFinite(value.Z);

    internal static Quaternion ModelDelta(Quaternion current, Quaternion reference) =>
        Quaternion.Normalize(current * Quaternion.Conjugate(reference));

    internal static Quaternion WorldDelta(Quaternion modelDelta, Quaternion modelWorld, float playerYaw) =>
        Quaternion.Normalize(modelWorld * modelDelta *
            Quaternion.Conjugate(Quaternion.CreateFromAxisAngle(Vector3.UnitY, playerYaw)));

    internal static Quaternion HeadRelativeToBody(Quaternion head, Quaternion bodyDelta) =>
        Quaternion.Normalize(Quaternion.Conjugate(bodyDelta) * head);

    internal static Quaternion Smooth(Quaternion previous, Quaternion target, float seconds)
    {
        // Exponential slerp has the same response at different frame rates.
        var dt = float.IsFinite(seconds) ? Math.Clamp(seconds, 0, 0.1f) : 0;
        return Quaternion.Normalize(Quaternion.Slerp(previous, target, 1 - MathF.Exp(-dt / 0.06f)));
    }

    internal static bool TryView(Vector3 position, Vector3 lookAt, float yaw, Quaternion delta,
        out Vector3 transformedLookAt, out Vector3 transformedUp)
    {
        transformedLookAt = lookAt;
        transformedUp = Vector3.UnitY;
        var forward = lookAt - position;
        if (!Finite(position) || !Finite(forward) || forward.LengthSquared() < 0.000001f ||
            !float.IsFinite(yaw) || !TryNormalize(delta, out delta)) return false;
        // A yaw-derived right vector remains defined at straight up/down. Using
        // world-up here would flip the horizon or become singular at the poles.
        var right = new Vector3(MathF.Cos(yaw), 0, -MathF.Sin(yaw));
        var up = Vector3.Cross(Vector3.Normalize(forward), right);
        if (up.LengthSquared() < 0.000001f) return false;
        transformedLookAt = position + Vector3.Transform(forward, delta);
        transformedUp = Vector3.Transform(Vector3.Normalize(up), delta);
        return Finite(transformedLookAt) && Finite(transformedUp);
    }
}
