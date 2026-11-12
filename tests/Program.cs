using PovPlus;
using FFXIVClientStructs.Havok.Animation.Rig;
using FFXIVClientStructs.Havok.Common.Base.Math.QsTransform;
using FFXIVClientStructs.Havok.Common.Base.Math.Vector;

internal static partial class Program
{
    private static int assertions;
    private static void Check(bool value, string name)
    {
        assertions++;
        if (!value) throw new Exception(name);
    }

    private static bool Near(float a, float b) => MathF.Abs(a - b) < 0.0001f;

    private static unsafe void Main()
    {
        var r = FirstPersonMath.DegreesToRadians;
        var side = 80 * r;
        Check(Near(FirstPersonMath.ClampYaw(150 * r, 0, side), side), "Right boundary");
        Check(Near(FirstPersonMath.ClampYaw(-150 * r, 0, side), -side), "Left boundary");
        Check(Near(FirstPersonMath.ClampYaw(-179 * r, 179 * r, side), -179 * r), "Wrap through north");
        Check(Near(FirstPersonMath.ClampYaw(179 * r, -179 * r, side), 179 * r), "Reverse wrap through north");
        for (var body = -360; body <= 360; body += 5)
        for (var look = -720; look <= 720; look += 7)
        {
            var result = FirstPersonMath.ClampYaw(look * r, body * r, side);
            Check(MathF.Abs(FirstPersonMath.Wrap(result - body * r)) <= side + 0.00001f, "Body-relative range");
            Check(Near(result, FirstPersonMath.ClampYaw(result, body * r, side)), "Stable at boundary");
        }
        Check(Near(FirstPersonMath.ClampPitch(120 * r, 60 * r, 75 * r), 60 * r), "Look up boundary");
        Check(Near(FirstPersonMath.ClampPitch(-120 * r, 60 * r, 75 * r), -75 * r), "Look down boundary");
        Check(Near(FirstPersonMath.ClampPitch(0, 60 * r, 75 * r), 0), "Horizon");
        Check(Near(FirstPersonMath.Degrees(float.NaN, 80, 85), 80 * r), "Corrupt config fallback");
        Check(Near(FirstPersonMath.Degrees(400, 80, 85), 85 * r), "Prevent config bypass");
        Check(float.IsFinite(FirstPersonMath.ClampYaw(float.NaN, 0, side)), "Non-finite yaw");

        var soft = new SoftLookLimit();
        var band = 12 * r;
        Check(Near(soft.Apply(0, 0, -side, side, band), 0), "Soft limit seeds without jumping");
        Check(Near(soft.Apply(60 * r, 0, -side, side, band), 60 * r), "Inside soft zone keeps original sensitivity");
        var softened = soft.Apply(75 * r, 0, -side, side, band);
        Check(softened > 68 * r && softened < 75 * r, "Outward turn slows near boundary");
        for (var i = 0; i < 50; i++) Check(Near(soft.Apply(softened, 0, -side, side, band), softened), "Multiple callbacks do not drift");
        Check(Near(soft.Apply(softened - 2 * r, 0, -side, side, band), softened - 2 * r), "Inward turn immediately has full sensitivity");
        soft.Reset();
        Check(Near(soft.Apply(120 * r, 0, -side, side, band), side), "Entry beyond edge uses hard protection");
        Check(Near(soft.Apply(side + 2 * r, 0, -side, side, band), side), "Outward pressure cannot exceed edge");
        Check(Near(soft.Apply(side - 1 * r, 0, -side, side, band), side - 1 * r), "No accumulated input debt at edge");
        soft.Reset();
        soft.Apply(179 * r, 179 * r, -side, side, band);
        Check(Near(soft.Apply(-179 * r, 179 * r, -side, side, band), -179 * r), "Soft yaw crosses world wrap");
        soft.Reset();
        soft.Apply(75 * r, 0, -side, side, band);
        Check(Near(soft.Apply(75 * r, 20 * r, -side, side, band), 75 * r), "Body turning under unchanged camera creates no extra input");
        Check(Math.Abs(FirstPersonMath.Wrap(soft.Apply(75 * r, -80 * r, -side, side, band) + 80 * r)) <= side + 0.00001f,
            "Large body turn still enforces hard limit");
        float OneTurn(int steps, int sign)
        {
            var limiter = new SoftLookLimit();
            var value = limiter.Apply(50 * r * sign, 0, -side, side, band);
            for (var j = 0; j < steps; j++) value = limiter.Apply(value + 28 * r * sign / steps, 0, -side, side, band);
            return value;
        }
        foreach (var steps in new[] { 1, 2, 30, 60, 144, 360 })
        {
            Check(Near(OneTurn(steps, 1), OneTurn(1, 1)), "Soft response independent of callback/input rate");
            Check(Near(OneTurn(steps, -1), -OneTurn(1, 1)), "Soft response symmetric on left and right");
        }
        soft.Reset();
        soft.Apply(-65 * r, 0, -75 * r, 60 * r, band);
        var downSoft = soft.Apply(-70 * r, 0, -75 * r, 60 * r, band);
        Check(downSoft < -65 * r && downSoft > -70 * r, "Asymmetric pitch lower edge softened");
        Check(Near(soft.Apply(90 * r, 0, -75 * r, 60 * r, 0), 60 * r), "Disabling soft boundary immediately restores hard clamp");

        var kinds = HeadDetails.Classify(["j_kubi", "j_kao", "j_kami_a", "custom_strand", "j_mimi_l", "earring", "j_ago", "collar"],
            [-1, 0, 1, 2, 1, 4, 1, 0]);
        Check(kinds.SequenceEqual(new[] { HeadDetailKind.None, HeadDetailKind.None, HeadDetailKind.Hair, HeadDetailKind.Hair,
            HeadDetailKind.Ears, HeadDetailKind.Ears, HeadDetailKind.None, HeadDetailKind.None }), "Only recognized hair/ear branches qualify");
        Check(HeadDetails.Classify(["custom_a", "custom_b"], [1, 0]).All(k => k == HeadDetailKind.None), "Detail classification tolerates cycles");
        Check(HeadDetails.Classify(["custom_a"], [900])[0] == HeadDetailKind.None, "Detail classification tolerates invalid parents");
        Check(HeadDetails.Classify(["j_kao", "j_kami_a"], [1, -1])[0] == HeadDetailKind.None, "Face root never qualifies through a custom parent");
        Check(HeadDetails.Classify(["j_kao", "j_ex_h0123_ke_a", "j_zerc_a_l", "n_ear_a_r", "j_ex_met_a"], [-1, 0, 0, 0, 0])
            .SequenceEqual(new[] { HeadDetailKind.None, HeadDetailKind.Hair, HeadDetailKind.Ears, HeadDetailKind.Ears, HeadDetailKind.None }),
            "Extended hairstyles, Viera ears and earrings recognized; helmet bones stay hidden");
        Check(HeadDetails.Visibility(0.1f, 0.25f) == 0 && HeadDetails.Visibility(0.25f, 0.25f) == 0,
            "Close detail bones stay fully hidden");
        Check(Near(HeadDetails.Visibility(0.35f, 0.25f), 0.5f) && Near(HeadDetails.Visibility(0.5f, 0.25f), 1),
            "Distant details blend to original pose");
        Check(HeadDetails.Visibility(float.NaN, 0.25f) == 0, "Unmeasurable detail stays hidden");

        Check(NearVfxMath.IsSelf(10, 10, 10), "Own self-targeted effect qualifies");
        Check(!NearVfxMath.IsSelf(0, 0, 0) && !NearVfxMath.IsSelf(10, 20, 10) && !NearVfxMath.IsSelf(10, 10, 20) &&
            !NearVfxMath.IsSelf(10, 10, 0), "Unknown, incoming, outgoing and untargeted effects excluded");
        Check(Near(NearVfxMath.Factor(0, 1.2f, 0.25f), 0.25f), "Closest self effect retains configured minimum");
        Check(NearVfxMath.Factor(1.2f, 1.2f, 0.25f) == 1 && NearVfxMath.Factor(100, 1.2f, 0.25f) == 1,
            "Distant self effects retain full strength");
        Check(NearVfxMath.Factor(float.NaN, 1, 0.25f) == 1 && NearVfxMath.Factor(-1, 1, 0.25f) == 1,
            "Unknown VFX distance is left unchanged");
        var previousFactor = 0f;
        for (var i = 0; i <= 200; i++)
        {
            var factor = NearVfxMath.Factor(i / 100f, 1.2f, 0.25f);
            Check(factor >= previousFactor && factor <= 1, "VFX attenuation smoothly relaxes with distance");
            previousFactor = factor;
        }
        var vfx = new FFXIVClientStructs.FFXIV.Client.Graphics.Scene.VfxObject();
        vfx.Color = new FFXIVClientStructs.FFXIV.Common.Math.Vector4(0.2f, 0.4f, 0.6f, 0.8f);
        var alphaPatch = new VfxAlphaPatch(&vfx, 0.25f);
        Check(Near(vfx.Color.W, 0.2f) && Near(vfx.Color.X, 0.2f) && Near(vfx.Color.Y, 0.4f) && Near(vfx.Color.Z, 0.6f),
            "VFX patch changes only alpha, respects preexisting transparency");
        alphaPatch.Restore(&vfx);
        Check(Near(vfx.Color.W, 0.8f), "VFX source alpha restored after render");
        alphaPatch = new VfxAlphaPatch(&vfx, 0.25f);
        vfx.Color.W = 0.7f;
        alphaPatch.Restore(&vfx);
        Check(Near(vfx.Color.W, 0.7f), "External VFX alpha change preserved");
        for (var i = 0; i < 120; i++)
        {
            alphaPatch = new VfxAlphaPatch(&vfx, 0.25f);
            alphaPatch.Restore(&vfx);
        }
        Check(Near(vfx.Color.W, 0.7f), "Repeated VFX updates do not compound attenuation");

        string[] names = ["n_root", "j_sebo_c", "j_kubi", "j_kao", "j_ago", "j_kami_a", "j_mimi_l", "j_ude_a_l", "ivcs_extra_head", "j_hiza_l"];
        short[] parents = [-1, 0, 1, 2, 3, 3, 3, 1, 5, 0];
        var mask = FirstPersonMath.HeadBones(names, parents, false);
        Check(mask.SequenceEqual(new[] { false, false, false, true, true, true, true, false, true, false }), "Head descendants hidden; neck, torso and arms preserved");
        Check(FirstPersonMath.HeadBones(["root", "j_kubi", "j_kao", "necklace"], [-1, 0, 1, 1], false)
            .SequenceEqual(new[] { false, false, true, false }), "Neck accessory outside head subtree remains visible");
        Check(!FirstPersonMath.HeadBones(["root", "j_kubi"], [-1, 0], false).Any(x => x), "No fallback to neck hiding if head is missing");
        Check(FirstPersonMath.HeadBones(["hair_root", "hair_tip"], [-1, 0], true).All(x => x), "Attached head partial");
        Check(!FirstPersonMath.HeadBones(["weapon_root", "weapon_tip"], [-1, 0], false).Any(x => x), "Unrelated partial untouched");
        Check(FirstPersonMath.HeadBones(["hair", "j_kao", "root"], [1, 2, -1], false).SequenceEqual(new[] { true, true, false }), "Unsorted hierarchy");
        Check(!FirstPersonMath.HeadBones(["a", "b"], [1, 0], false).Any(x => x), "Cyclic hierarchy terminates");
        Check(!FirstPersonMath.HeadBones(["a"], [400], false).Any(x => x), "Out-of-range parent terminates");

        var transforms = stackalloc hkQsTransformf[names.Length];
        var saved = new hkQsTransformf[names.Length];
        for (var i = 0; i < names.Length; i++)
        {
            transforms[i] = default;
            transforms[i].Translation = new hkVector4f { X = i, Y = i + 1, Z = i + 2, W = 1 };
            transforms[i].Scale = new hkVector4f { X = 1.2f, Y = 0.8f, Z = 1.1f, W = 1 };
            transforms[i].Rotation.W = 1;
            saved[i] = transforms[i];
        }
        var pose = new hkaPose();
        pose.ModelPose.Data = transforms;
        pose.ModelPose.Length = names.Length;
        pose.ModelInSync = 1;
        var patch = new HeadPosePatch();
        var anchor = transforms[3].Translation;
        try
        {
            patch.Apply(&pose, mask, anchor);
            for (var i = 0; i < names.Length; i++)
                if (mask[i])
                {
                    Check(transforms[i].Scale.X == saved[i].Scale.X * HeadPosePatch.HiddenScale &&
                        transforms[i].Scale.Y == saved[i].Scale.Y * HeadPosePatch.HiddenScale &&
                        transforms[i].Scale.Z == saved[i].Scale.Z * HeadPosePatch.HiddenScale, "Hidden bone collapsed without singular scale");
                    Check(transforms[i].Translation.X == anchor.X && transforms[i].Translation.Y == anchor.Y && transforms[i].Translation.Z == anchor.Z, "Common anchor prevents stretched faces");
                }
                else Check(transforms[i].Equals(saved[i]), "Body and neck bone unchanged");
            throw new InvalidOperationException("Simulated render failure");
        }
        catch (InvalidOperationException) { }
        finally { patch.Restore(&pose); patch.Clear(); }
        for (var i = 0; i < names.Length; i++) Check(transforms[i].Equals(saved[i]), "Exact restoration, including custom scales");
        var detailWeights = new float[names.Length];
        detailWeights[5] = 1;
        detailWeights[6] = 0.5f;
        patch.Apply(&pose, mask, anchor, detailWeights);
        Check(transforms[5].Equals(saved[5]), "Fully retained detail remains completely untouched");
        Check(Near(transforms[6].Translation.X, (anchor.X + saved[6].Translation.X) * 0.5f) &&
            Near(transforms[6].Scale.X, saved[6].Scale.X * (1 + HeadPosePatch.HiddenScale) * 0.5f), "Partial detail blends both position and scale");
        Check(transforms[3].Scale.X == saved[3].Scale.X * HeadPosePatch.HiddenScale && transforms[2].Equals(saved[2]),
            "Detail retention leaves head collapsed and neck intact");
        patch.Restore(&pose);
        patch.Clear();
        for (var i = 0; i < names.Length; i++) Check(transforms[i].Equals(saved[i]), "Partial detail transforms restore exactly");
        patch.Restore(&pose);
        patch.Apply(&pose, mask, anchor);
        Check(patch.TryGetOriginal(&pose, 3, out var originalHead) && originalHead.Equals(saved[3]), "Camera reads unhidden head");
        transforms[3].Rotation.X = 0.25f;
        transforms[3].Translation.X = 999f;
        patch.Restore(&pose);
        Check(transforms[3].Translation.X == 999f && transforms[3].Rotation.X == 0.25f && transforms[3].Scale.Equals(saved[3].Scale), "External updates preserved while own scale restored");
        transforms[3] = saved[3];
        patch.Apply(&pose, mask, anchor);
        var replacedPose = new hkaPose();
        patch.Restore(&replacedPose);
        Check(transforms[3].Scale.X == saved[3].Scale.X * HeadPosePatch.HiddenScale, "Different live pose cannot restore stale memory");
        patch.Restore(&pose);
        patch.Clear();
        pose.ModelInSync = 0;
        patch.Apply(&pose, mask, anchor);
        Check(transforms[3].Equals(saved[3]), "Unsynced pose skipped");
        pose.ModelInSync = 1;
        patch.Apply(&pose, new bool[2], anchor);
        Check(transforms[3].Equals(saved[3]), "Changed skeleton size skipped");
        patch.Apply(null, mask, anchor);
        patch.Restore(&pose);
        patch.Clear();
        var nearPatch = new NearPlanePatch();
        var camera = new FFXIVClientStructs.FFXIV.Client.Graphics.Render.Camera { NearPlane = 0.1f };
        nearPatch.Apply(&camera, 0.001f);
        Check(camera.NearPlane == 0.001f, "Near plane supports requested 0.001");
        nearPatch.Apply(&camera, 0.02f);
        nearPatch.Restore(&camera);
        Check(camera.NearPlane == 0.1f, "Changing slider does not overwrite original near plane");
        camera.NearPlane = 0.25f;
        nearPatch.Apply(&camera, 0.001f);
        nearPatch.Restore(&camera);
        Check(camera.NearPlane == 0.25f, "Restore actual original value instead of hard-coded default");
        nearPatch.Apply(&camera, 0.001f);
        camera.NearPlane = 0.3f;
        nearPatch.Restore(&camera);
        Check(camera.NearPlane == 0.3f, "External near-plane change survives restore");
        nearPatch.Apply(&camera, 0.001f);
        camera.NearPlane = 0.4f;
        nearPatch.Apply(&camera, 0.001f);
        nearPatch.Restore(&camera);
        Check(camera.NearPlane == 0.4f, "Updated external baseline retained");
        nearPatch.Apply(&camera, 0.001f);
        var replacementCamera = new FFXIVClientStructs.FFXIV.Client.Graphics.Render.Camera { NearPlane = 0.5f };
        nearPatch.Restore(&replacementCamera);
        Check(replacementCamera.NearPlane == 0.5f && camera.NearPlane == 0.001f, "Camera replacement never dereferences stale address");
        nearPatch.Apply(&replacementCamera, -1f);
        Check(replacementCamera.NearPlane == 0.001f, "Negative config clamped");
        nearPatch.Apply(&replacementCamera, 20f);
        Check(replacementCamera.NearPlane == 1f, "Excessive config clamped");
        foreach (var value in new[] { float.NaN, float.PositiveInfinity, float.NegativeInfinity })
        {
            nearPatch.Apply(&replacementCamera, value);
            Check(replacementCamera.NearPlane == 0.015f, "Non-finite near-plane config uses the new default");
        }
        nearPatch.Restore(&replacementCamera);
        Check(replacementCamera.NearPlane == 0.5f, "Restore after config safeguards");
        nearPatch.Apply(null, 0.001f);
        nearPatch.Restore(null);
        replacementCamera.NearPlane = float.NaN;
        nearPatch.Apply(&replacementCamera, 0.001f);
        Check(float.IsNaN(replacementCamera.NearPlane), "Invalid native camera left untouched");
        var transition = new HeadHideTransition();
        foreach (var distance in new[] { 6f, 3f, 1f, 0.5f, 0.26f })
            Check(!transition.Update(true, true, distance, distance, 0.25f, 0.18f), "Head remains visible during approach");
        Check(transition.Update(true, true, 0.25f, 0.25f, 0.25f, 0.18f), "Head hides at near-arrival threshold");
        Check(transition.Update(true, true, 0.6f, 0.6f, 0.25f, 0.18f), "Head does not flicker back during motion");
        Check(transition.Update(false, true, 0f, 0f, 0.25f, 0.18f), "Switch to third person inside head retains hiding");
        Check(transition.Update(false, true, 5f, 0.3f, 0.25f, 0.18f), "Large target offset cannot release head while camera remains close");
        Check(!transition.Update(false, true, 0f, 0.35f, 0.25f, 0.18f), "Actual head clearance restores visibility at threshold");
        Check(!transition.Update(true, true, 2f, 2f, 0.25f, 0.18f), "Re-entry gets a fresh transition");
        transition.Update(false, true, 1f, 1f, 0.25f, 0.18f);
        Check(!transition.Update(true, true, 1f, 1f, 0.25f, 0.18f), "Cancelled approach does not leave a hidden latch");
        Check(transition.Update(true, true, 0f, 0f, 0.25f, 0.18f), "Already in first person hides immediately");
        transition.Reset();
        Check(transition.Update(true, true, 2f, 0.15f, 0.25f, 0.18f), "Custom offset cannot let camera pass through visible head");
        transition.Reset();
        Check(transition.Update(true, false, 5f, 5f, 0.25f, 0.18f), "Disabling delay retains old immediate behavior");
        transition.Reset();
        Check(transition.Update(true, true, float.NaN, 5f, 0.25f, 0.18f), "Invalid measurement falls back to head protection");
        Check(HeadHideTransition.SanitizeDistance(float.NaN) == 0.25f &&
            HeadHideTransition.SanitizeDistance(-1f) == 0.05f &&
            HeadHideTransition.SanitizeDistance(100f) == 0.75f, "Hide distance configuration bounded");
        Check(HeadHideTransition.SanitizeShowDistance(float.NaN) == 0.35f &&
            HeadHideTransition.SanitizeShowDistance(-1f) == 0.2f &&
            HeadHideTransition.SanitizeShowDistance(100f) == 1f, "Show distance configuration bounded");
        transition.Reset();
        Check(!transition.Update(false, true, 0f, 0f, 0.25f, 0.18f), "Ordinary third person does not start hiding");
        transition.Update(true, true, 0f, 0f, 0.25f, 0.18f);
        Check(transition.Update(false, true, 0.2f, 0.2f, 0.25f, 0.18f), "Retreat keeps hidden state");
        Check(transition.Update(true, true, 1f, 1f, 0.25f, 0.18f), "Reversing retreat into first person has no head flash");
        Check(transition.Update(false, false, 0.2f, 0.2f, 0.25f, 0.18f), "Exit protection independent of entry-delay checkbox");
        Check(transition.Update(false, true, 1f, float.NaN, 0.25f, 0.18f), "Unknown camera distance does not expose hidden head");
        Check(transition.Update(false, true, 1f, 0.6f, 0.25f, 0.18f, 0.7f), "Custom release distance respected");
        Check(!transition.Update(false, true, 0f, 0.7f, 0.25f, 0.18f, 0.7f), "Custom release threshold restores head");
        Check(!transition.Update(false, true, 0f, 0f, 0.25f, 0.18f), "After exit, ordinary camera collision does not re-hide head");
        transition.Update(true, true, 0f, 0f, 0.25f, 0.18f);
        Check(transition.Update(false, true, 1f, 0.5f, 0.25f, 0.54f, 0.35f), "Scaled head clearance takes precedence over smaller release setting");
        Check(!transition.Update(false, true, 1f, 0.6f, 0.25f, 0.54f, 0.35f), "Scaled head released when safely clear");
        transition.Update(true, true, 0f, 0f, 0.25f, 0.18f);
        transition.Reset();
        Check(!transition.IsHidden && !transition.Update(false, true, 0f, 0f, 0.25f, 0.18f),
            "Explicit reset for disable/GPose/model replacement clears pending exit");
        // Sampling at a different frame rate changes the sample count, not the distance threshold.
        foreach (var steps in new[] { 10, 30, 60, 144 })
        {
            transition.Reset();
            for (var step = 0; step <= steps; step++)
            {
                var distance = 4f * (1f - (float)step / steps);
                Check(transition.Update(true, true, distance, distance, 0.25f, 0.18f) == (distance <= 0.25f),
                    "Near-arrival behavior independent of frame count");
            }
            for (var step = 0; step <= steps; step++)
            {
                var distance = 4f * step / steps;
                Check(transition.Update(false, true, distance, distance, 0.25f, 0.18f) == (distance < 0.35f),
                    "Retreat behavior independent of frame count");
            }
        }

        var localHead = new System.Numerics.Vector3(0.1f, 1.7f, 0.2f);
        var headRotation = System.Numerics.Quaternion.CreateFromYawPitchRoll(0.3f, -0.4f, 0.2f);
        var worldMatrix = System.Numerics.Matrix4x4.CreateScale(0.8f, 1.1f, 0.9f) *
            System.Numerics.Matrix4x4.CreateRotationY(0.7f) * System.Numerics.Matrix4x4.CreateTranslation(23f, 4f, -9f);
        var cameraOffset = new System.Numerics.Vector3(0.05f, 0.1f, -0.03f);
        var forward = System.Numerics.Vector3.TransformNormal(System.Numerics.Vector3.Transform(System.Numerics.Vector3.UnitX, headRotation), worldMatrix);
        var up = System.Numerics.Vector3.TransformNormal(System.Numerics.Vector3.Transform(System.Numerics.Vector3.UnitY, headRotation), worldMatrix);
        var right = System.Numerics.Vector3.TransformNormal(System.Numerics.Vector3.Transform(System.Numerics.Vector3.UnitZ, headRotation), worldMatrix);
        var betaTarget = System.Numerics.Vector3.Transform(localHead, worldMatrix) + right * cameraOffset.X + up * cameraOffset.Y + forward * cameraOffset.Z;
        var sharedTarget = FirstPersonMath.HeadCameraPosition(localHead, headRotation, worldMatrix, cameraOffset);
        Check(System.Numerics.Vector3.Distance(betaTarget, sharedTarget) < 0.00001f, "Hide timing matches Beta camera axes, rotation, scale and offsets");
        TestLookDownLean();
        TestMouseLook();
        TestExtraDown();
        TestLeanAutomaticReturn();
        TestSideLeanAngle();
        TestRuntimeLifecycle();
        TestNearObjectScope();
        TestHeadShadow();
        TestStationaryLook();
        TestHeadRotationWeights();
        TestLocalization();
        TestCalibration();
        Console.WriteLine($"PASS: {assertions} assertions. Calibration migration/drafts/camera transition, stationary camera-turn policy and ordinary/direct mouse input, per-axis head-follow weights, head-shadow filtering/interop layout, runtime lifecycle, near-object distance, mouse restoration, blended leaning, limits, VFX, near plane and head transitions.");
    }
}
