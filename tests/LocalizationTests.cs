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
        Check(UiText.Translate("POV+ Enhanced 第一人称设置###PovPlus", PluginLanguage.English, "zh").EndsWith("###PovPlus"), "Stable window identity");
        Check(UiText.Translate("unknown", PluginLanguage.English, "en") == "unknown", "Unknown text remains readable");
        Check((int)PluginLanguage.Automatic == 0 && (int)PluginLanguage.SimplifiedChinese == 1 &&
            (int)PluginLanguage.English == 2 && (int)PluginLanguage.Bilingual == 3, "Saved language values remain compatible");
        Check(UiText.Translate("隐藏自己的头部", PluginLanguage.Bilingual, "en") ==
            "隐藏自己的头部\nHide your own head", "Bilingual text is Chinese followed by English");
        Check(UiText.Translate("隐藏自己的头部", PluginLanguage.Bilingual, "zh", inline: true) ==
            "隐藏自己的头部 / Hide your own head", "Compact labels remain single-line");
        Check(UiText.Label("隐藏自己的头部", PluginLanguage.Bilingual, "en", stacked: true) ==
            "隐藏自己的头部\nHide your own head###隐藏自己的头部", "Stacked checkbox retains identity");
        Check(UiText.Translate("按住快捷键\0按一下切换\0", PluginLanguage.Bilingual, "en") ==
            "按住快捷键 / Hold hotkey\0按一下切换 / Press to toggle\0", "Bilingual combo pairs each option and preserves sentinel");
        Check(UiText.Translate("POV+ Enhanced 第一人称设置###PovPlus", PluginLanguage.Bilingual, "en") ==
            "POV+ Enhanced 第一人称设置 / First-person Settings###PovPlus", "Bilingual title retains one brand and one window ID");
        Check(UiText.Translate("unknown", PluginLanguage.Bilingual, "en") == "unknown", "Bilingual unknown text is not duplicated");
        foreach (var (key, _) in UiText.Catalog)
        {
            Check(UiText.Translate(key, PluginLanguage.Bilingual, "zh") == UiText.Translate(key, PluginLanguage.Bilingual, "en"),
                "Bilingual output is independent of framework locale");
            Check(UiText.Label(key, PluginLanguage.Bilingual, "en").EndsWith("###" + key), "Bilingual widget identity is stable");
        }
    }
}
