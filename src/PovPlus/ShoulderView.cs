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
using NativeCharacter = FFXIVClientStructs.FFXIV.Client.Game.Character.Character;
using GameObject = FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject;

namespace PovPlus;

internal sealed unsafe class ShoulderView : IDisposable
{
    // The CN world camera asks this virtual permission after resolving the
    // game's CAMERA_MODE binding. Wheel zoom also asks, without this action.
    private const InputCode TogglePerspective = (InputCode)0x2e;
    private const string GateSignature = "40 53 48 83 EC 20 48 8B D9 48 8D 0D ?? ?? ?? ?? E8 ?? ?? ?? ?? 84 C0 75 ?? 38 83 ?? ?? ?? ?? 75 ?? B0 01";
    private delegate byte CanChangePerspective(GameCamera* camera);
    private readonly Hook<CanChangePerspective> gate;
    private readonly Hook<Camera.Delegates.UpdateState> inputUpdate;
    private readonly GameCamera* owner;
    private readonly nint table;
    private readonly ShoulderCycle cycle = new();
    private readonly ShoulderRig rig = new();
    private readonly ShoulderZoom zoom = new();
    private readonly ShoulderFlightHeight flightHeight = new();
    private float originalDistance, entryTime, blend;
    private long? saveDistanceAt;
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
        try
        {
            inputUpdate = Service.GameInteropProvider.HookFromAddress<Camera.Delegates.UpdateState>(
                (nint)((Camera*)camera)->VirtualTable->UpdateState, UpdateCameraInput);
            inputUpdate.Enable();
            gate.Enable();
        }
        catch { inputUpdate?.Dispose(); gate.Dispose(); throw; }
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

    private void UpdateCameraInput(Camera* camera)
    {
        var scope = default(ShoulderWheelScope);
        if (Active && (GameCamera*)camera == owner && owner->mode == 1)
        {
            try
            {
                // A real perspective toggle must see the original lens bounds
                // when the game saves the third-person view on its way out.
                if (InWorld() && !InputManager.Instance()->GetInputStatus(TogglePerspective))
                    scope = new ShoulderWheelScope(camera);
            }
            catch (Exception ex) { Stop(ex); }
        }
        try { inputUpdate.Original(camera); }
        finally { scope.Restore(camera); }
    }

    private byte OnPermission(GameCamera* camera)
    {
        var native = gate.Original(camera);
        if (native == 0 || Failed || camera != owner) return native;
        try
        {
            if (!InWorld()) return native;
            if (!InputManager.Instance()->GetInputStatus(TogglePerspective))
            {
                // This permission also gates scroll-to-first-person at minimum
                // zoom. Ordinary wheel distance is calculated separately.
                return Active && camera->mode == 1 && (camera->currentZoom <= camera->minZoom + 0.0001f ||
                    camera->controlType == 3) ? (byte)0 : native;
            }
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
                zoom.Begin(originalDistance, Config.ShoulderDistance);
                UpdateDistance();
                rig.Reset();
            }
            else if (wasActive && !Active)
            {
                if (camera->mode == 1) UpdateDistance();
                RestoreZoom();
            }
            return allow ? native : (byte)0;
        }
        catch (Exception ex) { Stop(ex); return native; }
    }

    private bool ValidZoomBounds() => float.IsFinite(owner->currentZoom) && float.IsFinite(owner->minZoom) &&
        float.IsFinite(owner->maxZoom) && owner->minZoom >= 0 && owner->maxZoom > 0 && owner->minZoom <= owner->maxZoom;

    private void UpdateDistance()
    {
        if (!ValidZoomBounds()) return;
        owner->currentZoom = zoom.Update(owner->currentZoom, owner->minZoom, owner->maxZoom,
            Config.ShoulderMinDistance, Config.ShoulderMaxDistance, blend);
        if (Config.ShoulderDistance == zoom.Selected) return;
        Config.ShoulderDistance = zoom.Selected;
        saveDistanceAt = Environment.TickCount64 + 450;
    }

    private void SaveDistance()
    {
        if (!saveDistanceAt.HasValue) return;
        try
        {
            Config.Save();
            saveDistanceAt = null;
        }
        catch (Exception ex)
        {
            saveDistanceAt = Environment.TickCount64 + 5000;
            Service.Log.Warning(ex, "Could not save the shoulder distance.");
        }
    }

    internal void Update()
    {
        if (saveDistanceAt <= Environment.TickCount64) SaveDistance();
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
            UpdateDistance();
            rig.Follow(Service.ObjectTable.LocalPlayer!.Position, dt,
                Config.ShoulderLag, Config.ShoulderMaxLag);
        }
        catch (Exception ex) { Stop(ex); }
    }

    internal void OffsetPivot(GameCamera* camera, nint target, Vector3* position)
    {
        if (!Active || camera != owner || camera->mode != 1 || position == null) return;
        try
        {
            if (!InWorld()) return;
            // The native camera reads its requested distance immediately after
            // this callback, including wheel input later than the framework tick.
            UpdateDistance();
            var player = Service.ObjectTable.LocalPlayer!;
            if (actor != player.Address) return;
            var mount = ((NativeCharacter*)actor)->Mount.MountObject;
            var flying = Service.Condition[ConditionFlag.InFlight];
            if (target != actor && !(flying && mount != null && target == (nint)mount)) return;
            if (!float.IsFinite(position->X) || !float.IsFinite(position->Y) || !float.IsFinite(position->Z)) return;
            var framework = Framework.Instance();
            if (framework != null)
            {
                var root = player.Position;
                var headY = flying ? ReadRiderHeadHeight(root) : null;
                var correctedY = flightHeight.Resolve(framework->FrameCounter, framework->FrameDeltaTime,
                    (nint)mount, flying, root.Y, headY, position->Y);
                position->Y = float.Lerp(position->Y, correctedY, blend);
            }
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

    private float? ReadRiderHeadHeight(Vector3 root)
    {
        // Attachment 1 belongs to the rider even when the native orbit targets
        // the mount. A stale attachment after a teleport cannot seed the filter.
        var read = Common.getWorldBonePosition.Invoke;
        var head = Vector3.Zero;
        if (read != null && read((GameObject*)actor, 1, &head) && Vector3.DistanceSquared(head, root) <= 32 * 32)
            return head.Y;
        if (CalibrationViewpoint.TryRead(Vector3.Zero, out var rider) && Vector3.DistanceSquared(rider.Position, root) <= 32 * 32)
            return rider.Position.Y;
        return null;
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
        flightHeight.Reset();
        // Flush on the next framework tick, outside the native permission call.
        if (saveDistanceAt.HasValue) saveDistanceAt = Environment.TickCount64;
    }

    internal void Reset()
    {
        if (Active && actor != 0 && InWorld() && owner->mode == 1 &&
            Service.ObjectTable.LocalPlayer?.Address == actor)
            UpdateDistance();
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
        inputUpdate.Dispose();
        gate.Dispose();
        Reset();
        SaveDistance();
    }
}
