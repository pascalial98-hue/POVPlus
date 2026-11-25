using System;
using System.Numerics;
using FFXIVClientStructs.FFXIV.Client.Game.Control;

namespace PovPlus;

internal struct ShoulderFreeLook
{
    internal const float ReturnSeconds = 0.28f;
    private const int Left = (int)InputManager.MouseButtonHoldState.Left;
    private const int Right = (int)InputManager.MouseButtonHoldState.Right;

    private enum Phase { Idle, Looking, Returning }

    private Phase phase;
    private float movementYaw, returnTime;
    private Vector2 originalOrbit, releaseOrbit;
    internal readonly bool Active => phase != Phase.Idle;
    internal readonly bool Returning => phase == Phase.Returning;

    internal static bool LeftDrag(int buttons, bool dragging) =>
        dragging && buttons == Left;

    internal void UpdateInput(int buttons, bool dragging, Vector2 orbit, float nativeYaw)
    {
        if ((buttons & Right) != 0 || !float.IsFinite(orbit.X) || !float.IsFinite(orbit.Y))
        {
            this = default;
            return;
        }

        if (LeftDrag(buttons, dragging))
        {
            if (!Active)
            {
                if (!float.IsFinite(nativeYaw)) return;
                movementYaw = nativeYaw;
                originalOrbit = orbit;
            }
            phase = Phase.Looking;
        }
        else if (phase == Phase.Looking && buttons != Left)
        {
            phase = Phase.Returning;
            releaseOrbit = orbit;
            returnTime = 0;
        }
    }

    internal readonly float MovementYaw(float nativeYaw) => Active ? movementYaw : nativeYaw;

    internal readonly int RotationMode(int nativeMode) => Returning && nativeMode == 0
        ? 3 : StationaryLook.CameraRotationMode(nativeMode, Active);

    internal Vector2 AdvanceReturn(float dt)
    {
        returnTime += dt;
        if (returnTime >= ReturnSeconds)
        {
            phase = Phase.Idle;
            return originalOrbit;
        }

        var fraction = returnTime / ReturnSeconds;
        var weight = fraction * fraction * (3 - 2 * fraction);
        var delta = FirstPersonMath.Wrap(originalOrbit.X - releaseOrbit.X);
        return new Vector2(FirstPersonMath.Wrap(releaseOrbit.X + delta * weight),
            float.Lerp(releaseOrbit.Y, originalOrbit.Y, weight));
    }
}
