using System;
using System.Numerics;

namespace PovPlus;

// Stabilize animation rotation only. Input angles and the head's camera
// position never enter this filter. Work in model space so character turning
// remains immediate when the result is transformed into world space.
internal sealed class BodyPoseMotionFilter
{
    private const float MovingSpeed = 0.15f;
    private const float MotionHoldSeconds = 0.2f;
    private const float MovingTimeConstant = 0.22f;
    private bool initialized;
    private Vector3 previousPosition;
    private float motionHold, movingBlend;
    private Quaternion bodyFast, bodyStageOne, bodyStageTwo;
    private Quaternion headStageOne, headStageTwo;

    internal readonly record struct Frame(Quaternion Body, Quaternion Head);

    internal void Reset() => initialized = false;

    internal Frame Update(Quaternion body, Quaternion head, Vector3 position, float seconds)
    {
        if (!BodyPoseMath.TryNormalize(body, out body) || !BodyPoseMath.TryNormalize(head, out head) ||
            !BodyPoseMath.Finite(position))
        {
            Reset();
            return new(Quaternion.Identity, Quaternion.Identity);
        }
        var distance = Vector3.Distance(position, previousPosition);
        // Do not carry a walking filter across a model change, teleport, or a
        // long sampling gap. Ordinary movement and start/stop never reseed it.
        if (!initialized || !float.IsFinite(seconds) || seconds < 0 || seconds > 0.5f || distance > 5)
        {
            initialized = true;
            previousPosition = position;
            motionHold = movingBlend = 0;
            bodyFast = bodyStageOne = bodyStageTwo = body;
            headStageOne = headStageTwo = head;
            return new(body, head);
        }
        // Repeated callbacks in the same instant must not consume an interval
        // or lose the displacement that belongs to the next real sample.
        if (seconds <= 0) return Current(head);
        var dt = Math.Min(seconds, 0.1f);
        // This is the actor root position, not the animated head/spine. Mouse
        // turns and stationary emotes therefore cannot activate stabilization.
        if (distance > MovingSpeed * seconds) motionHold = MotionHoldSeconds;
        else motionHold = Math.Max(0, motionHold - dt);
        previousPosition = position;
        var moving = motionHold > 0;
        var blendTime = moving ? 0.06f : 0.2f;
        movingBlend += ((moving ? 1f : 0f) - movingBlend) * (1 - MathF.Exp(-dt / blendTime));
        if (!moving && movingBlend < 0.0001f) movingBlend = 0;

        // Retain the existing stationary body response exactly. The slower
        // two-stage branch suppresses stride oscillation while retaining the
        // full angle of sustained bends/leans, without an angle dead zone.
        bodyFast = BodyPoseMath.Smooth(bodyFast, body, dt);
        Advance(ref bodyStageOne, ref bodyStageTwo, body, dt);
        Advance(ref headStageOne, ref headStageTwo, head, dt);
        return Current(head);
    }

    private Frame Current(Quaternion head) => new(
        Quaternion.Normalize(Quaternion.Slerp(bodyFast, bodyStageTwo, movingBlend)),
        Quaternion.Normalize(Quaternion.Slerp(head, headStageTwo, movingBlend)));

    private static void Advance(ref Quaternion first, ref Quaternion second, Quaternion target, float seconds)
    {
        // Exact decay/coupling for two equal time constants with a held input.
        // Both stages use the OLD first stage, avoiding callback-rate bias.
        var decay = MathF.Exp(-seconds / MovingTimeConstant);
        var coupling = seconds / MovingTimeConstant * decay;
        var total = 1 - decay;
        if (total <= 0) return;
        var intermediate = Quaternion.Slerp(target, first, Math.Clamp(coupling / total, 0, 1));
        second = Quaternion.Normalize(Quaternion.Slerp(second, intermediate, total));
        first = Quaternion.Normalize(Quaternion.Slerp(first, target, total));
    }
}
