namespace PovPlus;

// Input edges belong to the real keyboard, not to a render frame or repeat event.
internal sealed class MouseLookState
{
    private bool previousKey;
    private bool released;
    private int binding = -1;
    private bool toggle;
    internal bool Captured { get; private set; }
    internal void Suspend() => Captured = false;

    internal bool Update(bool eligible, bool paused, bool keyDown, bool toggleMode, int bindingId, bool mouseDown)
    {
        if (binding != bindingId || toggle != toggleMode)
        {
            binding = bindingId;
            toggle = toggleMode;
            released = false;
            previousKey = keyDown;
        }
        var pressed = keyDown && !previousKey;
        previousKey = keyDown;
        if (!eligible)
        {
            released = false;
            return Captured = false;
        }
        // Opening settings / typing must not turn a key typed there into a toggle.
        if (paused) return Captured = false;
        if (toggleMode)
        {
            if (pressed) released = !released;
        }
        else released = keyDown;
        // Do not start capture in the middle of a click or UI drag.
        return Captured = !released && (Captured || !mouseDown);
    }

    internal void Reset()
    {
        Captured = released = previousKey = false;
        binding = -1;
    }
}
