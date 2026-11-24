using System;
using System.Numerics;

namespace PovPlus;

internal static class ShoulderWalkingHeight
{
    internal static float Resolve(Vector3 root, Vector3 drawnRoot, float cameraOffsetY, float nativeY)
    {
        if (!float.IsFinite(root.LengthSquared()) || !float.IsFinite(cameraOffsetY)) return nativeY;
        // The world camera starts from the actor's CameraOffset with a 0.2 minimum,
        // then smooths the result in world space. Anchor to the visible root before
        // that smoothing so jump and mount heights cannot become the walking baseline.
        var sameModelPosition = float.IsFinite(drawnRoot.LengthSquared()) && Vector3.DistanceSquared(root, drawnRoot) <= 16;
        var baseY = sameModelPosition ? drawnRoot.Y : root.Y;
        return baseY + Math.Max(0.2f, cameraOffsetY);
    }
}
