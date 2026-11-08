using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Control;

namespace PovPlus;

internal static class StationaryLook
{
    // The installed client's GetPosture checks (Mode - 3) & 0xF7: modes
    // 3 and 11 cover ground/chair sitting, dozing and stationary emote loops.
    internal static bool Protects(CharacterModes mode) =>
        mode is CharacterModes.EmoteLoop or CharacterModes.InPositionLoop;

    internal static int Buttons(int physical, bool protect) =>
        protect && physical == (int)InputManager.MouseButtonHoldState.Right
            ? (int)InputManager.MouseButtonHoldState.Left : physical;
}

// Ordinary physical RMB must keep the native drag distance, coordinates and
// cursor behaviour. Change only the button interpreted by camera/movement;
// real L+R movement is preserved. Restore before the next native UI input pass.
internal sealed unsafe class StationaryDragPatch
{
    private InputManager* owner;
    private int saved, written;
    private Gesture gesture;
    private readonly record struct Gesture(float Distance, int X, int Y, int StartX, int StartY, bool Active)
    {
        internal static Gesture Read(InputManager* manager) => new(manager->MouseDragDistance,
            manager->MouseDragDeltaX, manager->MouseDragDeltaY, manager->MouseDragStartX,
            manager->MouseDragStartY, manager->MouseDragActive);
    }

    internal void Apply(InputManager* manager, bool protect)
    {
        Restore();
        if (manager == null) return;
        var original = manager->MouseButtonHoldStateRaw;
        var filtered = StationaryLook.Buttons(original, protect);
        if (filtered == original) return;
        owner = manager;
        saved = original;
        written = filtered;
        gesture = Gesture.Read(manager);
        manager->MouseButtonHoldStateRaw = filtered;
    }

    internal void Restore()
    {
        if (owner != null && owner->MouseButtonHoldStateRaw == written && Gesture.Read(owner) == gesture)
            owner->MouseButtonHoldStateRaw = saved;
        owner = null;
    }
}
