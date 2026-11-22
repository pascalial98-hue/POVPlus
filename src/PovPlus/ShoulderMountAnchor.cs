using System;
using System.Numerics;

namespace PovPlus;

internal sealed class ShoulderMountAnchor
{
    private Vector3? seat;
    private float weight, missingSeconds;
    private uint? previousFrame;
    private nint mount;

    internal void Reset()
    {
        seat = null;
        previousFrame = null;
        weight = missingSeconds = 0;
        mount = 0;
    }

    internal Vector3 Resolve(uint frame, float dt, nint mountIdentity, bool riding, Vector3 root,
        Vector3? head, Vector3 native)
    {
        if (!float.IsFinite(root.LengthSquared()) || !float.IsFinite(native.LengthSquared())) { Reset(); return native; }
        if (riding && mountIdentity != 0 && mount != mountIdentity) { Reset(); mount = mountIdentity; }
        // Multiple target queries in one camera frame must not advance smoothing twice.
        dt = previousFrame == frame ? 0 : ShoulderRig.Bound(dt, 0, 0.25f, 0);
        previousFrame = frame;
        if (riding && head is Vector3 point && Vector3.DistanceSquared(point, root) <= 32 * 32 && point.Y >= root.Y - 8)
        {
            // Filter the riding pose relative to the actor. Climbing or dropping
            // moves the pivot by the full change in altitude in this same frame.
            var shoulder = point - root - new Vector3(0, 0.12f, 0);
            seat = seat.HasValue ? Vector3.Lerp(seat.Value, shoulder, 1 - MathF.Exp(-dt / 0.09f)) : shoulder;
            missingSeconds = 0;
        }
        else missingSeconds += dt;
        if (seat == null) return native;
        var tracking = riding && missingSeconds <= 0.25f;
        weight = Math.Clamp(weight + (tracking ? 1 : -1) * dt / 0.12f, 0, 1);
        if (!tracking && weight == 0) { Reset(); return native; }
        return Vector3.Lerp(native, root + seat.Value, weight);
    }
}
