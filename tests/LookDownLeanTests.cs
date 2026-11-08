using System.Numerics;
using PovPlus;

internal static partial class Program
{
    private static void TestLookDownLean()
    {
        var r = FirstPersonMath.DegreesToRadians;
        const float front = 0.08f, side = 0.06f;
        var edge = 80 * r;
        var band = 12 * r;
        var axis = new BoundaryLookAxis();
        var angle = axis.Apply(0, 70 * r, -edge, edge, 0, true, true);
        Check(Near(angle, 70 * r) && axis.Excess == 0, "Ordinary rotation does not extend");
        angle = axis.Apply(angle, 10 * r, -edge, edge, 0, true, true);
        Check(Near(angle, edge) && MathF.Abs(axis.Excess) < 0.000001f, "Reaching exact hard edge does not extend");
        for (var i = 0; i < 100; i++) angle = axis.Apply(angle, 0, -edge, edge, 0, true, true);
        Check(Near(angle, edge) && MathF.Abs(axis.Excess) < 0.000001f, "Waiting at edge never accumulates extension");
        angle = axis.Apply(angle, 5 * r, -edge, edge, 0, true, true);
        Check(Near(angle, edge) && Near(axis.Excess, 5 * r), "Only continued outward input extends");
        angle = axis.Apply(angle, -2 * r, -edge, edge, 0, true, true);
        Check(Near(angle, edge) && Near(axis.Excess, 3 * r), "Reverse drag retracts extension before turning");
        angle = axis.Apply(angle, -6 * r, -edge, edge, 0, true, true);
        Check(Near(angle, 77 * r) && axis.Excess == 0, "Unused reverse drag resumes normal rotation");
        axis.Reset();
        angle = axis.Apply(edge, 100 * r, -edge, edge, band, true, true);
        for (var i = 0; i < 100; i++) angle = axis.Apply(angle, 100 * r, -edge, edge, band, true, true);
        Check(Near(axis.Excess, BoundaryLookAxis.ExtensionTravel), "Holding outward movement cannot exceed maximum extension travel");
        angle = axis.Apply(angle, -r, -edge, edge, band, true, true);
        Check(Near(axis.Excess, 14 * r) && Near(angle, edge), "Long outward drag creates no hidden reverse-input debt");

        axis.Reset();
        angle = axis.Apply(67 * r, 5 * r, -edge, edge, band, true, true);
        Check(angle > 68 * r && angle < 72 * r && axis.Excess == 0, "Soft slowing is not mistaken for overflow");
        axis.Reset();
        angle = axis.Apply(68 * r, 12 * r, -edge, edge, band, true, true);
        Check(angle < edge && axis.Excess == 0, "Raw input reaching hard angle while softened remains rotation only");
        var softCost = band / 0.8f * MathF.Log(5);
        axis.Reset();
        angle = axis.Apply(68 * r, softCost + 3 * r, -edge, edge, band, true, true);
        Check(Near(angle, edge) && Near(axis.Excess, 3 * r), "Soft zone input cost is consumed before extension starts");
        var reflected = new BoundaryLookAxis();
        var negative = reflected.Apply(-68 * r, -softCost - 3 * r, -edge, edge, band, true, true);
        Check(Near(negative, -angle) && Near(reflected.Excess, -axis.Excess), "Left and right boundaries are symmetric");

        (float Angle, float Excess) Travel(int steps)
        {
            var limiter = new BoundaryLookAxis();
            var value = 60 * r;
            for (var i = 0; i < steps; i++) value = limiter.Apply(value, 42 * r / steps, -edge, edge, band, true, true);
            return (value, limiter.Excess);
        }
        foreach (var count in new[] { 1, 2, 30, 60, 144, 360 })
        {
            var result = Travel(count);
            Check(Near(result.Angle, Travel(1).Angle) && Near(result.Excess, Travel(1).Excess),
                "Splitting the same input across callbacks preserves turn and extension");
        }

        var pitch = new BoundaryLookAxis();
        var down = 75 * r;
        var up = 60 * r;
        var nativeBefore = -down;
        var nativeDelta = -2 * r;
        var nativeAfterClamp = Math.Clamp(nativeBefore + nativeDelta, -down, up);
        Check(Near(nativeAfterClamp, nativeBefore), "Native pitch angle alone loses outward input at the edge");
        var limitedPitch = pitch.Apply(nativeBefore, nativeDelta, -down, up, band, true, false);
        Check(Near(limitedPitch, -down) && Near(pitch.Excess, -2 * r), "Processed native pitch input recovers clipped downward travel");
        pitch.Reset();
        pitch.Apply(up, 10 * r, -down, up, band, true, false);
        Check(pitch.Excess == 0, "Looking beyond upper edge never causes forward extension");
        pitch.Apply(-down, -10 * r, -down, up, band, false, false);
        Check(pitch.Excess == 0, "Disabled forward amplitude does not accumulate travel");
        pitch.Apply(-down, -5 * r, -down, up, band, true, false);
        var heldPitch = pitch.Excess;
        for (var i = 0; i < 600; i++)
        {
            pitch.UpdateLimits(-down, up);
            pitch.Apply(-down + MathF.Sin(i) * r, 0, -down, up, band, true, false);
            Check(pitch.Excess == heldPitch, "Idle camera/animation jitter never releases held forward extension");
        }
        pitch.Apply(-50 * r, 0, -down, up, band, true, false);
        Check(pitch.Excess == heldPitch, "Automatic camera recenter without mouse input preserves extension");
        var recoveringPitch = pitch.Apply(-50 * r, 2 * r, -down, up, band, true, false);
        Check(Near(pitch.Excess, -3 * r) && Near(recoveringPitch, -50 * r), "Reverse drag retracts without snapping an automatically moved camera back to its limit");
        pitch.UpdateLimits(-70 * r, up);
        Check(pitch.Excess == 0, "Changing original angle limits clears old extension");
        axis.Apply(edge, 5 * r, -edge, edge, band, true, true);
        axis.Reset();
        Check(axis.Excess == 0, "Mode, player and feature reset clears extension");
        axis.Apply(edge, float.NaN, -edge, edge, band, true, true);
        Check(axis.Excess == 0, "Non-finite input cannot create extension");

        var slower = new BoundaryLookAxis();
        var normalTurn = slower.Apply(0, 50 * r, -edge, edge, band, true, true, 0.35f);
        Check(Near(normalTurn, 50 * r) && slower.Excess == 0, "Extension sensitivity does not slow normal turning");
        slower.Apply(edge, 10 * r, -edge, edge, band, true, true, 0.35f);
        Check(Near(slower.Excess, 3.5f * r), "35 percent sensitivity proportionally reduces extension speed");
        slower.Apply(edge - r, -5 * r, -edge, edge, band, true, true, 0.35f);
        Check(Near(slower.Excess, 1.75f * r), "Reduced sensitivity also applies to retraction despite angle drift");
        var reverseToTurn = slower.Apply(edge, -10 * r, -edge, edge, band, true, true, 0.35f);
        Check(slower.Excess == 0 && Near(reverseToTurn, edge - 5 * r), "Reverse input left after retracting resumes normal turning at full sensitivity");
        slower.Reset();
        var crossing = slower.Apply(75 * r, 10 * r, -edge, edge, 0, true, true, 0.35f);
        Check(Near(crossing, edge) && Near(slower.Excess, 5 * r * 0.35f), "Only the input past the boundary uses reduced sensitivity");
        var heldSide = slower.Excess;
        for (var i = 0; i < 600; i++)
        {
            slower.Apply(edge - MathF.Abs(MathF.Cos(i)) * 2 * r, 0, -edge, edge, band, true, true, 0.35f);
            Check(slower.Excess == heldSide, "Idle camera/animation jitter never releases held side extension");
        }
        slower.Apply(edge, 1 * r, -edge, edge, band, true, true, 0.1f);
        Check(Near(slower.Excess, heldSide + 0.1f * r), "Changing sensitivity affects future mouse travel without rescaling the held position");
        Check(BoundaryLookAxis.SanitizeSensitivity(float.NaN) == 0.35f && BoundaryLookAxis.SanitizeSensitivity(0) == 0.1f &&
            BoundaryLookAxis.SanitizeSensitivity(5) == 1, "Sensitivity settings have safe bounds");
        slower.Reset();
        // Full native adjusted delta can include automatic following. Only the
        // captured one-degree mouse component is sent to the extension stage.
        var automatic = 35 * r;
        var manual = 1 * r;
        var nativeFacing = edge + automatic + manual;
        slower.Apply(nativeFacing - manual, manual, -edge, edge, 0, true, true, 0.35f);
        Check(Near(slower.Excess, 0.35f * r), "Automatic camera correction cannot become a large extension jump");

        Check(LookDownLean.Target(Vector2.Zero, front, side) == Vector2.Zero, "No extra input means no position offset at any look angle");
        Check(LookDownLean.Target(new Vector2(1, 0), front, side) == new Vector2(front, 0), "Full downward overflow only extends forward");
        var left = LookDownLean.Target(new Vector2(1, 1), front, side);
        var right = LookDownLean.Target(new Vector2(1, -1), front, side);
        Check(left == new Vector2(front, side) && right == new Vector2(front, -side), "Independent limit overflow can combine diagonally");
        Check(LookDownLean.Target(new Vector2(0, 1), front, side) == new Vector2(0, side), "Side boundary triggers without reaching downward boundary");
        Check(LookDownLean.Target(new Vector2(1, 1), 0, side).X == 0 && LookDownLean.Target(new Vector2(1, 1), front, 0).Y == 0,
            "Forward and side amplitude remain independent");
        Check(LookDownLean.Target(new Vector2(0.25f, -0.25f), front, side) == new Vector2(front * 0.25f, -side * 0.25f), "Quarter mouse travel produces exactly quarter extension without easing acceleration");
        Check(LookDownLean.Target(new Vector2(float.NaN, float.PositiveInfinity), front, side) == Vector2.Zero, "Invalid pressure cannot displace the camera");
        Check(LookDownLean.Sanitize(-1, front) == 0 && LookDownLean.Sanitize(99, front) == LookDownLean.Maximum && LookDownLean.Sanitize(float.NaN, front) == front,
            "Invalid saved amplitudes are bounded");
        for (var i = -200; i <= 200; i++)
        {
            var value = LookDownLean.Target(new Vector2(i / 100f, i / 100f), front, side);
            Check(value.X >= 0 && value.X <= front && MathF.Abs(value.Y) <= side, "Extreme pressure remains within distance sliders");
        }
        foreach (var body in new[] { 0f, 90f, -90f, 179f, -179f })
        {
            var heading = body * r;
            var forward = new Vector3(MathF.Sin(heading), 0, MathF.Cos(heading));
            var tangent = new Vector3(MathF.Cos(heading), 0, -MathF.Sin(heading));
            var world = LookDownLean.ToWorld(left, heading);
            Check(Near(Vector3.Dot(world, forward), left.X) && Near(Vector3.Dot(world, tangent), left.Y) && world.Y == 0,
                "Extension follows body orientation and preserves camera height");
        }
        Check(LookDownLean.ToWorld(left, float.NaN) == Vector3.Zero, "Invalid body heading fails closed");

        var stationary = LookDownLean.Target(new Vector2(0.31f, -0.27f), front, side);
        for (var frame = 0; frame < 1000; frame++)
            Check(LookDownLean.Target(new Vector2(0.31f, -0.27f), front, side) == stationary,
                "Repeated frames neither chase a target nor retract a stationary mouse position");
        var earlier = LookDownLean.Target(new Vector2(0.1f, 0.1f), front, side);
        var later = LookDownLean.Target(new Vector2(0.2f, 0.2f), front, side);
        Check(Near(later.X - earlier.X, front * 0.1f) && Near(later.Y - earlier.Y, side * 0.1f),
            "New input is immediately visible even if an earlier camera callback ran in the same frame");
        Check(LookDownLean.Target(Vector2.Zero, front, side) == Vector2.Zero, "Explicit reset leaves no delayed movement");
        Check(LookDownLean.Target(new Vector2(0.4f, 0.6f), 0, side) == new Vector2(0, side * 0.6f),
            "Disabling one amplitude immediately leaves the other axis alone");
        Check(LookDownLean.Target(new Vector2(0.4f, 0.6f), front, 0.01f).Y <= 0.01f,
            "Reducing amplitude bounds the displayed offset immediately");
        Vector2 Sample(int fps)
        {
            var limiter = new BoundaryLookAxis();
            var value = edge;
            // Same physical mouse travel over half a second, sampled at different rates.
            for (var frame = 0; frame < fps / 2; frame++)
                value = limiter.Apply(value, 20 * r / (fps / 2), -edge, edge, band, true, true, 0.35f);
            return LookDownLean.Target(new Vector2(0, limiter.Excess / BoundaryLookAxis.ExtensionTravel), front, side);
        }
        foreach (var fps in new[] { 30, 60, 120, 144 })
            Check(Vector2.Distance(Sample(fps), Sample(60)) < 0.00001f, "Mouse-to-distance response does not depend on frame rate");
        var continuity = new BoundaryLookAxis();
        var immediatelyBefore = continuity.Apply(edge - 0.001f * r, 0, -edge, edge, 0, true, true, 0.35f);
        continuity.Apply(immediatelyBefore, 0.002f * r, -edge, edge, 0, true, true, 0.35f);
        var firstOffset = LookDownLean.Target(new Vector2(0, continuity.Excess / BoundaryLookAxis.ExtensionTravel), front, side);
        Check(firstOffset.Y > 0 && firstOffset.Y < 0.00001f, "Crossing the limit with tiny mouse travel has no activation jump");
        var displayedBeforeReverse = firstOffset.Y;
        continuity.Apply(edge, -0.0005f * r, -edge, edge, 0, true, true, 0.35f);
        var displayedAfterReverse = LookDownLean.Target(new Vector2(0, continuity.Excess / BoundaryLookAxis.ExtensionTravel), front, side).Y;
        Check(displayedAfterReverse < displayedBeforeReverse && displayedAfterReverse > 0, "Small reverse travel immediately makes a proportional retraction");
        var applied = LookDownLean.ToWorld(new Vector2(0.3f, 0.3f), 0);
        var eye = applied + new Vector3(0, 0.1f, 0);
        var visibility = new HeadHideTransition();
        Check(visibility.Update(true, true, Vector3.Distance(eye, applied), eye.Length(), 0.25f, 0.18f), "Hide timing uses actual shifted destination");
        Check(visibility.Update(false, true, 0, 0.3f, 0.25f, 0.18f, 0.35f), "Exit still waits for head clearance");
        Check(!visibility.Update(false, true, 0, 0.4f, 0.25f, 0.18f, 0.35f), "Exit release distance is unchanged");
    }
}
