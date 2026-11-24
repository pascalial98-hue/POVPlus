using System;
using System.Numerics;

namespace PovPlus;

internal sealed class ShoulderWalkingHeight
{
    private const float HoldSeconds = 0.35f;
    private const float LandingWatchSeconds = 1.5f, StallSeconds = 0.24f;
    private const float StuckHeight = 0.2f, MinClosingSpeed = 0.2f;

    private enum Landing { None, AirOrMounted, Watching, Recovering }

    private uint? lastFrame;
    private nint lastSkeleton;
    private Vector3 previousRoot;
    private float framingHeight, levelTime, correction;
    private Landing landing;
    private float groundBias, watchTime, stalledTime, previousExcess, landingOffset;

    internal void Reset()
    {
        lastFrame = null;
        landing = Landing.None;
        groundBias = landingOffset = 0;
    }

    internal void SuspendForAirOrMount()
    {
        lastFrame = null;
        landing = Landing.AirOrMounted;
        landingOffset = 0;
    }

    internal float Update(uint frame, float dt, nint skeleton, Vector3 root, Vector3 drawnRoot, float nativeY,
        float cameraOffsetY = float.NaN)
    {
        if (!float.IsFinite(nativeY) || !float.IsFinite(root.LengthSquared()))
        {
            Reset();
            return 0;
        }
        if (skeleton == 0 || !float.IsFinite(drawnRoot.LengthSquared()) || Vector3.DistanceSquared(root, drawnRoot) > 16)
        {
            lastFrame = null;
            return 0;
        }
        if (frame == lastFrame && skeleton == lastSkeleton) return correction + landingOffset;

        var relative = nativeY - drawnRoot.Y;
        var initialDt = ShoulderRig.Bound(dt, 0, 0.25f, 0);
        dt *= unchecked(frame - lastFrame.GetValueOrDefault(frame));
        if (lastFrame.HasValue && Vector3.DistanceSquared(root, previousRoot) > 9) Reset();
        if (lastFrame == null || skeleton != lastSkeleton ||
            !(dt > 0 && dt <= 0.25f))
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
        UpdateLanding(dt > 0 && dt <= 0.25f ? dt : initialDt, relative + correction, cameraOffsetY);
        return correction + landingOffset;
    }

    private void UpdateLanding(float dt, float relativeHeight, float cameraOffsetY)
    {
        if (!float.IsFinite(cameraOffsetY)) return;
        // The unfiltered actor offset is available even when shoulder view was
        // entered on a mount. Keep measured framing differences from stable ground.
        var unbiased = relativeHeight - Math.Max(0.2f, cameraOffsetY);
        if (landing == Landing.None)
        {
            if (correction == 0 && levelTime >= HoldSeconds && MathF.Abs(unbiased) < 0.5f)
                groundBias = unbiased;
            return;
        }

        var excess = unbiased - groundBias;
        if (landing == Landing.AirOrMounted)
        {
            landing = Landing.Watching;
            watchTime = stalledTime = 0;
            previousExcess = excess;
            return;
        }
        if (landing == Landing.Watching)
        {
            watchTime += dt;
            if (excess <= StuckHeight)
            {
                stalledTime = 0;
                previousExcess = excess;
            }
            else if ((stalledTime += dt) >= StallSeconds)
            {
                // Measure progress over a window so small model movements
                // and frame-to-frame camera easing cannot mask a plateau.
                if (previousExcess - excess < MinClosingSpeed * stalledTime) landing = Landing.Recovering;
                stalledTime = 0;
                previousExcess = excess;
            }
            if (landing == Landing.Watching && watchTime >= LandingWatchSeconds) landing = Landing.None;
            if (landing != Landing.Recovering) return;
        }

        var target = -Math.Max(0, excess);
        landingOffset = float.Lerp(landingOffset, target, 1 - MathF.Exp(-dt / 0.12f));
        // If the game catches up suddenly, remove enough correction to avoid
        // dipping below the pre-landing framing.
        landingOffset = Math.Max(target, landingOffset);
        if (excess <= 0.025f && MathF.Abs(landingOffset) <= 0.025f)
        {
            landingOffset = 0;
            landing = Landing.None;
        }
    }
}
