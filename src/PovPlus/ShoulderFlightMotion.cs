using System;
using System.Numerics;

namespace PovPlus;

internal sealed class ShoulderFlightMotion
{
    internal const float DefaultSway = 60, DefaultBankDegrees = 5, MaxBankDegrees = 12;
    private Vector3? previous;
    private Vector3 velocity;
    private float phase;
    internal Vector2 Sway { get; private set; }
    // Horizontal vector pointing into the turn; its magnitude is the roll in radians.
    internal Vector3 Bank { get; private set; }

    internal void Reset()
    {
        previous = null;
        velocity = Bank = Vector3.Zero;
        Sway = Vector2.Zero;
        phase = 0;
    }

    internal void Update(Vector3 position, float dt, bool airborne, float swayPercent, float bankDegrees)
    {
        if (!float.IsFinite(position.LengthSquared()) || !float.IsFinite(dt) || dt < 0 || dt > 0.25f)
        {
            Reset();
            return;
        }
        if (dt == 0) return;
        if (!airborne)
        {
            previous = null;
            velocity = Vector3.Zero;
            var decay = MathF.Exp(-dt / 0.12f);
            Sway *= decay;
            Bank *= decay;
            if (Sway.LengthSquared() + Bank.LengthSquared() < 1e-8f) Reset();
            return;
        }

        var delta = position - previous.GetValueOrDefault(position);
        var maxStep = Math.Max(3, 40 * dt);
        if (delta.LengthSquared() > maxStep * maxStep)
        {
            Reset();
            previous = position;
            return;
        }
        previous = position;
        var prior = velocity;
        velocity = Vector3.Lerp(prior, delta / dt, 1 - MathF.Exp(-dt / 0.18f));
        var speed = velocity.Length();
        var weight = Math.Clamp((speed - 0.6f) / 13, 0, 1);
        phase = (phase + MathF.Tau * (0.45f + 0.32f * weight) * dt) % MathF.Tau;
        var strength = weight * ShoulderRig.Bound(swayPercent, 0, 100, DefaultSway) / 100;
        var wave = new Vector2(0.055f * MathF.Sin(phase), 0.05f * MathF.Sin(2 * phase + 0.5f) + 0.025f * MathF.Sin(phase));
        Sway = Vector2.Lerp(Sway, wave * strength, 1 - MathF.Exp(-dt / 0.08f));

        // Isolate acceleration across the horizontal travel direction. Looking
        // around, accelerating in a straight line and climbing cannot initiate a bank.
        var horizontal = (prior + velocity) * 0.5f;
        horizontal.Y = 0;
        var target = Vector3.Zero;
        if (horizontal.LengthSquared() > 1)
        {
            var heading = Vector3.Normalize(horizontal);
            var acceleration = (velocity - prior) / dt;
            acceleration.Y = 0;
            var lateral = acceleration - heading * Vector3.Dot(acceleration, heading);
            var radians = ShoulderRig.Bound(bankDegrees, 0, MaxBankDegrees, DefaultBankDegrees) * (MathF.PI / 180);
            target = lateral / Math.Max(12, lateral.Length()) * weight * radians;
        }
        Bank = Vector3.Lerp(Bank, target, 1 - MathF.Exp(-dt / 0.2f));
        if (target == Vector3.Zero && Bank.LengthSquared() < 1e-9f) Bank = Vector3.Zero;
    }
}
