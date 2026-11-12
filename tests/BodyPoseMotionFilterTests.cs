using System.Numerics;
using PovPlus;

internal static partial class Program
{
    private static void TestBodyPoseMotionFilter()
    {
        static Quaternion Pitch(float degrees) => Quaternion.CreateFromAxisAngle(Vector3.UnitX, degrees * MathF.PI / 180);
        static float Angle(Quaternion q) => MathF.Atan2(2 * (q.W * q.X + q.Y * q.Z), 1 - 2 * (q.X * q.X + q.Y * q.Y));
        static bool Same(Quaternion a, Quaternion b) => MathF.Abs(Quaternion.Dot(a, b)) > 0.999999f;
        var identity = Quaternion.Identity;
        var filter = new BodyPoseMotionFilter();
        var first = filter.Update(Pitch(90), Pitch(-15), new(10, 20, 30), 0);
        Check(Same(first.Body, Pitch(90)) && Same(first.Head, Pitch(-15)), "Entry while already bowed uses full current pose");
        var originalBody = first.Body;
        for (var i = 0; i < 300; i++)
        {
            var target = Pitch(90 + 20 * MathF.Sin(i / 30f));
            var head = Pitch(10 * MathF.Sin(i / 20f));
            var result = filter.Update(target, head, new(10, 20, 30), 1f / 60);
            originalBody = BodyPoseMath.Smooth(originalBody, target, 1f / 60);
            Check(Same(result.Body, originalBody) && Same(result.Head, head), "Stationary body response and head-follow input unchanged");
        }

        var measures = new List<(int Fps, float Hz, float Ratio)>();
        foreach (var fps in new[] { 30, 60, 144 })
        foreach (var hz in new[] { 1.5f, 2.5f, 3.5f })
        {
            filter.Reset();
            var position = Vector3.Zero;
            filter.Update(identity, identity, position, 0);
            var baseline = identity;
            double originalEnergy = 0, filteredEnergy = 0, originalHeadEnergy = 0, filteredHeadEnergy = 0;
            for (var i = 1; i <= 8 * fps; i++)
            {
                var time = (float)i / fps;
                var stride = 12 * MathF.Sin(time * hz * 2 * MathF.PI);
                // The head counter-rotates against the torso during locomotion.
                // Both orientation paths must reject this high-frequency motion.
                var body = Pitch(stride);
                var head = Pitch(-stride * 0.75f);
                position.Z += 6f / fps;
                var frame = filter.Update(body, head, position, 1f / fps);
                baseline = BodyPoseMath.Smooth(baseline, body, 1f / fps);
                if (i < 3 * fps) continue;
                originalEnergy += Math.Pow(Angle(baseline), 2);
                filteredEnergy += Math.Pow(Angle(frame.Body), 2);
                originalHeadEnergy += Math.Pow(Angle(head), 2);
                filteredHeadEnergy += Math.Pow(Angle(frame.Head), 2);
            }
            var ratio = (float)Math.Sqrt(filteredEnergy / originalEnergy);
            measures.Add((fps, hz, ratio));
            Check(ratio < 0.23f, "Walking/running stride rotation strongly suppressed across tested frequencies/frame rates");
            Check(Math.Sqrt(filteredHeadEnergy / originalHeadEnergy) < 0.20,
                "Counter-motion cannot reintroduce stride through relative head follow");
        }
        foreach (var hz in new[] { 1.5f, 2.5f, 3.5f })
        {
            var atHz = measures.Where(m => m.Hz == hz).Select(m => m.Ratio).ToArray();
            Check(atHz.Max() - atHz.Min() < 0.015f, "Similar motion attenuation at 30/60/144 FPS");
        }
        Console.WriteLine("Body motion filter RMS ratios versus previous torso follow: " +
            string.Join(", ", measures.Where(m => m.Fps == 60).Select(m => $"{m.Hz:F1} Hz = {m.Ratio:P1}")));

        // A sustained bend must keep its full angle even while the actor moves.
        filter.Reset();
        filter.Update(identity, identity, Vector3.Zero, 0);
        BodyPoseMotionFilter.Frame latest = default;
        for (var i = 1; i <= 240; i++) latest = filter.Update(Pitch(90), Pitch(-20), new(0, 0, i / 10f), 1f / 60);
        Check(Same(latest.Body, Pitch(90)) && Same(latest.Head, Pitch(-20)), "Moving filter preserves full sustained bow and head angle");
        var yaw = Quaternion.CreateFromAxisAngle(Vector3.UnitY, 1.2f);
        var world = BodyPoseMath.WorldDelta(latest.Body, yaw, 1.2f);
        Check(Vector3.Distance(Vector3.Transform(Vector3.UnitZ, world * yaw),
            Vector3.Transform(Vector3.UnitZ, yaw * Pitch(90))) < 0.0001f, "Character/world heading does not enter locomotion smoothing");

        // Stopping releases the slow branch gradually instead of jumping to a
        // cached idle pose. Static neck animation returns to normal afterwards.
        var before = latest.Body;
        for (var i = 0; i < 180; i++)
        {
            latest = filter.Update(Pitch(70), Pitch(5), new(0, 0, 24), 1f / 60);
            Check(MathF.Abs(Angle(latest.Body) - Angle(before)) < 2 * MathF.PI / 180,
                "Stopping does not snap between moving and stationary pose");
            before = latest.Body;
        }
        latest = filter.Update(Pitch(70), Pitch(12), new(0, 0, 24), 1f / 60);
        Check(Same(latest.Body, Pitch(70)) && Same(latest.Head, Pitch(12)), "Idle response fully restored after stopping");
        latest = filter.Update(Pitch(-30), Pitch(-10), new(100, 0, 24), 1f / 60);
        Check(Same(latest.Body, Pitch(-30)) && Same(latest.Head, Pitch(-10)), "Teleport does not inherit old filtered bend");
        latest = filter.Update(Pitch(50), identity, new(100, 0, 24), 1);
        Check(Same(latest.Body, Pitch(50)), "Long sampling gap reseeds pose");
        filter.Reset();
        latest = filter.Update(Pitch(80), identity, Vector3.Zero, 0);
        latest = filter.Update(-Pitch(80), -identity, Vector3.Zero, 0.000000001f);
        Check(Same(latest.Body, Pitch(80)) && Same(latest.Head, identity), "Tiny intervals and quaternion signs remain finite");
        latest = filter.Update(new(float.NaN, 0, 0, 1), identity, Vector3.Zero, 0.016f);
        Check(Same(latest.Body, identity), "Invalid pose cannot contaminate filter state");
    }
}
