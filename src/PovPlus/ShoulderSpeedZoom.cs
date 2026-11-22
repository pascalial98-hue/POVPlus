using System;
using System.Numerics;

namespace PovPlus;

internal sealed class ShoulderSpeedZoom
{
    internal const float DefaultDistance = 0.6f, MaxDistance = 2f;
    private const float DeadZone = 0.5f;
    private Vector3? previous;
    private bool wasRiding;
    private float speed;
    internal float Offset { get; private set; }

    internal void Reset()
    {
        previous = null;
        speed = Offset = 0;
    }

    private static float SmoothRamp(float speed, float start, float end)
    {
        var t = Math.Clamp((speed - start) / (end - start), 0, 1);
        return t * t * (3 - 2 * t);
    }

    private static float DistanceFraction(float speed, bool riding)
    {
        var normalPace = riding ? 9.5f : 6f;
        var fullSpeed = riding ? 18f : 10f;
        // The broad ramp gives slow travel a little motion. Most of the distance
        // comes from the overlapping ramp around the normal-to-fast transition.
        var broad = SmoothRamp(speed, DeadZone, fullSpeed);
        var boost = SmoothRamp(speed, normalPace * 0.97f, normalPace * 1.50f);
        return 0.18f * broad + 0.82f * boost;
    }

    internal void Update(Vector3 position, float dt, bool riding, ShoulderProfile profile)
    {
        if ((!profile.SpeedZoomEnabled && Offset == 0) || !float.IsFinite(position.LengthSquared()) ||
            !float.IsFinite(dt) || dt < 0 || dt > 0.25f)
        {
            Reset();
            return;
        }
        if (dt == 0) return;

        var delta = position - previous.GetValueOrDefault(position);
        var distance = riding ? delta.Length() : new Vector2(delta.X, delta.Z).Length();
        // Changing seats or teleporting can displace the actor within a single tick.
        if (riding != wasRiding || distance > Math.Max(3, 40 * dt))
            speed = 0;
        else
            speed = float.Lerp(speed, distance / dt, 1 - MathF.Exp(-dt / 0.14f));
        wasRiding = riding;
        previous = position;

        var limit = profile.SpeedZoomEnabled ? ShoulderRig.Bound(profile.SpeedZoomDistance, 0, MaxDistance, DefaultDistance) : 0;
        var target = limit * DistanceFraction(speed, riding);
        var seconds = target > Offset ? 0.20f : 0.42f;
        Offset = float.Lerp(Offset, target, 1 - MathF.Exp(-dt / seconds));
        if (target == 0 && Offset < 0.0001f) Offset = 0;
    }
}
