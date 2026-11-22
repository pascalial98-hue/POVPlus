using System;
using System.Numerics;

namespace PovPlus;

// The native camera keeps the orbit, pitch and collision response. This rig
// describes only a displacement of its target and a bounded movement lag.
internal sealed class ShoulderRig
{
    internal const float DefaultSide = 0.46f, DefaultHeight = 0.12f, DefaultDistance = 2.2f;
    internal const float DefaultLag = 0.10f, DefaultMaxLag = 0.32f;
    internal const float DefaultEntryTime = 0.22f;
    private Vector3 previous, lag;
    private bool sampled;

    internal static float Bound(float value, float min, float max, float fallback) =>
        float.IsFinite(value) ? Math.Clamp(value, min, max) : fallback;

    internal static float EntryBlend(float elapsed, float duration)
    {
        duration = Bound(duration, 0, 0.6f, DefaultEntryTime);
        if (duration == 0) return 1;
        var t = Math.Clamp(elapsed / duration, 0, 1);
        return t * t * (3 - 2 * t);
    }

    internal void Reset()
    {
        sampled = false;
        lag = Vector3.Zero;
    }

    internal void Follow(Vector3 position, float dt, float time, float limit)
    {
        if (!float.IsFinite(position.X) || !float.IsFinite(position.Y) || !float.IsFinite(position.Z))
        {
            Reset();
            return;
        }
        var delta = position - previous;
        previous = position;
        time = Bound(time, 0, 0.4f, DefaultLag);
        limit = Bound(limit, 0, 0.8f, DefaultMaxLag);
        if (!sampled || !(dt > 0 && dt < 0.25f) || delta.LengthSquared() > 9 || time < 0.001f || limit == 0)
            lag = Vector3.Zero;
        else
        {
            // Exact first-order response to linear target motion over this frame.
            // Only horizontal translation drives lag, including when riding.
            delta.Y = 0;
            var decay = MathF.Exp(-dt / time);
            lag = lag * decay - delta * (time / dt * (1 - decay));
            var length = lag.Length();
            if (length > limit) lag *= limit / length;
        }
        sampled = true;
    }

    internal Vector3 Offset(float orbitYaw, float side, float height)
    {
        if (!float.IsFinite(orbitYaw)) return Vector3.Zero;
        side = Bound(side, -1, 1, DefaultSide);
        height = Bound(height, -0.6f, 0.8f, DefaultHeight);
        return lag + new Vector3(MathF.Cos(orbitYaw) * side, height, -MathF.Sin(orbitYaw) * side);
    }
}

// Native permission can be queried multiple times during the same input frame.
internal sealed class ShoulderCycle
{
    private uint? lastFrame;
    private bool allowNative;
    internal bool Active { get; private set; }

    internal bool AllowToggle(uint frame, bool thirdPerson)
    {
        if (lastFrame == frame) return allowNative;
        lastFrame = frame;
        Active = thirdPerson && !Active;
        return allowNative = !Active;
    }

    internal void Reset()
    {
        Active = false;
        lastFrame = null;
    }
}
