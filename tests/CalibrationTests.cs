using System.Numerics;
using System.Runtime.InteropServices;
using FFXIVClientStructs.FFXIV.Client.Game;
using PovPlus;

internal static partial class Program
{
    private static unsafe void TestCalibration()
    {
        Check(!CalibrationState.ResolveSeen(null, false), "New configuration waits for first-use guide");
        Check(CalibrationState.ResolveSeen(null, true), "Legacy configuration does not interrupt an existing player");
        Check(!CalibrationState.ResolveSeen(false, true), "Pending first use survives a restart before login");
        Check(CalibrationState.ResolveSeen(true, false), "Seen guide is not repeated");
        Check(NearPlanePatch.Default == 0.015f, "New NearPlane default is 0.015");
        Check(NearPlanePatch.Sanitize(0.001f) == 0.001f && NearPlanePatch.Sanitize(0.1f) == 0.1f,
            "Existing saved near-plane values remain valid");

        var state = new CalibrationState();
        var original = new Vector3(0.1f, 0.02f, -0.03f);
        Check(state.End(true) == null, "Idle close cannot overwrite settings");
        Check(state.Begin(original) && state.Active && state.Draft == original, "Draft starts from existing offsets");
        state.Draft.X = 0.2f;
        Check(!state.Begin(Vector3.Zero) && state.Draft.X == 0.2f, "Repeated open retains the active draft");
        Check(state.End(false) == null && !state.Active, "Cancel discards edits");
        Check(state.Begin(original) && state.Draft == original, "Reopen restores saved offsets after cancel");
        state.Draft = new Vector3(0.3f, -0.1f, 0.04f);
        Check(state.End(true) == new Vector3(0.3f, -0.1f, 0.04f) && !state.Active, "Save returns all three offsets together");
        Check(state.End(true) == null, "Window close after Save cannot write twice");
        Check(state.Begin(new Vector3(float.NaN, 4, -8)) && state.Draft == new Vector3(0, 1, -1),
            "Invalid legacy draft values are finite and bounded");
        state.Draft = new Vector3(float.PositiveInfinity, float.NegativeInfinity, 4);
        Check(state.End(true) == new Vector3(0, 0, 1), "Direct numeric entry is sanitized on save");

        foreach (var (field, offset) in new (string, int)[] {
            (nameof(Camera.Distance), 0x124), (nameof(Camera.MinDistance), 0x128),
            (nameof(Camera.MaxDistance), 0x12C), (nameof(Camera.ZoomMode), 0x180),
            (nameof(Camera.ControlMode), 0x184), (nameof(Camera.InterpDistance), 0x18C),
            (nameof(Camera.SavedDistance), 0x198), (nameof(Camera.IsEventCameraAutoControl), 0x1E8) })
            Check((int)Marshal.OffsetOf<Camera>(field) == offset, "Calibration camera field layout: " + field);
        Check((int)CameraZoomMode.FirstPerson == 0 && (int)CameraZoomMode.ThirdPerson == 1,
            "Camera zoom mode matches the existing world-camera wrapper");
        Check(!CalibrationCamera.EnsureThirdPerson(null), "Missing camera does not start calibration");
        var camera = new Camera { ZoomMode = CameraZoomMode.FirstPerson, Distance = 0, InterpDistance = 0,
            MinDistance = 1.5f, MaxDistance = 20, SavedDistance = 6, ControlMode = (CameraControlMode)2 };
        var before = camera;
        Check(CalibrationCamera.EnsureThirdPerson(&camera) && camera.ZoomMode == CameraZoomMode.ThirdPerson &&
            camera.Distance == 6 && camera.InterpDistance == 6, "First-person entry moves both camera distances to saved third person");
        Check(camera.SavedDistance == 6 && camera.ControlMode == before.ControlMode &&
            camera.MinDistance == before.MinDistance && camera.MaxDistance == before.MaxDistance,
            "Calibration preserves saved distance, input mode and zoom limits");
        camera.Distance = 4; camera.InterpDistance = 3;
        Check(CalibrationCamera.EnsureThirdPerson(&camera) && camera.Distance == 4 && camera.InterpDistance == 3,
            "Orbit and zoom remain untouched once in third person");
        Check(OnlyTransitionFieldsChanged(before, camera), "Native transition modifies no unrelated camera fields");

        foreach (var saved in new[] { float.NaN, float.PositiveInfinity, -4, 0, 0.1f, 30, 5 })
        {
            camera = before; camera.SavedDistance = saved;
            var expected = !float.IsFinite(saved) || saved <= 0 ? 3 : Math.Clamp(saved, 1.5f, 20);
            Check(CalibrationCamera.EnsureThirdPerson(&camera) && camera.Distance == expected && camera.InterpDistance == expected,
                "Saved third-person distance is finite and within native bounds");
        }
        foreach (var (min, max) in new[] { (float.NaN, 20f), (float.PositiveInfinity, 20f), (1f, float.NaN),
            (1f, float.PositiveInfinity), (2f, 1f), (0f, 0f) })
        {
            camera = before; camera.MinDistance = min; camera.MaxDistance = max;
            var snapshot = camera;
            Check(!CalibrationCamera.EnsureThirdPerson(&camera) && SameCamera(snapshot, camera),
                "Invalid native camera limits leave every byte untouched");
        }
        camera = before; camera.ZoomMode = (CameraZoomMode)9;
        var special = camera;
        Check(!CalibrationCamera.EnsureThirdPerson(&camera) && SameCamera(special, camera), "Special camera mode is not forced into third person");
        camera = before; camera.IsEventCameraAutoControl = true;
        special = camera;
        Check(!CalibrationCamera.EnsureThirdPerson(&camera) && SameCamera(special, camera), "Event camera stays untouched");

        static bool SameCamera(Camera a, Camera b) =>
            new ReadOnlySpan<byte>(&a, sizeof(Camera)).SequenceEqual(new ReadOnlySpan<byte>(&b, sizeof(Camera)));
        static bool OnlyTransitionFieldsChanged(Camera a, Camera b)
        {
            a.ZoomMode = b.ZoomMode; a.Distance = b.Distance; a.InterpDistance = b.InterpDistance;
            return SameCamera(a, b);
        }
    }
}
