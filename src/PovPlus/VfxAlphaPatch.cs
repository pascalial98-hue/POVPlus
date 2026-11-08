using System;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;

namespace PovPlus;

// A call-scoped patch: it is never retained across frames or object lifetimes.
internal readonly unsafe struct VfxAlphaPatch
{
    private readonly float original;
    private readonly float applied;
    private readonly bool changed;

    internal VfxAlphaPatch(VfxObject* vfx, float factor)
    {
        original = vfx->Color.W;
        changed = float.IsFinite(original) && float.IsFinite(factor) && factor >= 0 && factor < 1;
        applied = changed ? original * factor : original;
        if (changed) vfx->Color.W = applied;
    }

    internal void Restore(VfxObject* liveVfx)
    {
        if (changed && liveVfx->Color.W == applied) liveVfx->Color.W = original;
    }
}
