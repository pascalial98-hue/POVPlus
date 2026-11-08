using System;
using System.Collections.Generic;
using System.Text.Json;

namespace PovPlus;

public enum PluginLanguage { Automatic, SimplifiedChinese, English }

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

    internal static string Translate(string key, PluginLanguage language, string? frameworkLanguage) =>
        IsChinese(language, frameworkLanguage) ? key : English.GetValueOrDefault(key, key);

    internal static string Label(string key, PluginLanguage language, string? frameworkLanguage) =>
        Translate(key, language, frameworkLanguage) + "###" + key;

    internal static IReadOnlyDictionary<string, string> Catalog => English;
}
