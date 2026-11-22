using System;
using System.Numerics;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Hooking;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Common.Component.BGCollision;
using Hypostasis.Game;
using Hypostasis.Game.Structures;
using SceneManager = FFXIVClientStructs.FFXIV.Client.Graphics.Scene.CameraManager;

namespace PovPlus;

internal sealed unsafe class ShoulderView : IDisposable
{
    // The CN world camera asks this virtual permission after resolving the
    // game's CAMERA_MODE binding. Wheel zoom also asks, without this action.
    private const InputCode TogglePerspective = (InputCode)0x2e;
    private const string GateSignature = "40 53 48 83 EC 20 48 8B D9 48 8D 0D ?? ?? ?? ?? E8 ?? ?? ?? ?? 84 C0 75 ?? 38 83 ?? ?? ?? ?? 75 ?? B0 01";
    private delegate byte CanChangePerspective(GameCamera* camera);
    private readonly Hook<CanChangePerspective> gate;
    private readonly GameCamera* owner;
    private readonly nint table;
    private readonly ShoulderCycle cycle = new();
    private readonly ShoulderRig rig = new();
    private float originalDistance, selectedDistance;
    private float entryTime, blend, lastAppliedZoom;
    private bool blendingDistance;
    private nint actor;
    internal bool Active => cycle.Active && !Failed;
    internal bool Failed { get; private set; }
    private static Configuration Config => Plugin.P.Configuration;

    internal ShoulderView(GameCamera* camera)
    {
        owner = camera;
        table = (nint)camera->vtbl;
        if (camera->vtbl[23] != Service.SigScanner.ScanText(GateSignature) ||
            InputManager.StaticAddressPointers.pInstance == null || InputManager.MemberFunctionPointers.GetInputStatus == null ||
            BGCollisionModule.MemberFunctionPointers.RaycastMaterialFilter == null)
            throw new InvalidOperationException("World camera or input/collision bindings are unavailable.");
        gate = Service.GameInteropProvider.HookFromAddress<CanChangePerspective>(camera->vtbl[23], OnPermission);
        try { gate.Enable(); }
        catch { gate.Dispose(); throw; }
    }

    private bool SameCamera() => Common.CameraManager != null && Common.CameraManager->worldCamera == owner &&
        (nint)owner->vtbl == table;

    private bool InWorld()
    {
        if (!Config.Setting_ModEnabled || !Config.ShoulderCycleEnabled || Plugin.P.CalibrationActive ||
            !SameCamera() || owner->mode is not (0 or 1) || ((Camera*)owner)->IsEventCameraAutoControl ||
            Service.ClientState.IsGPosing || Service.ObjectTable.LocalPlayer == null) return false;
        var c = Service.Condition;
        if (c[ConditionFlag.BetweenAreas] || c[ConditionFlag.BetweenAreas51] || c[ConditionFlag.WatchingCutscene] ||
            c[ConditionFlag.WatchingCutscene78] || c[ConditionFlag.OccupiedInCutSceneEvent]) return false;
        var scene = SceneManager.Instance();
        return scene != null && scene->CurrentCamera == &((CameraBase*)owner)->SceneCamera;
    }

    private byte OnPermission(GameCamera* camera)
    {
        var native = gate.Original(camera);
        if (native == 0 || Failed || camera != owner) return native;
        try
        {
            if (!InWorld() || !InputManager.Instance()->GetInputStatus(TogglePerspective)) return native;
            var framework = Framework.Instance();
            if (framework == null || !ValidZoomBounds()) return native;
            var wasActive = Active;
            var allow = cycle.AllowToggle(framework->FrameCounter, camera->mode == 1);
            if (!wasActive && Active)
            {
                actor = Service.ObjectTable.LocalPlayer!.Address;
                originalDistance = owner->currentZoom;
                entryTime = 0;
                blend = ShoulderRig.EntryBlend(0, Config.ShoulderEntryTime);
                blendingDistance = blend < 1;
                ApplyDistance();
                rig.Reset();
            }
            else if (wasActive && !Active) RestoreZoom();
            return allow ? native : (byte)0;
        }
        catch (Exception ex) { Stop(ex); return native; }
    }

    private bool ValidZoomBounds() => float.IsFinite(owner->currentZoom) && float.IsFinite(owner->minZoom) &&
        float.IsFinite(owner->maxZoom) && owner->minZoom >= 0 && owner->maxZoom > 0 && owner->minZoom <= owner->maxZoom;

    private void ApplyDistance()
    {
        selectedDistance = ShoulderRig.Bound(Config.ShoulderDistance, 1, 5, ShoulderRig.DefaultDistance);
        var target = Math.Clamp(selectedDistance, owner->minZoom, owner->maxZoom);
        lastAppliedZoom = owner->currentZoom = float.Lerp(originalDistance, target, blend);
    }

    internal void Update()
    {
        if (!Active) return;
        try
        {
            if (!InWorld() || owner->mode != 1 || Service.ObjectTable.LocalPlayer!.Address != actor)
            {
                Reset();
                return;
            }
            var dt = (float)Service.Framework.UpdateDelta.TotalSeconds;
            if (blend < 1)
            {
                entryTime += MathF.Max(0, dt);
                blend = MathF.Max(blend, ShoulderRig.EntryBlend(entryTime, Config.ShoulderEntryTime));
            }
            // Native wheel input can take over at any point in the transition.
            if (owner->currentZoom != lastAppliedZoom) blendingDistance = false;
            if (ValidZoomBounds() && (blendingDistance ||
                selectedDistance != ShoulderRig.Bound(Config.ShoulderDistance, 1, 5, ShoulderRig.DefaultDistance)))
                ApplyDistance();
            if (blend >= 1) blendingDistance = false;
            rig.Follow(Service.ObjectTable.LocalPlayer!.Position, dt,
                Config.ShoulderLag, Config.ShoulderMaxLag);
        }
        catch (Exception ex) { Stop(ex); }
    }

    internal void OffsetPivot(GameCamera* camera, nint target, Vector3* position)
    {
        if (!Active || camera != owner || camera->mode != 1 || position == null || actor != target) return;
        try
        {
            if (!InWorld()) return;
            if (!float.IsFinite(position->X) || !float.IsFinite(position->Y) || !float.IsFinite(position->Z)) return;
            var offset = blend * rig.Offset(camera->currentHRotation, Config.ShoulderSide, Config.ShoulderHeight);
            var length = offset.Length();
            if (length < 0.0001f) return;
            var direction = offset / length;
            // Guard the short sideways path to the new pivot. The native camera
            // continues to resolve obstructions between that pivot and the eye.
            if (BGCollisionModule.RaycastMaterialFilter(*position, direction, out var hit, length + 0.08f))
                length = Math.Clamp(Vector3.Dot((Vector3)hit.Point - *position, direction) - 0.08f, 0, length);
            *position += direction * length;
        }
        catch (Exception ex) { Stop(ex); }
    }

    private void RestoreZoom()
    {
        if (actor != 0 && SameCamera())
        {
            if (owner->mode == 1) owner->currentZoom = originalDistance;
            else if (owner->mode == 0) ((Camera*)owner)->SavedDistance = originalDistance;
        }
        actor = 0;
        rig.Reset();
    }

    internal void Reset()
    {
        RestoreZoom();
        cycle.Reset();
    }

    private void Stop(Exception ex)
    {
        Failed = true;
        Reset();
        Service.Log.Error(ex, "POV+ shoulder camera stopped. Native perspective controls are active.");
    }

    public void Dispose()
    {
        gate.Dispose();
        Reset();
    }
}
