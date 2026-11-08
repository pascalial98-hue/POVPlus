using System;
using System.Text.RegularExpressions;

namespace PovPlus;

[Flags]
internal enum HeadDetailKind : byte { None = 0, Hair = 1, Ears = 2 }

internal static class HeadDetails
{
    private static readonly Regex ExtraHair = new(@"^j_ex_h[0-9]{4}_ke_[abcdeflrsu](_[abcdeflrsu])?$", RegexOptions.CultureInvariant);
    internal static HeadDetailKind[] Classify(string[] names, short[] parents)
    {
        var result = new HeadDetailKind[names.Length];
        for (var bone = 0; bone < names.Length; bone++)
        {
            for (int i = bone, steps = 0; i >= 0 && i < names.Length && i < parents.Length && steps < names.Length; steps++)
            {
                var name = names[i];
                // Never restore the face/root through a custom parent assignment.
                if (name == "j_kao" || name == "j_kubi") break;
                if (name.StartsWith("j_kami", StringComparison.Ordinal) || ExtraHair.IsMatch(name))
                { result[bone] = HeadDetailKind.Hair; break; }
                if (name.StartsWith("j_mimi", StringComparison.Ordinal) || name.StartsWith("n_ear_", StringComparison.Ordinal) ||
                    name.StartsWith("j_zera_", StringComparison.Ordinal) || name.StartsWith("j_zerb_", StringComparison.Ordinal) ||
                    name.StartsWith("j_zerc_", StringComparison.Ordinal) || name.StartsWith("j_zerd_", StringComparison.Ordinal))
                { result[bone] = HeadDetailKind.Ears; break; }
                i = parents[i];
            }
        }
        return result;
    }

    internal static float Visibility(float distance, float inner)
    {
        if (!float.IsFinite(distance)) return 0;
        inner = Math.Clamp(float.IsFinite(inner) ? inner : 0.25f, 0.15f, 0.6f);
        var t = Math.Clamp((distance - inner) / 0.2f, 0, 1);
        return t * t * (3 - 2 * t);
    }
}
