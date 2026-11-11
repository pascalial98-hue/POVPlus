using System;
using System.Numerics;

namespace PovPlus;

internal readonly record struct CalibrationFrame(Vector3 Position, Vector3 Right, Vector3 Up, Vector3 Forward);

internal static unsafe class CalibrationViewpoint
{
    internal static bool TryRead(Vector3 offset, out CalibrationFrame frame)
    {
        frame = default;
        var player = Service.ObjectTable.LocalPlayer;
        if (player == null) return false;
        var model = POVCameraFunctionality.GetCharacterBase(player);
        if (model == null || model->Skeleton == null) return false;
        var skeleton = model->Skeleton;
        if (skeleton->PartialSkeletons == null || skeleton->PartialSkeletonCount is 0 or > 32) return false;
        var pose = skeleton->PartialSkeletons[0].GetHavokPose(0);
        if (!POVCameraFunctionality.TryFindHead(pose, out var index) || pose->ModelPose.Data == null ||
            pose->ModelPose.Length <= index || pose->ModelInSync == 0) return false;
        var head = Plugin.P.CameraHeadTransform(pose, index);
        var localHead = new Vector3(head.Translation.X, head.Translation.Y, head.Translation.Z);
        var rotation = new Quaternion(head.Rotation.X, head.Rotation.Y, head.Rotation.Z, head.Rotation.W);
        var worldRotation = (Quaternion)skeleton->Transform.Rotation;
        var scale = new Vector3(model->Scale.X, model->Scale.Y, model->Scale.Z) * ((float*)model)[169];
        if (!Finite(localHead) || !Finite(scale) || !float.IsFinite(rotation.LengthSquared()) ||
            rotation.LengthSquared() < 0.001f || !float.IsFinite(worldRotation.LengthSquared()) ||
            worldRotation.LengthSquared() < 0.001f) return false;
        rotation = Quaternion.Normalize(rotation);
        var matrix = Matrix4x4.CreateScale(scale) * Matrix4x4.CreateFromQuaternion(Quaternion.Normalize(worldRotation)) *
            Matrix4x4.CreateTranslation((Vector3)skeleton->Transform.Position);
        var position = FirstPersonMath.HeadCameraPosition(localHead, rotation, matrix, offset);
        var right = Vector3.TransformNormal(Vector3.Transform(Vector3.UnitZ, rotation), matrix) * 0.08f;
        var up = Vector3.TransformNormal(Vector3.Transform(Vector3.UnitY, rotation), matrix) * 0.08f;
        var forward = Vector3.TransformNormal(Vector3.Transform(Vector3.UnitX, rotation), matrix) * 0.08f;
        if (!Finite(position) || !Finite(right) || !Finite(up) || !Finite(forward)) return false;
        frame = new CalibrationFrame(position, right, up, forward);
        return true;
    }

    private static bool Finite(Vector3 v) => float.IsFinite(v.X) && float.IsFinite(v.Y) && float.IsFinite(v.Z);
}
