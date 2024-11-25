using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;

using KLPlugins.DynLeaderboards.Helpers;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace KLPlugins.DynLeaderboards.Settings;

internal class PluginSettings {
    [JsonProperty] public int Version { get; set; } = 3;
    [JsonProperty] public string? AccDataLocation { get; set; }
    [JsonProperty] public string? AcRootLocation { get; set; }
    [JsonProperty] public bool Log { get; set; }
    [JsonProperty] public int BroadcastDataUpdateRateMs { get; set; }

    [JsonProperty] public OutGeneralProps OutGeneralProps = new(OutGeneralProp.NONE);
    [JsonProperty] public bool IncludeSt21InGt2 { get; set; }
    [JsonProperty] public bool IncludeChlInGt2 { get; set; }

    [JsonIgnore] private const int _CURRENT_SETTINGS_VERSION = 3;
    [JsonIgnore] internal List<DynLeaderboardConfig> DynLeaderboardConfigs { get; set; }

    [JsonIgnore] internal const string PLUGIN_DATA_DIR = "PluginsData\\KLPlugins\\DynLeaderboards";

    [JsonIgnore]
    internal const string LEADERBOARD_CONFIGS_DATA_DIR = PluginSettings.PLUGIN_DATA_DIR + "\\leaderboardConfigs";

    [JsonIgnore]
    internal const string LEADERBOARD_CONFIGS_DATA_BACKUP_DIR = PluginSettings.LEADERBOARD_CONFIGS_DATA_DIR + "\\b";

    [JsonIgnore] internal const double LAP_DATA_TIME_DELAY_SEC = 0.5;

    [JsonIgnore] private static readonly string _defAccDataLocation =
        "C:\\Users\\" + Environment.UserName + "\\Documents\\Assetto Corsa Competizione";

    private delegate JObject Migration(JObject o);

    internal PluginSettings() {
        this.AccDataLocation = PluginSettings._defAccDataLocation;
        this.Log = false;
        this.BroadcastDataUpdateRateMs = 500;
        this.DynLeaderboardConfigs = [];
        this.IncludeChlInGt2 = false;
        this.IncludeSt21InGt2 = false;
        this.SaveDynLeaderboardConfigs();
    }

    internal void ReadDynLeaderboardConfigs() {
        Directory.CreateDirectory(PluginSettings.LEADERBOARD_CONFIGS_DATA_DIR);

        foreach (var fileName in Directory.GetFiles(PluginSettings.LEADERBOARD_CONFIGS_DATA_DIR)) {
            if (!File.Exists(fileName) || !fileName.EndsWith(".json")) {
                continue;
            }

            using var file = File.OpenText(fileName);
            var serializer = new JsonSerializer();
            DynLeaderboardConfig cfg;
            try {
                var result = (DynLeaderboardConfig?)serializer.Deserialize(file, typeof(DynLeaderboardConfig));
                if (result == null) {
                    continue;
                }

                cfg = result;
            } catch (Exception e) {
                SimHub.Logging.Current.Error(
                    $"Failed to deserialize leaderboard \"{fileName}\" configuration. Error {e}."
                );
                continue;
            }

            // Check for conflicting leaderboard names. Add CONFLICT to the end of the name.
            if (this.DynLeaderboardConfigs.Any(x => x.Name == cfg.Name)) {
                var num = 1;
                while (this.DynLeaderboardConfigs.Any(x => x.Name == $"{cfg.Name}_CONFLICT{num}")) {
                    num++;
                }

                cfg.Name = $"{cfg.Name}_CONFLICT{num}";
            }

            // Make sure all leaderboard kinds are present.
            foreach (var l in (LeaderboardKind[])Enum.GetValues(typeof(LeaderboardKind))) {
                if (l == LeaderboardKind.NONE || cfg.Order.Contains(x => x.Kind == l)) {
                    continue;
                }

                var newLeaderboard = new Leaderboard(l);
                cfg.Order.Add(newLeaderboard);
            }

            this.DynLeaderboardConfigs.Add(cfg);
        }
    }

    internal void SaveDynLeaderboardConfigs() {
        // Keep 5 latest backups of each config.
        // New config is only saved and backups are made if the config has changed.

        Directory.CreateDirectory(PluginSettings.LEADERBOARD_CONFIGS_DATA_BACKUP_DIR);

        foreach (var cfg in this.DynLeaderboardConfigs) {
            var cfgFileName = $"{PluginSettings.LEADERBOARD_CONFIGS_DATA_DIR}\\{cfg.Name}.json";
            var serializedCfg = JsonConvert.SerializeObject(cfg, Formatting.Indented);
            var isSame = File.Exists(cfgFileName) && serializedCfg == File.ReadAllText(cfgFileName);

            if (!isSame) {
                RenameOrDeleteOldBackups(cfg);
                if (File.Exists(cfgFileName)) {
                    File.Move(
                        cfgFileName,
                        $"{PluginSettings.LEADERBOARD_CONFIGS_DATA_BACKUP_DIR}\\{cfg.Name}_b{1}.json"
                    );
                }

                File.WriteAllText(cfgFileName, serializedCfg);
            }
        }

        static void RenameOrDeleteOldBackups(DynLeaderboardConfig cfg) {
            for (var i = 5; i > -1; i--) {
                var currentBackupName =
                    $"{PluginSettings.LEADERBOARD_CONFIGS_DATA_BACKUP_DIR}\\{cfg.Name}_b{i + 1}.json";
                if (File.Exists(currentBackupName)) {
                    if (i != 4) {
                        File.Move(
                            currentBackupName,
                            $"{PluginSettings.LEADERBOARD_CONFIGS_DATA_BACKUP_DIR}\\{cfg.Name}_b{i + 2}.json"
                        );
                    } else {
                        File.Delete(currentBackupName);
                    }
                }
            }
        }
    }

    internal void RemoveLeaderboardAt(int i) {
        var fname = $"{PluginSettings.LEADERBOARD_CONFIGS_DATA_DIR}\\{this.DynLeaderboardConfigs[i].Name}.json";
        if (File.Exists(fname)) {
            File.Delete(fname);
        }

        this.DynLeaderboardConfigs.RemoveAt(i);
    }

    internal void RemoveLeaderboard(DynLeaderboardConfig cfg) {
        var fname = $"{PluginSettings.LEADERBOARD_CONFIGS_DATA_DIR}\\{cfg.Name}.json";
        if (File.Exists(fname)) {
            File.Delete(fname);
        }

        this.DynLeaderboardConfigs.Remove(cfg);
    }

    public int GetMaxNumClassPos() {
        var max = 0;
        if (this.DynLeaderboardConfigs.Count > 0) {
            foreach (var v in this.DynLeaderboardConfigs) {
                if (!v.IsEnabled) {
                    continue;
                }

                max = Math.Max(max, v.NumClassPos.Value);
            }
        }

        return max;
    }

    public int GetMaxNumCupPos() {
        var max = 0;
        if (this.DynLeaderboardConfigs.Count > 0) {
            foreach (var v in this.DynLeaderboardConfigs) {
                if (!v.IsEnabled) {
                    continue;
                }

                max = Math.Max(max, v.NumCupPos.Value);
            }
        }

        return max;
    }

    internal bool SetAccDataLocation(string newLoc) {
        if (!Directory.Exists($"{newLoc}\\Config")) {
            if (Directory.Exists($"{PluginSettings._defAccDataLocation}\\Config")) {
                this.AccDataLocation = PluginSettings._defAccDataLocation;
                DynLeaderboardsPlugin.LogWarn(
                    $"Set ACC data location doesn't exist. Using default location '{PluginSettings._defAccDataLocation}'"
                );
                return false;
            }

            DynLeaderboardsPlugin.LogWarn("Set ACC data location doesn't exist. Please check your settings.");
            return false;
        }

        this.AccDataLocation = newLoc;
        return true;
    }

    internal bool SetAcRootLocation(string newLoc) {
        if (!Directory.Exists($"{newLoc}\\content\\cars")) {
            DynLeaderboardsPlugin.LogWarn("Set AC root location is wrong. Please check your settings.");
            return false;
        }

        this.AcRootLocation = newLoc;
        return true;
    }

    internal bool IsAccDataLocationValid() {
        return Directory.Exists($"{this.AccDataLocation}\\Config");
    }

    internal bool IsAcRootLocationValid() {
        return Directory.Exists($"{this.AcRootLocation}\\content\\cars");
    }

    /// <summary>
    ///     Checks if settings version is changed since last save and migrates to current version if needed.
    ///     Old settings file is rewritten by the new one.
    ///     Should be called before reading the settings from file.
    /// </summary>
    internal static void Migrate() {
        Dictionary<string, Migration> migrations = PluginSettings.CreateMigrationsDict();

        var settingsFname = "PluginsData\\Common\\DynLeaderboardsPlugin.GeneralSettings.json";
        if (!File.Exists(settingsFname)) {
            return;
        }

        var savedSettings = JObject.Parse(File.ReadAllText(settingsFname));

        var version = 0; // If settings doesn't contain version key, it's 0
        if (savedSettings.TryGetValue("Version", out var savedSetting)) {
            version = (int)savedSetting!;
        }

        if (version != PluginSettings._CURRENT_SETTINGS_VERSION) {
            // Migrate step by step to current version.
            while (version != PluginSettings._CURRENT_SETTINGS_VERSION) {
                savedSettings = migrations[$"{version}_{version + 1}"](savedSettings);
                version += 1;
            }

            // Save up-to-date setting back to the disk
            using var file = File.CreateText(settingsFname);
            var serializer = new JsonSerializer { Formatting = Formatting.Indented };
            serializer.Serialize(file, savedSettings);
        }

        DynLeaderboardConfig.Migrate();
    }

    /// <summary>
    ///     Creates dictionary of migrations to be called. Key is "old_version_new_version".
    /// </summary>
    /// <returns></returns>
    private static Dictionary<string, Migration> CreateMigrationsDict() {
        var migrations = new Dictionary<string, Migration> {
            ["0_1"] = PluginSettings.Mig0To1, ["1_2"] = PluginSettings.Mig1To2, ["2_3"] = PluginSettings.Mig2To3,
        };

        #if DEBUG
        for (var i = 0; i < PluginSettings._CURRENT_SETTINGS_VERSION; i++) {
            Debug.Assert(migrations.ContainsKey($"{i}_{i + 1}"), $"Migration from v{i} to v{i + 1} is not set.");
        }
        #endif

        return migrations;
    }

    /// <summary>
    ///     Migration of setting from version 0 to version 1
    /// </summary>
    /// <param name="o"></param>
    /// <returns></returns>
    private static JObject Mig0To1(JObject o) {
        // v0 to v1 changes:
        // - Added version number to PluginSetting and DynLeaderboardConfig
        // - DynLeaderboardConfigs are saved separately in PluginsData\KLPlugins\DynLeaderboards\leaderboardConfigs
        //   and not saved in PluginsData\Common\DynLeaderboardsPlugin.GeneralSettings.json

        o["Version"] = 1;

        Directory.CreateDirectory(PluginSettings.LEADERBOARD_CONFIGS_DATA_DIR);
        Directory.CreateDirectory(PluginSettings.LEADERBOARD_CONFIGS_DATA_BACKUP_DIR);
        const string key = "DynLeaderboardConfigs";
        if (o.TryGetValue(key, out var configs)) {
            foreach (var cfg in configs) {
                var fname = $"{PluginSettings.LEADERBOARD_CONFIGS_DATA_DIR}\\{cfg["Name"]}.json";
                // Don't overwrite existing configs
                if (File.Exists(fname)) {
                    continue;
                }

                using var file = File.CreateText(fname);
                var serializer = new JsonSerializer { Formatting = Formatting.Indented };
                serializer.Serialize(file, cfg);
            }
        }

        SimHub.Logging.Current.Info("Migrated settings from v0 to v1.");
        o.Remove("DynLeaderboardConfigs");

        return o;
    }

    /// <summary>
    ///     Migration of setting from version 0 to version 1
    /// </summary>
    /// <param name="o"></param>
    /// <returns></returns>
    private static JObject Mig1To2(JObject o) {
        // v1 to v2 changes:
        // - added Cup leaderboards and options to change number of position in those leaderboards, but these are not breaking changes
        // - no breaking changes of old configuration
        // - added Include_ST21_In_GT2 and Include_CHL_In_GT2
        // - only need to bump version and add new options

        o["Version"] = 2;
        o["Include_ST21_In_GT2"] = false;
        o["Include_CHL_In_GT2"] = false;

        SimHub.Logging.Current.Info("Migrated settings from v1 to v2.");

        return o;
    }

    /// <summary>
    ///     Migration of setting from version 0 to version 1
    /// </summary>
    /// <param name="o"></param>
    /// <returns></returns>
    private static JObject Mig2To3(JObject o) {
        // v2 to v3 changes:
        // - Bump versions!
        // - DynLeaderboardConfigs Order is of type Leaderboard and includes RemoveIfSingleClass/Cup properties,
        //   Although the conversion is automatic since Leaderboard can be converted from LeaderboardKind/int
        // - 

        o["Version"] = 3;

        SimHub.Logging.Current.Info("Migrated settings from v2 to v3.");

        return o;
    }
}

internal class DynLeaderboardConfig {
    [JsonIgnore] private const int _CURRENT_CONFIG_VERSION = 3;

    [JsonProperty] public int Version { get; set; } = 3;

    [JsonIgnore] private string _name = "";

    [JsonProperty]
    public string Name {
        get => this._name;
        set {
            var arr = value.ToCharArray();
            arr = Array.FindAll(arr, char.IsLetterOrDigit);
            this._name = new string(arr);
        }
    }

    [JsonIgnore] public string NextLeaderboardActionName => $"{this.Name}.NextLeaderboard";
    [JsonIgnore] public string PreviousLeaderboardActionName => $"{this.Name}.PreviousLeaderboard";

    [JsonProperty] public OutCarProps OutCarProps = new(
        OutCarProp.CAR_NUMBER
        | OutCarProp.CAR_CLASS
        | OutCarProp.IS_FINISHED
        | OutCarProp.CAR_CLASS_COLOR
        | OutCarProp.TEAM_CUP_CATEGORY_COLOR
        | OutCarProp.TEAM_CUP_CATEGORY_TEXT_COLOR
        | OutCarProp.RELATIVE_ON_TRACK_LAP_DIFF
    );

    [JsonProperty] public OutPitProps OutPitProps = new(OutPitProp.IS_IN_PIT_LANE);
    [JsonProperty] public OutPosProps OutPosProps = new(OutPosProp.DYNAMIC_POSITION);
    [JsonProperty] public OutGapProps OutGapProps = new(OutGapProp.DYNAMIC_GAP_TO_FOCUSED);
    [JsonProperty] public OutStintProps OutStintProps = new(OutStintProp.NONE);
    [JsonProperty] public OutDriverProps OutDriverProps = new(OutDriverProp.INITIAL_PLUS_LAST_NAME);

    [JsonProperty] public OutLapProps OutLapProps = new(
        OutLapProp.LAPS
        | OutLapProp.LAST_LAP_TIME
        | OutLapProp.BEST_LAP_TIME
        | OutLapProp.DYNAMIC_BEST_LAP_DELTA_TO_FOCUSED_BEST
        | OutLapProp.DYNAMIC_LAST_LAP_DELTA_TO_FOCUSED_LAST
    );

    [JsonProperty]
    [JsonConverter(typeof(BoxJsonConverter<int>))]
    public Box<int> NumOverallPos { get; set; } = new(16);

    [JsonProperty]
    [JsonConverter(typeof(BoxJsonConverter<int>))]
    public Box<int> NumClassPos { get; set; } = new(16);

    [JsonProperty]
    [JsonConverter(typeof(BoxJsonConverter<int>))]
    public Box<int> NumCupPos { get; set; } = new(16);

    [JsonProperty]
    [JsonConverter(typeof(BoxJsonConverter<int>))]
    public Box<int> NumOnTrackRelativePos { get; set; } = new(5);

    [JsonProperty]
    [JsonConverter(typeof(BoxJsonConverter<int>))]
    public Box<int> NumOverallRelativePos { get; set; } = new(5);

    [JsonProperty]
    [JsonConverter(typeof(BoxJsonConverter<int>))]
    public Box<int> NumClassRelativePos { get; set; } = new(5);

    [JsonProperty]
    [JsonConverter(typeof(BoxJsonConverter<int>))]
    public Box<int> NumCupRelativePos { get; set; } = new(5);

    [JsonProperty]
    [JsonConverter(typeof(BoxJsonConverter<int>))]
    public Box<int> NumDrivers { get; set; } = new(1);

    [JsonProperty]
    [JsonConverter(typeof(BoxJsonConverter<int>))]
    public Box<int> PartialRelativeOverallNumOverallPos { get; set; } = new(5);

    [JsonProperty]
    [JsonConverter(typeof(BoxJsonConverter<int>))]
    public Box<int> PartialRelativeOverallNumRelativePos { get; set; } = new(5);

    [JsonProperty]
    [JsonConverter(typeof(BoxJsonConverter<int>))]
    public Box<int> PartialRelativeClassNumClassPos { get; set; } = new(5);

    [JsonProperty]
    [JsonConverter(typeof(BoxJsonConverter<int>))]
    public Box<int> PartialRelativeClassNumRelativePos { get; set; } = new(5);

    [JsonProperty]
    [JsonConverter(typeof(BoxJsonConverter<int>))]
    public Box<int> PartialRelativeCupNumCupPos { get; set; } = new(5);

    [JsonProperty]
    [JsonConverter(typeof(BoxJsonConverter<int>))]
    public Box<int> PartialRelativeCupNumRelativePos { get; set; } = new(5);

    [JsonProperty] public List<Leaderboard> Order { get; set; } = [];

    [JsonProperty]
    public int CurrentLeaderboardIdx {
        get => this._currentLeaderboardIdx;
        set {
            this._currentLeaderboardIdx = value > -1 && value < this.Order.Count ? value : 0;
            this.CurrentLeaderboardName = this.CurrentLeaderboard().Kind.ToString();
        }
    }

    [JsonIgnore] private int _currentLeaderboardIdx = 0;
    [JsonIgnore] internal string CurrentLeaderboardName = "";
    [JsonProperty] public bool IsEnabled { get; set; } = true;

    private delegate JObject Migration(JObject o);

    public Leaderboard CurrentLeaderboard() {
        return this.Order.ElementAt(this.CurrentLeaderboardIdx);
    }

    public DynLeaderboardConfig() { }

    internal DynLeaderboardConfig(string name) {
        this.Name = name;
        this.Order = [
            new Leaderboard(LeaderboardKind.OVERALL),
            new Leaderboard(LeaderboardKind.CLASS, true, true),
            new Leaderboard(LeaderboardKind.CUP, true, true),
            new Leaderboard(LeaderboardKind.PARTIAL_RELATIVE_OVERALL),
            new Leaderboard(LeaderboardKind.PARTIAL_RELATIVE_CLASS, true, true),
            new Leaderboard(LeaderboardKind.PARTIAL_RELATIVE_CUP, true, true),
            new Leaderboard(LeaderboardKind.RELATIVE_OVERALL),
            new Leaderboard(LeaderboardKind.RELATIVE_CLASS, true, true),
            new Leaderboard(LeaderboardKind.RELATIVE_CUP, true, true),
            new Leaderboard(LeaderboardKind.RELATIVE_ON_TRACK),
            new Leaderboard(LeaderboardKind.RELATIVE_ON_TRACK_WO_PIT),
        ];
        this.CurrentLeaderboardName = this.Order[this._currentLeaderboardIdx].Kind.ToString();
    }

    internal DynLeaderboardConfig DeepClone() {
        return JsonConvert.DeserializeObject<DynLeaderboardConfig>(JsonConvert.SerializeObject(this))!;
    }

    internal void Rename(string newName) {
        var configFileName = $"{PluginSettings.LEADERBOARD_CONFIGS_DATA_DIR}\\{this.Name}.json";
        if (File.Exists(configFileName)) {
            File.Move(configFileName, $"{PluginSettings.LEADERBOARD_CONFIGS_DATA_DIR}\\{newName}.json");
        }

        for (var i = 5; i > -1; i--) {
            var currentBackupName = $"{PluginSettings.LEADERBOARD_CONFIGS_DATA_BACKUP_DIR}\\{this.Name}_b{i + 1}.json";
            if (File.Exists(currentBackupName)) {
                File.Move(
                    currentBackupName,
                    $"{PluginSettings.LEADERBOARD_CONFIGS_DATA_BACKUP_DIR}\\{newName}_b{i + 1}.json"
                );
            }
        }

        this.Name = newName;
    }

    private int? _maxPositions = null;

    internal int MaxPositions() {
        if (this._maxPositions == null) {
            var numPos = new[] {
                this.NumOverallPos.Value,
                this.NumClassPos.Value,
                this.NumCupPos.Value,
                this.NumOverallRelativePos.Value * 2 + 1,
                this.NumClassRelativePos.Value * 2 + 1,
                this.NumCupRelativePos.Value * 2 + 1,
                this.NumOnTrackRelativePos.Value * 2 + 1,
                this.PartialRelativeClassNumClassPos.Value + this.PartialRelativeClassNumRelativePos.Value * 2 + 1,
                this.PartialRelativeOverallNumOverallPos.Value
                + this.PartialRelativeOverallNumRelativePos.Value * 2
                + 1,
                this.PartialRelativeCupNumCupPos.Value + this.PartialRelativeCupNumRelativePos.Value * 2 + 1,
            };

            this._maxPositions = numPos.Max();
        }

        return this._maxPositions.Value;
    }

    /// <summary>
    ///     Checks if settings version is changed since last save and migrates to current version if needed.
    ///     Old settings file is rewritten by the new one.
    ///     Should be called before reading the settings from file.
    /// </summary>
    internal static void Migrate() {
        Dictionary<string, Migration> migrations = DynLeaderboardConfig.CreateMigrationsDict();

        foreach (var fileName in Directory.GetFiles(PluginSettings.LEADERBOARD_CONFIGS_DATA_DIR)) {
            if (!File.Exists(fileName) || !fileName.EndsWith(".json")) {
                continue;
            }

            var savedSettings = JObject.Parse(File.ReadAllText(fileName));

            var version = 0; // If settings doesn't contain version key, it's 0
            if (savedSettings.TryGetValue("Version", out var setting)) {
                version = (int)setting!;
            }

            if (version == DynLeaderboardConfig._CURRENT_CONFIG_VERSION) {
                return;
            }

            // Migrate step by step to current version.
            while (version != DynLeaderboardConfig._CURRENT_CONFIG_VERSION) {
                savedSettings = migrations[$"{version}_{version + 1}"](savedSettings);
                version += 1;
            }

            // Save up-to-date setting back to the disk
            using var file = File.CreateText(fileName);
            var serializer = new JsonSerializer { Formatting = Formatting.Indented };
            serializer.Serialize(file, savedSettings);
        }
    }

    /// <summary>
    ///     Creates dictionary of migrations to be called. Key is "old_version_new_version".
    /// </summary>
    /// <returns></returns>
    private static Dictionary<string, Migration> CreateMigrationsDict() {
        var migrations = new Dictionary<string, Migration> {
            ["1_2"] = DynLeaderboardConfig.Mig1To2, ["2_3"] = DynLeaderboardConfig.Mig2To3,
        };

        #if DEBUG
        for (var i = 1; i < DynLeaderboardConfig._CURRENT_CONFIG_VERSION; i++) {
            Debug.Assert(
                migrations.ContainsKey($"{i}_{i + 1}"),
                $"Migration from v{i} to v{i + 1} is not set for DynLeaderboardConfig."
            );
        }
        #endif

        return migrations;
    }

    /// <summary>
    ///     Migration of setting from version 0 to version 1
    /// </summary>
    /// <param name="cfg"></param>
    /// <returns></returns>
    private static JObject Mig1To2(JObject cfg) {
        cfg["Version"] = 2;
        cfg["NumCupPos"] = cfg["NumClassPos"];
        cfg["NumCupRelativePos"] = cfg["NumClassRelativePos"];
        cfg["PartialRelativeCupNumCupPos"] = cfg["PartialRelativeClassNumClassPos"];
        cfg["PartialRelativeCupNumRelativePos"] = cfg["PartialRelativeClassNumRelativePos"];

        SimHub.Logging.Current.Info($"Migrated DynLeaderboardConfig {cfg["Name"]} from v1 to v2.");

        return cfg;
    }

    /// <summary>
    ///     Migration of setting from version 0 to version 1
    /// </summary>
    private static JObject Mig2To3(JObject cfg) {
        // v2 to v3 changes:
        // - Bump versions!
        // - DynLeaderboardConfigs Order is of type Leaderboard and includes RemoveIfSingleClass/Cup properties,
        //   Although the conversion is automatic since Leaderboard can be converted from LeaderboardKind/int
        // - 

        cfg["Version"] = 3;

        SimHub.Logging.Current.Info($"Migrated DynLeaderboardConfig {cfg["Name"]} from v2 to v3.");

        return cfg;
    }
}

[TypeConverter(typeof(LeaderboardKindTypeConverter))]
internal class Leaderboard {
    [JsonProperty] public LeaderboardKind Kind { get; private set; }
    [JsonProperty] public bool RemoveIfSingleClass { get; internal set; }
    [JsonProperty] public bool RemoveIfSingleCup { get; internal set; }
    [JsonProperty] public bool IsEnabled { get; internal set; }

    [JsonConstructor]
    internal Leaderboard(
        LeaderboardKind kind,
        bool removeIfSingleClass,
        bool removeIfSingleCup,
        bool isEnabled = false
    ) {
        this.Kind = kind;
        this.RemoveIfSingleClass = removeIfSingleClass;
        this.RemoveIfSingleCup = removeIfSingleCup;
        this.IsEnabled = isEnabled;
    }

    internal Leaderboard(LeaderboardKind kind) {
        this.Kind = kind;
    }
}

internal class LeaderboardKindTypeConverter : TypeConverter {
    public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) {
        return sourceType == typeof(long);
    }

    public override object ConvertFrom(
        ITypeDescriptorContext context,
        System.Globalization.CultureInfo culture,
        object? value
    ) {
        if (value is long val) {
            return new Leaderboard((LeaderboardKind)val);
        }

        throw new NotSupportedException($"cannot convert object of type `{value?.GetType()}` to `LeaderboardKind`");
    }

    public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType) {
        return false;
    }

    // public override object ConvertTo(
    //     ITypeDescriptorContext context,
    //     System.Globalization.CultureInfo culture,
    //     object value,
    //     Type destinationType
    // ) {
    //     return base.ConvertTo(context, culture, value, destinationType);
    // }
}