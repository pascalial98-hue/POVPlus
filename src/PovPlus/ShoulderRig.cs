using System;
using System.Numerics;

namespace PovPlus;

[Serializable]
public sealed class ShoulderProfile
{
    public float Side = ShoulderRig.DefaultSide, Height = ShoulderRig.DefaultHeight;
    public float Distance = ShoulderRig.DefaultDistance, MinDistance = ShoulderZoom.DefaultMin, MaxDistance = ShoulderZoom.DefaultMax;
    public float Lag = ShoulderRig.DefaultLag, MaxLag = ShoulderRig.DefaultMaxLag;
    public float VerticalLagPercent;

    internal static ShoulderProfile Riding() => new()
    {
        Side = 0.75f, Height = 0.25f, Distance = 3.5f, MinDistance = 1.5f, MaxDistance = 7,
        Lag = 0.08f, MaxLag = 0.35f
    };
}

// The native camera keeps the orbit, pitch and collision response. This rig
// describes only a displacement of its target and a bounded movement lag.
internal sealed class ShoulderRig
{
    internal const float DefaultSide = 0.46f, DefaultHeight = 0.12f, DefaultDistance = 2.2f;
    internal const float DefaultLag = 0.10f, DefaultMaxLag = 0.32f;
    internal const float DefaultEntryTime = 0.22f;
    private Vector3 previous, filteredTarget, lag;
    private float previousYaw;
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
        filteredTarget = lag = Vector3.Zero;
    }

    internal void Follow(Vector3 position, float dt, float time, float limit, float yaw = 0, float verticalPercent = 0)
    {
        if (!float.IsFinite(position.X) || !float.IsFinite(position.Y) || !float.IsFinite(position.Z) || !float.IsFinite(yaw))
        {
            Reset();
            return;
        }
        var delta = position - previous;
        previous = position;
        time = Bound(time, 0, 0.4f, DefaultLag);
        limit = Bound(limit, 0, 0.8f, DefaultMaxLag);
        if (!sampled || !(dt > 0 && dt < 0.25f) || delta.LengthSquared() > 9 || time < 0.001f || limit == 0)
        {
            filteredTarget = lag = Vector3.Zero;
        }
        else
        {
            // Express movement at the middle of the orbit step. Lag history stays
            // in camera space, so turning cannot fling an old backward offset sideways.
            float turn = MathF.IEEERemainder(yaw - previousYaw, 2 * MathF.PI);
            var local = Vector3.Transform(delta, Quaternion.CreateFromAxisAngle(Vector3.UnitY, -previousYaw - turn * 0.5f));
            local.Y *= Bound(verticalPercent, 0, 100, 0) / 100;
            var target = local * (-time / dt);
            var length = target.Length();
            if (length > limit) target *= limit / length;

            // Exact critically damped response to this velocity sample. Both
            // stages are convex blends of bounded offsets, including on reversals.
            var step = dt / Math.Max(0.02f, time * 0.3f);
            var decay = MathF.Exp(-step);
            var carry = step * decay;
            lag = decay * lag + carry * filteredTarget + (1 - decay - carry) * target;
            filteredTarget = decay * filteredTarget + (1 - decay) * target;
        }
        previousYaw = yaw;
        sampled = true;
    }

    internal Vector3 Offset(float orbitYaw, float side, float height)
    {
        if (!float.IsFinite(orbitYaw)) return Vector3.Zero;
        side = Bound(side, -2, 2, DefaultSide);
        height = Bound(height, -1.5f, 2, DefaultHeight);
        return Vector3.Transform(lag + new Vector3(side, height, 0), Quaternion.CreateFromAxisAngle(Vector3.UnitY, orbitYaw));
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
