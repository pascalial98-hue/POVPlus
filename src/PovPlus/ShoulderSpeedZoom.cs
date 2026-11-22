using System;
using System.Numerics;

namespace PovPlus;

internal sealed class ShoulderSpeedZoom
{
    internal const float DefaultDistance = 0.6f, MaxDistance = 2f;
    private const float DeadZone = 0.5f, FullSpeed = 18f;
    private Vector3? previous;
    private bool wasRiding;
    private float speed;
    internal float Offset { get; private set; }

    internal void Reset()
    {
        previous = null;
        speed = Offset = 0;
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
        var target = limit * Math.Clamp((speed - DeadZone) / (FullSpeed - DeadZone), 0, 1);
        var seconds = target > Offset ? 0.20f : 0.42f;
        Offset = float.Lerp(Offset, target, 1 - MathF.Exp(-dt / seconds));
        if (target == 0 && Offset < 0.0001f) Offset = 0;
    }
}
