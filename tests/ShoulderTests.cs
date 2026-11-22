using System.Numerics;
using PovPlus;

internal static partial class Program
{
    private static void TestShoulder()
    {
        var cycle = new ShoulderCycle();
        Check(ShoulderRig.EntryBlend(0, 0.2f) == 0 && Near(ShoulderRig.EntryBlend(0.1f, 0.2f), 0.5f) &&
            ShoulderRig.EntryBlend(5, 0.2f) == 1, "Entry blend eases between original and shoulder framing");
        Check(ShoulderRig.EntryBlend(0, 0) == 1, "Zero entry time snaps immediately");
        Check(!cycle.AllowToggle(100, true) && cycle.Active, "First toggle stays in native third person at the shoulder");
        Check(!cycle.AllowToggle(100, true) && cycle.Active, "Duplicate camera permission query does not advance");
        Check(cycle.AllowToggle(103, true) && !cycle.Active, "Second toggle hands off to native first person");
        Check(cycle.AllowToggle(103, false) && !cycle.Active, "Native mode change within the frame does not bounce");
        Check(cycle.AllowToggle(115, false) && !cycle.Active, "First person exits to normal third person");
        Check(!cycle.AllowToggle(116, true), "Subsequent cycle starts normally");
        cycle.Reset();
        Check(!cycle.Active && cycle.AllowToggle(116, false), "Reset in first person preserves native exit even in same frame");

        static Vector3 Simulate(int fps, float seconds, float limit, bool stopped)
        {
            var rig = new ShoulderRig();
            var dt = 1f / fps;
            rig.Follow(Vector3.Zero, dt, seconds, limit);
            var p = Vector3.Zero;
            for (var frame = 0; frame < fps; frame++)
            {
                p.X += 4 * dt;
                p.Y += 2 * dt;
                rig.Follow(p, dt, seconds, limit);
            }
            if (stopped)
                for (var frame = 0; frame < fps; frame++) rig.Follow(p, dt, seconds, limit);
            return rig.Offset(0, 0, 0);
        }
        var steady = Simulate(60, 0.1f, 0.8f, false);
        Check(MathF.Abs(steady.X + 0.4f) < 0.001f && steady.Y == 0, "Movement lag is horizontal and proportional to speed");
        foreach (var fps in new[] { 30, 120, 144 })
            Check(Vector3.Distance(steady, Simulate(fps, 0.1f, 0.8f, false)) < 0.0001f, "Frame-rate independent movement");
        Check(Near(Simulate(30, 0.3f, 0.2f, false).Length(), 0.2f), "Maximum lag bounds motion at speed");
        Check(Simulate(30, 0.1f, 0.8f, true).Length() < 0.0001f, "Standing still smoothly removes lag");
        Check(Simulate(30, 0, 0.8f, false) == Vector3.Zero && Simulate(30, 0.1f, 0, false) == Vector3.Zero,
            "Zero lag time or distance disables motion");
        var rig = new ShoulderRig();
        Check(rig.Offset(0, -0.5f, 0.2f) == new Vector3(-0.5f, 0.2f, 0), "Signed shoulder selection");
        Check(Vector3.Distance(rig.Offset(MathF.PI / 2, 0.5f, 0.1f), new(0, 0.1f, -0.5f)) < 0.0001f,
            "Shoulder rotates immediately with the orbit");
        rig.Follow(Vector3.Zero, 0.016f, 0.1f, 0.8f);
        rig.Follow(Vector3.One, 0.016f, 0.1f, 0.8f);
        rig.Follow(new(100, 0, 0), 0.016f, 0.1f, 0.8f);
        Check(rig.Offset(0, 0, 0) == Vector3.Zero, "Teleport snaps the rig to its new origin");
        rig.Follow(new(101, 0, 0), 1f, 0.1f, 0.8f);
        Check(rig.Offset(0, 0, 0) == Vector3.Zero, "Suspending the game clears motion history");
        rig.Follow(new(float.NaN), 0.016f, 0.1f, 0.8f);
        Check(float.IsFinite(rig.Offset(0, float.NaN, float.NaN).Length()), "Corrupt positions and config keep the offset finite");
    }
}
