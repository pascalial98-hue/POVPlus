using System;
using System.Numerics;

namespace PovPlus;

[Serializable]
public sealed class ShoulderProfile
{
    public float Side = ShoulderRig.DefaultSide, Height = ShoulderRig.DefaultHeight;
    public float Distance = ShoulderRig.DefaultDistance, MinDistance = ShoulderZoom.DefaultMin, MaxDistance = ShoulderZoom.DefaultMax;
    public bool SpeedZoomEnabled;
    public float SpeedZoomDistance = ShoulderSpeedZoom.DefaultDistance;
    public float SpeedZoomEnterSeconds = ShoulderSpeedZoom.DefaultEnterSeconds;
    public float SpeedZoomExitSeconds = ShoulderSpeedZoom.DefaultExitSeconds;
    public float Lag = ShoulderRig.DefaultLag, MaxLag = ShoulderRig.DefaultMaxLag;
    public float VerticalLagPercent;
    public float RecoverySeconds = ShoulderRig.DefaultRecovery, TurnInertiaPercent = ShoulderRig.DefaultTurnInertia;

    internal static ShoulderProfile Riding() => new()
    {
        Side = 0.75f, Height = 0.25f, Distance = 3.5f, MinDistance = 1.5f, MaxDistance = 7,
        Lag = 0.25f, MaxLag = 0.35f
    };
}

// The native camera keeps the orbit, pitch and collision response. This rig
// describes only a displacement of its target and a bounded movement lag.
internal sealed class ShoulderRig
{
    internal const float DefaultSide = 0.46f, DefaultHeight = 0.12f, DefaultDistance = 2.2f;
    internal const float DefaultLag = 0.10f, DefaultMaxLag = 0.32f;
    internal const float DefaultEntryTime = 0.22f;
    internal const float MaxRidingLagTime = 1.5f, MaxRidingLagDistance = 2f;
    internal const float MinRecovery = 0.15f, MaxRecovery = 3, DefaultRecovery = 1.0f, DefaultTurnInertia = 35;
    private Vector3 previous, filteredTarget, lag, travelVelocity;
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
        filteredTarget = lag = travelVelocity = Vector3.Zero;
    }

    internal void Follow(Vector3 position, float dt, float time, float limit, float yaw = 0, float verticalPercent = 0,
        float ridingWeight = 0, float recoverySeconds = DefaultRecovery, float turnInertiaPercent = DefaultTurnInertia)
    {
        if (!float.IsFinite(position.X) || !float.IsFinite(position.Y) || !float.IsFinite(position.Z) || !float.IsFinite(yaw))
        {
            Reset();
            return;
        }
        if (dt == 0 && sampled) return;
        var delta = position - previous;
        previous = position;
        ridingWeight = Bound(ridingWeight, 0, 1, 0);
        time = Bound(time, 0, float.Lerp(0.4f, MaxRidingLagTime, ridingWeight), DefaultLag);
        limit = Bound(limit, 0, float.Lerp(0.8f, MaxRidingLagDistance, ridingWeight), DefaultMaxLag);
        var jump = ridingWeight > 0 ? Math.Max(3, 32 * dt) : 3;
        if (!sampled || !(dt > 0 && dt < (ridingWeight > 0 ? 0.5f : 0.25f)) ||
            delta.LengthSquared() > jump * jump || time < 0.001f || limit == 0)
        {
            filteredTarget = lag = travelVelocity = Vector3.Zero;
        }
        else
        {
            // Express movement at the middle of the orbit step. Lag history stays
            // in camera space, so turning cannot fling an old backward offset sideways.
            float turn = MathF.IEEERemainder(yaw - previousYaw, 2 * MathF.PI);
            var toCamera = Quaternion.CreateFromAxisAngle(Vector3.UnitY, -previousYaw - turn * 0.5f);
            var velocity = delta / dt;
            var trailing = -time * velocity;
            trailing.Y *= Bound(verticalPercent, 0, 100, 0) / 100;
            var target = WithinLimit(Vector3.Transform(trailing, toCamera), limit);
            var walkTau = Math.Max(0.02f, time * 0.3f);
            if (ridingWeight == 0)
            {
                travelVelocity = Vector3.Zero;
                var step = dt / walkTau;
                var decay = MathF.Exp(-step);
                var carry = step * decay;
                lag = decay * lag + carry * filteredTarget + (1 - decay - carry) * target;
                filteredTarget = decay * filteredTarget + (1 - decay) * target;
            }
            else
            {
                var strength = ridingWeight * Bound(turnInertiaPercent, 0, 100, DefaultTurnInertia) / 100;
                var sway = TurnOffset(velocity, dt, limit * 0.75f * strength);
                target = WithinLimit(target + Vector3.Transform(sway, toCamera), limit);
                var attack = float.Lerp(walkTau, Math.Max(0.015f, time / 3), ridingWeight);
                var release = float.Lerp(walkTau, Bound(recoverySeconds, MinRecovery, MaxRecovery, DefaultRecovery) / 3, ridingWeight);
                var outputTau = float.Lerp(walkTau, 0.055f, ridingWeight);
                var outputDecay = MathF.Exp(-dt / outputTau);
                var follow = new Response(dt, attack, outputTau);
                var recover = new Response(dt, release, outputTau);
                follow.Advance(ref lag.X, ref filteredTarget.X, target.X, outputDecay, recover);
                follow.Advance(ref lag.Y, ref filteredTarget.Y, target.Y, outputDecay, recover);
                follow.Advance(ref lag.Z, ref filteredTarget.Z, target.Z, outputDecay, recover);
                filteredTarget = WithinLimit(filteredTarget, limit);
                lag = WithinLimit(lag, limit);
            }
        }
        previousYaw = yaw;
        sampled = true;
    }

    private static Vector3 WithinLimit(Vector3 offset, float limit)
    {
        var squared = offset.LengthSquared();
        return squared > limit * limit ? offset * (limit / MathF.Sqrt(squared)) : offset;
    }

    private Vector3 TurnOffset(Vector3 velocity, float dt, float amplitude)
    {
        velocity.Y = 0;
        var change = (velocity - travelVelocity) * (1 - MathF.Exp(-dt / 0.14f));
        var heading = travelVelocity + change * 0.5f;
        travelVelocity += change;
        var speed = heading.Length();
        if (amplitude <= 0 || speed < 1) return Vector3.Zero;
        heading /= speed;
        // Only acceleration across the actual path produces outward sway.
        var lateral = (change - heading * Vector3.Dot(change, heading)) / dt;
        var speedWeight = Math.Clamp((speed - 1) / 3, 0, 1);
        return WithinLimit(-lateral / 12, 1) * (amplitude * speedWeight);
    }

    // Exact response of two one-pole filters to a constant target over dt.
    // Only the first pole switches between attack and release. The visible
    // offset and its velocity (intermediate - visible) / outputTau stay continuous.
    private readonly struct Response(float dt, float inputTau, float outputTau)
    {
        private readonly float inputDecay = MathF.Exp(-dt / inputTau);
        private readonly float carry = Math.Abs(inputTau - outputTau) < 0.00001f
            ? dt / outputTau * MathF.Exp(-dt / outputTau)
            : (float)(inputTau / (double)(inputTau - outputTau) * (Math.Exp(-dt / (double)inputTau) - Math.Exp(-dt / (double)outputTau)));

        internal void Advance(ref float visible, ref float intermediate, float target, float outputDecay, Response release)
        {
            var recovering = intermediate * target < 0 || MathF.Abs(target) < MathF.Abs(intermediate);
            var response = recovering ? release : this;
            visible = outputDecay * visible + response.carry * intermediate + (1 - outputDecay - response.carry) * target;
            intermediate = response.inputDecay * intermediate + (1 - response.inputDecay) * target;
        }
    }

    internal Vector3 Offset(float orbitYaw, float side, float height, Vector2 extraFraming = default)
    {
        if (!float.IsFinite(orbitYaw)) return Vector3.Zero;
        side = Bound(side, -2, 2, DefaultSide);
        height = Bound(height, -1.5f, 2, DefaultHeight);
        return Vector3.Transform(lag + new Vector3(side + extraFraming.X, height + extraFraming.Y, 0),
            Quaternion.CreateFromAxisAngle(Vector3.UnitY, orbitYaw));
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
