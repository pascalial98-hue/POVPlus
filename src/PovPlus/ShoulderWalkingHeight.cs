using System;
using System.Numerics;

namespace PovPlus;

internal sealed class ShoulderWalkingHeight
{
    private const float HoldSeconds = 0.35f;
    private uint? lastFrame;
    private nint lastSkeleton;
    private Vector3 previousRoot;
    private float framingHeight, levelTime, correction;

    internal void Reset() => lastFrame = null;

    internal float Update(uint frame, float dt, nint skeleton, Vector3 root, Vector3 drawnRoot, float nativeY)
    {
        if (skeleton == 0 || !float.IsFinite(nativeY) || !float.IsFinite(root.LengthSquared()) ||
            !float.IsFinite(drawnRoot.LengthSquared()) || Vector3.DistanceSquared(root, drawnRoot) > 16)
        {
            Reset();
            return 0;
        }
        if (frame == lastFrame && skeleton == lastSkeleton) return correction;

        var relative = nativeY - drawnRoot.Y;
        dt *= unchecked(frame - lastFrame.GetValueOrDefault(frame));
        if (lastFrame == null || skeleton != lastSkeleton ||
            !(dt > 0 && dt <= 0.25f) || Vector3.DistanceSquared(root, previousRoot) > 9)
        {
            framingHeight = relative;
            levelTime = HoldSeconds;
            correction = 0;
        }
        else
        {
            // Preserve the framing measured on level ground throughout the climb.
            // The model transform follows displayed elevation without gait or head bob.
            if (MathF.Abs(root.Y - previousRoot.Y) > 0.025f * dt) levelTime = 0;
            else levelTime = Math.Min(HoldSeconds, levelTime + dt);
            if (levelTime >= HoldSeconds)
            {
                // Flat-ground pose and preset changes normally pass straight through.
                framingHeight = correction == 0 ? relative :
                    float.Lerp(framingHeight, relative, 1 - MathF.Exp(-dt / 0.12f));
            }
            correction = framingHeight - relative;
            if (MathF.Abs(correction) < 0.0001f) correction = 0;
        }
        previousRoot = root;
        lastSkeleton = skeleton;
        lastFrame = frame;
        return correction;
    }
}
