using System;

namespace PovPlus;

// CN client 2026.10.06: these two visibility bits gate the main-camera
// auxiliary and colour branches; shadow cascades and lights use other bits.
internal static class HeadShadowMask
{
    internal const uint MainCameraBits = 0x03000000;
    internal const int MainView = 30;

    internal static uint Filter(uint visibility, int view, bool selected) =>
        selected && view == MainView ? visibility & ~MainCameraBits : visibility;

    // Human slots verified against installed Penumbra.GameData.HumanSlot.
    // Slot 12 is race-specific: only a positively identified zear is hidden,
    // never a tail model that happens to occupy the same slot.
    internal static bool IsHeadSlot(uint slot, ReadOnlySpan<byte> resourcePath = default) =>
        slot is 0 or 5 or 10 or 11 or 16 || slot == 12 && IsEarResource(resourcePath);

    private static bool IsEarResource(ReadOnlySpan<byte> path)
    {
        // Resource names are UTF-8. Match the ASCII directory without
        // allocating a managed string every frame, including redirected paths.
        for (var i = 0; i + 5 < path.Length; i++)
            if (Separator(path[i]) && (path[i + 1] | 32) == 'z' &&
                (path[i + 2] | 32) == 'e' && (path[i + 3] | 32) == 'a' &&
                (path[i + 4] | 32) == 'r' && Separator(path[i + 5])) return true;
        return false;
    }

    private static bool Separator(byte value) => value is (byte)'/' or (byte)'\\';
}
