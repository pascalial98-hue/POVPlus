using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Hooking;
using FFXIVClientStructs.FFXIV.Client.Graphics.Render;
using FFXIVClientStructs.Havok.Animation.Rig;
using FFXIVClientStructs.Havok.Common.Base.Math.QsTransform;
using Hypostasis.Game;

namespace PovPlus;

internal sealed unsafe class FirstPersonHeadVisibility : IDisposable
{
    // Render preparation hook used by Customize+ for model-space bone transforms.
    // Source: Aether-Tools/CustomizePlus, Core/Data/Constants.cs (RenderHookAddress).
    internal const string RenderSignature = "E8 ?? ?? ?? ?? 48 81 C3 ?? ?? ?? ?? BF ?? ?? ?? ?? 33 ED";
    private delegate nint RenderDelegate(nint a1, nint a2, nint a3, int a4);
    private readonly Hook<RenderDelegate> hook;
    private readonly HeadPosePatch patch = new();
    private readonly Dictionary<int, BonePlan> plans = new();
    private readonly HeadHideTransition transition = new();
    private nint transitionActor;
    private nint transitionModel;
    private nint transitionSkeleton;
    private bool rendering;
    private bool failed;
    private nint actorAddress;
    private nint modelAddress;
    private nint skeletonAddress;
    private Matrix4x4 detailToWorld;
    private Vector3 detailEye;
    private FirstPersonHeadShadow? headShadow;
    private bool shadowUnavailable;
    internal string ShadowStatus => shadowUnavailable ? L.T("完整头影试验不可用，已使用原骨骼隐藏方式。") : "";
    internal bool Failed => failed;

    private sealed record BonePlan(nint Skeleton, nint Names, nint Parents, int Count,
        bool Attached, bool[] Hidden, int Anchor, HeadDetailKind[] Details, float[] Visibility);

    internal FirstPersonHeadVisibility()
    {
        hook = Service.GameInteropProvider.HookFromAddress<RenderDelegate>(
            Service.SigScanner.ScanText(RenderSignature), Render);
        try { hook.Enable(); }
        catch { hook.Dispose(); throw; }
    }

    private nint Render(nint a1, nint a2, nint a3, int a4)
    {
        if (rendering || failed) return hook.Original(a1, a2, a3, a4);
        rendering = true;
        try
        {
            Restore();
            headShadow?.ClearTargets();
            try
            {
                if (IsAllowedView() && (Common.CameraManager->worldCamera->mode == 0 || transition.IsHidden))
                    HideHead();
                else
                {
                    plans.Clear();
                    ResetTransition();
                }
            }
            catch (Exception ex)
            {
                headShadow?.ClearTargets();
                Restore();
                failed = true;
                Service.Log.Error(ex, "POV+ head hiding stopped after an error; camera remains usable.");
            }
            try { return hook.Original(a1, a2, a3, a4); }
            catch { headShadow?.ClearTargets(); Restore(); throw; }
        }
        finally
        {
            rendering = false;
        }
    }

    private void HideHead()
    {
        var player = Service.ObjectTable.LocalPlayer;
        if (player == null) return;
        var model = POVCameraFunctionality.GetCharacterBase(player);
        if (model == null || model->Skeleton == null) return;
        var skeleton = model->Skeleton;
        if (skeleton->PartialSkeletons == null || skeleton->PartialSkeletonCount is 0 or > 32) return;
        actorAddress = player.Address;
        modelAddress = (nint)model;
        skeletonAddress = (nint)skeleton;
        var basePose = skeleton->PartialSkeletons[0].GetHavokPose(0);
        var basePlan = GetPlan(0, basePose, false);
        if (basePlan == null || basePlan.Anchor < 0 || basePose->ModelInSync == 0 ||
            basePose->ModelPose.Data == null || basePose->ModelPose.Length != basePlan.Count) return;
        if (!ShouldHideNow(model, player.Address, basePose->ModelPose[basePlan.Anchor])) return;
        if (Plugin.P.Configuration.FirstPersonFullHeadShadow && !shadowUnavailable && headShadow?.Select(model) == true) return;
        var anchor = basePose->ModelPose[basePlan.Anchor].Translation;
        ApplyPlan(basePose, basePlan, anchor);
        for (var partial = 1; partial < skeleton->PartialSkeletonCount; partial++)
        {
            var part = skeleton->PartialSkeletons + partial;
            var parent = part->ConnectedParentBoneIndex;
            var attachedToHead = parent >= 0 && parent < basePlan.Hidden.Length && basePlan.Hidden[parent];
            var pose = part->GetHavokPose(0);
            var plan = GetPlan(partial, pose, attachedToHead);
            if (plan != null) ApplyPlan(pose, plan, anchor);
        }
    }

    private void ApplyPlan(hkaPose* pose, BonePlan plan,
        FFXIVClientStructs.Havok.Common.Base.Math.Vector.hkVector4f anchor)
    {
        if (pose == null || pose->ModelInSync == 0 || pose->ModelPose.Data == null || pose->ModelPose.Length != plan.Count) return;
        var config = Plugin.P.Configuration;
        var enabled = (config.FirstPersonPreserveHair ? HeadDetailKind.Hair : HeadDetailKind.None) |
            (config.FirstPersonPreserveEars ? HeadDetailKind.Ears : HeadDetailKind.None);
        for (var i = 0; i < plan.Count; i++)
        {
            plan.Visibility[i] = 0;
            if (!plan.Hidden[i] || (plan.Details[i] & enabled) == 0) continue;
            var t = pose->ModelPose[i].Translation;
            var world = Vector3.Transform(new Vector3(t.X, t.Y, t.Z), detailToWorld);
            plan.Visibility[i] = HeadDetails.Visibility(Vector3.Distance(detailEye, world), config.FirstPersonDetailHideRadius);
        }
        patch.Apply(pose, plan.Hidden, anchor, plan.Visibility);
    }

    private bool ShouldHideNow(FFXIVClientStructs.FFXIV.Client.Graphics.Scene.CharacterBase* model,
        nint actor, hkQsTransformf head)
    {
        var world = Common.CameraManager->worldCamera;
        var camera = (FFXIVClientStructs.FFXIV.Client.Game.CameraBase*)world;
        var scenes = FFXIVClientStructs.FFXIV.Client.Graphics.Scene.CameraManager.Instance();
        if (scenes == null || scenes->CurrentCamera != &camera->SceneCamera)
        {
            ResetTransition();
            return false;
        }
        if (transitionActor != actor || transitionModel != (nint)model ||
            transitionSkeleton != (nint)model->Skeleton)
        {
            transition.Reset();
            transitionActor = actor;
            transitionModel = (nint)model;
            transitionSkeleton = (nint)model->Skeleton;
        }

        var config = Plugin.P.Configuration;
        var skeleton = model->Skeleton;
        // Same scale field and transform order used by the original Beta camera.
        var scale = new Vector3(model->Scale.X, model->Scale.Y, model->Scale.Z) * ((float*)model)[169];
        var toWorld = Matrix4x4.CreateScale(scale) *
            Matrix4x4.CreateFromQuaternion(Quaternion.Normalize(skeleton->Transform.Rotation)) *
            Matrix4x4.CreateTranslation(skeleton->Transform.Position);
        var localHead = new Vector3(head.Translation.X, head.Translation.Y, head.Translation.Z);
        var rotation = Quaternion.Normalize(new Quaternion(head.Rotation.X, head.Rotation.Y, head.Rotation.Z, head.Rotation.W));
        var offset = new Vector3(config.SETTINGS_CameraOffsetX, config.SETTINGS_CameraOffsetY, config.SETTINGS_CameraOffsetZ);
        var target = FirstPersonMath.HeadCameraPosition(localHead, rotation, toWorld, offset);
        target += POVCameraFunctionality.AppliedLookDownLean(world, actor, (nint)model, (nint)skeleton);
        Vector3 eye = camera->SceneCamera.Position;
        detailToWorld = toWorld;
        detailEye = eye;
        var headWorld = Vector3.Transform(localHead, toWorld);
        var largestScale = MathF.Max(MathF.Abs(scale.X), MathF.Max(MathF.Abs(scale.Y), MathF.Abs(scale.Z)));
        // Custom camera offsets can place the destination beyond the head. Hide
        // before the camera crosses the head even if it is not near that destination.
        var clearance = 0.18f * (float.IsFinite(largestScale) ? Math.Clamp(largestScale, 0.1f, 10f) : 1f);
        return transition.Update(world->mode == 0, config.FirstPersonDelayHeadHide, Vector3.Distance(eye, target),
            Vector3.Distance(eye, headWorld), config.FirstPersonHeadHideDistance, clearance, config.FirstPersonHeadShowDistance);
    }

    internal void UpdateState()
    {
        // Third person must keep the latch alive while its camera is retreating.
        // Disable/GPose/logout/another camera still restore normal appearance.
        var allowed = IsAllowedView();
        if (!allowed) { ResetTransition(); headShadow?.ClearTargets(); }
        var wanted = !failed && allowed && Plugin.P.Configuration.FirstPersonFullHeadShadow &&
            (Common.CameraManager->worldCamera->mode == 0 || transition.IsHidden);
        // Change hook state on the framework thread, never while inside the
        // render-preparation detour. Off/ordinary third person incurs no new
        // per-material managed detour after the exit transition finishes.
        if (!wanted) headShadow?.ClearTargets();
        if (shadowUnavailable) return;
        try
        {
            if (wanted && headShadow == null) headShadow = new FirstPersonHeadShadow();
            headShadow?.SetEnabled(wanted);
        }
        catch (Exception ex)
        {
            headShadow?.ClearTargets();
            shadowUnavailable = true;
            Service.Log.Error(ex, "POV+ full head-shadow experiment unavailable; retaining bone hiding.");
        }
    }

    private static bool IsAllowedView()
    {
        var config = Plugin.P.Configuration;
        var manager = Common.CameraManager;
        if (!config.Setting_ModEnabled || !config.FirstPersonHideHead || Service.ClientState.IsGPosing ||
            Service.ObjectTable.LocalPlayer == null || manager == null || manager->worldCamera == null ||
            manager->worldCamera->mode is not (0 or 1)) return false;
        var camera = (FFXIVClientStructs.FFXIV.Client.Game.CameraBase*)manager->worldCamera;
        var scenes = FFXIVClientStructs.FFXIV.Client.Graphics.Scene.CameraManager.Instance();
        return scenes != null && scenes->CurrentCamera == &camera->SceneCamera;
    }

    private void ResetTransition()
    {
        transition.Reset();
        transitionActor = transitionModel = transitionSkeleton = 0;
    }

    private BonePlan? GetPlan(int partial, hkaPose* pose, bool attached)
    {
        if (pose == null || pose->Skeleton == null) return null;
        var skeleton = pose->Skeleton;
        var count = skeleton->Bones.Length;
        if (count is <= 0 or > 2048 || skeleton->Bones.Data == null ||
            skeleton->ParentIndices.Data == null || skeleton->ParentIndices.Length != count) return null;
        if (plans.TryGetValue(partial, out var cached) && cached.Skeleton == (nint)skeleton &&
            cached.Names == (nint)skeleton->Bones.Data && cached.Parents == (nint)skeleton->ParentIndices.Data &&
            cached.Count == count && cached.Attached == attached) return cached;
        var names = new string[count];
        var parents = new short[count];
        var head = -1;
        for (var i = 0; i < count; i++)
        {
            names[i] = skeleton->Bones[i].Name.String;
            parents[i] = skeleton->ParentIndices[i];
            if (names[i] == "j_kao") head = i;
        }
        var plan = new BonePlan((nint)skeleton, (nint)skeleton->Bones.Data, (nint)skeleton->ParentIndices.Data,
            count, attached, FirstPersonMath.HeadBones(names, parents, attached), head,
            HeadDetails.Classify(names, parents), new float[count]);
        plans[partial] = plan;
        return plan;
    }

    public void Dispose()
    {
        hook.Disable();
        headShadow?.Dispose();
        headShadow = null;
        Restore();
        hook.Dispose();
        plans.Clear();
        ResetTransition();
    }

    internal void Restore()
    {
        var player = Service.ObjectTable.LocalPlayer;
        if (player != null && player.Address == actorAddress)
        {
            var model = POVCameraFunctionality.GetCharacterBase(player);
            if (model != null && (nint)model == modelAddress && (nint)model->Skeleton == skeletonAddress)
            {
                var skeleton = model->Skeleton;
                if (skeleton != null && skeleton->PartialSkeletons != null && skeleton->PartialSkeletonCount is > 0 and <= 32)
                    for (var i = 0; i < skeleton->PartialSkeletonCount; i++)
                        patch.Restore(skeleton->PartialSkeletons[i].GetHavokPose(0));
            }
        }
        // Model redraw / logout may have freed old data. Discard it without dereferencing.
        patch.Clear();
        actorAddress = modelAddress = skeletonAddress = 0;
    }

    internal bool TryGetOriginal(hkaPose* pose, int index, out hkQsTransformf original) =>
        patch.TryGetOriginal(pose, index, out original);
}
