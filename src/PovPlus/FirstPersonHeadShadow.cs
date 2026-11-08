using System;
using System.Threading;
using Dalamud.Hooking;
using FFXIVClientStructs.FFXIV.Client.Graphics.Render;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using FFXIVClientStructs.Interop;

namespace PovPlus;

// Small experiment: keep the real pose intact; remove only the local head
// models' main-camera visibility bits on the stack-local draw parameters.
internal sealed unsafe class FirstPersonHeadShadow : IDisposable
{
    internal const string MaterialSignature = "E8 ?? ?? ?? ?? 40 38 75 ?? 74 ?? 8B 45";
    internal const string TlsSignature = "44 8B 3D ?? ?? ?? ?? 49 8B F0 B8 C4 03 02 00 4C 8B F2";
    internal const string MainMaskSignature = "41 F7 46 38 00 00 00 03 0F 84 ?? ?? ?? ?? 49 8B 06 48 8B 08";
    internal const string ShadowMaskSignature = "41 8B 46 40 C1 E8 02 A8 01 0F 84 ?? ?? ?? ?? 41 8B 46 38";
    private delegate nint MaterialDelegate(nint renderer, byte* parameters, nint material, uint materialIndex);
    private readonly Hook<MaterialDelegate> hook;
    private readonly uint* tlsIndex;
    private sealed record Targets(nint Owner, nint Skeleton, nint[] Models);
    private Targets? active, cached;

    internal FirstPersonHeadShadow()
    {
        // Reject a client whose consumers no longer match the verified mask
        // layout. Do not use the upstream Params2.OutFlags offset here: this
        // client's material flags are +0x40, its visibility mask is +0x38.
        Service.SigScanner.ScanText(MainMaskSignature);
        Service.SigScanner.ScanText(ShadowMaskSignature);
        var tls = (byte*)Service.SigScanner.ScanText(TlsSignature);
        tlsIndex = (uint*)(tls + 7 + *(int*)(tls + 3));
        if (*tlsIndex > 1087 || ThreadLocals.MemberFunctionPointers.NtCurrentTeb == null)
            throw new InvalidOperationException("Unsupported graphics TLS layout.");
        hook = Service.GameInteropProvider.HookFromAddress<MaterialDelegate>(
            Service.SigScanner.ScanText(MaterialSignature), Material);
    }

    internal void ClearTargets() => Volatile.Write(ref active, null);

    internal void SetEnabled(bool enabled)
    {
        if (!enabled) ClearTargets();
        if (enabled == hook.IsEnabled) return;
        if (enabled) hook.Enable();
        else hook.Disable();
    }

    // Called at the same pose/hide-transition boundary as the old head patch.
    // Renderer workers only consume an immutable pointer snapshot, never the
    // object table or mutable configuration. Reuse it until the model changes.
    internal bool Select(CharacterBase* owner)
    {
        if (!hook.IsEnabled || owner == null || owner->GetModelType() != CharacterBase.ModelType.Human ||
            owner->Skeleton == null || owner->Models == null || owner->SlotCount is < 12 or > 32 ||
            owner->Models[11] == null) return false;
        Span<nint> selected = stackalloc nint[6];
        var count = 0;
        for (uint i = 0; i < owner->SlotCount; i++)
        {
            var model = owner->Models[i];
            if (model == null) continue;
            ReadOnlySpan<byte> path = i == 12 && model->ModelResourceHandle != null &&
                model->ModelResourceHandle->FileName.Length <= 4096
                ? model->ModelResourceHandle->FileName.AsSpan() : default;
            if (HeadShadowMask.IsHeadSlot(i, path)) selected[count++] = (nint)model;
        }
        selected = selected[..count];
        if (cached == null || cached.Owner != (nint)owner || cached.Skeleton != (nint)owner->Skeleton ||
            !selected.SequenceEqual(cached.Models))
            cached = new Targets((nint)owner, (nint)owner->Skeleton, selected.ToArray());
        Volatile.Write(ref active, cached);
        return true;
    }

    private nint Material(nint renderer, byte* parameters, nint material, uint materialIndex)
    {
        var result = hook.Original(renderer, parameters, material, materialIndex);
        var targets = Volatile.Read(ref active);
        if (targets == null || parameters == null) return result;
        var inner = *(nint*)parameters;
        if (inner == 0) return result;
        var model = *(Model**)inner;
        if (model == null || Array.IndexOf(targets.Models, (nint)model) < 0 ||
            (nint)model->Skeleton != targets.Skeleton || model->Skeleton == null ||
            (nint)model->Skeleton->Owner != targets.Owner) return result;
        var teb = ThreadLocals.NtCurrentTeb();
        if (teb == null || teb->ThreadLocalStoragePointer == null) return result;
        var local = teb->ThreadLocalStoragePointer[*tlsIndex];
        if (local == null || !local->IsInitialized || local->GraphicsKernelContext == null) return result;
        var visibility = (uint*)(parameters + 0x38);
        var filtered = HeadShadowMask.Filter(*visibility, local->GraphicsKernelContext->ViewIndex, true);
        if (filtered != *visibility)
        {
            *visibility = filtered;
        }
        return result;
    }

    public void Dispose()
    {
        ClearTargets();
        hook.Dispose();
        cached = null;
    }
}
