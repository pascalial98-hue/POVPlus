using FFXIVClientStructs.FFXIV.Client.Game.Control;

namespace PovPlus;

internal struct ShoulderFreeLook
{
    private float? movementYaw;

    internal static bool LeftDrag(int buttons, bool dragging) =>
        dragging && buttons == (int)InputManager.MouseButtonHoldState.Left;

    internal float Resolve(float nativeYaw, bool active)
    {
        if (!active) movementYaw = null;
        else if (float.IsFinite(nativeYaw)) movementYaw ??= nativeYaw;
        return movementYaw ?? nativeYaw;
    }
}
