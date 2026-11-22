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
    private readonly ShoulderCycle cycle;
    private readonly ShoulderRig rig = new();
    private readonly ShoulderZoom zoom = new();
    private readonly ShoulderSpeedZoom speedZoom = new();
    private readonly ShoulderFlightMotion flight = new();
    private readonly ShoulderMountAnchor mountAnchor = new();
    private CameraValueOverride orbitFloor;
    private float originalDistance, entryTime, zoomTime, blend, mountWeight;
    private float combatPhase;
    private Vector3 combatOffset;
    private bool riding, processingWheel;
    private ShoulderProfile Profile => riding ? Config.MountedShoulder : Config.WalkingShoulder;
    private long? saveDistanceAt;
    private uint? motionFrame;
    private nint actor;
    internal bool Active => cycle.Active && actor != 0 && !Failed;
    internal bool Failed { get; private set; }
    private static Configuration Config => Plugin.P.Configuration;

    internal ShoulderView(GameCamera* camera, ShoulderCycle cycle)
    {
        this.cycle = cycle;
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

    private static bool ChangingArea => Service.Condition[ConditionFlag.BetweenAreas] || Service.Condition[ConditionFlag.BetweenAreas51];

    private bool InWorld()
    {
        if (!Config.Setting_ModEnabled || !Config.ShoulderCycleEnabled || Plugin.P.CalibrationActive ||
            !SameCamera() || owner->mode is not (0 or 1) || ((Camera*)owner)->IsEventCameraAutoControl ||
            Service.ClientState.IsGPosing || Service.ObjectTable.LocalPlayer == null) return false;
        var c = Service.Condition;
        if (ChangingArea || c[ConditionFlag.WatchingCutscene] ||
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
                {
                    UpdateDistance();
                    scope = new ShoulderWheelScope(camera);
                    processingWheel = true;
                }
            }
            catch (Exception ex) { Stop(ex); }
        }
        try { inputUpdate.Original(camera); }
        finally
        {
            scope.Restore(camera);
            if (processingWheel)
            {
                processingWheel = false;
                try { if (Active && InWorld() && owner->mode == 1) UpdateDistance(wheel: true); }
                catch (Exception ex) { Stop(ex); }
            }
        }
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
            var wasActive = cycle.Active;
            var allow = cycle.AllowToggle(framework->FrameCounter, camera->mode == 1);
            if (!wasActive && cycle.Active) Attach();
            else if (wasActive && !cycle.Active && actor != 0)
            {
                RestoreZoom();
            }
            return allow ? native : (byte)0;
        }
        catch (Exception ex) { Stop(ex); return native; }
    }

    private bool ValidZoomBounds() => float.IsFinite(owner->currentZoom) && float.IsFinite(owner->minZoom) &&
        float.IsFinite(owner->maxZoom) && owner->minZoom >= 0 && owner->maxZoom > 0 && owner->minZoom <= owner->maxZoom;

    private static bool IsRiding() => Service.Condition[ConditionFlag.Mounted] || Service.Condition[ConditionFlag.RidingPillion];

    private void Attach()
    {
        actor = Service.ObjectTable.LocalPlayer!.Address;
        riding = IsRiding();
        mountWeight = riding ? 1 : 0;
        originalDistance = owner->currentZoom;
        entryTime = zoomTime = 0;
        blend = ShoulderRig.EntryBlend(0, Config.ShoulderEntryTime);
        zoom.Begin(originalDistance, Profile.Distance);
        UpdateDistance();
        rig.Reset();
    }

    private void UpdateDistance(bool wheel = false)
    {
        if (!ValidZoomBounds()) return;
        var profile = Profile;
        var distanceOffset = combatOffset.Z + speedZoom.Offset;
        if (!processingWheel)
        {
            var range = ShoulderZoom.Range(profile.MinDistance, profile.MaxDistance);
            ShoulderZoom.ApplyFloor(ref orbitFloor, ref owner->minZoom, ShoulderZoom.PhysicalDistance(range.Min, distanceOffset));
        }
        var inputDistance = owner->currentZoom;
        if (!wheel && !processingWheel && inputDistance != zoom.LastWritten)
        {
            // A new territory's preset can arrive after our first world tick.
            // Keep it for returning to third person, outside the wheel memory.
            originalDistance = inputDistance;
            inputDistance = zoom.LastWritten;
        }
        owner->currentZoom = zoom.Update(inputDistance, owner->minZoom, owner->maxZoom,
            profile.MinDistance, profile.MaxDistance, ShoulderRig.EntryBlend(zoomTime, Config.ShoulderEntryTime), distanceOffset);
        if (profile.Distance == zoom.Selected) return;
        profile.Distance = zoom.Selected;
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
        if (!cycle.Active || Failed)
        {
            if (actor != 0) RestoreZoom();
            return;
        }
        try
        {
            if (!InWorld())
            {
                // The destination can replace both the local actor and the world
                // camera. Keep only the mode selection while no player camera is ready.
                Suspend();
                return;
            }
            if (owner->mode != 1)
            {
                Reset();
                return;
            }
            var player = Service.ObjectTable.LocalPlayer!;
            if (actor != player.Address)
            {
                if (actor != 0) RestoreZoom(preserveExternal: true);
                if (!ValidZoomBounds()) return;
                Attach();
            }
            var elapsed = (float)Service.Framework.UpdateDelta.TotalSeconds;
            var dt = ShoulderRig.Bound(elapsed, 0, 0.25f, 0);
            var mountedNow = IsRiding();
            if (riding != mountedNow)
            {
                riding = mountedNow;
                zoom.Begin(owner->currentZoom, Profile.Distance);
                zoomTime = 0;
            }
            entryTime += dt;
            zoomTime += dt;
            blend = ShoulderRig.EntryBlend(entryTime, Config.ShoulderEntryTime);
            mountWeight = Math.Clamp(mountWeight + (riding ? dt : -dt) / 0.22f, 0, 1);
            combatOffset = Config.CombatShoulder.Advance(ref combatPhase, Service.Condition[ConditionFlag.InCombat], dt, riding);
            speedZoom.Update(player.Position, elapsed, riding, Profile);
            if (riding && Config.ShoulderFlightMotionEnabled && !Service.Condition[ConditionFlag.Diving])
                flight.Update(player.Position, elapsed, Service.Condition[ConditionFlag.InFlight],
                    Config.ShoulderFlightSwayPercent, Config.ShoulderFlightBankDegrees);
            else flight.Reset();
            UpdateDistance();
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
            if (target != actor && !(riding && mount != null && target == (nint)mount)) return;
            if (!float.IsFinite(position->X) || !float.IsFinite(position->Y) || !float.IsFinite(position->Z)) return;
            var framework = Framework.Instance();
            var foot = Config.WalkingShoulder;
            var mounted = Config.MountedShoulder;
            if (framework != null)
            {
                var root = player.Position;
                // Sample motion from the same camera evaluation that consumes
                // the offset. A second pivot query in this frame reuses the sample.
                if (motionFrame != framework->FrameCounter)
                {
                    motionFrame = framework->FrameCounter;
                    rig.Follow(root, framework->FrameDeltaTime,
                        float.Lerp(foot.Lag, mounted.Lag, mountWeight), float.Lerp(foot.MaxLag, mounted.MaxLag, mountWeight),
                        camera->currentHRotation, float.Lerp(foot.VerticalLagPercent, mounted.VerticalLagPercent, mountWeight),
                        mountWeight, mounted.RecoverySeconds, mounted.TurnInertiaPercent);
                }
                var head = riding ? ReadRiderHead(root) : null;
                var anchor = mountAnchor.Resolve(framework->FrameCounter, framework->FrameDeltaTime,
                    (nint)mount, riding, root, head, *position);
                *position = Vector3.Lerp(*position, anchor, blend);
            }
            var offset = blend * rig.Offset(camera->currentHRotation,
                float.Lerp(Config.ShoulderSide, mounted.Side, mountWeight), float.Lerp(Config.ShoulderHeight, mounted.Height, mountWeight),
                new Vector2(combatOffset.X, combatOffset.Y) + flight.Sway);
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

    internal Vector3 FlightBank(GameCamera* camera) =>
        flight.Bank != Vector3.Zero && camera == owner && Active && riding && camera->mode == 1 && InWorld()
            ? flight.Bank * blend : Vector3.Zero;

    private Vector3? ReadRiderHead(Vector3 root)
    {
        // Attachment 1 belongs to the rider even when the native orbit targets
        // the mount. A stale attachment after a teleport cannot seed the filter.
        var read = Common.getWorldBonePosition.Invoke;
        var head = Vector3.Zero;
        if (read != null && read((GameObject*)actor, 1, &head) && Vector3.DistanceSquared(head, root) <= 32 * 32)
            return head;
        if (CalibrationViewpoint.TryRead(Vector3.Zero, out var rider) && Vector3.DistanceSquared(rider.Position, root) <= 32 * 32)
            return rider.Position;
        return null;
    }

    private void RestoreZoom(bool preserveExternal = false)
    {
        if (actor != 0 && SameCamera())
        {
            orbitFloor.Restore(ref owner->minZoom);
            if (owner->mode == 1 && ValidZoomBounds() && (!preserveExternal || owner->currentZoom == zoom.LastWritten))
                owner->currentZoom = Math.Clamp(originalDistance, owner->minZoom, owner->maxZoom);
            else if (owner->mode == 0) ((Camera*)owner)->SavedDistance = originalDistance;
        }
        actor = 0;
        motionFrame = null;
        combatPhase = 0;
        combatOffset = Vector3.Zero;
        speedZoom.Reset();
        flight.Reset();
        orbitFloor = default;
        rig.Reset();
        mountAnchor.Reset();
        // Flush on the next framework tick, outside the native permission call.
        if (saveDistanceAt.HasValue) saveDistanceAt = Environment.TickCount64;
    }

    internal void Suspend()
    {
        if (actor != 0) RestoreZoom(preserveExternal: true);
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
        inputUpdate.Dispose();
        gate.Dispose();
        RestoreZoom(preserveExternal: true);
        SaveDistance();
    }
}
