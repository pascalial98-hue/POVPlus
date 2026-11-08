using System.Runtime.InteropServices;
using PovPlus;
using FFXIVClientStructs.FFXIV.Client.Graphics.Render;
using FFXIVClientStructs.FFXIV.Client.Graphics.Kernel;
using FFXIVClientStructs.Interop;

internal static partial class Program
{
    private static void TestHeadShadow()
    {
        // Native evidence is independently checked by verify_head_shadow.py.
        // Here exercise combinations of unrelated flags: removing colour must
        // not erase, introduce or merge any individual shadow visibility bit.
        for (var bit = 0; bit < 32; bit++)
        {
            var visibility = 1u << bit;
            var result = HeadShadowMask.Filter(visibility, 30, true);
            Check(result == (bit is 24 or 25 ? 0 : visibility), "Only main-camera bits removed");
        }
        var random = new Random(1401);
        for (var sample = 0; sample < 500; sample++)
        {
            var visibility = (uint)random.NextInt64(0, 1L << 32);
            var filtered = HeadShadowMask.Filter(visibility, 30, true);
            Check((filtered & 0x03000000) == 0, "Both main-camera branches disabled");
            Check((filtered & 0xFCFFFFFF) == (visibility & 0xFCFFFFFF), "Every other visibility bit preserved");
            Check((filtered & 0x00C003FF) == (visibility & 0x00C003FF), "Cascade and additional-light shadow masks preserved");
            Check(HeadShadowMask.Filter(filtered, 30, true) == filtered, "Repeated filtering is stable");
            foreach (var view in new[] { -1, 0, 19, 20, 25, 29, 31, 32, 85, int.MaxValue })
                Check(HeadShadowMask.Filter(visibility, view, true) == visibility, "Other views remain untouched");
            Check(HeadShadowMask.Filter(visibility, 30, false) == visibility, "Unselected models remain untouched");
        }
        foreach (var slot in new uint[] { 0, 5, 10, 11, 16 })
            Check(HeadShadowMask.IsHeadSlot(slot), "Headgear, earrings, hair, face and glasses selected");
        foreach (var slot in new uint[] { 1, 2, 3, 4, 6, 7, 8, 9, 13, 14, 15, 17, 31, uint.MaxValue })
            Check(!HeadShadowMask.IsHeadSlot(slot, "chara/human/c1801/obj/zear/z0001/model/test.mdl"u8),
                "Body, necklace, weapons and unrelated slots stay visible even with a misleading filename");
        Check(HeadShadowMask.IsHeadSlot(12, "chara/human/c1801/obj/zear/z0001/model/test.mdl"u8), "Independent ears selected");
        Check(HeadShadowMask.IsHeadSlot(12, "D:\\mods\\ZEAR\\ears.mdl"u8), "Redirected paths support case and separators");
        Check(!HeadShadowMask.IsHeadSlot(12, "chara/human/c0801/obj/tail/t0001/model/test.mdl"u8), "Race slot holding a tail preserved");
        Check(!HeadShadowMask.IsHeadSlot(12) && !HeadShadowMask.IsHeadSlot(12, "/zearish/a.mdl"u8) &&
            !HeadShadowMask.IsHeadSlot(12, "/my_zear/a.mdl"u8) && !HeadShadowMask.IsHeadSlot(12, "/zear"u8),
            "Unknown or merely similarly named race-slot resources preserved");

        // Guard the installed interop assembly against the native offsets used
        // by the hook. This checks the actual referenced DLL, not a mock type.
        Check(Marshal.OffsetOf<ThreadLocals._TEB>(nameof(ThreadLocals._TEB.ThreadLocalStoragePointer)) == 0x58, "Native TLS array offset");
        Check(Marshal.OffsetOf<ThreadLocals>(nameof(ThreadLocals.GraphicsKernelContext)) == 0x238, "Native graphics context offset");
        Check(Marshal.OffsetOf<ThreadLocals>(nameof(ThreadLocals.IsInitialized)) == 0x203C4, "Native TLS initialization offset");
        Check(Marshal.OffsetOf<Context>(nameof(Context.ViewIndex)) == 0xC, "Native view index offset");
        Check(Marshal.OffsetOf<Model>(nameof(Model.Skeleton)) == 0x40, "Native model skeleton offset");
        Check(Marshal.OffsetOf<Skeleton>(nameof(Skeleton.Owner)) == 0xB8, "Native skeleton owner offset");
    }
}
