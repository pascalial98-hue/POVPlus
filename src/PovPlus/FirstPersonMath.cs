using System;
using System.Numerics;

namespace PovPlus;

internal static class FirstPersonMath
{
    internal const float DegreesToRadians = MathF.PI / 180f;

    internal static float Wrap(float angle) => float.IsFinite(angle)
        ? MathF.IEEERemainder(angle, 2f * MathF.PI) : 0f;

    internal static float Degrees(float value, float fallback, float maximum) =>
        Math.Clamp(float.IsFinite(value) ? value : fallback, 5f, maximum) * DegreesToRadians;

    internal static float ClampYaw(float cameraYaw, float bodyYaw, float limitRadians)
    {
        var center = Wrap(bodyYaw);
        return Wrap(center + Math.Clamp(Wrap(cameraYaw - center), -limitRadians, limitRadians));
    }

    internal static float ClampPitch(float pitch, float upRadians, float downRadians) =>
        Math.Clamp(Wrap(pitch), -downRadians, upRadians);

    // The Beta's camera offset axes are right=local Z, up=local Y, forward=local X.
    // Share this calculation between the camera destination and hide timing.
    internal static Vector3 HeadCameraPosition(Vector3 head, Quaternion rotation,
        Matrix4x4 modelToWorld, Vector3 offset)
    {
        var localOffset = Vector3.Transform(Vector3.UnitZ, rotation) * offset.X +
            Vector3.Transform(Vector3.UnitY, rotation) * offset.Y +
            Vector3.Transform(Vector3.UnitX, rotation) * offset.Z;
        return Vector3.Transform(head, modelToWorld) + Vector3.TransformNormal(localOffset, modelToWorld);
    }

    internal static bool[] HeadBones(string[] names, short[] parents, bool attachedToHead)
    {
        var hidden = new bool[names.Length];
        for (var bone = 0; bone < names.Length; bone++)
        {
            if (attachedToHead) { hidden[bone] = true; continue; }
            // Follow parents instead of assuming that the skeleton is sorted.
            for (int ancestor = bone, steps = 0; ancestor >= 0 && ancestor < names.Length && steps < names.Length; steps++)
            {
                // Keep the neck and its non-head branches at their original size.
                // Hiding from j_kubi would expose the neck-to-torso connection.
                if (names[ancestor] == "j_kao")
                {
                    hidden[bone] = true;
                    break;
                }
                ancestor = ancestor < parents.Length ? parents[ancestor] : -1;
            }
        }
        return hidden;
    }
}
