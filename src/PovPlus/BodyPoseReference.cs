using System.Numerics;
using FFXIVClientStructs.Havok.Animation.Rig;

namespace PovPlus;

internal static unsafe class BodyPoseReference
{
    internal static bool TryFind(hkaSkeleton* skeleton, out int index, out Quaternion reference)
    {
        index = -1;
        reference = Quaternion.Identity;
        if (skeleton == null || skeleton->Bones.Data == null || skeleton->ParentIndices.Data == null ||
            skeleton->ReferencePose.Data == null || skeleton->Bones.Length is <= 0 or > 2048 ||
            skeleton->ParentIndices.Length != skeleton->Bones.Length ||
            skeleton->ReferencePose.Length != skeleton->Bones.Length) return false;
        // Use the highest recognized spine, never the head/neck (which also
        // move during independent head-look animations).
        var priority = 0;
        for (var i = 0; i < skeleton->Bones.Length; i++)
        {
            var candidate = skeleton->Bones[i].Name.String switch
            {
                "j_sebo_c" => 3,
                "j_sebo_b" => 2,
                "j_sebo_a" => 1,
                _ => 0
            };
            if (candidate > priority) { priority = candidate; index = i; }
        }
        if (index < 0) return false;
        return TryModelReference(skeleton, index, out reference);
    }

    internal static bool TryModelReference(hkaSkeleton* skeleton, int index, out Quaternion reference)
    {
        reference = Quaternion.Identity;
        if (skeleton == null || skeleton->ParentIndices.Data == null || skeleton->ReferencePose.Data == null ||
            skeleton->ReferencePose.Length is <= 0 or > 2048 ||
            skeleton->ParentIndices.Length != skeleton->ReferencePose.Length ||
            index < 0 || index >= skeleton->ReferencePose.Length) return false;
        // Havok's reference pose is LOCAL space, unlike AccessBoneModelSpace.
        // Compose the entire parent chain, including its bind-axis rotations.
        for (var count = 0; count < skeleton->ReferencePose.Length; count++)
        {
            var q = skeleton->ReferencePose[index].Rotation;
            if (!BodyPoseMath.TryNormalize(new(q.X, q.Y, q.Z, q.W), out var local)) return false;
            reference = Quaternion.Normalize(local * reference);
            var parent = skeleton->ParentIndices[index];
            if (parent == -1) return true;
            if (parent < 0 || parent >= skeleton->ReferencePose.Length) return false;
            index = parent;
        }
        return false; // malformed/cyclic custom skeleton
    }
}
