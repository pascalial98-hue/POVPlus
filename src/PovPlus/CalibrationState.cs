using System;
using System.Numerics;
using FFXIVClientStructs.FFXIV.Client.Game;

namespace PovPlus;

// Draft offsets are isolated from persistent configuration until Save.
internal sealed class CalibrationState
{
    internal bool Active { get; private set; }
    internal Vector3 Draft;
    internal static bool ResolveSeen(bool? saved, bool existingConfiguration) => saved ?? existingConfiguration;
    internal static float Offset(float value) => float.IsFinite(value) ? Math.Clamp(value, -1f, 1f) : 0f;

    internal bool Begin(Vector3 current)
    {
        if (Active) return false;
        Draft = new Vector3(Offset(current.X), Offset(current.Y), Offset(current.Z));
        Active = true;
        return true;
    }

    internal Vector3? End(bool save)
    {
        if (!Active) return null;
        Active = false;
        return save ? new Vector3(Offset(Draft.X), Offset(Draft.Y), Offset(Draft.Z)) : null;
    }
}

internal static unsafe class CalibrationCamera
{
    // Use existing native fields, not synthetic keys or a new native hook.
    // Do not write saved camera settings or change a third-person user's orbit.
    internal static bool EnsureThirdPerson(Camera* camera)
    {
        if (camera == null || camera->ZoomMode is not (CameraZoomMode.FirstPerson or CameraZoomMode.ThirdPerson) ||
            camera->IsEventCameraAutoControl) return false;
        if (camera->ZoomMode == CameraZoomMode.ThirdPerson) return true;
        var min = MathF.Max(0.5f, camera->MinDistance);
        var max = camera->MaxDistance;
        if (!float.IsFinite(min) || !float.IsFinite(max) || max < min) return false;
        var distance = float.IsFinite(camera->SavedDistance) && camera->SavedDistance > 0f
            ? camera->SavedDistance : 3f;
        camera->Distance = camera->InterpDistance = Math.Clamp(distance, min, max);
        camera->ZoomMode = CameraZoomMode.ThirdPerson;
        return true;
    }
}
