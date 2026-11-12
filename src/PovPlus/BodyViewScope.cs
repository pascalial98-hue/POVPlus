using System.Numerics;
using SceneCamera = FFXIVClientStructs.FFXIV.Client.Graphics.Scene.Camera;
using NativeVector = FFXIVClientStructs.FFXIV.Common.Math.Vector3;

namespace PovPlus;

// Scoped inputs to Scene.Camera.UpdateRender, which builds and uploads both
// view matrices. Restore the inputs afterwards, leaving native input, movement,
// and auto-return in their original logical frame. No projection matrix edits.
internal readonly unsafe struct BodyViewScope
{
    private readonly bool active;
    private readonly NativeVector originalLook, originalUp, appliedLook, appliedUp;

    internal BodyViewScope(SceneCamera* camera, float yaw, Quaternion delta)
    {
        if (camera == null || (camera->Flags & 1) == 0 ||
            !BodyPoseMath.TryView(camera->Position, camera->LookAtVector, yaw, delta, out var look, out var up)) return;
        originalLook = camera->LookAtVector;
        originalUp = camera->Vector_1;
        appliedLook = look;
        appliedUp = up;
        camera->LookAtVector = appliedLook;
        camera->Vector_1 = appliedUp;
        active = true;
    }

    internal void Restore(SceneCamera* camera)
    {
        if (!active || camera == null) return;
        if (camera->LookAtVector.Equals(appliedLook)) camera->LookAtVector = originalLook;
        if (camera->Vector_1.Equals(appliedUp)) camera->Vector_1 = originalUp;
    }
}
