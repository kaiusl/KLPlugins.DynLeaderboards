using KLPlugins.DynLeaderboards.Car;
using KLPlugins.DynLeaderboards.ksBroadcastingNetwork;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Xml;

namespace KLPlugins.DynLeaderboards.Settings {
    internal class PluginSettings {
        public int Version { get; set; } = 1;
        public string AccDataLocation { get; set; }
        public bool Log { get; set; }
        public int BroadcastDataUpdateRateMs { get; set; }
        public OutGeneralProp OutGeneralProps = OutGeneralProp.None;
        public Dictionary<CarClass, string> CarClassColors { get; set; }
        public Dictionary<TeamCupCategory, string> TeamCupCategoryColors { get; set; }
        public Dictionary<TeamCupCategory, string> TeamCupCategoryTextColors { get; set; }
        public Dictionary<DriverCategory, string> DriverCategoryColors { get; set; }

        internal const int currentSettingsVersion = 1;
        internal List<DynLeaderboardConfig> DynLeaderboardConfigs { get; set; } = new List<DynLeaderboardConfig>();
        internal string PluginDataLocation { get; set; }

        internal const string pluginsDataDirName = "PluginsData\\KLPlugins\\DynLeaderboards";
        internal const string leaderboardConfigsDataDirName = "PluginsData\\KLPlugins\\DynLeaderboards\\leaderboardConfigs";
        internal const string leaderboardConfigsDataBackupDirName = "PluginsData\\KLPlugins\\DynLeaderboards\\leaderboardConfigs\\b";
        private static readonly string _defAccDataLocation = "C:\\Users\\" + Environment.UserName + "\\Documents\\Assetto Corsa Competizione";
        private delegate JObject Migration(JObject o);

        internal PluginSettings() {
            PluginDataLocation = pluginsDataDirName;
            AccDataLocation = _defAccDataLocation;
            Log = false;
            BroadcastDataUpdateRateMs = 500;
            DynLeaderboardConfigs = new List<DynLeaderboardConfig>();
            CarClassColors = CreateDefCarClassColors();
            TeamCupCategoryColors = CreateDefCupColors();
            TeamCupCategoryTextColors = CreateDefCupTextColors();
            DriverCategoryColors = CreateDefDriverCategoryColors();
            SaveDynLeaderboardConfigs();
        }

        internal void ReadDynLeaderboardConfigs() {
            Directory.CreateDirectory(leaderboardConfigsDataDirName);

            foreach (var fileName in Directory.GetFiles(leaderboardConfigsDataDirName)) {
                if (!File.Exists(fileName) || !fileName.EndsWith(".json"))
                    continue;

                using (StreamReader file = File.OpenText(fileName)) {
                    JsonSerializer serializer = new JsonSerializer();
                    DynLeaderboardConfig cfg;
                    try {
                        cfg = (DynLeaderboardConfig)serializer.Deserialize(file, typeof(DynLeaderboardConfig));
                    } catch (Exception e) {
                        SimHub.Logging.Current.Error($"Failed to deserialize leaderboard \"{fileName}\" configuration. Error {e}.");
                        continue;
                    }

                    // Check for conflicting leaderboard names. Add CONFLICT to the end of the name.
                    if (DynLeaderboardConfigs.Any(x => x.Name == cfg.Name)) {
                        var num = 1;
                        while (DynLeaderboardConfigs.Any(x => x.Name == $"{cfg.Name}_CONFLICT{num}")) {
                            num++;
                        }
                        cfg.Name = $"{cfg.Name}_CONFLICT{num}";
                    }

                    DynLeaderboardConfigs.Add(cfg);
                }
            }
        }

        internal void SaveDynLeaderboardConfigs() {
            // Keep 5 latest backups of each config.
            // New config is only saved and backups are made if the config has changed.

            Directory.CreateDirectory(leaderboardConfigsDataBackupDirName);

            foreach (var cfg in DynLeaderboardConfigs) {
                var cfgFileName = $"{leaderboardConfigsDataDirName}\\{cfg.Name}.json";
                var serializedCfg = JsonConvert.SerializeObject(cfg, Newtonsoft.Json.Formatting.Indented);
                var isSame = File.Exists(cfgFileName) && serializedCfg == File.ReadAllText(cfgFileName);

                if (!isSame) {
                    RenameOrDeleteOldBackups(cfg);
                    if (File.Exists(cfgFileName))
                        File.Move(cfgFileName, $"{leaderboardConfigsDataBackupDirName}\\{cfg.Name}_b{1}.json");
                    File.WriteAllText(cfgFileName, serializedCfg);
                }
            }

            void RenameOrDeleteOldBackups(DynLeaderboardConfig cfg) {
                for (int i = 5; i > -1; i--) {
                    var currentBackupName = $"{leaderboardConfigsDataBackupDirName}\\{cfg.Name}_b{i + 1}.json";
                    if (File.Exists(currentBackupName)) {
                        if (i != 4) {
                            File.Move(currentBackupName, $"{leaderboardConfigsDataBackupDirName}\\{cfg.Name}_b{i + 2}.json");
                        } else {
                            File.Delete(currentBackupName);
                        }
                    }
                }
            }

        }

        internal void RemoveLeaderboardAt(int i) {
            var fname = $"{leaderboardConfigsDataDirName}\\{DynLeaderboardConfigs[i].Name}.json";
            if (File.Exists(fname)) {
                File.Delete(fname);
            }
            DynLeaderboardConfigs.RemoveAt(i);
        }

        public int GetMaxNumClassPos() {
            int max = 0;
            if (DynLeaderboardConfigs.Count > 0) {
                foreach (var v in DynLeaderboardConfigs) {
                    if (!v.IsEnabled) continue;
                    max = Math.Max(max, v.NumClassPos);
                }
            }
            return max;
        }

        private static Dictionary<CarClass, string> CreateDefCarClassColors() {
            var carClassColors = new Dictionary<CarClass, string>(8);
            foreach (var c in Enum.GetValues(typeof(CarClass))) {
                var cls = (CarClass)c;
                carClassColors.Add(cls, cls.ACCColor());
            }
            return carClassColors;
        }

        private static Dictionary<TeamCupCategory, string> CreateDefCupColors() {
            var cupColors = new Dictionary<TeamCupCategory, string>(5);
            foreach (var c in Enum.GetValues(typeof(TeamCupCategory))) {
                var cup = (TeamCupCategory)c;
                cupColors.Add(cup, cup.ACCColor());
            }
            return cupColors;
        }

        private static Dictionary<TeamCupCategory, string> CreateDefCupTextColors() {
            var cupTextColors = new Dictionary<TeamCupCategory, string>(5);
            foreach (var c in Enum.GetValues(typeof(TeamCupCategory))) {
                var cup = (TeamCupCategory)c;
                cupTextColors.Add(cup, cup.ACCTextColor());
            }
            return cupTextColors;
        }

        private static Dictionary<DriverCategory, string> CreateDefDriverCategoryColors() {
            var dict = new Dictionary<DriverCategory, string>(4);
            foreach (var c in Enum.GetValues(typeof(DriverCategory))) {
                var cat = (DriverCategory)c;
                if (cat == DriverCategory.Error)
                    continue;
                dict.Add(cat, cat.GetAccColor());
            }
            return dict;
        }

        internal bool SetAccDataLocation(string newLoc) {
            if (!Directory.Exists($"{newLoc}\\Config")) {
                if (Directory.Exists($"{_defAccDataLocation}\\Config")) {
                    AccDataLocation = _defAccDataLocation;
                    DynLeaderboardsPlugin.LogWarn($"Set ACC data location doesn't exist. Using default location '{_defAccDataLocation}'");
                    return false;
                } else {
                    DynLeaderboardsPlugin.LogWarn("Set ACC data location doesn't exist. Please check your settings.");
                    return false;
                }
            } else {
                AccDataLocation = newLoc;
                return true;
            }
        }

        /// <summary>
        /// Checks if settings version is changed since last save and migrates to current version if needed.
        /// Old settings file is rewritten by the new one.
        /// Should be called before reading the settings from file.
        /// </summary>
        internal static void Migrate() {
            Dictionary<string, Migration> _migrations = CreateMigrationsDict();

            string settingsFname = "PluginsData\\Common\\DynLeaderboardsPlugin.GeneralSettings.json";
            if (!File.Exists(settingsFname))
                return;
            JObject savedSettings = JObject.Parse(File.ReadAllText(settingsFname));

            int version = 0; // If settings doesn't contain version key, it's 0
            if (savedSettings.ContainsKey("Version")) {
                version = (int)savedSettings["Version"];
            }

            if (version == currentSettingsVersion)
                return;

            // Migrate step by step to current version.
            while (version != currentSettingsVersion) {
                savedSettings = _migrations[$"{version}_{version + 1}"](savedSettings);
                version += 1;
            }

            // Save up to date setting back to the disk
            using (StreamWriter file = File.CreateText(settingsFname)) {
                JsonSerializer serializer = new JsonSerializer();
                serializer.Formatting = Newtonsoft.Json.Formatting.Indented;
                serializer.Serialize(file, savedSettings);
            }

        }

        /// <summary>
        ///  Creates dictionary of migrations to be called. Key is "oldversion_newversion".
        /// </summary>
        /// <returns></returns>
        private static Dictionary<string, Migration> CreateMigrationsDict() {
            var migrations = new Dictionary<string, Migration>();
            migrations["0_1"] = Mig0To1;

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

            Directory.CreateDirectory(leaderboardConfigsDataDirName);
            Directory.CreateDirectory(leaderboardConfigsDataBackupDirName);
            if (o.ContainsKey("DynLeaderboardConfigs")) {
                foreach (var cfg in o["DynLeaderboardConfigs"]) {
                    var fname = $"{leaderboardConfigsDataDirName}\\{cfg["Name"]}.json";
                    if (File.Exists(fname)) // Don't overwrite existing configs
                        continue;
                    using (StreamWriter file = File.CreateText(fname)) {
                        JsonSerializer serializer = new JsonSerializer();
                        serializer.Formatting = Newtonsoft.Json.Formatting.Indented;
                        serializer.Serialize(file, cfg);
                    }
                }
            }

            SimHub.Logging.Current.Info($"Migrated settings from v0 to v1.");
            o.Remove("DynLeaderboardConfigs");

            return o;
        }
    }

    public class DynLeaderboardConfig {
        internal const int currentConfigVersion = 1;

        public int Version { get; set; } = 1;

        private string _name;
        public string Name {
            get { return _name; }
            set {
                char[] arr = value.ToCharArray();
                arr = Array.FindAll<char>(arr, (c => (char.IsLetterOrDigit(c))));
                _name = new string(arr);
            }
        }

        public OutCarProp OutCarProps = OutCarProp.CarNumber
            | OutCarProp.CarClass
            | OutCarProp.IsFinished
            | OutCarProp.CarClassColor
            | OutCarProp.TeamCupCategoryColor
            | OutCarProp.TeamCupCategoryTextColor
            | OutCarProp.RelativeOnTrackLapDiff;

        public OutPitProp OutPitProps = OutPitProp.IsInPitLane;
        public OutPosProp OutPosProps = OutPosProp.DynamicPosition;
        public OutGapProp OutGapProps = OutGapProp.DynamicGapToFocused;
        public OutStintProp OutStintProps = OutStintProp.None;
        public OutDriverProp OutDriverProps = OutDriverProp.InitialPlusLastName;

        public OutLapProp OutLapProps = OutLapProp.Laps
            | OutLapProp.LastLapTime
            | OutLapProp.BestLapTime
            | OutLapProp.DynamicBestLapDeltaToFocusedBest
            | OutLapProp.DynamicLastLapDeltaToFocusedLast;

        public int NumOverallPos { get; set; } = 16;
        public int NumClassPos { get; set; } = 16;
        public int NumOnTrackRelativePos { get; set; } = 5;
        public int NumOverallRelativePos { get; set; } = 5;
        public int NumClassRelativePos { get; set; } = 5;
        public int NumDrivers { get; set; } = 1;
        public int PartialRelativeOverallNumOverallPos { get; set; } = 5;
        public int PartialRelativeOverallNumRelativePos { get; set; } = 5;
        public int PartialRelativeClassNumClassPos { get; set; } = 5;
        public int PartialRelativeClassNumRelativePos { get; set; } = 5;

        public List<Leaderboard> Order { get; set; } = new List<Leaderboard>();

        public int CurrentLeaderboardIdx {
            get => _currentLeaderboardIdx;
            set {
                _currentLeaderboardIdx = value > -1 && value < Order.Count ? value : 0;
                CurrentLeaderboardName = CurrentLeaderboard().ToString();
            }
        }
        private int _currentLeaderboardIdx = 0;
        internal string CurrentLeaderboardName;
        public bool IsEnabled { get; set; } = true;

        public Leaderboard CurrentLeaderboard() {
            return Order.ElementAtOrDefault(CurrentLeaderboardIdx);
        }

        public DynLeaderboardConfig() { }

        internal DynLeaderboardConfig(string name) {
            Name = name;
            Order = new List<Leaderboard>() {
                Leaderboard.Overall,
                Leaderboard.Class,
                Leaderboard.PartialRelativeOverall,
                Leaderboard.PartialRelativeClass,
                Leaderboard.RelativeOverall,
                Leaderboard.RelativeClass,
                Leaderboard.RelativeOnTrack,
                Leaderboard.RelativeOnTrackWoPit
            };
            CurrentLeaderboardName = Order[_currentLeaderboardIdx].ToString();
        }

        internal void Rename(string newName) {
            var configFileName = $"{PluginSettings.leaderboardConfigsDataDirName}\\{Name}.json";
            if (File.Exists(configFileName)) {
                File.Move(configFileName, $"{PluginSettings.leaderboardConfigsDataDirName}\\{newName}.json");
            }

            for (int i = 5; i > -1; i--) {
                var currentBackupName = $"{PluginSettings.leaderboardConfigsDataBackupDirName}\\{Name}_b{i + 1}.json";
                if (File.Exists(currentBackupName)) {
                    File.Move(currentBackupName, $"{PluginSettings.leaderboardConfigsDataBackupDirName}\\{newName}_b{i + 1}.json");
                }
            }

            Name = newName;
        }

    }
}