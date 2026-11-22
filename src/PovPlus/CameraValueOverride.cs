using System;

namespace PovPlus;

// Capture on the first write. If the game or a preset changes the field
// during use, its new value becomes the one to leave behind.
internal struct CameraValueOverride
{
    private float original, applied;
    private bool owned;

    internal readonly float Underlying(float value) => owned && value == applied ? original : value;

    internal void Apply(ref float value, float requested)
    {
        if (owned && value != applied) owned = false;
        if (value == requested) return;
        if (!owned) original = value;
        value = applied = requested;
        owned = true;
    }

    internal bool Restore(ref float value)
    {
        bool restore = owned && value == applied;
        if (restore) value = original;
        owned = false;
        return restore;
    }
}

internal struct FirstPersonFov
{
    private CameraValueOverride minimum, maximum, current;

    internal void Apply(ref float value, ref float min, ref float max, float requestedMin)
    {
        if (!float.IsFinite(requestedMin) || !float.IsFinite(value) || !float.IsFinite(max)) return;
        var floor = Math.Clamp(requestedMin, 0, 5);
        if (floor > maximum.Underlying(max)) maximum.Apply(ref max, floor);
        else maximum.Restore(ref max);
        minimum.Apply(ref min, floor);
        current.Apply(ref value, Math.Clamp(value, min, max));
    }

    internal void Restore(ref float value, ref float min, ref float max)
    {
        minimum.Restore(ref min);
        maximum.Restore(ref max);
        if (current.Restore(ref value) && float.IsFinite(min) && float.IsFinite(max) && min <= max)
            value = Math.Clamp(value, min, max);
    }
}

internal struct CameraSettingOverrides
{
    internal FirstPersonFov Fov;
    internal CameraValueOverride MinPitch, MaxPitch, Tilt;
}
