using System;

namespace PovPlus;

// Temporary per-call distance classification; never alter a real object position.
internal readonly struct NearObjectDistanceScope : IDisposable
{
    [ThreadStatic] private static nint camera;
    [ThreadStatic] private static nint eye;
    private readonly nint previousCamera, previousEye;

    internal NearObjectDistanceScope(bool enabled, nint owner, nint position)
    {
        previousCamera = camera; previousEye = eye;
        camera = enabled ? owner : 0;
        eye = enabled ? position : 0;
    }

    internal static float Filter(float distanceSquared, nint owner, nint position) =>
        camera != 0 && eye != 0 && owner == camera && position == eye &&
        float.IsFinite(distanceSquared) && distanceSquared >= 0 ? float.MaxValue : distanceSquared;

    public void Dispose() { camera = previousCamera; eye = previousEye; }
}
