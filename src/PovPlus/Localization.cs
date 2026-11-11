using System;
using System.Collections.Generic;
using System.Text.Json;

namespace PovPlus;

// Keep the existing numeric values: saved language selections must not change.
public enum PluginLanguage { Automatic, SimplifiedChinese, English, Bilingual }

internal static class UiText
{
    private static readonly Dictionary<string, string> English = Load();
    private static Dictionary<string, string> Load()
    {
        using var stream = typeof(UiText).Assembly.GetManifestResourceStream("PovPlus.Translations.en.json")
            ?? throw new InvalidOperationException("Missing bundled English translations.");
        return JsonSerializer.Deserialize<Dictionary<string, string>>(stream)!;
    }

    internal static bool IsChinese(PluginLanguage language, string? frameworkLanguage) =>
        language == PluginLanguage.SimplifiedChinese || language != PluginLanguage.English &&
        (frameworkLanguage?.StartsWith("zh", StringComparison.OrdinalIgnoreCase) ?? false);

    internal static string Translate(string key, PluginLanguage language, string? frameworkLanguage,
        bool inline = false)
    {
        if (language != PluginLanguage.Bilingual)
            return IsChinese(language, frameworkLanguage) ? key : English.GetValueOrDefault(key, key);
        if (!English.TryGetValue(key, out var english) || key == english) return key;

        // Each NUL-delimited combo entry remains one option, including the final sentinel.
        if (key.Contains('\0'))
        {
            var chineseItems = key.Split('\0');
            var englishItems = english.Split('\0');
            for (var i = 0; i < chineseItems.Length; i++)
                chineseItems[i] = Pair(chineseItems[i], i < englishItems.Length ? englishItems[i] : chineseItems[i], " / ");
            return string.Join('\0', chineseItems);
        }

        // Window titles must stay on one line and retain exactly one stable ImGui ID.
        var id = key.IndexOf("###", StringComparison.Ordinal);
        if (id >= 0)
        {
            var englishId = english.IndexOf("###", StringComparison.Ordinal);
            return Pair(key[..id], englishId < 0 ? english : english[..englishId], " / ") + key[id..];
        }
        return Pair(key, english, inline ? " / " : "\n");
    }

    private static string Pair(string chinese, string english, string separator)
    {
        if (chinese == english || english.Length == 0) return chinese;
        const string brand = "POV+ Enhanced ";
        if (chinese.StartsWith(brand, StringComparison.Ordinal) && english.StartsWith(brand, StringComparison.Ordinal))
            english = english[brand.Length..];
        return chinese + separator + english;
    }

    internal static string Label(string key, PluginLanguage language, string? frameworkLanguage, bool stacked = false) =>
        Translate(key, language, frameworkLanguage, inline: !stacked) + "###" + key;

    internal static IReadOnlyDictionary<string, string> Catalog => English;
}
