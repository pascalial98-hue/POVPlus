using System;
using System.Collections.Generic;
using FFXIVClientStructs.Havok.Animation.Rig;
using FFXIVClientStructs.Havok.Common.Base.Math.QsTransform;
using FFXIVClientStructs.Havok.Common.Base.Math.Vector;

namespace PovPlus;

// Restore through a currently live pose, never through an unvalidated saved pointer.
// The camera can read the original head transform while rendering uses the hidden pose.
internal sealed unsafe class HeadPosePatch
{
    internal const float HiddenScale = 0.0001f;
    private sealed record Entry(nint Pose, nint Skeleton, nint Data, int Count, int Index,
        hkQsTransformf Original, hkQsTransformf Applied);
    private readonly List<Entry> originals = new();

    internal void Apply(hkaPose* pose, bool[] hidden, hkVector4f anchor, ReadOnlySpan<float> visibility = default)
    {
        if (pose == null || pose->ModelInSync == 0 || pose->ModelPose.Data == null ||
            pose->ModelPose.Length != hidden.Length) return;
        for (var i = 0; i < hidden.Length; i++)
        {
            if (!hidden[i]) continue;
            var keep = i < visibility.Length && float.IsFinite(visibility[i]) ? Math.Clamp(visibility[i], 0, 1) : 0;
            if (keep >= 1) continue;
            var transform = pose->ModelPose.Data + i;
            var original = *transform;
            // All affected bones share an anchor so faces cannot stretch between
            // different translations. A tiny nonzero scale avoids singular matrices.
            transform->Translation = anchor;
            if (keep > 0)
            {
                transform->Translation.X += (original.Translation.X - anchor.X) * keep;
                transform->Translation.Y += (original.Translation.Y - anchor.Y) * keep;
                transform->Translation.Z += (original.Translation.Z - anchor.Z) * keep;
            }
            var scale = HiddenScale + (1 - HiddenScale) * keep;
            transform->Scale.X *= scale;
            transform->Scale.Y *= scale;
            transform->Scale.Z *= scale;
            originals.Add(new Entry((nint)pose, (nint)pose->Skeleton, (nint)pose->ModelPose.Data,
                pose->ModelPose.Length, i, original, *transform));
        }
    }

    private static bool Matches(Entry entry, hkaPose* pose) => pose != null &&
        entry.Pose == (nint)pose && entry.Skeleton == (nint)pose->Skeleton &&
        entry.Data == (nint)pose->ModelPose.Data && entry.Count == pose->ModelPose.Length;

    internal bool TryGetOriginal(hkaPose* pose, int index, out hkQsTransformf original)
    {
        foreach (var entry in originals)
        {
            if (entry.Index != index || !Matches(entry, pose)) continue;
            var current = pose->ModelPose.Data[index];
            original = current;
            if (current.Translation.Equals(entry.Applied.Translation)) original.Translation = entry.Original.Translation;
            if (current.Scale.Equals(entry.Applied.Scale)) original.Scale = entry.Original.Scale;
            return true;
        }
        original = default;
        return false;
    }

    internal void Restore(hkaPose* livePose)
    {
        for (var i = originals.Count - 1; i >= 0; i--)
        {
            var entry = originals[i];
            if (!Matches(entry, livePose)) continue;
            var current = livePose->ModelPose.Data + entry.Index;
            // Preserve new animation / another plugin's changes to untouched fields.
            if (current->Translation.Equals(entry.Applied.Translation)) current->Translation = entry.Original.Translation;
            if (current->Scale.Equals(entry.Applied.Scale)) current->Scale = entry.Original.Scale;
            originals.RemoveAt(i);
        }
    }

    internal void Clear() => originals.Clear();
}
