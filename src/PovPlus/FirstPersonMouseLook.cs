using System;
using Dalamud.Hooking;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.System.Input;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using NativeCharacter = FFXIVClientStructs.FFXIV.Client.Game.Character.Character;

namespace PovPlus;

internal sealed unsafe class FirstPersonMouseLook : IDisposable
{
    private readonly Hook<AtkModule.Delegates.HandleInput> inputHook;
    private readonly Hook<Camera.Delegates.GetCameraInputSource> sourceHook;
    private readonly MouseLookState state = new();
    private readonly MouseLookDragPatch drag = new();
    private readonly StationaryDragPatch stationaryDrag = new();
    private readonly InputManager* manager;
    private bool active, restoreVisible, failed, freeLook, nativeInputReady;
    private AtkStage* cursorOwner;
    private int restoreX, restoreY, dx, dy, centerX, centerY;
    internal bool Failed => failed;
    internal bool Active => active;

    internal FirstPersonMouseLook()
    {
        manager = InputManager.Instance();
        if (manager == null || (nint)MouseDevice.MemberFunctionPointers.ScheduleCursorMove == 0 ||
            (nint)AtkCursor.MemberFunctionPointers.SetVisible == 0 || (nint)AtkModule.MemberFunctionPointers.IsTextInputActive == 0)
            throw new InvalidOperationException("Mouse-look native input/cursor interfaces unavailable.");
        inputHook = Service.GameInteropProvider.HookFromAddress<AtkModule.Delegates.HandleInput>(
            (nint)AtkModule.MemberFunctionPointers.HandleInput, HandleInput);
        try
        {
            sourceHook = Service.GameInteropProvider.HookFromAddress<Camera.Delegates.GetCameraInputSource>(
                (nint)Camera.MemberFunctionPointers.GetCameraInputSource, GetSource);
            sourceHook.Enable();
            inputHook.Enable();
        }
        catch { sourceHook?.Dispose(); inputHook.Dispose(); throw; }
    }

    private bool Eligible() => !failed && Plugin.P.Configuration.FirstPersonMouseLook &&
        MouseLookKeys.Valid(Plugin.P.Configuration.FirstPersonCursorKey) && MouseLookKeys.Foreground &&
        POVCameraFunctionality.FirstPersonInputActive();

    private static bool Paused(AtkModule* module) => Plugin.P.MouseLookUiPaused ||
        (module != null && module->IsTextInputActive());

    private bool ProtectPose()
    {
        if (failed || !MouseLookKeys.Foreground || !POVCameraFunctionality.FirstPersonInputActive()) return false;
        var player = Service.ObjectTable.LocalPlayer;
        return player != null && StationaryLook.Protects(((NativeCharacter*)player.Address)->Mode);
    }

    private bool FreeLook => freeLook || ProtectPose();

    private void ProtectNativeDrag()
    {
        stationaryDrag.Apply(manager, !active && nativeInputReady && !Plugin.P.MouseLookUiPaused && ProtectPose());
    }

    // Framework watchdog also runs when the UI-input hook stops being called.
    internal void Update()
    {
        if (!Eligible()) { state.Reset(); if (active) Release(); }
        else if (Plugin.P.MouseLookUiPaused) Release();
        if (!ProtectPose() || Plugin.P.MouseLookUiPaused) stationaryDrag.Restore();
    }

    private byte HandleInput(AtkModule* module, UIInputData* input, bool padMouse)
    {
        // Fresh physical input must reach native UI handling before we reapply drag.
        stationaryDrag.Restore();
        drag.Restore();
        var capture = false;
        try
        {
            var config = Plugin.P.Configuration;
            var stage = AtkStage.Instance();
            var valid = input != null && stage != null && stage->ScreenSize.Width > 0 && stage->ScreenSize.Height > 0;
            nativeInputReady = valid && !padMouse && input->CursorInputs.IsGameWindowFocused && !Paused(module);
            var eligible = valid && Eligible() && !padMouse && input->CursorInputs.IsGameWindowFocused;
            var keyDown = MouseLookKeys.ChordDown(config.FirstPersonCursorKey, config.FirstPersonCursorModifiers);
            var physicalButtons = valid ? input->CursorInputs.MouseButtonHeldFlags | input->CursorInputs.MouseButtonPressedFlags : MouseButtonFlags.None;
            var physicalDown = (physicalButtons & (MouseButtonFlags.LBUTTON | MouseButtonFlags.RBUTTON)) != 0;
            capture = state.Update(eligible, !valid || Paused(module), keyDown, config.FirstPersonCursorToggle,
                config.FirstPersonCursorKey | (config.FirstPersonCursorModifiers << 8), physicalDown);
            if (capture)
            {
                // Sample physical left BEFORE suppressing UI clicks. The same
                // frame's camera and movement queries must see left-only drag.
                freeLook = (physicalButtons & MouseButtonFlags.LBUTTON) != 0;
                centerX = stage->ScreenSize.Width / 2;
                centerY = stage->ScreenSize.Height / 2;
                dx = active ? input->CursorInputs.DeltaX : 0;
                dy = active ? input->CursorInputs.DeltaY : 0;
                if (!active)
                {
                    restoreX = Math.Clamp(input->CursorInputs.PositionX, 0, stage->ScreenSize.Width - 1);
                    restoreY = Math.Clamp(input->CursorInputs.PositionY, 0, stage->ScreenSize.Height - 1);
                    restoreVisible = stage->AtkCursor.IsVisible;
                    cursorOwner = stage;
                    active = true;
                }
                Filter(ref input->CursorInputs);
                Filter(ref input->UIFilteredCursorInputs);
                drag.Apply(manager, centerX, centerY, dx, dy, FreeLook);
                MouseDevice.ScheduleCursorMove(centerX, centerY);
                stage->AtkCursor.SetVisible(false);
            }
            else
            {
                Release();
                ProtectNativeDrag();
            }
        }
        catch (Exception ex) { Fail(ex); capture = false; }

        var result = inputHook.Original(module, input, padMouse);
        try
        {
            // Enter/chat can start text input in the native call above.
            nativeInputReady &= !Paused(module);
            if (capture && Eligible() && !Paused(module))
            {
                drag.Apply(manager, centerX, centerY, dx, dy, FreeLook);
                AtkStage.Instance()->AtkCursor.SetVisible(false);
            }
            else
            {
                Release();
                ProtectNativeDrag();
            }
        }
        catch (Exception ex) { Fail(ex); }
        return result;
    }

    private void Filter(ref CursorInputData input)
    {
        const MouseButtonFlags clicks = MouseButtonFlags.LBUTTON | MouseButtonFlags.RBUTTON;
        input.MouseButtonHeldFlags &= ~clicks;
        input.MouseButtonPressedFlags &= ~clicks;
        input.MouseButtonReleasedFlags &= ~clicks;
        input.MouseButtonHeldThrottledFlags &= ~clicks;
        input.PositionX = centerX; input.PositionY = centerY;
        input.DeltaX = dx; input.DeltaY = dy;
    }

    private CameraInputSource GetSource()
    {
        try
        {
            if (active && Eligible() && !Plugin.P.MouseLookUiPaused)
            {
                // Native camera paths can refresh the drag state between UI and camera updates.
                drag.Apply(manager, centerX, centerY, dx, dy, FreeLook);
                return CameraInputSource.MouseDrag;
            }
            if (active) Release();
            stationaryDrag.Restore();
        }
        catch (Exception ex) { Fail(ex); }
        var source = sourceHook.Original();
        try { ProtectNativeDrag(); }
        catch (Exception ex) { Fail(ex); }
        return source;
    }

    private void Release()
    {
        state.Suspend();
        stationaryDrag.Restore();
        drag.Restore();
        freeLook = false;
        if (!active) return;
        active = false;
        var stage = AtkStage.Instance();
        if (stage != null && stage == cursorOwner && restoreVisible) stage->AtkCursor.SetVisible(true);
        if (MouseLookKeys.Foreground && stage != null)
            MouseDevice.ScheduleCursorMove(Math.Clamp(restoreX, 0, Math.Max(0, stage->ScreenSize.Width - 1)),
                Math.Clamp(restoreY, 0, Math.Max(0, stage->ScreenSize.Height - 1)));
        cursorOwner = null;
        dx = dy = 0;
    }

    private void Fail(Exception ex)
    {
        failed = true;
        nativeInputReady = false;
        state.Reset();
        Release();
        Service.Log.Error(ex, "POV+ mouse-look/pose protection stopped; ordinary mouse input restored.");
    }

    public void Dispose()
    {
        failed = true;
        inputHook.Dispose();
        sourceHook.Dispose();
        state.Reset();
        Release();
    }
}
