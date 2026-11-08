using System.Globalization;

namespace PovPlus;

internal static class L
{
    internal static string T(string key) => UiText.Translate(key,
        Plugin.P?.Configuration.Language ?? PluginLanguage.Automatic,
        Service.PluginInterface?.UiLanguage ?? CultureInfo.CurrentUICulture.Name);

    internal static string Label(string key) => UiText.Label(key,
        Plugin.P?.Configuration.Language ?? PluginLanguage.Automatic,
        Service.PluginInterface?.UiLanguage ?? CultureInfo.CurrentUICulture.Name);
}
