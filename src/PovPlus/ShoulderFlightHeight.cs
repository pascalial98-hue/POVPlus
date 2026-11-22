using System;

namespace PovPlus;

internal sealed class ShoulderFlightHeight
{
    private float? seatHeight;
    private float weight, missingSeconds;
    private uint? previousFrame;
    private nint mount;

    internal void Reset()
    {
        seatHeight = null;
        previousFrame = null;
        weight = missingSeconds = 0;
        mount = 0;
    }

    internal float Resolve(uint frame, float dt, nint mountIdentity, bool flying, float rootY,
        float? headY, float nativeY)
    {
        if (!float.IsFinite(rootY) || !float.IsFinite(nativeY)) { Reset(); return nativeY; }
        if (flying && mount != mountIdentity) { Reset(); mount = mountIdentity; }
        // Multiple target queries in one camera frame must not advance smoothing twice.
        dt = previousFrame == frame ? 0 : ShoulderRig.Bound(dt, 0, 0.25f, 0);
        previousFrame = frame;
        var relative = headY - rootY;
        if (flying && relative is >= -8 and <= 32)
        {
            // Filter the riding pose relative to the actor. Climbing or dropping
            // moves the pivot by the full change in altitude in this same frame.
            var shoulder = relative.Value - 0.12f;
            seatHeight = seatHeight.HasValue
                ? float.Lerp(seatHeight.Value, shoulder, 1 - MathF.Exp(-dt / 0.09f)) : shoulder;
            missingSeconds = 0;
        }
        else missingSeconds += dt;
        if (seatHeight == null) return nativeY;
        var tracking = flying && missingSeconds <= 0.25f;
        weight = Math.Clamp(weight + (tracking ? 1 : -1) * dt / 0.12f, 0, 1);
        if (!tracking && weight == 0) { Reset(); return nativeY; }
        return float.Lerp(nativeY, rootY + seatHeight.Value, weight);
    }
}
