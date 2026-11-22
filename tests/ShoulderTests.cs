using System.Numerics;
using PovPlus;

internal static partial class Program
{
    private static void TestShoulder()
    {
        TestShoulderZoom();
        TestShoulderFlight();
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

    private static void TestShoulderZoom()
    {
        var zoom = new ShoulderZoom();
        float Step(float distance, float blend = 1, float min = 1.5f, float max = 4) =>
            zoom.Update(distance, 1, 20, min, max, blend);
        zoom.Begin(12, 2.2f);
        Check(Near(Step(12, 0), 12) && Near(zoom.Selected, 2.2f), "Entry remembers the previous config distance");
        var interpolated = Step(12, 0.5f);
        Check(Near(interpolated, 7.1f) && Near(zoom.Selected, 2.2f), "Entry blend is never saved as wheel distance");
        Check(Near(Step(interpolated + 0.5f, 0.6f), 2.7f), "Wheel during entry moves from the remembered shoulder setting");
        Check(Near(Step(16), 4) && Near(Step(4), 4) && Near(Step(3.75f), 3.75f), "Outer stop clamps before rendering and reverses immediately");
        Check(Near(Step(1), 1.5f) && Near(Step(2.25f), 2.25f), "Inner stop has no input debt");
        var remembered = zoom.Selected;
        zoom = new ShoulderZoom();
        zoom.Begin(14, remembered);
        Check(Near(Step(14), 2.25f), "New session returns to last wheel distance independently of third-person zoom");
        Check(Near(Step(2.25f, min: 3, max: 4), 3) && Near(zoom.Selected, 3), "Editing a limit also bounds the remembered distance");
        Check(Near(Step(5, min: 3, max: 3), 3), "Equal limits fix the zoom");
        Check(ShoulderZoom.Range(8, 2) == (2, 8) && ShoulderZoom.Range(float.NaN, float.PositiveInfinity) == (1.5f, 4),
            "Malformed configuration still provides a valid interval");
        zoom.Begin(9, 3);
        Check(Near(zoom.Update(9, 5, 20, 1, 4, 1), 5) && Near(zoom.Selected, 3), "Native minimum wins without losing the remembered value");
        Check(Near(zoom.Update(6, 5, 20, 1, 4, 1), 5) && Near(zoom.Selected, 3), "No interval overlap cannot overwrite user preference");
    }

    private static void TestShoulderFlight()
    {
        foreach (var fps in new[] { 30, 60, 144 })
        {
            var height = new ShoulderFlightHeight();
            var dt = 1f / fps;
            const float seat = 3.5f;
            float y = 50, pivot = 0;
            uint frame = 0;
            foreach (var speed in new[] { 0f, 18f, -24f, 0f })
            {
                for (var i = 0; i < fps; ++i)
                {
                    y += speed * dt;
                    pivot = height.Resolve(++frame, dt, 1, true, y, y + seat, y - 4);
                    if (i > fps / 4)
                        Check(Near(pivot, y + seat - 0.12f), "Hover, climb and dive retain rider shoulder height");
                }
            }
            var missing = height.Resolve(++frame, dt, 1, true, y + 2, null, y - 4);
            Check(Near(missing, pivot + 2), "A missing pose still tracks live altitude");
            Check(Near(missing, height.Resolve(frame, dt, 1, true, y + 2, null, y - 4)), "Duplicate camera query is stable");
            for (var i = 0; i < fps; i++) pivot = height.Resolve(++frame, dt, 1, true, y, float.NaN, y - 4);
            Check(Near(pivot, y - 4), "Extended pose loss returns to native height");
            for (var i = 0; i < fps; i++) pivot = height.Resolve(++frame, dt, 1, true, y, y + seat, y - 4);
            for (var i = 0; i < fps; i++) pivot = height.Resolve(++frame, dt, 1, false, y, null, y + 1);
            Check(Near(pivot, y + 1), "Landing returns to the native ground pivot");
            height.Resolve(++frame, dt, 1, true, y, y + seat, y);
            Check(Near(height.Resolve(++frame, dt, 2, true, y, null, y), y), "Changing mounts discards the former seat");
        }
    }
}
