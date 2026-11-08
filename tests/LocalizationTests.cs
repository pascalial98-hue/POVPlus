using PovPlus;

internal static partial class Program
{
    private static void TestLocalization()
    {
        Check(UiText.IsChinese(PluginLanguage.Automatic, "zh"), "Auto Chinese");
        Check(UiText.IsChinese(PluginLanguage.Automatic, "ZH-CN"), "Auto Chinese locale variant");
        Check(!UiText.IsChinese(PluginLanguage.Automatic, "en"), "Auto English");
        Check(!UiText.IsChinese(PluginLanguage.Automatic, "ja"), "Unsupported locale uses English");
        Check(!UiText.IsChinese(PluginLanguage.Automatic, null), "Missing locale uses English");
        Check(UiText.IsChinese(PluginLanguage.SimplifiedChinese, "en"), "Explicit Chinese override");
        Check(!UiText.IsChinese(PluginLanguage.English, "zh"), "Explicit English override");
        Check(UiText.IsChinese((PluginLanguage)900, "zh"), "Unknown saved option follows Auto");
        Check(UiText.Catalog.Count >= 100, "English catalog is embedded");
        foreach (var (key, value) in UiText.Catalog)
        {
            Check(!string.IsNullOrWhiteSpace(value), "Nonempty translation");
            Check(UiText.Translate(key, PluginLanguage.SimplifiedChinese, "en") == key, "Chinese is unchanged");
            Check(UiText.Translate(key, PluginLanguage.English, "zh") == value, "English override translates");
            Check(UiText.Label(key, PluginLanguage.English, "en").EndsWith("###" + key), "Stable widget identity");
        }
        var combo = UiText.Translate("按住快捷键\0按一下切换\0", PluginLanguage.English, "zh");
        Check(combo == "Hold hotkey\0Press to toggle\0", "Combo retains separators");
        Check(UiText.Translate("POV+ 第一人称设置###PovPlus", PluginLanguage.English, "zh").EndsWith("###PovPlus"), "Stable window identity");
        Check(UiText.Translate("unknown", PluginLanguage.English, "en") == "unknown", "Unknown text remains readable");
    }
}
