using System;
using System.Diagnostics;
using System.Numerics;
using Dalamud.Hooking;
using FFXIVClientStructs.Havok.Animation.Rig;
using Hypostasis.Game;
using Hypostasis.Game.Structures;
using SceneCamera = FFXIVClientStructs.FFXIV.Client.Graphics.Scene.Camera;
using NativeCamera = FFXIVClientStructs.FFXIV.Client.Game.Camera;

namespace PovPlus;

internal static unsafe class BodyRelativeLook
{
    private static Hook<SceneCamera.Delegates.UpdateRender>? renderHook;
    private static nint actor, model, skeleton;
    private static int spine = -1;
    private static Quaternion reference;
    private static readonly BodyPoseMotionFilter motionFilter = new();
    private static long lastSample;
    private static bool sampled;
    internal static bool Active { get; private set; }
    internal static int Generation { get; private set; }
    internal static Quaternion WorldRotation { get; private set; } = Quaternion.Identity;
    internal static string Status => renderHook == null
        ? L.T("身体姿态视角不可用；其他第一人称功能仍可使用，请查看 Dalamud 日志。") : "";

    internal static void Initialize(GameCamera* camera)
    {
        Reset();
        try
        {
            var scene = &((NativeCamera*)camera)->SceneCamera;
            renderHook = Service.GameInteropProvider.HookFromAddress<SceneCamera.Delegates.UpdateRender>(
                (nint)scene->VirtualTable->UpdateRender, UpdateRender);
            renderHook.Enable();
        }
        catch (Exception ex)
        {
            renderHook?.Dispose();
            renderHook = null;
            Service.Log.Error(ex, "POV+ body-relative view unavailable.");
        }
    }

    internal static void Reset()
    {
        Generation++;
        Active = sampled = false;
        actor = model = skeleton = 0;
        spine = -1;
        lastSample = 0;
        motionFilter.Reset();
        WorldRotation = Quaternion.Identity;
    }

    internal static void BeginPosition(GameCamera* camera)
    {
        if (Common.CameraManager != null && camera == Common.CameraManager->worldCamera) Active = false;
    }

    internal static void Dispose()
    {
        renderHook?.Dispose();
        renderHook = null;
        Reset();
    }

    internal static bool Sample(GameCamera* camera, nint actorAddress, nint modelAddress,
        hkaPose* pose, Quaternion modelWorld, float playerYaw, Vector3 playerPosition,
        Quaternion head, out Quaternion headForFollow)
    {
        headForFollow = head;
        // Shared camera hooks may run for other cameras; don't clear a valid
        // world-camera sample on their behalf.
        if (Common.CameraManager == null || camera != Common.CameraManager->worldCamera) return false;
        Active = false;
        if (renderHook == null || !Plugin.P.Configuration.FirstPersonBodyRelativeLook ||
            !POVCameraFunctionality.FirstPersonViewActive() || ((NativeCamera*)camera)->IsEventCameraAutoControl ||
            (((NativeCamera*)camera)->SceneCamera.Flags & 1) == 0 ||
            pose == null || pose->Skeleton == null || !float.IsFinite(playerYaw) ||
            !BodyPoseMath.Finite(playerPosition) || !BodyPoseMath.TryNormalize(head, out head) ||
            !BodyPoseMath.TryNormalize(modelWorld, out modelWorld)) { Reset(); return false; }
        if (actor != actorAddress || model != modelAddress || skeleton != (nint)pose->Skeleton)
        {
            Reset();
            actor = actorAddress;
            model = modelAddress;
            skeleton = (nint)pose->Skeleton;
            if (!BodyPoseReference.TryFind(pose->Skeleton, out spine, out reference)) return false;
        }
        if (spine < 0 || spine >= pose->ModelPose.Length || pose->ModelPose.Data == null) return false;
        var transform = pose->AccessBoneModelSpace(spine, hkaPose.PropagateOrNot.DontPropagate);
        if (transform == null) return false;
        var q = transform->Rotation;
        if (!BodyPoseMath.TryNormalize(new(q.X, q.Y, q.Z, q.W), out var current)) return false;
        var modelDelta = BodyPoseMath.ModelDelta(current, reference);
        var now = Stopwatch.GetTimestamp();
        var seconds = sampled ? (float)Stopwatch.GetElapsedTime(lastSample, now).TotalSeconds : 0;
        // Strip the RAW torso motion before filtering relative head motion;
        // otherwise the removed stride leaks back through head-follow deltas.
        var frame = motionFilter.Update(modelDelta, BodyPoseMath.HeadRelativeToBody(head, modelDelta),
            playerPosition, seconds);
        headForFollow = frame.Head;
        WorldRotation = BodyPoseMath.WorldDelta(frame.Body, modelWorld, playerYaw);
        lastSample = now;
        sampled = Active = true;
        return true;
    }

    private static void UpdateRender(SceneCamera* scene)
    {
        var manager = Common.CameraManager;
        var camera = manager == null ? null : manager->worldCamera;
        if (!Active || !Plugin.P.Configuration.FirstPersonBodyRelativeLook || camera == null ||
            scene != &((NativeCamera*)camera)->SceneCamera || !POVCameraFunctionality.FirstPersonViewActive() ||
            ((NativeCamera*)camera)->IsEventCameraAutoControl ||
            Stopwatch.GetElapsedTime(lastSample).TotalSeconds > 0.25)
        {
            renderHook!.Original(scene);
            return;
        }
        var scope = new BodyViewScope(scene, camera->GameObjectHRotation, WorldRotation);
        try { renderHook!.Original(scene); }
        finally { scope.Restore(scene); }
    }
}
