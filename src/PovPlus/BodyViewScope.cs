using System;
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

    internal BodyViewScope(SceneCamera* camera, Vector3 flightBank)
    {
        if (flightBank == Vector3.Zero || camera == null || (camera->Flags & 1) == 0) return;
        var forward = (Vector3)camera->LookAtVector - (Vector3)camera->Position;
        var up = (Vector3)camera->Vector_1;
        var right = Vector3.Cross(forward, up);
        if (!float.IsFinite(right.LengthSquared()) || right.LengthSquared() < 1e-8f || forward.LengthSquared() < 1e-8f) return;
        var angle = Vector3.Dot(flightBank, Vector3.Normalize(right));
        if (!float.IsFinite(angle) || MathF.Abs(angle) < 1e-5f) return;
        // Tilt the native up vector towards the turn, preserving the eye and aim.
        originalLook = appliedLook = camera->LookAtVector;
        originalUp = camera->Vector_1;
        appliedUp = Vector3.Transform(up, Quaternion.CreateFromAxisAngle(Vector3.Normalize(forward), angle));
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
