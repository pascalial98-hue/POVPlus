using System.Numerics;
using PovPlus;

internal static partial class Program
{
    private static void TestSideLeanAngle()
    {
        var r = FirstPersonMath.DegreesToRadians;
        var edge = 80 * r;
        foreach (var sign in new[] { -1f, 1f })
        {
            var axis = new BoundaryLookAxis();
            axis.SetExtraSideLimit(BoundaryLookAxis.ExtraSideRange(10));
            var angle = axis.Apply(sign * 70 * r, sign * 10 * r, -edge, edge, 0, true, true);
            Check(Near(angle, sign * edge) && axis.Excess == 0 && axis.ExtraAngle == 0,
                "Reaching ordinary yaw limit alone adds no side travel or angle");
            angle = axis.Apply(angle, sign * 7.5f * r, -edge, edge, 0, true, true);
            var offset = LookDownLean.Target(new Vector2(0, axis.Excess / BoundaryLookAxis.ExtensionTravel), 0.08f, 0.06f);
            Check(Near(angle, sign * 85 * r) && Near(axis.ExtraAngle, sign * 5 * r) && Near(offset.Y, sign * 0.03f),
                "Half side extension blends half the extra outward angle symmetrically");
            for (var frame = 0; frame < 100; frame++)
            {
                angle = axis.Apply(angle, 0, -edge, edge, 0, true, true);
                Check(Near(angle, sign * 85 * r) && Near(axis.Excess, sign * 7.5f * r), "Idle holds side position and angle without drifting");
            }
            angle = axis.Apply(angle, sign * 7.5f * r, -edge, edge, 0, true, true);
            Check(Near(angle, sign * 90 * r) && Near(axis.ExtraAngle, sign * 10 * r), "Full side extension reaches configured extra angle");
            angle = axis.Apply(angle, sign * 1000 * r, -edge, edge, 0, true, true);
            Check(Near(angle, sign * 90 * r), "Overshooting maximum creates no extra side rotation or debt");
            angle = axis.Apply(angle, -sign * 3 * r, -edge, edge, 0, true, true);
            Check(Near(angle, sign * 88 * r) && Near(axis.Excess, sign * 12 * r), "Reverse side input retracts position and angle together");
            angle = axis.Apply(angle, -sign * 20 * r, -edge, edge, 0, true, true);
            Check(Near(angle, sign * 72 * r) && axis.Excess == 0, "Remaining reverse input immediately returns to normal yaw");

            axis.Reset(); axis.SetExtraSideLimit(20 * r);
            angle = axis.Apply(sign * edge, sign * 15 * r, -edge, edge, 0, true, true);
            Check(Near(angle, sign * 100 * r), "Maximum extra slider adds 20 degrees to ordinary 80-degree side limit");
            Check(!axis.ReleaseAfterAutomaticReturn(sign * 90 * r, 0, -sign * 10 * r, -edge, edge, false),
                "Auto-return does not drop side bounds while camera remains outside ordinary range");
            Check(axis.ReleaseAfterAutomaticReturn(sign * 30 * r, 0, -sign * 60 * r, -edge, edge, false),
                "Confirmed auto-return clears side extension including extra yaw");
            Check(Near(axis.Apply(0, sign * r, -edge, edge, 0, true, true, 0.1f), sign * r),
                "Extra side rotation preserves the previous fix for input after auto-return");
        }

        foreach (var body in new[] { 0f, 90f, 179f, -179f })
        foreach (var sign in new[] { -1f, 1f })
        {
            var axis = new BoundaryLookAxis();
            axis.SetExtraSideLimit(20 * r);
            var relative = axis.Apply(sign * edge, sign * 15 * r, -edge, edge, 0, true, true);
            var facing = FirstPersonMath.Wrap(body * r + relative);
            var limiter = new SoftLookLimit();
            limiter.Sync(facing);
            for (var callback = 0; callback < 15; callback++)
            {
                facing = limiter.Apply(facing, body * r, -edge + Math.Min(0, axis.ExtraAngle), edge + Math.Max(0, axis.ExtraAngle), 12 * r);
                Check(Near(FirstPersonMath.Wrap(facing - body * r), relative),
                    "Render callbacks retain earned extra yaw across world-angle wrap without shrinking it");
            }
        }
        foreach (var sign in new[] { -1f, 1f })
        foreach (var fps in new[] { 1, 30, 60, 144 })
        foreach (var sensitivity in new[] { 0.1f, 0.35f, 1f })
        {
            var axis = new BoundaryLookAxis();
            axis.SetExtraSideLimit(10 * r);
            var value = sign * edge;
            for (var frame = 0; frame < fps; frame++)
                value = axis.Apply(value, sign * 12 * r / sensitivity / fps, -edge, edge, 12 * r, true, true, sensitivity);
            Check(Near(value, sign * 88 * r) && Near(axis.Excess, sign * 12 * r), "Blended extra yaw is frame-rate independent at every sensitivity");
            for (var frame = 0; frame < fps; frame++)
                value = axis.Apply(value, -sign * 12 * r / sensitivity / fps, -edge, edge, 12 * r, true, true, sensitivity);
            Check(Near(value, sign * edge) && MathF.Abs(axis.Excess) < 0.00001f, "Side extra angle reverses with symmetric sensitivity");
        }
        var disabled = new BoundaryLookAxis();
        disabled.SetExtraSideLimit(0);
        Check(Near(disabled.Apply(edge, 10 * r, -edge, edge, 0, true, true), edge) && disabled.Excess > 0,
            "Zero extra yaw retains the existing translation-only side lean");
        disabled.Reset(); disabled.SetExtraSideLimit(20 * r);
        Check(Near(disabled.Apply(edge, 10 * r, -edge, edge, 0, false, false), edge) && disabled.ExtraAngle == 0,
            "Disabling side amplitude does not unlock extra yaw");
        Check(BoundaryLookAxis.ExtraSideRange(-5) == 0 && Near(BoundaryLookAxis.ExtraSideRange(float.NaN), 10 * r) &&
            Near(BoundaryLookAxis.ExtraSideRange(100), 20 * r), "Corrupt extra-side settings cannot bypass bounds");

        var pitch = new BoundaryLookAxis();
        var yaw = new BoundaryLookAxis();
        pitch.SetExtraDownLimit(20 * r); yaw.SetExtraSideLimit(10 * r);
        var pitchAngle = pitch.Apply(-75 * r, -7.5f * r, -75 * r, 60 * r, 0, true, false);
        var yawAngle = yaw.Apply(edge, 7.5f * r, -edge, edge, 0, true, true);
        var diagonal = LookDownLean.Target(new Vector2(-pitch.Excess, yaw.Excess) / BoundaryLookAxis.ExtensionTravel, 0.08f, 0.06f);
        Check(Near(pitchAngle, -85 * r) && Near(yawAngle, 85 * r) && Near(diagonal.X, 0.04f) && Near(diagonal.Y, 0.03f),
            "Diagonal lean combines independent blended pitch and yaw with forward and side travel");
        yaw.Reset();
        Check(yaw.ExtraAngle == 0 && Near(pitch.ExtraDown, 10 * r), "Resetting side gesture does not erase independent downward lean");
    }
}
