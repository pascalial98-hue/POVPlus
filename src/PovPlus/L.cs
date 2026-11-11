using System.Globalization;

namespace PovPlus;

internal static class L
{
    internal static string T(string key, bool inline = false) => UiText.Translate(key,
        Plugin.P?.Configuration.Language ?? PluginLanguage.Bilingual,
        Service.PluginInterface?.UiLanguage ?? CultureInfo.CurrentUICulture.Name, inline);

    internal static string Label(string key, bool stacked = false) => UiText.Label(key,
        Plugin.P?.Configuration.Language ?? PluginLanguage.Bilingual,
        Service.PluginInterface?.UiLanguage ?? CultureInfo.CurrentUICulture.Name, stacked);
}
