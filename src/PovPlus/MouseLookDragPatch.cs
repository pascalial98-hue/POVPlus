using FFXIVClientStructs.FFXIV.Client.Game.Control;

namespace PovPlus;

// Only restore our own unchanged drag tuple. Never write back the whole manager.
internal sealed unsafe class MouseLookDragPatch
{
    private InputManager* owner;
    private Drag saved, written;
    private readonly record struct Drag(int Buttons, float Distance, int X, int Y, int StartX, int StartY, bool Active)
    {
        internal static Drag Read(InputManager* p) => new(p->MouseButtonHoldStateRaw, p->MouseDragDistance,
            p->MouseDragDeltaX, p->MouseDragDeltaY, p->MouseDragStartX, p->MouseDragStartY, p->MouseDragActive);
        internal void Write(InputManager* p)
        {
            p->MouseButtonHoldStateRaw = Buttons; p->MouseDragDistance = Distance;
            p->MouseDragDeltaX = X; p->MouseDragDeltaY = Y;
            p->MouseDragStartX = StartX; p->MouseDragStartY = StartY; p->MouseDragActive = Active;
        }
    }
    internal void Apply(InputManager* p, int x, int y, int dx, int dy, bool freeLook = false)
    {
        Restore();
        owner = p;
        saved = Drag.Read(p);
        // A single native button: left observes without turning the character;
        // never combine it with simulated right (the game treats both as move).
        var button = freeLook ? InputManager.MouseButtonHoldState.Left : InputManager.MouseButtonHoldState.Right;
        written = new Drag((int)button, 11f, -dx, -dy, x, y, true);
        written.Write(p);
    }
    internal void Restore()
    {
        if (owner != null && Drag.Read(owner) == written) saved.Write(owner);
        owner = null;
    }
}
