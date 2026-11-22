using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace PovPlus;

public sealed class CameraPreset
{
    public string Name { get; set; } = "";
    public JObject Settings { get; set; } = new();
}

internal static class CameraPresets
{
    internal const int MaxNameLength = 48, MaxCount = 100;
    private const long MaxFileBytes = 1024 * 1024;
    private const string Format = "POVPlus.CameraPresets";
    private const int Version = 1;
    private static readonly PresetContract Contract = new();
    private static readonly JsonLoadSettings LoadSettings = new() { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error };

    // Keep legacy configuration keys and scope exports to camera settings.
    private sealed class PresetContract : DefaultContractResolver
    {
        private static readonly HashSet<string> Included = new(StringComparer.Ordinal)
        {
            nameof(Configuration.SETTINGS_CameraOffsetX), nameof(Configuration.SETTINGS_CameraOffsetY),
            nameof(Configuration.SETTINGS_CameraOffsetZ), nameof(Configuration.CameraTiltOffset),
            nameof(Configuration.Setting_FOV), nameof(Configuration.CameraNearPlane),
            nameof(Configuration.Setting_FirstPersonOnly), nameof(Configuration.Setting_GposeEnabled),
            nameof(Configuration.YawEnabled), nameof(Configuration.PitchEnabled), nameof(Configuration.RollEnabled),
            nameof(Configuration.HeadYawWeight), nameof(Configuration.HeadPitchWeight), nameof(Configuration.HeadRollWeight),
            nameof(Configuration.Settings_YawSmoothing), nameof(Configuration.Settings_PitchSmoothing), nameof(Configuration.Settings_RollSmoothing),
            nameof(Configuration.FirstPersonBodyRelativeLook), nameof(Configuration.FirstPersonMouseLook),
            nameof(Configuration.FirstPersonLimitLook), nameof(Configuration.FirstPersonSoftLimits), nameof(Configuration.FirstPersonSoftZoneDegrees),
            nameof(Configuration.FirstPersonYawDegrees), nameof(Configuration.FirstPersonLookUpDegrees), nameof(Configuration.FirstPersonLookDownDegrees),
            nameof(Configuration.FirstPersonLookDownLean), nameof(Configuration.FirstPersonLeanForward), nameof(Configuration.FirstPersonLeanSide),
            nameof(Configuration.FirstPersonLeanSensitivity), nameof(Configuration.FirstPersonLeanExtraDownDegrees), nameof(Configuration.FirstPersonLeanExtraSideDegrees),
            nameof(Configuration.FirstPersonHideHead), nameof(Configuration.FirstPersonFullHeadShadow), nameof(Configuration.FirstPersonDelayHeadHide),
            nameof(Configuration.FirstPersonHeadHideDistance), nameof(Configuration.FirstPersonHeadShowDistance), nameof(Configuration.FirstPersonPreserveHair),
            nameof(Configuration.FirstPersonPreserveEars), nameof(Configuration.FirstPersonDetailHideRadius), nameof(Configuration.FirstPersonKeepNearObjects),
            nameof(Configuration.FirstPersonReduceSelfVfx), nameof(Configuration.FirstPersonVfxRadius), nameof(Configuration.FirstPersonVfxOpacity),
            nameof(Configuration.ShoulderCycleEnabled), nameof(Configuration.ShoulderEntryTime),
            nameof(Configuration.ShoulderSide), nameof(Configuration.ShoulderHeight), nameof(Configuration.ShoulderDistance),
            nameof(Configuration.ShoulderMinDistance), nameof(Configuration.ShoulderMaxDistance), nameof(Configuration.ShoulderSpeedZoomEnabled),
            nameof(Configuration.ShoulderSpeedZoomDistance), nameof(Configuration.ShoulderLag), nameof(Configuration.ShoulderMaxLag),
            nameof(Configuration.ShoulderVerticalLagPercent), nameof(Configuration.MountedShoulder), nameof(Configuration.CombatShoulder),
            nameof(Configuration.ShoulderFlightMotionEnabled), nameof(Configuration.ShoulderFlightSwayPercent), nameof(Configuration.ShoulderFlightBankDegrees),
            nameof(Configuration.ShoulderGlideFovEnabled), nameof(Configuration.ShoulderGlideFovDegrees), nameof(Configuration.ShoulderGlideEnterDelay),
            nameof(Configuration.ShoulderGlideEnterSeconds), nameof(Configuration.ShoulderGlideExitDelay), nameof(Configuration.ShoulderGlideExitSeconds)
        };

        protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
        {
            var properties = base.CreateProperties(type, memberSerialization);
            return type == typeof(Configuration)
                ? properties.Where(property => Included.Contains(property.UnderlyingName!)).ToList()
                : properties;
        }
    }

    private static JsonSerializer Serializer() => new()
    {
        ContractResolver = Contract,
        MissingMemberHandling = MissingMemberHandling.Error,
        ObjectCreationHandling = ObjectCreationHandling.Replace,
        MetadataPropertyHandling = MetadataPropertyHandling.Ignore
    };

    internal static JObject Capture(Configuration source) => JObject.FromObject(source, Serializer());

    internal static void Apply(Configuration target, JObject settings)
    {
        using var reader = Normalize(settings).CreateReader();
        Serializer().Populate(reader, target);
    }

    private static JObject Normalize(JObject settings)
    {
        var defaults = Capture(new Configuration());
        if (!settings.HasValues || !MatchesShape(settings, defaults))
            throw new InvalidDataException("Invalid camera settings.");
        var config = settings.ToObject<Configuration>(Serializer())!;
        config.SETTINGS_CameraOffsetX = CalibrationState.Offset(config.SETTINGS_CameraOffsetX);
        config.SETTINGS_CameraOffsetY = CalibrationState.Offset(config.SETTINGS_CameraOffsetY);
        config.SETTINGS_CameraOffsetZ = CalibrationState.Offset(config.SETTINGS_CameraOffsetZ);
        config.CameraTiltOffset = Math.Clamp(config.CameraTiltOffset, -3, 3);
        config.Setting_FOV = Math.Clamp(config.Setting_FOV, 0, 5);
        config.CameraNearPlane = NearPlanePatch.Sanitize(config.CameraNearPlane);
        config.Settings_YawSmoothing = Math.Clamp(config.Settings_YawSmoothing, 0, 100);
        config.Settings_PitchSmoothing = Math.Clamp(config.Settings_PitchSmoothing, 0, 100);
        config.Settings_RollSmoothing = Math.Clamp(config.Settings_RollSmoothing, 0, 100);
        return Capture(config);
    }

    private static bool MatchesShape(JObject settings, JObject defaults)
    {
        foreach (var property in settings.Properties())
        {
            var value = property.Value;
            var valid = defaults[property.Name] switch
            {
                JObject section => value is JObject child && MatchesShape(child, section),
                { Type: JTokenType.Boolean } => value.Type == JTokenType.Boolean,
                { Type: JTokenType.Integer or JTokenType.Float } =>
                    value.Type is JTokenType.Integer or JTokenType.Float && float.IsFinite(value.Value<float>()),
                _ => false
            };
            if (!valid) return false;
        }
        return true;
    }

    internal static bool ValidName(string name) =>
        name.Length is > 0 and <= MaxNameLength && !name.Any(char.IsControl);

    internal static bool HasName(IEnumerable<CameraPreset> list, string name) =>
        list.Any(preset => string.Equals(preset.Name, name, StringComparison.OrdinalIgnoreCase));

    internal static string UniqueName(IEnumerable<CameraPreset> list, string name)
    {
        if (!HasName(list, name)) return name;
        for (var index = 2; ; index++)
        {
            var suffix = $" ({index})";
            var length = Math.Min(name.Length, MaxNameLength - suffix.Length);
            if (char.IsHighSurrogate(name[length - 1])) length--;
            var candidate = name[..length] + suffix;
            if (!HasName(list, candidate)) return candidate;
        }
    }

    internal static List<CameraPreset> Read(string path)
    {
        using var stream = File.OpenRead(path);
        if (stream.Length > MaxFileBytes) throw new InvalidDataException("Preset file is too large.");
        using var text = new StreamReader(stream);
        using var reader = new JsonTextReader(text) { DateParseHandling = DateParseHandling.None, MaxDepth = 12 };
        var document = JObject.Load(reader, LoadSettings);
        if (reader.Read() || document["Format"]?.Type != JTokenType.String || (string?)document["Format"] != Format ||
            document["Version"]?.Type != JTokenType.Integer || (long?)document["Version"] != Version ||
            document["Presets"] is not JArray presets || presets.Count is 0 or > MaxCount)
            throw new InvalidDataException("Unsupported preset file.");

        var result = new List<CameraPreset>();
        foreach (var item in presets)
        {
            if (item is not JObject entry || entry["Name"]?.Type != JTokenType.String ||
                entry["Settings"] is not JObject settings)
                throw new InvalidDataException("Invalid preset entry.");
            var name = ((string)entry["Name"]!).Trim();
            if (!ValidName(name)) throw new InvalidDataException("Invalid preset name.");
            result.Add(new CameraPreset { Name = name, Settings = Normalize(settings) });
        }
        return result;
    }

    internal static void Write(string path, IReadOnlyCollection<CameraPreset> presets)
    {
        if (presets.Count is 0 or > MaxCount) throw new InvalidDataException("Invalid number of presets.");
        var entries = new JArray(presets.Select(preset => new JObject
        {
            ["Name"] = preset.Name,
            ["Settings"] = Normalize(preset.Settings)
        }));
        var document = new JObject { ["Format"] = Format, ["Version"] = Version, ["Presets"] = entries };
        var temporary = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(path))!, $".povplus-{Guid.NewGuid():N}.tmp");
        try
        {
            using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write))
            using (var text = new StreamWriter(stream))
            using (var writer = new JsonTextWriter(text) { Formatting = Formatting.Indented })
                document.WriteTo(writer);
            File.Move(temporary, path);
        }
        finally
        {
            if (File.Exists(temporary)) File.Delete(temporary);
        }
    }
}
