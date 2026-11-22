using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.ImGuiFileDialog;

namespace PovPlus.Windows;

public partial class MainWindow
{
    private enum PresetEdit { None, Update, Delete }

    private const string PresetFileFilter = ".json";
    private readonly FileDialogManager presetFiles = new();
    private int selectedPreset = -1;
    private string newPresetName = "", selectedPresetName = "", presetStatus = "";
    private PresetEdit pendingPresetEdit;

    private void PresetsPage()
    {
        ImGui.TextWrapped(L.T("预设保存视点校准、第一人称和越肩设置。语言、插件启用状态及鼠标快捷键由全局设置管理。"));
        Heading("保存当前设置");
        ImGui.SetNextItemWidth(-1);
        ImGui.InputTextWithHint("##newPreset", L.T("新预设名称", inline: true), ref newPresetName, 193);
        if (ImGui.Button(L.Label("保存为新预设")))
            SaveNewPreset();

        Heading("已保存的预设");
        var presets = Config.Presets ??= new();
        if (presets.Count == 0)
            ImGui.TextWrapped(L.T("暂无预设。可以保存当前设置或导入预设文件。"));
        else
        {
            var listHeight = ImGui.GetTextLineHeightWithSpacing() * Math.Clamp(presets.Count + 1, 3, 8);
            if (ImGui.BeginListBox("##savedPresets", new Vector2(-1, listHeight)))
            {
                for (var index = 0; index < presets.Count; index++)
                {
                    ImGui.PushID(index);
                    var label = presets[index].Name.Replace('#', '＃');
                    if (ImGui.Selectable(label + "###preset", selectedPreset == index))
                        SelectPreset(index);
                    ImGui.PopID();
                }
                ImGui.EndListBox();
            }
            if (selectedPreset >= 0 && selectedPreset < presets.Count)
                DrawSelectedPreset(presets[selectedPreset]);
        }

        Heading("预设文件");
        if (ImGui.Button(L.Label("导入 JSON")))
            presetFiles.OpenFileDialog(L.T("导入镜头预设", inline: true), PresetFileFilter,
                (confirmed, paths) => { if (confirmed && paths.Count == 1) ImportPresets(paths[0]); },
                selectionCountMax: 1, isModal: true);
        ImGui.SameLine();
        ImGui.BeginDisabled(presets.Count == 0);
        if (ImGui.Button(L.Label("导出全部")))
            ExportPresets(presets.ToArray());
        ImGui.EndDisabled();
        if (presetStatus.Length > 0) ImGui.TextWrapped(L.T(presetStatus));
    }

    private void DrawSelectedPreset(CameraPreset preset)
    {
        if (ImGui.Button(L.Label("应用预设")))
            ApplyPreset(preset);
        ImGui.SameLine();
        if (ImGui.Button(L.Label("导出所选")))
            ExportPresets([preset]);

        ImGui.SetNextItemWidth(-1);
        ImGui.InputTextWithHint("##renamePreset", L.T("预设名称", inline: true), ref selectedPresetName, 193);
        if (ImGui.Button(L.Label("重命名")))
            RenamePreset(preset);

        if (ImGui.Button(L.Label("用当前设置更新预设")))
            pendingPresetEdit = PresetEdit.Update;
        if (ImGui.Button(L.Label("删除预设")))
            pendingPresetEdit = PresetEdit.Delete;
        if (pendingPresetEdit == PresetEdit.None) return;
        ImGui.PushTextWrapPos(0);
        ImGui.TextUnformatted(string.Format(L.T(pendingPresetEdit == PresetEdit.Update
            ? "用当前镜头设置更新「{0}」？" : "删除预设「{0}」？"), preset.Name));
        ImGui.PopTextWrapPos();
        if (ImGui.Button(L.Label("确认")))
            EditSelectedPreset(preset);
        ImGui.SameLine();
        if (ImGui.Button(L.Label("取消")))
            pendingPresetEdit = PresetEdit.None;
    }

    private void SelectPreset(int index)
    {
        selectedPreset = index;
        selectedPresetName = index >= 0 ? Config.Presets[index].Name : "";
        pendingPresetEdit = PresetEdit.None;
    }

    private void SaveNewPreset()
    {
        var name = newPresetName.Trim();
        if (!CheckPresetName(name, Config.Presets)) return;
        var presets = new List<CameraPreset>(Config.Presets);
        if (presets.Count >= CameraPresets.MaxCount)
        {
            presetStatus = "已达到预设数量上限。";
            return;
        }
        presets.Add(new CameraPreset { Name = name, Settings = CameraPresets.Capture(Config) });
        if (SavePresetList(presets, presets.Count - 1, "已保存新预设。"))
            newPresetName = "";
    }

    private bool CheckPresetName(string name, IEnumerable<CameraPreset> others)
    {
        if (!CameraPresets.ValidName(name))
            presetStatus = "请输入有效的预设名称（最多 48 个字符）。";
        else if (CameraPresets.HasName(others, name))
            presetStatus = "已有同名预设，请换一个名称。";
        else return true;
        return false;
    }

    private void RenamePreset(CameraPreset preset)
    {
        var name = selectedPresetName.Trim();
        if (name == preset.Name || !CheckPresetName(name, Config.Presets.Where(item => item != preset))) return;
        var presets = new List<CameraPreset>(Config.Presets);
        presets[selectedPreset] = new CameraPreset { Name = name, Settings = preset.Settings };
        SavePresetList(presets, selectedPreset, "已重命名预设。");
    }

    private void EditSelectedPreset(CameraPreset preset)
    {
        var presets = new List<CameraPreset>(Config.Presets);
        var update = pendingPresetEdit == PresetEdit.Update;
        var index = selectedPreset;
        if (update)
            presets[index] = new CameraPreset { Name = preset.Name, Settings = CameraPresets.Capture(Config) };
        else
        {
            presets.RemoveAt(index);
            index = Math.Min(index, presets.Count - 1);
        }
        SavePresetList(presets, index, update ? "已更新预设。" : "已删除预设。");
    }

    private bool SavePresetList(List<CameraPreset> presets, int index, string success)
    {
        var previous = Config.Presets;
        try
        {
            Config.Presets = presets;
            Config.Save();
        }
        catch (Exception ex)
        {
            Config.Presets = previous;
            PresetError(ex, "无法保存预设，请查看 Dalamud 日志。");
            return false;
        }
        SelectPreset(index);
        presetStatus = success;
        return true;
    }

    private void ApplyPreset(CameraPreset preset)
    {
        var previous = CameraPresets.Capture(Config);
        try
        {
            CameraPresets.Apply(Config, preset.Settings);
            try { Config.Save(); }
            catch
            {
                CameraPresets.Apply(Config, previous);
                throw;
            }
            pendingPresetEdit = PresetEdit.None;
            presetStatus = "已应用预设。";
        }
        catch (Exception ex)
        {
            PresetError(ex, "无法应用预设，请查看 Dalamud 日志。");
        }
    }

    private void ImportPresets(string path)
    {
        try
        {
            var imported = CameraPresets.Read(path);
            var presets = new List<CameraPreset>(Config.Presets);
            var first = presets.Count;
            if (first + imported.Count > CameraPresets.MaxCount)
            {
                presetStatus = "导入后会超过预设数量上限。";
                return;
            }
            foreach (var preset in imported)
            {
                preset.Name = CameraPresets.UniqueName(presets, preset.Name);
                presets.Add(preset);
            }
            SavePresetList(presets, first, "预设已导入，可从列表选择并应用。");
        }
        catch (Exception ex)
        {
            PresetError(ex, "无法导入。请确认文件是有效的 POV+ Enhanced 预设 JSON。");
        }
    }

    private void ExportPresets(CameraPreset[] presets)
    {
        var fileName = presets.Length == 1 ? "POVPlus-preset" : "POVPlus-presets";
        presetFiles.SaveFileDialog(L.T("导出镜头预设", inline: true), PresetFileFilter, fileName, ".json",
            (confirmed, path) =>
            {
                if (!confirmed) return;
                if (File.Exists(path))
                {
                    presetStatus = "文件已存在，请另选文件名。";
                    return;
                }
                try
                {
                    CameraPresets.Write(path, presets);
                    presetStatus = "预设已导出为 JSON。";
                }
                catch (Exception ex)
                {
                    PresetError(ex, "无法导出预设，请查看 Dalamud 日志。");
                }
            }, startPath: null, isModal: true);
    }

    private void PresetError(Exception ex, string status)
    {
        Service.Log.Warning(ex, "POV+ preset operation failed.");
        presetStatus = status;
    }
}
