using System;

namespace PovPlus;

// Remembers the value actually written, so repeated camera callbacks do not
// repeatedly compress the same angle. Returning toward the centre is unmodified.
internal sealed class SoftLookLimit
{
    private bool seeded;
    private float previous;

    internal void Reset() => seeded = false;

    internal void Sync(float applied) { previous = FirstPersonMath.Wrap(applied); seeded = true; }

    internal float Apply(float input, float center, float lower, float upper, float band)
    {
        center = FirstPersonMath.Wrap(center);
        input = FirstPersonMath.Wrap(input);
        var relative = FirstPersonMath.Wrap(input - center);
        if (!seeded || !float.IsFinite(band) || band <= 0)
            relative = Math.Clamp(relative, lower, upper);
        else
        {
            var start = Math.Clamp(FirstPersonMath.Wrap(previous - center), lower, upper);
            var delta = FirstPersonMath.Wrap(input - previous);
            relative = Step(start, delta, lower, upper, band, out _);
            relative = Math.Clamp(relative, lower, upper);
        }
        seeded = true;
        previous = FirstPersonMath.Wrap(center + relative);
        return previous;
    }

    internal static float Step(float start, float delta, float lower, float upper, float band, out float excess)
    {
        if (delta < 0)
        {
            var result = -Advance(-start, -delta, -lower, Math.Min(band, -lower), out var extra);
            excess = -extra;
            return result;
        }
        return Advance(start, delta, upper, Math.Min(band, upper), out excess);
    }

    private static float Advance(float start, float delta, float edge, float band, out float excess)
    {
        excess = 0;
        if (delta <= 0) return start;
        if (!float.IsFinite(band) || band <= 0)
        {
            excess = Math.Max(0, start + delta - edge);
            return Math.Min(start + delta, edge);
        }
        var entry = edge - band;
        var linear = Math.Min(delta, Math.Max(0, entry - start));
        start += linear;
        delta -= linear;
        if (delta <= 0) return start;
        // Integrate a speed that falls linearly from 1 to 0.2 across the band.
        // Analytic integration gives the same response at different input rates.
        const float minimumRate = 0.2f;
        var rate = (1 - minimumRate) / band;
        var extension = minimumRate / rate;
        // Measure unused INPUT travel, not the angle removed by soft slowing.
        // Otherwise entering the soft zone itself would incorrectly trigger lean.
        var needed = MathF.Log((edge - start + extension) / extension) / rate;
        if (delta >= needed)
        {
            excess = delta - needed;
            return edge;
        }
        return Math.Min(edge, edge + extension - (edge - start + extension) * MathF.Exp(-rate * delta));
    }
}
