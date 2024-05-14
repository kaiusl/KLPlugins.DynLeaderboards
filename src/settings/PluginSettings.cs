using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace KLPlugins.DynLeaderboards.Settings {
    internal class PluginSettings {
        [JsonProperty] public int Version { get; set; } = 3;
        [JsonProperty] public string AccDataLocation { get; set; }
        [JsonProperty] public string? AcRootLocation { get; set; }
        [JsonProperty] public bool Log { get; set; }
        [JsonProperty] public int BroadcastDataUpdateRateMs { get; set; }
        [JsonProperty] public OutGeneralProp OutGeneralProps = OutGeneralProp.None;
        [JsonProperty] public bool Include_ST21_In_GT2 { get; set; }
        [JsonProperty] public bool Include_CHL_In_GT2 { get; set; }

        [JsonIgnore] internal const int currentSettingsVersion = 3;
        [JsonIgnore] internal List<DynLeaderboardConfig> DynLeaderboardConfigs { get; set; } = [];

        [JsonIgnore] internal const string PluginDataDir = "PluginsData\\KLPlugins\\DynLeaderboards";
        [JsonIgnore] internal const string LeaderboardConfigsDataDir = PluginDataDir + "\\leaderboardConfigs";
        [JsonIgnore] internal const string LeaderboardConfigsDataBackupDir = LeaderboardConfigsDataDir + "\\b";
        [JsonIgnore] internal const double LapDataTimeDelaySec = 0.5;
        [JsonIgnore] private static readonly string _defAccDataLocation = "C:\\Users\\" + Environment.UserName + "\\Documents\\Assetto Corsa Competizione";
        private delegate JObject Migration(JObject o);

        internal PluginSettings() {
            this.AccDataLocation = _defAccDataLocation;
            this.Log = false;
            this.BroadcastDataUpdateRateMs = 500;
            this.DynLeaderboardConfigs = [];
            this.Include_CHL_In_GT2 = false;
            this.Include_ST21_In_GT2 = false;
            this.SaveDynLeaderboardConfigs();
        }

        internal void ReadDynLeaderboardConfigs() {
            Directory.CreateDirectory(LeaderboardConfigsDataDir);

            foreach (var fileName in Directory.GetFiles(LeaderboardConfigsDataDir)) {
                if (!File.Exists(fileName) || !fileName.EndsWith(".json")) {
                    continue;
                }

                using StreamReader file = File.OpenText(fileName);
                var serializer = new JsonSerializer();
                DynLeaderboardConfig cfg;
                try {
                    var result = (DynLeaderboardConfig?)serializer.Deserialize(file, typeof(DynLeaderboardConfig));
                    if (result == null) {
                        continue;
                    }
                    cfg = result;
                } catch (Exception e) {
                    SimHub.Logging.Current.Error($"Failed to deserialize leaderboard \"{fileName}\" configuration. Error {e}.");
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

                this.DynLeaderboardConfigs.Add(cfg);
            }
        }

        internal void SaveDynLeaderboardConfigs() {
            // Keep 5 latest backups of each config.
            // New config is only saved and backups are made if the config has changed.

            Directory.CreateDirectory(LeaderboardConfigsDataBackupDir);

            foreach (var cfg in this.DynLeaderboardConfigs) {
                var cfgFileName = $"{LeaderboardConfigsDataDir}\\{cfg.Name}.json";
                var serializedCfg = JsonConvert.SerializeObject(cfg, Newtonsoft.Json.Formatting.Indented);
                var isSame = File.Exists(cfgFileName) && serializedCfg == File.ReadAllText(cfgFileName);

                if (!isSame) {
                    RenameOrDeleteOldBackups(cfg);
                    if (File.Exists(cfgFileName)) {
                        File.Move(cfgFileName, $"{LeaderboardConfigsDataBackupDir}\\{cfg.Name}_b{1}.json");
                    }

                    File.WriteAllText(cfgFileName, serializedCfg);
                }
            }

            static void RenameOrDeleteOldBackups(DynLeaderboardConfig cfg) {
                for (int i = 5; i > -1; i--) {
                    var currentBackupName = $"{LeaderboardConfigsDataBackupDir}\\{cfg.Name}_b{i + 1}.json";
                    if (File.Exists(currentBackupName)) {
                        if (i != 4) {
                            File.Move(currentBackupName, $"{LeaderboardConfigsDataBackupDir}\\{cfg.Name}_b{i + 2}.json");
                        } else {
                            File.Delete(currentBackupName);
                        }
                    }
                }
            }

        }

        internal void RemoveLeaderboardAt(int i) {
            var fname = $"{LeaderboardConfigsDataDir}\\{this.DynLeaderboardConfigs[i].Name}.json";
            if (File.Exists(fname)) {
                File.Delete(fname);
            }
            this.DynLeaderboardConfigs.RemoveAt(i);
        }

        public int GetMaxNumClassPos() {
            int max = 0;
            if (this.DynLeaderboardConfigs.Count > 0) {
                foreach (var v in this.DynLeaderboardConfigs) {
                    if (!v.IsEnabled) {
                        continue;
                    }

                    max = Math.Max(max, v.NumClassPos);
                }
            }
            return max;
        }

        public int GetMaxNumCupPos() {
            int max = 0;
            if (this.DynLeaderboardConfigs.Count > 0) {
                foreach (var v in this.DynLeaderboardConfigs) {
                    if (!v.IsEnabled) {
                        continue;
                    }

                    max = Math.Max(max, v.NumCupPos);
                }
            }
            return max;
        }

        internal bool SetAccDataLocation(string newLoc) {
            if (!Directory.Exists($"{newLoc}\\Config")) {
                if (Directory.Exists($"{_defAccDataLocation}\\Config")) {
                    this.AccDataLocation = _defAccDataLocation;
                    DynLeaderboardsPlugin.LogWarn($"Set ACC data location doesn't exist. Using default location '{_defAccDataLocation}'");
                    return false;
                } else {
                    DynLeaderboardsPlugin.LogWarn("Set ACC data location doesn't exist. Please check your settings.");
                    return false;
                }
            } else {
                this.AccDataLocation = newLoc;
                return true;
            }
        }

        internal bool SetAcRootLocation(string newLoc) {
            if (!Directory.Exists($"{newLoc}\\content\\cars")) {
                DynLeaderboardsPlugin.LogWarn("Set AC root location is wrong. Please check your settings.");
                return false;
            } else {
                this.AcRootLocation = newLoc;
                return true;
            }
        }

        internal bool IsAccDataLocationValid() {
            return Directory.Exists($"{this.AccDataLocation}\\Config");
        }

        internal bool IsAcRootLocationValid() {
            return Directory.Exists($"{this.AcRootLocation}\\content\\cars");
        }

        /// <summary>
        /// Checks if settings version is changed since last save and migrates to current version if needed.
        /// Old settings file is rewritten by the new one.
        /// Should be called before reading the settings from file.
        /// </summary>
        internal static void Migrate() {
            Dictionary<string, Migration> _migrations = CreateMigrationsDict();

            string settingsFname = "PluginsData\\Common\\DynLeaderboardsPlugin.GeneralSettings.json";
            if (!File.Exists(settingsFname)) {
                return;
            }

            JObject savedSettings = JObject.Parse(File.ReadAllText(settingsFname));

            int version = 0; // If settings doesn't contain version key, it's 0
            if (savedSettings.ContainsKey("Version")) {
                version = (int)savedSettings["Version"]!;
            }

            if (version != currentSettingsVersion) {
                // Migrate step by step to current version.
                while (version != currentSettingsVersion) {
                    savedSettings = _migrations[$"{version}_{version + 1}"](savedSettings);
                    version += 1;
                }

                // Save up to date setting back to the disk
                using StreamWriter file = File.CreateText(settingsFname);
                var serializer = new JsonSerializer {
                    Formatting = Newtonsoft.Json.Formatting.Indented
                };
                serializer.Serialize(file, savedSettings);
            }

            DynLeaderboardConfig.Migrate();
        }

        /// <summary>
        ///  Creates dictionary of migrations to be called. Key is "oldversion_newversion".
        /// </summary>
        /// <returns></returns>
        private static Dictionary<string, Migration> CreateMigrationsDict() {
            var migrations = new Dictionary<string, Migration> {
                ["0_1"] = Mig0To1,
                ["1_2"] = Mig1To2,
                ["2_3"] = Mig2To3
            };

#if DEBUG
            for (int i = 0; i < currentSettingsVersion; i++) {
                Debug.Assert(migrations.ContainsKey($"{i}_{i + 1}"), $"Migration from v{i} to v{i + 1} is not set.");
            }
#endif

            return migrations;
        }

        /// <summary>
        /// Migration of setting from version 0 to version 1
        /// </summary>
        /// <param name="o"></param>
        /// <returns></returns>
        private static JObject Mig0To1(JObject o) {
            // v0 to v1 changes:
            // - Added version number to PluginSetting and DynLeaderboardConfig
            // - DynLeaderboardConfigs are saved separately in PluginsData\KLPlugins\DynLeaderboards\leaderboardConfigs
            //   and not saved in PluginsData\Common\DynLeaderboardsPlugin.GeneralSettings.json

            o["Version"] = 1;

            Directory.CreateDirectory(LeaderboardConfigsDataDir);
            Directory.CreateDirectory(LeaderboardConfigsDataBackupDir);
            const string key = "DynLeaderboardConfigs";
            if (o.ContainsKey(key)) {
                foreach (var cfg in o[key]!) {
                    var fname = $"{LeaderboardConfigsDataDir}\\{cfg["Name"]}.json";
                    if (File.Exists(fname)) // Don't overwrite existing configs
{
                        continue;
                    }

                    using StreamWriter file = File.CreateText(fname);
                    var serializer = new JsonSerializer {
                        Formatting = Newtonsoft.Json.Formatting.Indented
                    };
                    serializer.Serialize(file, cfg);
                }
            }

            SimHub.Logging.Current.Info($"Migrated settings from v0 to v1.");
            o.Remove("DynLeaderboardConfigs");

            return o;
        }

        /// <summary>
        /// Migration of setting from version 0 to version 1
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

            SimHub.Logging.Current.Info($"Migrated settings from v1 to v2.");

            return o;
        }

        /// <summary>
        /// Migration of setting from version 0 to version 1
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

            SimHub.Logging.Current.Info($"Migrated settings from v2 to v3.");

            return o;
        }
    }

    internal class DynLeaderboardConfig {
        [JsonIgnore] internal const int CurrentConfigVersion = 3;

        [JsonProperty] public int Version { get; set; } = 3;

        [JsonIgnore] private string _name = "";
        [JsonProperty]
        public string Name {
            get => this._name;
            set {
                char[] arr = value.ToCharArray();
                arr = Array.FindAll(arr, c => char.IsLetterOrDigit(c));
                this._name = new string(arr);
            }
        }

        [JsonIgnore] public string NextLeaderboardActionName => $"{this.Name}.NextLeaderboard";
        [JsonIgnore] public string PreviousLeaderboardActionName => $"{this.Name}.PreviousLeaderboard";

        [JsonProperty]
        public OutCarProp OutCarProps = OutCarProp.CarNumber
             | OutCarProp.CarClass
             | OutCarProp.IsFinished
             | OutCarProp.CarClassColor
             | OutCarProp.TeamCupCategoryColor
             | OutCarProp.TeamCupCategoryTextColor
             | OutCarProp.RelativeOnTrackLapDiff;

        [JsonProperty] public OutPitProp OutPitProps = OutPitProp.IsInPitLane;
        [JsonProperty] public OutPosProp OutPosProps = OutPosProp.DynamicPosition;
        [JsonProperty] public OutGapProp OutGapProps = OutGapProp.DynamicGapToFocused;
        [JsonProperty] public OutStintProp OutStintProps = OutStintProp.None;
        [JsonProperty] public OutDriverProp OutDriverProps = OutDriverProp.InitialPlusLastName;

        [JsonProperty]
        public OutLapProp OutLapProps = OutLapProp.Laps
             | OutLapProp.LastLapTime
             | OutLapProp.BestLapTime
             | OutLapProp.DynamicBestLapDeltaToFocusedBest
             | OutLapProp.DynamicLastLapDeltaToFocusedLast;

        [JsonProperty] public int NumOverallPos { get; set; } = 16;
        [JsonProperty] public int NumClassPos { get; set; } = 16;
        [JsonProperty] public int NumCupPos { get; set; } = 16;
        [JsonProperty] public int NumOnTrackRelativePos { get; set; } = 5;
        [JsonProperty] public int NumOverallRelativePos { get; set; } = 5;
        [JsonProperty] public int NumClassRelativePos { get; set; } = 5;

        [JsonProperty] public int NumCupRelativePos { get; set; } = 5;
        [JsonProperty] public int NumDrivers { get; set; } = 1;
        [JsonProperty] public int PartialRelativeOverallNumOverallPos { get; set; } = 5;
        [JsonProperty] public int PartialRelativeOverallNumRelativePos { get; set; } = 5;
        [JsonProperty] public int PartialRelativeClassNumClassPos { get; set; } = 5;
        [JsonProperty] public int PartialRelativeClassNumRelativePos { get; set; } = 5;
        [JsonProperty] public int PartialRelativeCupNumCupPos { get; set; } = 5;
        [JsonProperty] public int PartialRelativeCupNumRelativePos { get; set; } = 5;

        [JsonProperty] public List<Leaderboard> Order { get; set; } = new();

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
            return this.Order.ElementAtOrDefault(this.CurrentLeaderboardIdx);
        }

        public DynLeaderboardConfig() { }

        internal DynLeaderboardConfig(string name) {
            this.Name = name;
            this.Order = [
                new Leaderboard(LeaderboardKind.Overall),
                new Leaderboard(LeaderboardKind.Class, true, true),
                new Leaderboard(LeaderboardKind.Cup, true, true),
                new Leaderboard(LeaderboardKind.PartialRelativeOverall),
                new Leaderboard(LeaderboardKind.PartialRelativeClass, true, true),
                new Leaderboard(LeaderboardKind.PartialRelativeCup, true, true),
                new Leaderboard(LeaderboardKind.RelativeOverall),
                new Leaderboard(LeaderboardKind.RelativeClass, true, true),
                new Leaderboard(LeaderboardKind.RelativeCup, true, true),
                new Leaderboard(LeaderboardKind.RelativeOnTrack),
                new Leaderboard(LeaderboardKind.RelativeOnTrackWoPit)
            ];
            this.CurrentLeaderboardName = this.Order[this._currentLeaderboardIdx].Kind.ToString();
        }

        internal void Rename(string newName) {
            var configFileName = $"{PluginSettings.LeaderboardConfigsDataDir}\\{this.Name}.json";
            if (File.Exists(configFileName)) {
                File.Move(configFileName, $"{PluginSettings.LeaderboardConfigsDataDir}\\{newName}.json");
            }

            for (int i = 5; i > -1; i--) {
                var currentBackupName = $"{PluginSettings.LeaderboardConfigsDataBackupDir}\\{this.Name}_b{i + 1}.json";
                if (File.Exists(currentBackupName)) {
                    File.Move(currentBackupName, $"{PluginSettings.LeaderboardConfigsDataBackupDir}\\{newName}_b{i + 1}.json");
                }
            }

            this.Name = newName;
        }

        private int? _maxPositions = null;
        internal int MaxPositions() {
            if (this._maxPositions == null) {
                var numPos = new int[] {
                    this.NumOverallPos,
                    this.NumClassPos,
                    this.NumCupPos,
                    this.NumOverallRelativePos*2+1,
                    this.NumClassRelativePos*2+1,
                    this.NumCupRelativePos*2+1,
                    this.NumOnTrackRelativePos*2+1,
                    this.PartialRelativeClassNumClassPos + this.PartialRelativeClassNumRelativePos*2+1,
                    this.PartialRelativeOverallNumOverallPos + this.PartialRelativeOverallNumRelativePos*2+1,
                    this.PartialRelativeCupNumCupPos + this.PartialRelativeCupNumRelativePos*2+1,
                };

                this._maxPositions = numPos.Max();
            }

            return this._maxPositions.Value;
        }

        /// <summary>
        /// Checks if settings version is changed since last save and migrates to current version if needed.
        /// Old settings file is rewritten by the new one.
        /// Should be called before reading the settings from file.
        /// </summary>
        internal static void Migrate() {
            Dictionary<string, Migration> _migrations = CreateMigrationsDict();

            foreach (var fileName in Directory.GetFiles(PluginSettings.LeaderboardConfigsDataDir)) {
                if (!File.Exists(fileName) || !fileName.EndsWith(".json")) {
                    continue;
                }

                JObject savedSettings = JObject.Parse(File.ReadAllText(fileName));

                int version = 0; // If settings doesn't contain version key, it's 0
                if (savedSettings.ContainsKey("Version")) {
                    version = (int)savedSettings["Version"]!;
                }

                if (version == CurrentConfigVersion) {
                    return;
                }

                // Migrate step by step to current version.
                while (version != CurrentConfigVersion) {
                    savedSettings = _migrations[$"{version}_{version + 1}"](savedSettings);
                    version += 1;
                }

                // Save up to date setting back to the disk
                using StreamWriter file = File.CreateText(fileName);
                var serializer = new JsonSerializer {
                    Formatting = Newtonsoft.Json.Formatting.Indented
                };
                serializer.Serialize(file, savedSettings);
            }
        }
        /// <summary>
        ///  Creates dictionary of migrations to be called. Key is "oldversion_newversion".
        /// </summary>
        /// <returns></returns>
        private static Dictionary<string, Migration> CreateMigrationsDict() {
            var migrations = new Dictionary<string, Migration> {
                ["1_2"] = Mig1To2,
                ["2_3"] = Mig2To3
            };

#if DEBUG
            for (int i = 1; i < CurrentConfigVersion; i++) {
                Debug.Assert(migrations.ContainsKey($"{i}_{i + 1}"), $"Migration from v{i} to v{i + 1} is not set for DynLeaderboardConfig.");
            }
#endif

            return migrations;
        }

        /// <summary>
        /// Migration of setting from version 0 to version 1
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
        /// Migration of setting from version 0 to version 1
        /// </summary>
        /// <param name="o"></param>
        /// <returns></returns>
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

        [JsonConstructor]
        internal Leaderboard(LeaderboardKind kind, bool removeIfSingleClass, bool removeIfSingleCup) {
            this.Kind = kind;
            this.RemoveIfSingleClass = removeIfSingleClass;
            this.RemoveIfSingleCup = removeIfSingleCup;
        }

        internal Leaderboard(LeaderboardKind kind) {
            this.Kind = kind;
        }
    }

    internal class LeaderboardKindTypeConverter : TypeConverter {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) {
            return sourceType == typeof(System.Int64);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value) {
            return new Leaderboard((LeaderboardKind)(System.Int64)value);
        }

        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType) {
            return false;
        }

        public override object ConvertTo(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType) {
            return base.ConvertTo(context, culture, value, destinationType);
        }
    }
}