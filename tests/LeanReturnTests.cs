using PovPlus;

internal static partial class Program
{
    private static void TestLeanAutomaticReturn()
    {
        var r = FirstPersonMath.DegreesToRadians;
        var down = 75 * r;
        var up = 60 * r;
        foreach (var extra in new[] { 0f, 10f, 20f })
        foreach (var sensitivity in new[] { 0.1f, 0.35f, 1f })
        {
            var axis = new BoundaryLookAxis();
            axis.SetExtraDownLimit(extra * r);
            var angle = axis.Apply(-down, -15 * r / sensitivity, -down, up, 0, true, false, sensitivity);
            var cleared = false;
            // Reproduce: extend, release RMB, move (native auto-return), then RMB up-drag.
            while (angle < -0.000001f)
            {
                var next = MathF.Min(0, angle + r);
                var released = axis.ReleaseAfterAutomaticReturn(next, 0, next - angle, -down, up, false);
                if (next < -down)
                    Check(!released, "Do not drop extra pitch bounds while native auto-return is still beyond ordinary down limit");
                if (released) cleared = true;
                angle = next;
            }
            Check(cleared && axis.Excess == 0 && axis.ExtraDown == 0, "Movement auto-return retires old forward/nod gesture");
            var controlled = axis.Apply(angle, 2 * r, -down, up, 0, true, false, sensitivity);
            Check(Near(controlled, 2 * r), "First RMB input at centre rotates immediately at ordinary sensitivity without retracting debt");
        }

        var pitch = new BoundaryLookAxis();
        pitch.SetExtraDownLimit(10 * r);
        pitch.Apply(-down, -15 * r, -down, up, 0, true, false);
        var explicitReset = new BoundaryLookAxis();
        explicitReset.SetExtraDownLimit(20 * r);
        var resetFrom = explicitReset.Apply(-down, -15 * r, -down, up, 0, true, false);
        Check(explicitReset.ReleaseAfterAutomaticReturn(0, 0, -resetFrom, -down, up, false),
            "Explicit native angle reset clears extension even when no incremental auto-input ran");
        Check(Near(explicitReset.Apply(0, r, -down, up, 0, true, false, 0.1f), r),
            "Right-drag input after explicit native reset has no residual debt");
        var saved = pitch.Excess;
        for (var frame = 0; frame < 600; frame++)
        {
            Check(!pitch.ReleaseAfterAutomaticReturn(-50 * r + MathF.Sin(frame) * r, 0, 0, -down, up, false),
                "Animation or pose changes without verified automatic input never release stationary extension");
        }
        Check(pitch.Excess == saved, "Idle extension stays held");
        Check(!pitch.ReleaseAfterAutomaticReturn(-74 * r, 0, r, -down, up, false), "One-degree near-boundary jitter does not release");
        Check(!pitch.ReleaseAfterAutomaticReturn(-40 * r, 0, 20 * r, -down, up, true), "Physical RMB or active mouse-look protects held gesture");
        Check(!pitch.ReleaseAfterAutomaticReturn(-40 * r, r, 20 * r, -down, up, false), "Manual camera input takes precedence over automatic release");
        Check(!pitch.ReleaseAfterAutomaticReturn(-40 * r, 0, -r, -down, up, false), "Outward correction is not an automatic return");
        Check(!pitch.ReleaseAfterAutomaticReturn(float.NaN, 0, r, -down, up, false), "Invalid observed angle cannot cancel gesture");
        Check(!pitch.ReleaseAfterAutomaticReturn(0, 0, float.NaN, -down, up, false), "Invalid automatic input cannot cancel gesture");
        Check(pitch.ReleaseAfterAutomaticReturn(-40 * r, 0, 20 * r, -down, up, false), "Confirmed released-camera auto-return inside ordinary range clears gesture");
        Check(Near(pitch.Apply(-40 * r, r, -down, up, 0, true, false, 0.1f), -39 * r),
            "RMB can resume partway through auto-return without sensitivity penalty");

        foreach (var sign in new[] { -1f, 1f })
        {
            var yaw = new BoundaryLookAxis();
            yaw.Apply(sign * 80 * r, sign * 10 * r, -80 * r, 80 * r, 0, true, true);
            Check(yaw.ReleaseAfterAutomaticReturn(sign * 30 * r, 0, -sign * 50 * r, -80 * r, 80 * r, false),
                "Left and right side extension are both released by inward auto-return");
            Check(Near(yaw.Apply(0, sign * r, -80 * r, 80 * r, 0, true, true, 0.35f), sign * r),
                "Side-look control resumes immediately after recenter");
        }
        var sideOnly = new BoundaryLookAxis();
        sideOnly.Apply(80 * r, 10 * r, -80 * r, 80 * r, 0, true, true);
        var forwardOnly = new BoundaryLookAxis();
        forwardOnly.Apply(-down, -10 * r, -down, up, 0, true, false);
        forwardOnly.ReleaseAfterAutomaticReturn(0, 0, down, -down, up, false);
        Check(sideOnly.Excess > 0, "An axis auto-return does not discard another axis without evidence");
    }
}
