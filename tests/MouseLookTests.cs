using PovPlus;
using FFXIVClientStructs.FFXIV.Client.Game.Control;

internal static partial class Program
{
    private static unsafe void TestMouseLook()
    {
        var state = new MouseLookState();
        bool Frame(bool key = false, bool toggle = false, bool eligible = true, bool paused = false, bool mouse = false, int binding = 18)
            => state.Update(eligible, paused, key, toggle, binding, mouse);
        Check(Frame(), "Mouse look starts without physical RMB");
        Check(Frame(mouse: true), "Holding left during active mouse-look keeps capture for free observation");
        Check(Frame(), "Releasing left keeps mouse-look capture instead of recentering cursor");
        Check(!Frame(key: true), "Hold shortcut frees cursor");
        for (var i = 0; i < 120; i++) Check(!Frame(key: true), "Hold release lasts while key is down");
        Check(Frame(), "Key up resumes hold-mode mouse look");
        Check(!Frame(eligible: false), "Third person / disable / focus loss releases cursor");
        Check(!Frame(key: true), "Entering first person with held release key leaves cursor free");
        Check(Frame(), "Release key restores first-person capture");
        Check(Frame(toggle: true), "Changing mode resets toggle latch");
        Check(!Frame(key: true, toggle: true), "First press toggles cursor free");
        for (var i = 0; i < 120; i++) Check(!Frame(key: true, toggle: true), "Held key does not repeat toggle");
        Check(!Frame(toggle: true), "Toggle remains free after key up");
        Check(Frame(key: true, toggle: true), "Second press recaptures");
        Check(!Frame(key: true, toggle: true, paused: true), "Settings and typing release capture");
        Check(!Frame(toggle: true, paused: true), "Pause stays free after key up");
        Check(!Frame(key: true, toggle: true, paused: true), "Typing shortcut during pause does not toggle");
        Check(Frame(key: true, toggle: true), "Leaving text input does not reinterpret held key as press");
        Frame(toggle: true);
        Check(!Frame(key: true, toggle: true), "Toggle release works after pause");
        Check(!Frame(toggle: true, paused: true), "Temporarily paused toggle release");
        Check(!Frame(toggle: true), "User toggle-release preference survives temporary UI pause");
        Check(!Frame(toggle: true, eligible: false), "Exit cancels toggle release state");
        Check(Frame(toggle: true), "Reentry defaults to mouse look");
        Check(Frame(key: true, toggle: true, binding: 70), "Rebinding while key held does not manufacture toggle edge");
        Check(!Frame(toggle: true, paused: true, binding: 70), "Drag setup releases");
        Check(!Frame(toggle: true, mouse: true, binding: 70), "Do not recapture midway through UI drag");
        Check(Frame(toggle: true, binding: 70), "Recapture when drag ends");
        state.Suspend();
        Check(!Frame(toggle: true, mouse: true, binding: 70), "Post-input chat/watchdog release also prevents recapture during UI drag");
        state.Reset();
        Check(!state.Captured && Frame(), "Unload/reset drops state");

        // Test ownership against real installed InputManager struct layout.
        var manager = new InputManager { MouseButtonHoldStateRaw = 0, MouseDragDistance = 2, MouseDragDeltaX = 3,
            MouseDragDeltaY = 4, MouseDragStartX = 50, MouseDragStartY = 60, MouseDragActive = false, CursorRectActive = true };
        var patch = new MouseLookDragPatch();
        patch.Apply(&manager, 800, 450, 8, -9);
        Check(manager.HeldMouseButtons == InputManager.MouseButtonHoldState.Right && manager.MouseDragActive,
            "Native drag uses right-look state without OS button injection");
        Check(manager.MouseDragDeltaX == -8 && manager.MouseDragDeltaY == 9 && manager.MouseDragDistance > 10,
            "Native delta sign and drag threshold match client");
        patch.Apply(&manager, 800, 450, 2, 1);
        patch.Restore();
        Check(manager.MouseButtonHoldStateRaw == 0 && manager.MouseDragDistance == 2 && manager.MouseDragDeltaX == 3 &&
            manager.MouseDragDeltaY == 4 && manager.MouseDragStartX == 50 && manager.MouseDragStartY == 60 && !manager.MouseDragActive,
            "Repeated application restores original drag tuple");
        Check(manager.CursorRectActive, "Never overwrites unrelated input-manager state");
        patch.Apply(&manager, 800, 450, 8, -9);
        patch.Apply(&manager, 800, 450, 8, -9, freeLook: true);
        Check(manager.HeldMouseButtons == InputManager.MouseButtonHoldState.Left && manager.MouseDragActive,
            "Left free-look replaces simulated right instead of combining both buttons");
        Check(manager.MouseDragDeltaX == -8 && manager.MouseDragDeltaY == 9,
            "Switching to free-look preserves the current frame's mouse delta");
        for (var i = 0; i < 20; i++) patch.Apply(&manager, 800, 450, 0, 0, freeLook: true);
        Check(manager.HeldMouseButtons == InputManager.MouseButtonHoldState.Left, "Repeated UI/camera queries preserve left-only free-look");
        patch.Apply(&manager, 800, 450, 2, 1);
        Check(manager.HeldMouseButtons == InputManager.MouseButtonHoldState.Right, "Left release immediately restores normal right-look input");
        patch.Restore();
        Check(manager.MouseButtonHoldStateRaw == 0 && !manager.MouseDragActive && manager.MouseDragDeltaX == 3,
            "Switching look modes never replaces the original restore snapshot with a virtual drag");
        patch.Apply(&manager, 800, 450, 0, 0, freeLook: true);
        patch.Restore();
        Check(manager.MouseButtonHoldStateRaw == 0 && !manager.MouseDragActive, "Shortcut/focus/disable release while free-looking restores ordinary input");
        patch.Apply(&manager, 800, 450, 0, 0);
        manager.HeldMouseButtons = InputManager.MouseButtonHoldState.Left;
        manager.MouseDragDeltaX = 99;
        patch.Restore();
        Check(manager.HeldMouseButtons == InputManager.MouseButtonHoldState.Left && manager.MouseDragDeltaX == 99,
            "Fresh native/other-plugin drag changes take precedence over old snapshot");
        patch.Restore();
        Check(manager.MouseDragDeltaX == 99, "Release is idempotent");
    }

    private static void TestExtraDown()
    {
        var r = FirstPersonMath.DegreesToRadians;
        var axis = new BoundaryLookAxis();
        var down = 75 * r;
        axis.SetExtraDownLimit(BoundaryLookAxis.ExtraDownRange(10, down));
        var angle = -down;
        float Move(float delta, float sensitivity = 1) => angle = axis.Apply(angle, delta * r, -down, 60 * r, 0, true, false, sensitivity);
        Move(-7.5f);
        Check(Near(angle, -80 * r) && Near(axis.ExtraDown, 5 * r) && Near(axis.Excess, -7.5f * r),
            "Half neck extension and half additional nod occur together");
        for (var i = 0; i < 120; i++) { Move(0); Check(Near(angle, -80 * r), "Idle holds the blended position and angle"); }
        Move(-7.5f);
        Check(Near(angle, -85 * r) && Near(axis.ExtraDown, 10 * r) && Near(axis.Excess, -15 * r),
            "Neck extension and additional nod reach their maxima together");
        Move(-1000);
        Check(Near(angle, -85 * r), "Full outward input creates no additional stage or hidden debt");
        Move(3);
        Check(Near(angle, -83 * r) && Near(axis.Excess, -12 * r), "Reverse input lifts head and retracts neck simultaneously");
        Move(20);
        Check(Near(angle, -67 * r) && axis.Excess == 0 && axis.ExtraDown == 0,
            "Remaining reverse input resumes ordinary looking in the same frame");
        axis.Reset(); angle = -down; Move(-200);
        axis.SetExtraDownLimit(0); Move(0);
        Check(angle >= -down - 0.00001f && axis.ExtraDown == 0 && Near(axis.Excess, -15 * r),
            "Disabling additional nod retains neck travel and bounds the camera");
        Check(Near(BoundaryLookAxis.ExtraDownRange(20, 85 * r), 10 * r), "Total nod bounded to five degrees past vertical");
        Check(Near(BoundaryLookAxis.ExtraDownRange(20, 75 * r), 20 * r), "Default ordinary limit can reach 95 degrees");
        var beyondVertical = new BoundaryLookAxis();
        beyondVertical.SetExtraDownLimit(BoundaryLookAxis.ExtraDownRange(20, down));
        var pastVertical = beyondVertical.Apply(-down, -100 * r, -down, 60 * r, 0, true, false);
        Check(Near(pastVertical, -95 * r), "Blended extension preserves the requested minus five degrees beyond straight down");
        Check(BoundaryLookAxis.ExtraDownRange(-10, down) == 0 && Near(BoundaryLookAxis.ExtraDownRange(float.NaN, down), 10 * r),
            "Corrupt extra-angle settings sanitized");

        foreach (var sensitivity in new[] { 0.1f, 0.35f, 1f })
        foreach (var frames in new[] { 1, 30, 60, 144 })
        {
            var split = new BoundaryLookAxis();
            split.SetExtraDownLimit(10 * r);
            var value = -down;
            for (var f = 0; f < frames; f++) value = split.Apply(value, -12 * r / sensitivity / frames,
                -down, 60 * r, 0, true, false, sensitivity);
            Check(Near(value, -83 * r) && Near(split.ExtraDown, 8 * r) && Near(split.Excess, -12 * r),
                "Blended position and angle are frame-rate independent at all sensitivities");
            for (var f = 0; f < frames; f++) value = split.Apply(value, 12 * r / sensitivity / frames,
                -down, 60 * r, 0, true, false, sensitivity);
            Check(Near(value, -down) && MathF.Abs(split.Excess) < 0.00001f && split.ExtraDown == 0,
                "Blended position and angle reverse symmetrically at all sensitivities");
        }
        axis.Reset(); angle = -down; axis.SetExtraDownLimit(10 * r);
        for (var step = 1; step <= 150; step++)
        {
            Move(-0.1f);
            var position = LookDownLean.Target(new System.Numerics.Vector2(-axis.Excess / BoundaryLookAxis.ExtensionTravel, 0), 0.08f, 0.06f).X;
            Check(Near(position / 0.08f, axis.ExtraDown / (10 * r)), "Every progress sample blends angle and position with the same weight");
        }
        axis.UpdateLimits(-70 * r, 60 * r);
        Check(axis.ExtraDown == 0 && axis.Excess == 0, "Changing ordinary limits resets the entire blended gesture");
        Move(-20); axis.Reset();
        Check(axis.ExtraDown == 0, "Exit / disable / model change clears blended nod");
    }
}
