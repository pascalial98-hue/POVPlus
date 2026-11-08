using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace PovPlus;

internal static class MouseLookKeys
{
    internal const int DefaultKey = 0x12; // Alt; Win32 virtual-key codes, not game keybind IDs.
    internal static readonly int[] Allowed = BuildKeys();
    [DllImport("user32.dll")] private static extern short GetAsyncKeyState(int key);
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(nint window, out uint process);
    internal static bool Foreground
    {
        get { GetWindowThreadProcessId(GetForegroundWindow(), out var process); return process == Environment.ProcessId; }
    }
    internal static bool Down(int key) => (GetAsyncKeyState(key) & 0x8000) != 0;
    internal static bool Modifier(int key) => key is 0x10 or 0x11 or 0x12;
    internal static int Modifiers => (Down(0x11) ? 1 : 0) | (Down(0x12) ? 2 : 0) | (Down(0x10) ? 4 : 0);
    internal static bool Valid(int key) => Array.IndexOf(Allowed, key) >= 0;
    internal static bool ChordDown(int key, int modifiers) => Valid(key) && Down(key) && (Modifiers & modifiers) == modifiers;
    internal static string Name(int key) => key switch
    {
        0x10 => "Shift", 0x11 => "Ctrl", 0x12 => "Alt", 8 => "Backspace", 9 => "Tab", 13 => "Enter", 32 => "Space",
        33 => "Page Up", 34 => "Page Down", 35 => "End", 36 => "Home", 37 => "←", 38 => "↑", 39 => "→", 40 => "↓",
        45 => "Insert", 46 => "Delete", >= 48 and <= 90 => ((char)key).ToString(),
        >= 112 and <= 135 => $"F{key - 111}", >= 96 and <= 105 => $"Num {key - 96}",
        _ => $"VK {key:X2}"
    };
    internal static string Name(int key, int modifiers) =>
        ((modifiers & 1) != 0 ? "Ctrl + " : "") + ((modifiers & 2) != 0 ? "Alt + " : "") +
        ((modifiers & 4) != 0 ? "Shift + " : "") + Name(key);
    private static int[] BuildKeys()
    {
        var keys = new List<int> { 8, 9, 13, 16, 17, 18, 32, 33, 34, 35, 36, 37, 38, 39, 40, 45, 46 };
        for (var k = 48; k <= 57; k++) keys.Add(k);
        for (var k = 65; k <= 90; k++) keys.Add(k);
        for (var k = 96; k <= 105; k++) keys.Add(k);
        for (var k = 112; k <= 135; k++) keys.Add(k);
        return keys.ToArray();
    }
}
