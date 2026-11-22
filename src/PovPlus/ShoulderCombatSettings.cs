using System;
using System.Numerics;

namespace PovPlus;

[Serializable]
public sealed class ShoulderCombatSettings
{
    internal const float MaxSide = 2, MaxHeight = 1.5f, MaxDistance = 5, MaxSeconds = 3;
    internal const float DefaultHeight = 0.15f, DefaultDistance = 1, DefaultEnter = 0.35f, DefaultExit = 0.8f;
    public bool Enabled;
    public float Side, Height = DefaultHeight, Distance = DefaultDistance;
    public float EnterSeconds = DefaultEnter, ExitSeconds = DefaultExit;

    // The phase belongs to the active shoulder camera, never to saved settings.
    internal Vector3 Advance(ref float phase, bool inCombat, float dt)
    {
        var entering = Enabled && inCombat;
        var seconds = entering ? ShoulderRig.Bound(EnterSeconds, 0, MaxSeconds, DefaultEnter)
                               : ShoulderRig.Bound(ExitSeconds, 0, MaxSeconds, DefaultExit);
        if (seconds == 0) phase = entering ? 1 : 0;
        else phase = Math.Clamp(phase + (entering ? dt : -dt) / seconds, 0, 1);
        var weight = phase * phase * (3 - 2 * phase);
        return weight * new Vector3(
            ShoulderRig.Bound(Side, -MaxSide, MaxSide, 0),
            ShoulderRig.Bound(Height, -MaxHeight, MaxHeight, DefaultHeight),
            ShoulderRig.Bound(Distance, -MaxDistance, MaxDistance, DefaultDistance));
    }
}
