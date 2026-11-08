using System;
using System.Numerics;
using System.Threading;
using Dalamud.Hooking;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using Hypostasis.Game;

namespace PovPlus;

// The native render-parameter update consumes Color.W synchronously, copying
// it to VfxResourceInstance before returning. Restore only the source value.
// This avoids retaining live VFX pointers or changing shared effect assets.
internal sealed unsafe class NearSelfVfx : IDisposable
{
    internal const string RenderSignature = "4C 8B DC 49 89 5B 10 49 89 7B 18 55 49 8D 6B D8 48 81 EC 20 01 00 00";
    internal const string CasterSignature = "40 53 48 83 EC 20 8B 51 50 85 D2 74 1D 83 FA 01 75 4B 48 8B 49 58";
    internal const string TargetSignature = "40 53 48 83 EC 20 44 8B 41 50 45 85 C0 74 21 41 83 F8 01 75 52 8B C2 48 8B 4C C1 60";
    private delegate void RenderDelegate(VfxObject* vfx);
    private readonly Hook<RenderDelegate> hook;
    private readonly nint casterFunction;
    private readonly nint targetFunction;
    private sealed record View(nint Player, Vector3 Eye, float Radius, float Minimum);
    private View? view;
    private bool failed;
    internal bool Failed => failed;

    internal NearSelfVfx()
    {
        casterFunction = Service.SigScanner.ScanText(CasterSignature);
        targetFunction = Service.SigScanner.ScanText(TargetSignature);
        hook = Service.GameInteropProvider.HookFromAddress<RenderDelegate>(
            Service.SigScanner.ScanText(RenderSignature), Render);
    }

    internal void UpdateState()
    {
        View? next = null;
        var config = Plugin.P.Configuration;
        var manager = Common.CameraManager;
        if (!failed && config.FirstPersonReduceSelfVfx && manager != null && FirstPersonLimits.IsActive(manager->worldCamera))
        {
            var camera = (FFXIVClientStructs.FFXIV.Client.Game.CameraBase*)manager->worldCamera;
            var scenes = CameraManager.Instance();
            var player = Service.ObjectTable.LocalPlayer;
            if (scenes != null && scenes->CurrentCamera == &camera->SceneCamera && player != null)
                next = new View(player.Address, camera->SceneCamera.Position, config.FirstPersonVfxRadius, config.FirstPersonVfxOpacity);
        }
        Volatile.Write(ref view, next);
        try
        {
            if (next != null && !hook.IsEnabled) hook.Enable();
            else if (next == null && hook.IsEnabled) hook.Disable();
        }
        catch (Exception ex)
        {
            if (!failed) Service.Log.Error(ex, "POV+ could not switch the self VFX hook.");
            failed = true;
            Volatile.Write(ref view, null);
        }
    }

    private void Render(VfxObject* vfx)
    {
        var factor = 1f;
        try
        {
            var current = Volatile.Read(ref view);
            if (current != null && vfx != null && vfx->VfxResourceInstance != null)
            {
                // Native Apricot listener: caster slot 1, first target slot 6.
                // Validate both function pointers, since the public actor-ID
                // fields do not describe this client's listener representation.
                var listener = (nint)vfx + 0x90;
                var table = *(nint**)listener;
                if (table != null && table[1] == casterFunction && table[6] == targetFunction)
                {
                    var caster = ((delegate* unmanaged<nint, nint>)casterFunction)(listener);
                    if (caster == current.Player)
                    {
                        var target = ((delegate* unmanaged<nint, uint, nint>)targetFunction)(listener, 0);
                        if (NearVfxMath.IsSelf(current.Player, caster, target))
                            factor = NearVfxMath.Factor(Vector3.Distance(vfx->Position, current.Eye), current.Radius, current.Minimum);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            if (!failed) Service.Log.Error(ex, "POV+ self VFX attenuation disabled after an error.");
            failed = true;
            Volatile.Write(ref view, null);
        }
        if (factor >= 1 || vfx == null || !float.IsFinite(vfx->Color.W))
        {
            hook.Original(vfx);
            return;
        }
        var patch = new VfxAlphaPatch(vfx, factor);
        try { hook.Original(vfx); }
        finally { patch.Restore(vfx); }
    }

    public void Dispose()
    {
        Volatile.Write(ref view, null);
        hook.Dispose();
    }
}
