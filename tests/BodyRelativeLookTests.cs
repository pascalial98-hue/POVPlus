using System.Numerics;
using System.Runtime.InteropServices;
using PovPlus;
using FFXIVClientStructs.Havok.Animation.Rig;
using FFXIVClientStructs.Havok.Common.Base.Math.QsTransform;
using SceneCamera = FFXIVClientStructs.FFXIV.Client.Graphics.Scene.Camera;

internal static partial class Program
{
    private static unsafe void TestBodyRelativeLook()
    {
        static bool Same(Vector3 a, Vector3 b) => Vector3.Distance(a, b) < 0.0001f;
        static bool SameRotation(Quaternion a, Quaternion b) => MathF.Abs(Quaternion.Dot(a, b)) > 0.99999f;
        var r = MathF.PI / 180;
        var bow = Quaternion.CreateFromAxisAngle(Vector3.UnitX, 90 * r);
        Check(BodyPoseMath.TryView(Vector3.Zero, Vector3.UnitZ, 0, bow, out var look, out var up) &&
            Same(look, -Vector3.UnitY) && Same(up, Vector3.UnitZ), "90-degree bow looks at ground with continuous body up");
        BodyPoseMath.TryView(Vector3.Zero, Vector3.UnitY, 0, bow, out look, out up);
        Check(Same(look, Vector3.UnitZ) && Same(up, Vector3.UnitY), "90-degree look-up from bow reaches horizon");
        BodyPoseMath.TryView(Vector3.Zero, new(0, -MathF.Sin(75 * r), MathF.Cos(75 * r)), 0, bow, out look, out up);
        Check(look.Z < -0.96f && look.Y < 0 && up.Y < -0.96f,
            "Looking down from bow naturally looks backwards upside down");
        Check(Near(FirstPersonMath.Degrees(90, 60, 90), MathF.PI / 2), "Upward bound accepts 90 degrees");

        // Exercise pole crossings, body yaw, side bends and inversions. Neither
        // look nor up can flip just because the view crosses world vertical.
        foreach (var bodyYaw in new[] { -179f, -90f, 0f, 65f, 179f })
        foreach (var bend in new[] { 0f, 45f, 90f, 130f })
        foreach (var roll in new[] { -55f, 0f, 55f })
        {
            var world = Quaternion.CreateFromAxisAngle(Vector3.UnitY, bodyYaw * r);
            var body = Quaternion.CreateFromYawPitchRoll(0, bend * r, roll * r);
            var delta = BodyPoseMath.WorldDelta(body, world, bodyYaw * r);
            var previousForward = Vector3.Zero;
            var previousUp = Vector3.Zero;
            for (var pitch = -95f; pitch <= 90; pitch += 0.5f)
            {
                var forward = Vector3.Transform(new Vector3(0, MathF.Sin(pitch * r), MathF.Cos(pitch * r)), world);
                var camera = new Vector3(10, 20, -30);
                Check(BodyPoseMath.TryView(camera, camera + forward, bodyYaw * r, delta, out look, out up), "Finite body view");
                var direction = Vector3.Normalize(look - camera);
                Check(MathF.Abs(Vector3.Dot(direction, up)) < 0.0001f && Near(up.Length(), 1), "Orthonormal view at poles");
                if (previousUp != Vector3.Zero)
                    Check(Vector3.Dot(previousUp, up) > 0.999f && Vector3.Dot(previousForward, direction) > 0.999f,
                        "No horizon flip crossing world/local vertical");
                previousUp = up;
                previousForward = direction;
            }
            var lean = LookDownLean.ToWorld(new(0.2f, 0.1f), bodyYaw * r);
            Check(Same(Vector3.Transform(lean, delta),
                Vector3.Transform(new Vector3(0.1f, 0, 0.2f), world * body)), "Lean follows body-local forward and side");
        }
        var bind = Quaternion.CreateFromYawPitchRoll(1.2f, -0.3f, 2.1f);
        var neck = Quaternion.CreateFromYawPitchRoll(0.2f, -0.4f, 0.15f);
        var modelDelta = BodyPoseMath.ModelDelta(bow * bind, bind);
        Check(SameRotation(modelDelta, bow), "Bind-axis rotations do not offset body reference");
        Check(SameRotation(BodyPoseMath.HeadRelativeToBody(bow * neck * bind, modelDelta), neck * bind),
            "Head follow removes torso bend exactly once while keeping neck animation");
        Quaternion Smoothed(int fps)
        {
            var q = Quaternion.Identity;
            for (var frame = 0; frame < fps; frame++) q = BodyPoseMath.Smooth(q, bow, 1f / fps);
            return q;
        }
        Check(SameRotation(Smoothed(30), Smoothed(144)), "Body smoothing is frame-rate independent");
        Check(SameRotation(BodyPoseMath.Smooth(bind, -bind, 0.016f), bind), "Quaternion sign changes cannot spin view");
        Check(!BodyPoseMath.TryNormalize(default, out _) &&
            !BodyPoseMath.TryView(default, new(float.NaN, 0, 0), 0, bow, out _, out _) &&
            !BodyPoseMath.TryView(default, default, 0, bow, out _, out _), "Invalid view data safely rejected");

        var locals = stackalloc hkQsTransformf[3];
        var parents = stackalloc short[3] { -1, 0, 1 };
        var rotations = new[] { Quaternion.CreateFromAxisAngle(Vector3.UnitY, 0.4f),
            Quaternion.CreateFromAxisAngle(Vector3.UnitZ, 0.7f), Quaternion.CreateFromAxisAngle(Vector3.UnitX, -0.8f) };
        for (var i = 0; i < 3; i++)
        {
            locals[i] = default;
            locals[i].Rotation.X = rotations[i].X;
            locals[i].Rotation.Y = rotations[i].Y;
            locals[i].Rotation.Z = rotations[i].Z;
            locals[i].Rotation.W = rotations[i].W;
        }
        var skeleton = new hkaSkeleton();
        skeleton.ReferencePose.Data = locals;
        skeleton.ReferencePose.Length = 3;
        skeleton.ParentIndices.Data = parents;
        skeleton.ParentIndices.Length = 3;
        Check(BodyPoseReference.TryModelReference(&skeleton, 2, out var reference) &&
            SameRotation(reference, rotations[0] * rotations[1] * rotations[2]), "Rest pose composes all parents in correct order");
        parents[0] = 2;
        Check(!BodyPoseReference.TryModelReference(&skeleton, 2, out _), "Cyclic skeleton rejected");
        parents[0] = 9;
        Check(!BodyPoseReference.TryModelReference(&skeleton, 2, out _), "Out-of-range skeleton parent rejected");
        parents[0] = -1;
        locals[1].Rotation.W = float.NaN;
        Check(!BodyPoseReference.TryModelReference(&skeleton, 2, out _), "Invalid bind quaternion rejected");
        Check(!BodyPoseReference.TryFind(null, out _, out _), "Missing skeleton safely falls back");

        var scene = new SceneCamera { Position = new(2, 3, 4), LookAtVector = new(2, 3, 5), Vector_1 = new(0, 1, 0), Flags = 3 };
        var original = scene;
        for (var i = 0; i < 100; i++)
        {
            var scope = new BodyViewScope(&scene, 0, bow);
            Check(Same(scene.LookAtVector, new(2, 2, 4)) && Same(scene.Vector_1, Vector3.UnitZ), "Render sees body-relative basis");
            // Simulate a native view output; scope must preserve render results.
            scene.ViewMatrix.M11 = 0.75f;
            scope.Restore(&scene);
            Check(scene.LookAtVector.Equals(original.LookAtVector) && scene.Vector_1.Equals(original.Vector_1) &&
                scene.Position.Equals(original.Position) && scene.Flags == original.Flags && Near(scene.ViewMatrix.M11, 0.75f),
                "Logical native camera restored, render output kept; no compounding");
        }
        scene.Flags = 0;
        var inactive = new BodyViewScope(&scene, 0, bow);
        Check(scene.LookAtVector.Equals(original.LookAtVector), "Non-look-at scene cameras untouched");
        inactive.Restore(&scene);
        Check(Marshal.OffsetOf<SceneCamera>(nameof(SceneCamera.Vector_1)).ToInt32() == 0x90 &&
            Marshal.OffsetOf<SceneCamera>(nameof(SceneCamera.LookAtVector)).ToInt32() == 0x80,
            "View scope fields match verified CN native view builder");
    }
}
