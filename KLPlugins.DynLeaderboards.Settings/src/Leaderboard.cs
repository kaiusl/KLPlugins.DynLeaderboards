using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;

using KLPlugins.DynLeaderboards.Common;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
#if DEBUG
using System.Diagnostics;
#endif

namespace KLPlugins.DynLeaderboards.Settings;

public sealed class DynLeaderboardConfig {
    [JsonIgnore] private const int _CURRENT_CONFIG_VERSION = 3;
    [JsonProperty] public int Version { get; internal set; } = DynLeaderboardConfig._CURRENT_CONFIG_VERSION;

    [JsonIgnore] private string _name = "";

    [JsonProperty]
    public string Name {
        get => this._name;
        internal set {
            var arr = value.ToCharArray();
            arr = Array.FindAll(arr, char.IsLetterOrDigit);
            this._name = new string(arr);
        }
    }


    [JsonProperty("OutCarProps")]
    internal OutCarProps OutCarPropsInternal { get; set; } = new(
        OutCarProp.CAR_NUMBER
        | OutCarProp.CAR_CLASS
        | OutCarProp.IS_FINISHED
        | OutCarProp.CAR_CLASS_COLOR
        | OutCarProp.TEAM_CUP_CATEGORY_COLOR
        | OutCarProp.TEAM_CUP_CATEGORY_TEXT_COLOR
        | OutCarProp.RELATIVE_ON_TRACK_LAP_DIFF
    );

    [JsonIgnore]
    public ReadonlyOutProps<OutPropsBase<OutCarProp>, OutCarProp> OutCarProps => this.OutCarPropsInternal.AsReadonly();

    [JsonProperty("OutPitProps")]
    internal OutPitProps OutPitPropsInternal { get; set; } = new(OutPitProp.IS_IN_PIT_LANE);

    [JsonIgnore]
    public ReadonlyOutProps<OutPropsBase<OutPitProp>, OutPitProp> OutPitProps => this.OutPitPropsInternal.AsReadonly();

    [JsonProperty("OutPosProps")]
    internal OutPosProps OutPosPropsInternal { get; set; } = new(OutPosProp.DYNAMIC_POSITION);

    [JsonIgnore]
    public ReadonlyOutProps<OutPropsBase<OutPosProp>, OutPosProp> OutPosProps => this.OutPosPropsInternal.AsReadonly();

    [JsonProperty("OutGapProps")]
    internal OutGapProps OutGapPropsInternal { get; set; } = new(OutGapProp.DYNAMIC_GAP_TO_FOCUSED);

    [JsonIgnore]
    public ReadonlyOutProps<OutPropsBase<OutGapProp>, OutGapProp> OutGapProps => this.OutGapPropsInternal.AsReadonly();

    [JsonProperty("OutStingProps")] internal OutStintProps OutStintPropsInternal { get; set; } = new(OutStintProp.NONE);

    [JsonIgnore]
    public ReadonlyOutProps<OutPropsBase<OutStintProp>, OutStintProp> OutStintProps =>
        this.OutStintPropsInternal.AsReadonly();

    [JsonProperty("OutDriverProps")]
    internal OutDriverProps OutDriverPropsInternal { get; set; } = new(OutDriverProp.INITIAL_PLUS_LAST_NAME);

    [JsonIgnore]
    public ReadonlyOutProps<OutPropsBase<OutDriverProp>, OutDriverProp> OutDriverProps =>
        this.OutDriverPropsInternal.AsReadonly();

    [JsonProperty("OutLapProps")]
    internal OutLapProps OutLapPropsInternal { get; set; } = new(
        OutLapProp.LAPS
        | OutLapProp.LAST_LAP_TIME
        | OutLapProp.BEST_LAP_TIME
        | OutLapProp.DYNAMIC_BEST_LAP_DELTA_TO_FOCUSED_BEST
        | OutLapProp.DYNAMIC_LAST_LAP_DELTA_TO_FOCUSED_LAST
    );

    [JsonIgnore]
    public ReadonlyOutProps<OutPropsBase<OutLapProp>, OutLapProp> OutLapProps => this.OutLapPropsInternal.AsReadonly();

    [JsonProperty]
    [JsonConverter(typeof(BoxJsonConverter<int>))]
    public Box<int> NumOverallPos { get; internal set; } = new(16);

    [JsonProperty]
    [JsonConverter(typeof(BoxJsonConverter<int>))]
    public Box<int> NumClassPos { get; internal set; } = new(16);

    [JsonProperty]
    [JsonConverter(typeof(BoxJsonConverter<int>))]
    public Box<int> NumCupPos { get; internal set; } = new(16);

    [JsonProperty]
    [JsonConverter(typeof(BoxJsonConverter<int>))]
    public Box<int> NumOnTrackRelativePos { get; internal set; } = new(5);

    [JsonProperty]
    [JsonConverter(typeof(BoxJsonConverter<int>))]
    public Box<int> NumOverallRelativePos { get; internal set; } = new(5);

    [JsonProperty]
    [JsonConverter(typeof(BoxJsonConverter<int>))]
    public Box<int> NumClassRelativePos { get; internal set; } = new(5);

    [JsonProperty]
    [JsonConverter(typeof(BoxJsonConverter<int>))]
    public Box<int> NumCupRelativePos { get; internal set; } = new(5);

    [JsonProperty]
    [JsonConverter(typeof(BoxJsonConverter<int>))]
    public Box<int> NumDrivers { get; internal set; } = new(1);

    [JsonProperty]
    [JsonConverter(typeof(BoxJsonConverter<int>))]
    public Box<int> PartialRelativeOverallNumOverallPos { get; internal set; } = new(5);

    [JsonProperty]
    [JsonConverter(typeof(BoxJsonConverter<int>))]
    public Box<int> PartialRelativeOverallNumRelativePos { get; internal set; } = new(5);

    [JsonProperty]
    [JsonConverter(typeof(BoxJsonConverter<int>))]
    public Box<int> PartialRelativeClassNumClassPos { get; internal set; } = new(5);

    [JsonProperty]
    [JsonConverter(typeof(BoxJsonConverter<int>))]
    public Box<int> PartialRelativeClassNumRelativePos { get; internal set; } = new(5);

    [JsonProperty]
    [JsonConverter(typeof(BoxJsonConverter<int>))]
    public Box<int> PartialRelativeCupNumCupPos { get; internal set; } = new(5);

    [JsonProperty]
    [JsonConverter(typeof(BoxJsonConverter<int>))]
    public Box<int> PartialRelativeCupNumRelativePos { get; internal set; } = new(5);

    [JsonProperty]
    public List<LeaderboardConfig> Order { get; internal set; } = [
        new(LeaderboardKind.OVERALL),
        new(LeaderboardKind.CLASS, true, true),
        new(LeaderboardKind.CUP, true, true),
        new(LeaderboardKind.PARTIAL_RELATIVE_OVERALL),
        new(LeaderboardKind.PARTIAL_RELATIVE_CLASS, true, true),
        new(LeaderboardKind.PARTIAL_RELATIVE_CUP, true, true),
        new(LeaderboardKind.RELATIVE_OVERALL),
        new(LeaderboardKind.RELATIVE_CLASS, true, true),
        new(LeaderboardKind.RELATIVE_CUP, true, true),
        new(LeaderboardKind.RELATIVE_ON_TRACK),
        new(LeaderboardKind.RELATIVE_ON_TRACK_WO_PIT),
    ];

    [JsonIgnore] private int _currentLeaderboardIdx = 0;

    [JsonProperty]
    public int CurrentLeaderboardIdx {
        get => this._currentLeaderboardIdx;
        set {
            this._currentLeaderboardIdx = value > -1 && value < this.Order.Count ? value : 0;
            var currentLeaderboard = this.CurrentLeaderboard();
            this.CurrentLeaderboardDisplayName = currentLeaderboard.Kind.ToDisplayString();
            this.CurrentLeaderboardCompactName = currentLeaderboard.Kind.ToCompactString();
        }
    }

    [JsonProperty] public bool IsEnabled { get; internal set; } = true;
    [JsonIgnore] public string NextLeaderboardActionName { get; }
    [JsonIgnore] public string PreviousLeaderboardActionName { get; }
    [JsonIgnore] public string CurrentLeaderboardDisplayName {get; private set;}
    [JsonIgnore] public string CurrentLeaderboardCompactName {get; private set;}

    private delegate JObject Migration(JObject o);

    public LeaderboardConfig CurrentLeaderboard() {
        return this.Order.ElementAt(this.CurrentLeaderboardIdx);
    }

    internal DynLeaderboardConfig(string name) {
        this.Name = name;
        this.NextLeaderboardActionName = $"{this.Name}.NextLeaderboard";
        this.PreviousLeaderboardActionName = $"{this.Name}.PreviousLeaderboard";
        this.CurrentLeaderboardDisplayName = this.Order[this._currentLeaderboardIdx].Kind.ToDisplayString();
        this.CurrentLeaderboardCompactName = this.Order[this._currentLeaderboardIdx].Kind.ToCompactString();
    }

    internal DynLeaderboardConfig DeepClone() {
        return JsonConvert.DeserializeObject<DynLeaderboardConfig>(JsonConvert.SerializeObject(this))!;
    }

    internal void Rename(string newName) {
        var configFileName = $"{PluginSettings.LeaderboardConfigsDataDir}\\{this.Name}.json";
        if (File.Exists(configFileName)) {
            File.Move(configFileName, $"{PluginSettings.LeaderboardConfigsDataDir}\\{newName}.json");
        }

        for (var i = 5; i > -1; i--) {
            var currentBackupName = $"{PluginSettings.LeaderboardConfigsDataBackupDir}\\{this.Name}_b{i + 1}.json";
            if (File.Exists(currentBackupName)) {
                File.Move(
                    currentBackupName,
                    $"{PluginSettings.LeaderboardConfigsDataBackupDir}\\{newName}_b{i + 1}.json"
                );
            }
        }

        this.Name = newName;
    }

    private int? _maxPositions = null;

    public int MaxPositions() {
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
        var migrations = DynLeaderboardConfig.CreateMigrationsDict();

        foreach (var filePath in Directory.GetFiles(PluginSettings.LeaderboardConfigsDataDir)) {
            if (!File.Exists(filePath) || !filePath.EndsWith(".json")) {
                continue;
            }

            var savedSettings = JObject.Parse(File.ReadAllText(filePath));

            var version = 0; // If settings doesn't contain version key, it's 0
            if (savedSettings.TryGetValue("Version", out var setting)) {
                version = (int)setting!;
            }

            if (version == DynLeaderboardConfig._CURRENT_CONFIG_VERSION) {
                return;
            }

            var fileName = Path.GetFileName(filePath);
            // Migrate step by step to current version.
            while (version != DynLeaderboardConfig._CURRENT_CONFIG_VERSION) {
                // create backup of old settings before migrating
                using var backupFile = File.CreateText(
                    $"{PluginSettings.LeaderboardConfigsDataBackupDir}\\{fileName}.v{version}.bak"
                );
                var serializer1 = new JsonSerializer { Formatting = Formatting.Indented };
                serializer1.Serialize(backupFile, savedSettings);

                // migrate
                savedSettings = migrations[$"{version}_{version + 1}"](savedSettings);
                version += 1;
            }

            // Save up-to-date setting back to the disk
            using var file = File.CreateText(filePath);
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

[TypeConverter(typeof(TyConverter))]
public sealed class LeaderboardConfig {
    [JsonProperty] public LeaderboardKind Kind { get; private set; }

    [JsonProperty] public bool RemoveIfSingleClass { get; internal set; }

    [JsonProperty] public bool RemoveIfSingleCup { get; internal set; }

    [JsonProperty] public bool IsEnabled { get; internal set; }

    [JsonConstructor]
    internal LeaderboardConfig(
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

    internal LeaderboardConfig(LeaderboardKind kind) {
        this.Kind = kind;
    }


    private class TyConverter : TypeConverter {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) {
            return sourceType == typeof(long);
        }

        public override object ConvertFrom(
            ITypeDescriptorContext context,
            System.Globalization.CultureInfo culture,
            object? value
        ) {
            if (value is long val) {
                return new LeaderboardConfig(LeaderboardKindExtensions.From(val));
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
}

// IMPORTANT: new leaderboards need to be added to the end in order to not break older configurations
public enum LeaderboardKind {
    NONE,
    OVERALL,
    CLASS,
    RELATIVE_OVERALL,
    RELATIVE_CLASS,
    PARTIAL_RELATIVE_OVERALL,
    PARTIAL_RELATIVE_CLASS,
    RELATIVE_ON_TRACK,
    RELATIVE_ON_TRACK_WO_PIT,
    CUP,
    RELATIVE_CUP,
    PARTIAL_RELATIVE_CUP,
}

internal static  class LeaderboardKindExtensions {
    internal const int MAX_VALUE = (int)LeaderboardKind.PARTIAL_RELATIVE_CUP;

    /// <summary>
    ///     Converts an integer to a corresponding <see cref="LeaderboardKind" /> enumeration value.
    /// </summary>
    /// <param name="i">The integer representation of a leaderboard kind.</param>
    /// <returns>The <see cref="LeaderboardKind" /> corresponding to the given integer.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     Thrown when the integer is not within the valid range of leaderboard kinds.
    /// </exception>
    internal static LeaderboardKind From(int i) {
        return LeaderboardKindExtensions.From((long)i);
    }

    /// <summary>
    ///     Converts a long integer to a corresponding <see cref="LeaderboardKind" /> enumeration value.
    /// </summary>
    /// <param name="i">The long integer representation of a leaderboard kind.</param>
    /// <returns>The <see cref="LeaderboardKind" /> corresponding to the given long integer.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     Thrown when the long integer is not within the valid range of leaderboard kinds.
    /// </exception>
    internal static LeaderboardKind From(long i) {
        if (i is >= 0 and <= LeaderboardKindExtensions.MAX_VALUE) {
            return (LeaderboardKind)i;
        }

        throw new ArgumentOutOfRangeException(nameof(i), i, null);
    }


    internal static string ToDisplayString(this LeaderboardKind kind) {
        return kind switch {
            LeaderboardKind.NONE => "None",
            LeaderboardKind.OVERALL => "Overall",
            LeaderboardKind.CLASS => "Class",
            LeaderboardKind.RELATIVE_OVERALL => "Relative overall",
            LeaderboardKind.RELATIVE_CLASS => "Relative class",
            LeaderboardKind.PARTIAL_RELATIVE_OVERALL => "Partial relative overall",
            LeaderboardKind.PARTIAL_RELATIVE_CLASS => "Partial relative class",
            LeaderboardKind.RELATIVE_ON_TRACK => "Relative on track",
            LeaderboardKind.RELATIVE_ON_TRACK_WO_PIT => "Relative on track (wo pit)",
            LeaderboardKind.CUP => "Cup",
            LeaderboardKind.RELATIVE_CUP => "Relative cup",
            LeaderboardKind.PARTIAL_RELATIVE_CUP => "Partial relative cup",
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
        };
    }

    internal static string ToCompactString(this LeaderboardKind kind) {
        return kind switch {
            LeaderboardKind.NONE => "None",
            LeaderboardKind.OVERALL => "Overall",
            LeaderboardKind.CLASS => "Class",
            LeaderboardKind.RELATIVE_OVERALL => "RelativeOverall",
            LeaderboardKind.RELATIVE_CLASS => "RelativeClass",
            LeaderboardKind.PARTIAL_RELATIVE_OVERALL => "PartialRelativeOverall",
            LeaderboardKind.PARTIAL_RELATIVE_CLASS => "PartialRelativeClass",
            LeaderboardKind.RELATIVE_ON_TRACK => "RelativeOnTrack",
            LeaderboardKind.RELATIVE_ON_TRACK_WO_PIT => "RelativeOnTrackWoPit",
            LeaderboardKind.CUP => "Cup",
            LeaderboardKind.RELATIVE_CUP => "RelativeCup",
            LeaderboardKind.PARTIAL_RELATIVE_CUP => "PartialRelativeCup",
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
        };
    }

    internal static string Tooltip(this LeaderboardKind l) {
        return l switch {
            LeaderboardKind.OVERALL => "`N` top overall positions. `N` can be set below.",
            LeaderboardKind.CLASS => "`N` top class positions. `N` can be set below.",
            LeaderboardKind.CUP => "`N` top class and cup positions. `N` can be set below.",
            LeaderboardKind.RELATIVE_OVERALL =>
                "`2N + 1` relative positions to the focused car in overall order. `N` can be set below.",
            LeaderboardKind.RELATIVE_CLASS =>
                "`2N + 1` relative positions to the focused car in focused car's class order. `N` can be set below.",
            LeaderboardKind.RELATIVE_CUP =>
                "`2N + 1` relative positions to the focused car in focused car's class and cup order. `N` can be set below.",
            LeaderboardKind.RELATIVE_ON_TRACK =>
                "`2N + 1` relative positions to the focused car on track. `N` can be set below.",
            LeaderboardKind.RELATIVE_ON_TRACK_WO_PIT =>
                "`2N + 1` relative positions to the focused car on track excluding the cars in the pit lane which are not on the same lap as the focused car. `N` can be set below.",
            LeaderboardKind.PARTIAL_RELATIVE_OVERALL =>
                "`N` top positions and `2M + 1` relative positions in overall order. If the focused car is inside the first `N + M + 1` positions the order will be just as the overall leaderboard. `N` and `M` can be set below.",
            LeaderboardKind.PARTIAL_RELATIVE_CLASS =>
                "`N` top positions and `2M + 1` relative positions in focused car's class order. If the focused car is inside the first `N + M + 1` positions the order will be just as the class leaderboard. `N` and `M` can be set below.",
            LeaderboardKind.PARTIAL_RELATIVE_CUP =>
                "`N` top positions and `2M + 1` relative positions in focused car's class and cup order. If the focused car is inside the first `N + M + 1` positions the order will be just as the cup leaderboard. `N` and `M` can be set below.",
            _ => "Unknown",
        };
    }
}