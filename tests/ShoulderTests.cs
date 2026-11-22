using System.Numerics;
using PovPlus;

internal static partial class Program
{
    private static void TestShoulder()
    {
        TestShoulderZoom();
        TestShoulderCombat();
        TestShoulderSpeedZoom();
        TestCameraLensOwnership();
        TestGlideLens();
        TestShoulderFlight();
        TestWalkingStairHeight();
        TestFlightSwayAndBank();
        TestShoulderTurning();
        TestRidingResponse();
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

    private static void TestFlightSwayAndBank()
    {
        static Vector3 Arc(float time, float side) =>
            16 * new Vector3(side * (1 - MathF.Cos(time)), 0, MathF.Sin(time));

        static ShoulderFlightMotion Turn(int fps, float side, float sway, float bank, bool airborne = true)
        {
            var motion = new ShoulderFlightMotion();
            for (int i = 0; i <= fps * 3; i++)
                motion.Update(Arc(i / (float)fps, side), 1f / fps, airborne, sway, bank);
            return motion;
        }
        var right = Turn(60, 1, 60, 5);
        var left = Turn(60, -1, 60, 5);
        var centerward = new Vector3(MathF.Cos(3), 0, -MathF.Sin(3));
        Check(Vector3.Dot(right.Bank, centerward) > 0.05f && right.Bank.Length() <= 5 * MathF.PI / 180 + 0.0001f,
            "Camera leans toward the center of the flight turn, within the configured angle");
        Check(Near(left.Bank.X, -right.Bank.X) && Near(left.Bank.Z, right.Bank.Z), "Mirrored flight paths mirror the bank");
        foreach (int fps in new[] { 30, 144 })
        {
            var other = Turn(fps, 1, 60, 5);
            Check(Vector3.Distance(other.Bank, right.Bank) < 0.003f && Vector2.Distance(other.Sway, right.Sway) < 0.006f,
                "Sway and banking are consistent across render rates");
        }
        Check(Turn(60, 1, 0, 5).Sway == Vector2.Zero && Turn(60, 1, 60, 0).Bank == Vector3.Zero,
            "Flight sway and banking can be disabled independently");
        var ground = Turn(60, 1, 100, 12, airborne: false);
        Check(ground.Sway == Vector2.Zero && ground.Bank == Vector3.Zero, "Ground riding cannot generate flight motion");
        var motion = new ShoulderFlightMotion();
        float peak = 0;
        for (int i = 0; i <= 180; i++)
        {
            motion.Update(Vector3.UnitY * (i / 4f), 1f / 60, true, 60, 5);
            peak = MathF.Max(peak, motion.Sway.Length());
        }
        Check(peak > 0.02f && peak < 0.07f && motion.Bank == Vector3.Zero, "Straight climbs produce bounded sway without bank");
        for (int i = 0; i < 240; i++) motion.Update(45 * Vector3.UnitY, 1f / 60, true, 60, 5);
        Check(motion.Sway.Length() < 0.00001f && motion.Bank == Vector3.Zero, "Hovering settles even with flight mode active");
        var beforeLanding = right.Bank;
        right.Update(Arc(3, 1), 1f / 60, false, 60, 5);
        Check(Vector3.Dot(beforeLanding, right.Bank) > 0 && right.Bank.Length() < beforeLanding.Length(),
            "Landing eases out the previous bank without reversing it");
        for (int i = 0; i < 120; i++) right.Update(Vector3.Zero, 1f / 60, false, 60, 5);
        Check(right.Sway == Vector2.Zero && right.Bank == Vector3.Zero, "Landing fully releases all flight offsets");
        left.Update(new(200, 0, 0), 1f / 60, true, 60, 5);
        Check(left.Bank == Vector3.Zero && left.Sway == Vector2.Zero, "Teleports discard old flight motion");
    }

    private static void TestShoulderCombat()
    {
        var settings = new ShoulderCombatSettings { Side = -0.4f, Height = 0.2f, Distance = 1.2f, EnterSeconds = 1, ExitSeconds = 2 };
        float phase = 0;
        Vector3 OnFoot(bool inCombat, float dt) => settings.Advance(ref phase, inCombat, dt, riding: false);
        Check(OnFoot(true, 0.25f) == Vector3.Zero, "Combat framing is opt-in for existing profiles");
        settings.Enabled = true;
        var full = new Vector3(settings.Side, settings.Height, settings.Distance);
        foreach (var fps in new[] { 30, 60, 144 })
        {
            phase = 0;
            var zoom = new ShoulderZoom();
            zoom.Begin(8, 2.5f);
            var distance = zoom.Update(8, 1, 20, 1, 4, 1);
            for (var i = 0; i < 3 * fps; ++i)
            {
                var offset = OnFoot(i < fps, 1f / fps);
                distance = zoom.Update(distance, 1, 20, 1, 4, 1, offset.Z);
                Check(Near(zoom.Selected, 2.5f) && Near(distance, 2.5f + offset.Z),
                    "Combat blend is independent of wheel memory and repeated camera queries");
                if (i == fps / 2 - 1 || i == 2 * fps - 1)
                    Check(Vector3.Distance(offset, full * 0.5f) < 0.0001f, "Independent combat entry and exit times at different frame rates");
                if (i == fps - 1)
                    Check(Vector3.Distance(offset, full) < 0.0001f, "Combat framing reaches the configured offset");
            }
            Check(Near(distance, 2.5f), "Leaving combat restores the original shoulder zoom");
        }
        phase = 0;
        var entering = OnFoot(true, 0.4f);
        Check(OnFoot(false, 0) == entering && OnFoot(false, 0.1f).Z < entering.Z,
            "A quick combat reversal continues from the current blend");
        Check(settings.Advance(ref phase, true, 0, riding: true) == Vector3.Zero && phase == 0,
            "Mounting clears the entire combat framing and its phase on the first tick");
        Check(settings.Advance(ref phase, true, 1, riding: true) == Vector3.Zero && OnFoot(false, 0.1f) == Vector3.Zero,
            "Remaining mounted cannot arm combat framing for a later dismount");
        Check(Vector3.Distance(OnFoot(true, 0.5f), full * 0.5f) < 0.0001f,
            "Dismounting into combat starts the normal on-foot entry blend");
        phase = 1;
        settings.Enabled = false;
        Check(OnFoot(true, 0.1f).Z < full.Z && phase > 0, "Disabling mid-combat fades out");
        settings.Enabled = true;
        settings.EnterSeconds = settings.ExitSeconds = 0;
        Check(OnFoot(true, 0) == full && OnFoot(false, 0) == Vector3.Zero,
            "Zero duration is immediate in either direction");
        settings.Distance = float.NaN;
        Check(float.IsFinite(OnFoot(true, 0).Z), "Invalid saved offsets are bounded");
        var rig = new ShoulderRig();
        var pivot = rig.Offset(MathF.PI / 2, 2, 2, new Vector2(0.5f, 0.2f));
        Check(Vector3.Distance(pivot, new Vector3(0, 2.2f, -2.5f)) < 0.0001f,
            "Additional framing at the rig limit rotates with the orbit");

        var wheel = new ShoulderZoom();
        wheel.Begin(6, 2.5f);
        float Step(float input, float offset, float max = 20) => wheel.Update(input, 0.25f, max, 1, 4, 1, offset);
        Check(Near(Step(6, 2), 4.5f) && Near(Step(5, 2), 5) && Near(wheel.Selected, 3),
            "Manual wheel input in combat only saves its base-distance delta");
        Check(Near(Step(5, 0), 3), "Exiting combat retains only the manual adjustment");
        Check(Near(Step(3, 5, max: 4), 4) && Near(Step(3, 5, max: 4), 4) && Near(wheel.Selected, 3),
            "An unreachable combat interval does not overwrite the saved distance");
        Check(Near(Step(4, -3), 0.25f) && Near(wheel.Selected, 3), "A large negative offset respects the physical floor");
        Check(Near(Step(0.5f, -3), 0.5f) && Near(wheel.Selected, 3.5f), "Wheel reverses immediately from the clipped combat floor");
        wheel.Begin(8, 2);
        Check(Near(wheel.Update(8, 0.25f, 20, 2, 2, 1, 2), 4) && Near(wheel.Selected, 2),
            "Fixed-distance profiles still receive the combat offset");
    }

    private static void TestShoulderSpeedZoom()
    {
        var foot = new ShoulderProfile();
        var mount = ShoulderProfile.Riding();
        Check(!foot.SpeedZoomEnabled && !mount.SpeedZoomEnabled, "Speed zoom is opt-in for both saved profiles");
        mount.SpeedZoomEnabled = true;
        Check(!foot.SpeedZoomEnabled, "Enabling mounted speed zoom is independent of walking");
        foot.SpeedZoomEnabled = true;

        static float Travel(Vector3 velocity, bool riding, ShoulderProfile profile, int fps)
        {
            var camera = new ShoulderSpeedZoom();
            for (int i = 0; i <= 4 * fps; i++)
                camera.Update(velocity * (i / (float)fps), 1f / fps, riding, profile);
            return camera.Offset;
        }
        float walk = Travel(2 * Vector3.UnitX, false, foot, 60);
        float run = Travel(6 * Vector3.UnitZ, false, foot, 60);
        float sprint = Travel(-8 * Vector3.UnitX, false, foot, 60);
        float ride = Travel(13 * Vector3.UnitZ, true, mount, 60);
        float climb = Travel(18 * Vector3.UnitY, true, mount, 60);
        Check(0 < walk && walk < run && run < sprint && sprint < ride && ride < climb, "Faster travel gradually pulls farther back across walk, sprint and mount speeds");
        Check(Near(climb, mount.SpeedZoomDistance) && Near(Travel(-18 * Vector3.UnitY, true, mount, 144), climb),
            "Climbing and descending affect distance equally across frame rates");
        Check(Near(Travel(18 * Vector3.Normalize(Vector3.One), true, mount, 30), climb), "Diagonal flight uses total speed once");
        Check(Travel(10 * Vector3.UnitY, false, foot, 60) == 0 && Travel(0.1f * Vector3.UnitX, true, mount, 60) == 0,
            "Standing jumps on foot and idle drift do not pull the camera back");
        foreach (bool riding in new[] { false, true })
        {
            var profile = riding ? mount : foot;
            float normal = riding ? 9.5f : 6f;
            float full = riding ? 18f : 10f;
            var last = 0f;
            for (float v = 0; v <= full + 1; v += 0.25f)
            {
                var offset = Travel(v * Vector3.UnitX, riding, profile, 60);
                Check(offset >= last - 0.00001f && offset <= profile.SpeedZoomDistance + 0.00001f,
                    "Speed response is monotonic and bounded across both overlapping ramps");
                last = offset;
            }
            foreach (int fps in new[] { 30, 60, 144 })
            {
                var ordinary = Travel(normal * Vector3.UnitX, riding, profile, fps);
                var boosted = Travel((normal * 1.20f) * Vector3.UnitX, riding, profile, fps);
                Check(ordinary > 0 && ordinary < 0.15f * profile.SpeedZoomDistance &&
                    boosted - ordinary > 2.5f * ordinary && boosted < profile.SpeedZoomDistance,
                    "A 20 percent pace increase produces a larger zoom change than starting from rest");
            }
        }
        mount.SpeedZoomDistance = 0.2f;
        Check(Near(Travel(25 * Vector3.UnitX, true, mount, 60), 0.2f) && Near(Travel(6 * Vector3.UnitZ, false, foot, 30), run),
            "Mounted cap is independent of the on-foot cap");

        var speed = new ShoulderSpeedZoom();
        var zoom = new ShoulderZoom();
        zoom.Begin(8, 2.5f);
        float nativeZoom = 8;
        var combat = new ShoulderCombatSettings { Enabled = true, Distance = 1, EnterSeconds = 0 };
        float combatPhase = 1;
        for (int i = 0; i <= 120; i++)
        {
            speed.Update(Vector3.UnitY * (i * 0.25f), 1f / 60, true, mount);
            var mountCombat = combat.Advance(ref combatPhase, true, 1f / 60, riding: true);
            nativeZoom = zoom.Update(nativeZoom, 0.25f, 20, 1, 4, 1, mountCombat.Z + speed.Offset);
            Check(Near(zoom.Selected, 2.5f) && Near(nativeZoom, 2.5f + speed.Offset), "Mounted zoom uses speed alone even with the combat flag set");
        }
        nativeZoom = zoom.Update(nativeZoom + 0.25f, 0.25f, 20, 1, 4, 1, speed.Offset);
        var moving = speed.Offset;
        for (int i = 0; i < 360; i++)
        {
            speed.Update(30 * Vector3.UnitY, 1f / 60, true, mount);
            nativeZoom = zoom.Update(nativeZoom, 0.25f, 20, 1, 4, 1, speed.Offset);
            if (i == 2) Check(speed.Offset > 0.9f * moving && speed.Offset < moving, "Stopping eases in without snapping");
        }
        Check(speed.Offset == 0 && Near(zoom.Selected, 2.75f) && Near(nativeZoom, 2.75f), "Stopping the mount preserves manual wheel changes");
        var footCombat = combat.Advance(ref combatPhase, true, 1f / 60, riding: false);
        Check(Near(zoom.Update(nativeZoom, 0.25f, 20, 1, 4, 1, footCombat.Z + run), 3.75f + run) && Near(zoom.Selected, 2.75f),
            "On foot the speed and combat effects still add to the manual distance");
        speed.Update(Vector3.Zero, 1f / 60, false, foot);
        speed.Update(100 * Vector3.UnitX, 1f / 60, false, foot);
        speed.Update(102 * Vector3.UnitX, 1f / 60, true, mount);
        Check(speed.Offset == 0, "Teleports and mounting do not seed artificial speed");
        speed.Update(103 * Vector3.UnitX, 1, true, mount);
        speed.Update(Vector3.Zero, 1f / 60, true, mount);
        Check(speed.Offset == 0, "First sample after a long pause starts fresh");
    }

    private static unsafe void TestCameraLensOwnership()
    {
        var camera = new FFXIVClientStructs.FFXIV.Client.Game.Camera();
        var fov = new FirstPersonFov();
        // Cammy unloaded after POV+ initialized, before first person was used.
        camera.FoV = camera.MaxFoV = 0.78f;
        camera.MinFoV = 0.69f;
        fov.Apply(ref camera.FoV, ref camera.MinFoV, ref camera.MaxFoV, 1.25f);
        Check(camera.MinFoV == camera.FoV && camera.FoV == camera.MaxFoV && camera.FoV == 1.25f,
            "First-person lens above native maximum always has valid bounds");
        fov.Restore(ref camera.FoV, ref camera.MinFoV, ref camera.MaxFoV);
        Check(camera.MinFoV == 0.69f && camera.MaxFoV == 0.78f && camera.FoV == 0.78f,
            "Returning leaves native limits captured at activation");
        fov.Apply(ref camera.FoV, ref camera.MinFoV, ref camera.MaxFoV, 1.25f);
        camera.MinFoV = 0.0547f;
        camera.MaxFoV = 1.209f;
        camera.FoV = 0.92f;
        fov.Apply(ref camera.FoV, ref camera.MinFoV, ref camera.MaxFoV, 1.25f);
        fov.Restore(ref camera.FoV, ref camera.MinFoV, ref camera.MaxFoV);
        Check(camera.MinFoV == 0.0547f && camera.MaxFoV == 1.209f && camera.FoV == 0.92f,
            "Preset applied while POV is active survives release");
        fov.Apply(ref camera.FoV, ref camera.MinFoV, ref camera.MaxFoV, 1.25f);
        camera.MinFoV = 0.69f;
        camera.FoV = camera.MaxFoV = 0.78f;
        fov.Restore(ref camera.FoV, ref camera.MinFoV, ref camera.MaxFoV);
        Check(camera.MinFoV == 0.69f && camera.MaxFoV == 0.78f && camera.FoV == 0.78f,
            "External writes just before leaving first person are preserved");
        camera.FoV = camera.MaxFoV = 1.2f;
        fov.Apply(ref camera.FoV, ref camera.MinFoV, ref camera.MaxFoV, 1.25f);
        camera.MaxFoV = 0.9f;
        fov.Restore(ref camera.FoV, ref camera.MinFoV, ref camera.MaxFoV);
        Check(camera.FoV == 0.9f && camera.MinFoV == 0.69f && camera.MaxFoV == 0.9f,
            "Restored lens respects a narrower third-person preset applied just before release");
        foreach (var nativeFloor in new[] { 0.5f, 1.5f })
        {
            camera.MinDistance = camera.Distance = nativeFloor;
            camera.MinFoV = 0.0547f;
            camera.MaxFoV = 1.209f;
            camera.FoV = 0.78f;
            var scope = new ShoulderWheelScope(&camera);
            Check(camera.Distance > camera.MinDistance && Near(camera.FoV, 1.209f) && camera.FoV == camera.MaxFoV,
                "At shoulder floor native input can increase physical distance on the next outward step");
            camera.FoV = Math.Clamp(camera.FoV - 0.6f, camera.MinFoV, camera.MaxFoV);
            Check(Near(camera.FoV, 1.209f), "Inward wheel cannot perform a second, optical zoom");
            scope.Restore(&camera);
            Check(camera.FoV == 0.78f && camera.MinFoV == 0.0547f && camera.MaxFoV == 1.209f && camera.MinDistance == nativeFloor,
                "The third-person optical zoom and native bounds are restored after shoulder input");
        }
        var changed = new ShoulderWheelScope(&camera);
        camera.MinFoV = 0.7f;
        camera.MaxFoV = 0.9f;
        camera.FoV = 0.9f;
        camera.MinDistance = 2;
        changed.Restore(&camera);
        Check(camera.FoV == 0.9f && camera.MinFoV == 0.7f && camera.MaxFoV == 0.9f && camera.MinDistance == 2,
            "Concurrent preset change survives scoped input");
    }

    private static void TestGlideLens()
    {
        Check(ShoulderLens.IsGlideTimeline(4049, 0), "Mount glide timeline activates the boost");
        Check(!ShoulderLens.IsGlideTimeline(4043, 6703), "The mount's flight animation takes priority over a stale rider glide");
        Check(!ShoulderLens.IsGlideTimeline(4040, 0) && !ShoulderLens.IsGlideTimeline(5005, 0),
            "Hovering and underwater glide do not activate the boost");
        Check(ShoulderLens.IsGlideTimeline(null, 6703) && !ShoulderLens.IsGlideTimeline(null, 4048),
            "The rider glide is a fallback when the mount is unavailable");
        const float normal = 1.209f;
        Check(Near(ShoulderLens.Target(normal, false, 8), normal) &&
              Near(ShoulderLens.Target(normal, true, 0), normal), "Regular shoulder view uses the current third-person preset");
        Check(Near(ShoulderLens.Target(normal, true, 8), normal + 8 * MathF.PI / 180),
            "Only gliding adds the configured angular amount");
        Check(Near(ShoulderLens.Target(2, true, 20), 120 * MathF.PI / 180) &&
              Near(ShoulderLens.Target(2.2f, true, 20), 2.2f), "Do not over-widen or narrow an already wide lens");
        var nativeFov = 0.1f;
        CameraValueOverride lens = default;
        lens.Apply(ref nativeFov, ShoulderLens.Target(normal, true, 8));
        lens.Restore(ref nativeFov);
        Check(nativeFov == 0.1f, "Rendering the glide preserves the stored third-person optical zoom");
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
        foreach (var ordinaryMin in new[] { 0.5f, 1.5f })
        {
            var floor = new CameraValueOverride();
            float applied = ordinaryMin;
            ShoulderZoom.ApplyFloor(ref floor, ref applied, 0.25f);
            zoom.Begin(8, 0.25f);
            Check(applied == 0.25f && zoom.Update(8, applied, 20, 0.25f, 3, 1) == 0.25f,
                "Near shoulder framing uses physical distance with either native or Cammy limits");
            Check(Near(zoom.Update(0.60f, applied, 20, 0.25f, 3, 1), 0.60f), "First outward step moves away from the closer stop");
            floor.Restore(ref applied);
            Check(applied == ordinaryMin, "Suspending the shoulder camera releases the orbit floor");
        }
    }

    private static void TestShoulderTurning()
    {
        foreach (var fps in new[] { 30, 60, 144 })
        foreach (var direction in new[] { Vector3.UnitX, -Vector3.UnitX, Vector3.UnitZ, -Vector3.UnitZ, Vector3.UnitY })
        {
            var rig = new ShoulderRig();
            var p = Vector3.Zero;
            var dt = 1f / fps;
            float yaw = 3;
            rig.Follow(p, dt, 0.20f, 0.8f, yaw, 100);
            for (int i = 0; i < fps * 2; i++)
            {
                var step = (i < fps ? 5 : -5) * dt;
                var half = Quaternion.CreateFromAxisAngle(Vector3.UnitY, yaw + step / 2);
                p += Vector3.Transform(direction, half) * (2 * dt);
                yaw = MathF.IEEERemainder(yaw + step, 2 * MathF.PI);
                rig.Follow(p, dt, 0.20f, 0.8f, yaw, 100);
                var offset = Vector3.Transform(rig.Offset(yaw, 0, 0), Quaternion.CreateFromAxisAngle(Vector3.UnitY, -yaw));
                if (i > fps / 2) Check(Vector3.Distance(offset, -direction * 0.4f) < 0.002f,
                    "Rapid orbit, reversal and angle wrapping preserve full directional lag without a sideways kick");
            }
        }
        var reversal = new ShoulderRig();
        Vector3 pos = Vector3.Zero;
        for (var i = 0; i < 180; i++) { pos.X += 0.025f; reversal.Follow(pos, 1f / 60, 0.2f, 0.8f); }
        var previous = reversal.Offset(0, 0, 0);
        pos.X -= 0.025f;
        reversal.Follow(pos, 1f / 60, 0.2f, 0.8f);
        Check(Vector3.Distance(previous, reversal.Offset(0, 0, 0)) < 0.03f, "Strafing reversal changes lag continuously");
    }

    private static void TestRidingResponse()
    {
        static (float onset, float tail, float end) StartAndStop(int fps, float follow, float recovery)
        {
            var rig = new ShoulderRig();
            var position = Vector3.Zero;
            var dt = 1f / fps;
            float onset = 0, tail = 0;
            void Step() => rig.Follow(position, dt, follow, 0.8f, 0, 0, 1, recovery, 35);
            Step();
            for (var i = 1; i <= 4 * fps; i++)
            {
                position.Z += 12 * dt;
                Step();
                if (i == fps / 6) onset = -rig.Offset(0, 0, 0).Z;
            }
            var previous = -rig.Offset(0, 0, 0).Z;
            for (var i = 1; i <= 3 * fps; i++)
            {
                Step();
                var current = -rig.Offset(0, 0, 0).Z;
                Check(current >= -0.000001f && current <= previous + 0.000002f, "Mounted stop is monotonic through the entire return");
                if (i == 1 && recovery >= 1) Check(current > previous * 0.96f, "Stopping avoids a one-frame snap even at 30 FPS");
                if (i == fps / 2) tail = current;
                previous = current;
            }
            return (onset, tail, previous);
        }
        var referenceTail = StartAndStop(60, 0.1f, 1.5f).tail;
        foreach (var fps in new[] { 30, 60, 144 })
        {
            var fast = StartAndStop(fps, 0.1f, 0.3f);
            var gentle = StartAndStop(fps, 0.1f, 1.5f);
            var longFollow = StartAndStop(fps, 0.8f, 1.5f);
            Check(Near(fast.onset, gentle.onset) && gentle.onset > longFollow.onset + 0.22f,
                "The mounted follow slider changes onset even at the travel limit, independently of recovery");
            Check(fast.tail < 0.025f && gentle.tail > 0.30f && Math.Abs(gentle.tail - longFollow.tail) < 0.001f,
                "The recovery slider controls the return independently of the follow slider");
            Check(Math.Abs(gentle.tail - referenceTail) < 0.001f && gentle.end < 0.004f,
                "Mounted recovery timing is stable across frame rates");
        }

        static Vector3 RideCurve(float signedRate, float strength, bool move = true)
        {
            var rig = new ShoulderRig();
            Vector3 position = default;
            float yaw = 0;
            const float dt = 1f / 60;
            for (var i = 0; i <= 300; i++)
            {
                var rate = i < 100 ? 0 : signedRate;
                var step = rate * dt;
                if (move) position += Vector3.Transform(Vector3.UnitZ * (7 * dt),
                    Quaternion.CreateFromAxisAngle(Vector3.UnitY, yaw + step / 2));
                yaw += step;
                rig.Follow(position, dt, 0.15f, 1.5f, yaw, 0, 1, 1, strength);
            }
            return Vector3.Transform(rig.Offset(yaw, 0, 0), Quaternion.CreateFromAxisAngle(Vector3.UnitY, -yaw));
        }
        var right = RideCurve(0.9f, 100);
        var left = RideCurve(-0.9f, 100);
        var subtle = RideCurve(0.9f, 35);
        var off = RideCurve(0.9f, 0);
        Check(right.X < subtle.X - 0.1f && subtle.X < off.X - 0.06f && Math.Abs(right.X + left.X) < 0.002f,
            "Ground-track acceleration gives proportional, mirrored outside-of-turn sway");
        Check(RideCurve(1.9f, 100, false) == Vector3.Zero && Vector3.Distance(RideCurve(0, 100), RideCurve(0, 0)) < 0.00001f,
            "Orbiting a stopped mount and travelling straight generate no artificial sway");
        Check(right.Length() <= 1.501f, "Cornering stays within the configured lag distance");

        var reversing = new ShoulderRig();
        var pos = Vector3.Zero;
        void Ride(float dt) => reversing.Follow(pos, dt, 0.4f, 0.8f, 0, 100, 1, 1, 35);
        for (var i = 0; i < 180; i++) { pos.X += 0.15f; Ride(1f / 60); }
        var before = reversing.Offset(0, 0, 0);
        Ride(0);
        Check(before == reversing.Offset(0, 0, 0), "Duplicate timestamps preserve the mounted spring");
        pos.X += 3.6f;
        Ride(0.3f);
        Check(Vector3.Distance(before, reversing.Offset(0, 0, 0)) < 0.001f, "A long riding frame preserves a plausible flight movement");
        pos.X -= 0.15f;
        Ride(1f / 60);
        Check(reversing.Offset(0, 0, 0).X < before.X + 0.015f, "Sudden reversal starts by gently unwinding the old offset");
        for (var i = 0; i < 150; i++) { pos.X -= 0.15f; Ride(1f / 60); }
        Check(reversing.Offset(0, 0, 0).X > 0.79f, "After reversal the full configured lag develops in the new direction");
    }

    private static void TestWalkingStairHeight()
    {
        foreach (int fps in new[] { 30, 60, 144 })
        {
            var height = new ShoulderWalkingHeight();
            const float normalFraming = 1.35f;
            float dt = 1f / fps;
            uint frame = 0;
            Vector3 root = new(2, 20, -3), drawn = root;
            float native = root.Y + normalFraming;
            float Correct() => height.Update(frame, dt, 1, root, drawn, native);
            Check(Correct() == 0, "Entering walking shoulder preserves the current native framing");
            foreach (var direction in new[] { 1, -1 })
            {
                for (int i = 0; i < 6 * fps; i++)
                {
                    frame++;
                    root.Z += dt * 2;
                    if (i % Math.Max(1, fps / 6) == 0) root.Y += direction * 0.16f;
                    // The rendered model eases each discrete step. Simulate a native
                    // pivot that also leads on ascent and undershoots on descent.
                    drawn = new(root.X, float.Lerp(drawn.Y, root.Y, 1 - MathF.Exp(-dt / 0.09f)), root.Z);
                    native = root.Y + normalFraming + direction * 0.3f;
                    if (i % 11 == 8) continue; // some input frames do not query the pivot
                    var delta = Correct();
                    Check(Near(native + delta - drawn.Y, normalFraming), "Walking a long staircase keeps the original height relative to the visible model");
                    Check(Correct() == delta, "Multiple pivot queries per frame apply the same stair correction");
                }
            }
            for (int i = 0; i < 2 * fps; i++)
            {
                frame++;
                drawn.Y = float.Lerp(drawn.Y, root.Y, 1 - MathF.Exp(-dt / 0.09f));
                native = float.Lerp(native, root.Y + normalFraming, 1 - MathF.Exp(-dt / 0.07f));
                Check(MathF.Abs(native + Correct() - drawn.Y - normalFraming) < 0.01f, "Stopping on a landing avoids a framing kick as native height settles");
            }
            frame++;
            native += 0.4f;
            Check(Correct() == 0, "A new flat-ground camera preset passes through");
            frame++;
            root.Y += 0.4f;
            native += 0.4f;
            Check(Correct() < -0.39f, "An ascending step tracks the lower rendered character immediately");
            Check(height.Update(frame, dt, 2, root, drawn, native) == 0, "A replacement skeleton gets a fresh framing reference");
            frame++;
            root.Y += 20;
            drawn = root;
            Check(Correct() == 0, "A teleport discards stair history");
            Check(height.Update(++frame, dt, 1, root, new(float.NaN), native) == 0, "Unavailable render data falls back to native height");
            height.Reset();
            Check(height.Update(++frame, dt, 1, root, drawn, native) == 0, "Mount, jump or scene transitions start fresh when returning to walking");
        }
    }

    private static void TestShoulderFlight()
    {
        foreach (var fps in new[] { 30, 60, 144 })
        {
            var anchor = new ShoulderMountAnchor();
            float Resolve(uint frameNumber, float seconds, nint mountId, bool mounted, float root, float? head, float native) =>
                anchor.Resolve(frameNumber, seconds, mountId, mounted, new(0, root, 0),
                    head.HasValue ? new Vector3(0, head.Value, 0) : null, new(0, native, 0)).Y;
            var dt = 1f / fps;
            const float seat = 3.5f;
            float y = 50, pivot = 0;
            uint frame = 0;
            foreach (var speed in new[] { 0f, 18f, -24f, 0f })
            {
                for (var i = 0; i < fps; ++i)
                {
                    y += speed * dt;
                    pivot = Resolve(++frame, dt, 1, true, y, y + seat, y - 4);
                    if (i > fps / 4)
                        Check(Near(pivot, y + seat - 0.12f), "Hover, climb and dive retain rider shoulder height");
                }
            }
            var missing = Resolve(++frame, dt, 1, true, y + 2, null, y - 4);
            Check(Near(missing, pivot + 2), "A missing pose still tracks live altitude");
            Check(Near(missing, Resolve(frame, dt, 1, true, y + 2, null, y - 4)), "Duplicate camera query is stable");
            for (var i = 0; i < fps; i++) pivot = Resolve(++frame, dt, 1, true, y, float.NaN, y - 4);
            Check(Near(pivot, y - 4), "Extended pose loss returns to native height");
            for (var i = 0; i < fps; i++) pivot = Resolve(++frame, dt, 1, true, y, y + seat, y - 4);
            for (var i = 0; i < fps; i++) pivot = Resolve(++frame, dt, 1, false, y, null, y + 1);
            Check(Near(pivot, y + 1), "Dismounting returns to the native ground pivot");
            Resolve(++frame, dt, 1, true, y, y + seat, y);
            Check(Near(Resolve(++frame, dt, 2, true, y, null, y), y), "Changing mounts discards the former seat");
            Vector3 groundPivot = default;
            for (var i = 0; i < fps; i++) groundPivot = anchor.Resolve(++frame, dt, 2, true, new(10, 0, 2),
                new(11, 3, 4), new(10, 1, 2));
            Check(Vector3.Distance(groundPivot, new(11, 2.88f, 4)) < 0.002f, "A long ground mount centers on the rider's saddle in all three axes");
        }
    }
}
