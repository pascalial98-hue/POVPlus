using System.Runtime.InteropServices;
using PovPlus;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Control;

internal static partial class Program
{
    private static unsafe void TestStationaryLook()
    {
        Check(StationaryLook.Protects(CharacterModes.EmoteLoop), "All stationary emote-loop poses protected, not one emote ID");
        Check(StationaryLook.Protects(CharacterModes.InPositionLoop), "Placed sitting/dozing loops protected");
        foreach (var mode in Enum.GetValues<CharacterModes>())
            if (mode is not (CharacterModes.EmoteLoop or CharacterModes.InPositionLoop))
                Check(!StationaryLook.Protects(mode), "Ordinary standing, mounts and unrelated modes keep normal turning");
        Check(Marshal.OffsetOf<Character>(nameof(Character.Mode)) == 0x2364, "Installed character-mode layout matches native posture branch");
        Check(Marshal.OffsetOf<InputManager>(nameof(InputManager.MouseButtonHoldStateRaw)) == 0x28, "Native held-button offset");
        foreach (var mode in Enum.GetValues<CharacterModes>())
        foreach (var nativeMode in new[] { -1, 0, 1, 2, 3, 4, 255 })
        {
            var protectedPose = StationaryLook.Protects(mode);
            var expected = protectedPose && nativeMode == 1 ? 3 : nativeMode;
            Check(StationaryLook.CameraRotationMode(nativeMode, protectedPose) == expected,
                "Native camera turn request becomes free look only during fixed loops");
            Check(StationaryLook.CameraRotationMode(nativeMode, false) == nativeMode,
                "Inactive/other-camera scope preserves every native mode");
        }
        Check(StationaryLook.CameraRotationMode(1, true) == 3 && StationaryLook.CameraRotationMode(1, false) == 1,
            "Ending a pose resumes ordinary camera turning immediately without stored input state");
        foreach (var buttons in new[] { 0, 1, 2, 3, 4, -1, 255 })
        {
            Check(StationaryLook.Buttons(buttons, false) == buttons, "Unprotected input unchanged");
            Check(StationaryLook.Buttons(buttons, true) == (buttons == 2 ? 1 : buttons), "Only standalone right drag converted; physical both-button move retained");
        }
        var manager = new InputManager { MouseButtonHoldStateRaw = 2, MouseDragDistance = 2,
            MouseDragDeltaX = 17, MouseDragDeltaY = -8, MouseDragStartX = 100, MouseDragStartY = 200,
            MouseDragActive = true, CursorRectActive = true };
        var patch = new StationaryDragPatch();
        for (var frame = 0; frame < 60; frame++)
        {
            patch.Apply(&manager, true);
            Check(manager.HeldMouseButtons == InputManager.MouseButtonHoldState.Left, "Manual RMB repeatedly uses native free-look");
            Check(manager.MouseDragDeltaX == 17 && manager.MouseDragDeltaY == -8 && manager.MouseDragDistance == 2 &&
                manager.MouseDragStartX == 100 && manager.MouseDragStartY == 200 && manager.MouseDragActive && manager.CursorRectActive,
                "Protection preserves physical drag speed, threshold, origin and unrelated fields");
        }
        patch.Apply(&manager, false);
        Check(manager.MouseButtonHoldStateRaw == 2, "Ending pose or leaving POV restores original right drag");
        patch.Apply(&manager, true);
        patch.Restore();
        patch.Restore();
        Check(manager.MouseButtonHoldStateRaw == 2, "Disable/unload restoration is idempotent");
        patch.Apply(&manager, true);
        manager.MouseButtonHoldStateRaw = 0;
        patch.Restore();
        Check(manager.MouseButtonHoldStateRaw == 0, "Fresh button release wins over old restore snapshot");
        manager.MouseButtonHoldStateRaw = 2;
        patch.Apply(&manager, true);
        manager.MouseDragDeltaX = 99;
        patch.Restore();
        Check(manager.MouseButtonHoldStateRaw == 1 && manager.MouseDragDeltaX == 99, "Fresh native gesture is not replaced by old right drag");
        manager.MouseButtonHoldStateRaw = 3;
        patch.Apply(&manager, true);
        Check(manager.MouseButtonHoldStateRaw == 3, "Real L+R movement remains available for standing up");
        patch.Restore();

        // Direct capture shares the same pose classifier but uses its existing
        // full drag patch; ordinary RMB never needs cursor capture/recentering.
        manager.MouseButtonHoldStateRaw = 0;
        var direct = new MouseLookDragPatch();
        direct.Apply(&manager, 800, 450, 4, 6, StationaryLook.Protects(CharacterModes.EmoteLoop));
        Check(manager.HeldMouseButtons == InputManager.MouseButtonHoldState.Left, "Direct control protects the same pose as manual RMB");
        direct.Apply(&manager, 800, 450, 4, 6, StationaryLook.Protects(CharacterModes.Normal));
        Check(manager.HeldMouseButtons == InputManager.MouseButtonHoldState.Right, "Standing resumes direct right-look without release/re-entry");
        direct.Restore();
        Check(manager.MouseButtonHoldStateRaw == 0, "Direct pose switch preserves original input restoration");
    }

    private static void TestHeadRotationWeights()
    {
        var r = FirstPersonMath.DegreesToRadians;
        Check(Near(HeadRotationWeight.Scale(60 * r, 50, true), 30 * r), "60 degree head tilt at 50 percent yields 30 degrees");
        Check(Near(HeadRotationWeight.Scale(-60 * r, 50, true), -30 * r), "Weight applies symmetrically to opposite direction");
        foreach (var percent in new[] { -10f, 0f, 25f, 50f, 75f, 100f, 200f, float.NaN, float.PositiveInfinity })
        {
            var fraction = float.IsFinite(percent) ? Math.Clamp(percent, 0, 100) / 100 : 1;
            var axis = new HeadRotationWeight();
            var camera = 17 * r; // existing manual mouse angle is outside weighted animation
            for (var i = 0; i < 100; i++)
            {
                var pose = MathF.Sin(i / 13f) * 75 * r;
                camera += axis.Update(pose, percent, true);
                Check(Near(camera, 17 * r + pose * fraction), "Weighted animation preserves independent manual camera angle");
            }
            camera += axis.Update(0, percent, true);
            Check(Near(camera, 17 * r), "Returning head to neutral leaves no accumulated weight drift");
        }
        var weighted = new HeadRotationWeight();
        var angle = weighted.Update(60 * r, 100, true);
        angle += weighted.Update(60 * r, 50, true);
        Check(Near(angle, 30 * r), "Changing strength while head is still replaces the existing contribution");
        for (var i = 0; i < 100; i++) Check(weighted.Update(60 * r, 50, true) == 0, "Repeated callbacks never re-add the same tilt");
        angle += weighted.Update(60 * r, 0, true);
        Check(Near(angle, 0), "Zero strength removes existing animation offset");
        angle += weighted.Update(60 * r, 75, true);
        Check(Near(angle, 45 * r), "Re-enabling a still pose uses current weight");
        angle += weighted.Update(60 * r, 75, false);
        Check(Near(angle, 0), "Disabling following removes its own contribution");
        Check(weighted.Update(0, 75, false) == 0, "Head returning while disabled does not move camera");
        weighted.Update(40 * r, 100, true);
        weighted.Reset();
        Check(Near(weighted.Update(20 * r, 50, true), 10 * r), "POV re-entry resets contribution history");
        Check(HeadRotationWeight.Scale(float.NaN, 100, true) == 0 &&
            HeadRotationWeight.Scale(40 * r, 100, false) == 0, "Invalid angles and disabled follow remain finite/neutral");

        foreach (var gain in new[] { 0.01f, 0.064f, 1f })
        {
            var full = new HeadRotationWeight();
            var half = new HeadRotationWeight();
            float smoothed = 0, fullCamera = 0, halfCamera = 0;
            for (var step = 0; step < 1500; step++)
            {
                smoothed += (60 * r - smoothed) * gain;
                fullCamera += full.Update(smoothed, 100, true);
                halfCamera += half.Update(smoothed, 50, true);
                Check(Near(fullCamera, smoothed) && Near(halfCamera, fullCamera * 0.5f), "Smoothing changes time response, not follow ratio");
            }
            Check(Near(fullCamera, 60 * r) && Near(halfCamera, 30 * r), "All smoothing speeds settle to configured amplitude");
        }
        var yaw = new HeadRotationWeight();
        var pitch = new HeadRotationWeight();
        Check(Near(yaw.Update(60 * r, 25, true), 15 * r) && Near(pitch.Update(60 * r, 75, true), 45 * r) &&
            Near(HeadRotationWeight.Scale(60 * r, 50, true), 30 * r), "Three axes have independent weights and histories");
    }
}
