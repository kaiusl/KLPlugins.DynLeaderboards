﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

using KLPlugins.DynLeaderboards.Car;
using KLPlugins.DynLeaderboards.ksBroadcastingNetwork;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace KLPlugins.DynLeaderboards.Settings {
    internal class PluginSettings {
        public int Version { get; set; } = 2;
        public string AccDataLocation { get; set; }
        public bool Log { get; set; }
        public int BroadcastDataUpdateRateMs { get; set; }
        public OutGeneralProp OutGeneralProps = OutGeneralProp.None;
        public Dictionary<CarClass, string> CarClassColors { get; set; }
        public Dictionary<TeamCupCategory, string> TeamCupCategoryColors { get; set; }
        public Dictionary<TeamCupCategory, string> TeamCupCategoryTextColors { get; set; }
        public Dictionary<DriverCategory, string> DriverCategoryColors { get; set; }

        internal const int currentSettingsVersion = 2;
        internal List<DynLeaderboardConfig> DynLeaderboardConfigs { get; set; } = new List<DynLeaderboardConfig>();
        internal string PluginDataLocation { get; set; }

        internal const string pluginsDataDirName = "PluginsData\\KLPlugins\\DynLeaderboards";
        internal const string leaderboardConfigsDataDirName = "PluginsData\\KLPlugins\\DynLeaderboards\\leaderboardConfigs";
        internal const string leaderboardConfigsDataBackupDirName = "PluginsData\\KLPlugins\\DynLeaderboards\\leaderboardConfigs\\b";
        private static readonly string _defAccDataLocation = "C:\\Users\\" + Environment.UserName + "\\Documents\\Assetto Corsa Competizione";
        private delegate JObject Migration(JObject o);

        internal PluginSettings() {
            this.PluginDataLocation = pluginsDataDirName;
            this.AccDataLocation = _defAccDataLocation;
            this.Log = false;
            this.BroadcastDataUpdateRateMs = 500;
            this.DynLeaderboardConfigs = new List<DynLeaderboardConfig>();
            this.CarClassColors = CreateDefCarClassColors();
            this.TeamCupCategoryColors = CreateDefCupColors();
            this.TeamCupCategoryTextColors = CreateDefCupTextColors();
            this.DriverCategoryColors = CreateDefDriverCategoryColors();
            this.SaveDynLeaderboardConfigs();
        }

        internal void ReadDynLeaderboardConfigs() {
            Directory.CreateDirectory(leaderboardConfigsDataDirName);

            foreach (var fileName in Directory.GetFiles(leaderboardConfigsDataDirName)) {
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

            Directory.CreateDirectory(leaderboardConfigsDataBackupDirName);

            foreach (var cfg in this.DynLeaderboardConfigs) {
                var cfgFileName = $"{leaderboardConfigsDataDirName}\\{cfg.Name}.json";
                var serializedCfg = JsonConvert.SerializeObject(cfg, Newtonsoft.Json.Formatting.Indented);
                var isSame = File.Exists(cfgFileName) && serializedCfg == File.ReadAllText(cfgFileName);

                if (!isSame) {
                    RenameOrDeleteOldBackups(cfg);
                    if (File.Exists(cfgFileName)) {
                        File.Move(cfgFileName, $"{leaderboardConfigsDataBackupDirName}\\{cfg.Name}_b{1}.json");
                    }

                    File.WriteAllText(cfgFileName, serializedCfg);
                }
            }

            static void RenameOrDeleteOldBackups(DynLeaderboardConfig cfg) {
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
            var fname = $"{leaderboardConfigsDataDirName}\\{this.DynLeaderboardConfigs[i].Name}.json";
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
                if (cat == DriverCategory.Error) {
                    continue;
                }

                dict.Add(cat, cat.GetAccColor());
            }
            return dict;
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

            if (version == currentSettingsVersion) {
                return;
            }

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

        /// <summary>
        ///  Creates dictionary of migrations to be called. Key is "oldversion_newversion".
        /// </summary>
        /// <returns></returns>
        private static Dictionary<string, Migration> CreateMigrationsDict() {
            var migrations = new Dictionary<string, Migration> {
                ["0_1"] = Mig0To1,
                ["1_2"] = Mig1To2,
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

            Directory.CreateDirectory(leaderboardConfigsDataDirName);
            Directory.CreateDirectory(leaderboardConfigsDataBackupDirName);
            const string key = "DynLeaderboardConfigs";
            if (o.ContainsKey(key)) {
                foreach (var cfg in o[key]!) {
                    var fname = $"{leaderboardConfigsDataDirName}\\{cfg["Name"]}.json";
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
            // - no breaking changes
            // - only need to bump version and add new options

            o["Version"] = 2;

            foreach (var fileName in Directory.GetFiles(leaderboardConfigsDataDirName)) {
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
                file.Close();

                cfg.Version = 2;
                cfg.NumCupPos = cfg.NumClassPos;
                cfg.NumCupRelativePos = cfg.NumClassRelativePos;
                cfg.PartialRelativeCupNumClassPos = cfg.PartialRelativeClassNumClassPos;
                cfg.PartialRelativeCupNumRelativePos = cfg.PartialRelativeClassNumRelativePos;

                using StreamWriter fileOut = File.CreateText(fileName);
                var serializerOut = new JsonSerializer {
                    Formatting = Newtonsoft.Json.Formatting.Indented
                };
                serializerOut.Serialize(fileOut, cfg);

            }

            SimHub.Logging.Current.Info($"Migrated settings from v1 to v2.");

            return o;
        }
    }

    public class DynLeaderboardConfig {
        internal const int currentConfigVersion = 2;

        public int Version { get; set; } = 2;

        private string _name = "";
        public string Name {
            get => this._name;
            set {
                char[] arr = value.ToCharArray();
                arr = Array.FindAll(arr, c => char.IsLetterOrDigit(c));
                this._name = new string(arr);
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
        public int NumCupPos { get; set; } = 16;
        public int NumOnTrackRelativePos { get; set; } = 5;
        public int NumOverallRelativePos { get; set; } = 5;
        public int NumClassRelativePos { get; set; } = 5;
        public int NumCupRelativePos { get; set; } = 5;
        public int NumDrivers { get; set; } = 1;
        public int PartialRelativeOverallNumOverallPos { get; set; } = 5;
        public int PartialRelativeOverallNumRelativePos { get; set; } = 5;
        public int PartialRelativeClassNumClassPos { get; set; } = 5;
        public int PartialRelativeClassNumRelativePos { get; set; } = 5;
        public int PartialRelativeCupNumClassPos { get; set; } = 5;
        public int PartialRelativeCupNumRelativePos { get; set; } = 5;

        public List<Leaderboard> Order { get; set; } = new List<Leaderboard>();

        public int CurrentLeaderboardIdx {
            get => this._currentLeaderboardIdx;
            set {
                this._currentLeaderboardIdx = value > -1 && value < this.Order.Count ? value : 0;
                this.CurrentLeaderboardName = this.CurrentLeaderboard().ToString();
            }
        }
        private int _currentLeaderboardIdx = 0;
        internal string CurrentLeaderboardName = "";
        public bool IsEnabled { get; set; } = true;

        public Leaderboard CurrentLeaderboard() {
            return this.Order.ElementAtOrDefault(this.CurrentLeaderboardIdx);
        }

        public DynLeaderboardConfig() { }

        internal DynLeaderboardConfig(string name) {
            this.Name = name;
            this.Order = new List<Leaderboard>() {
                Leaderboard.Overall,
                Leaderboard.Class,
                Leaderboard.Cup,
                Leaderboard.PartialRelativeOverall,
                Leaderboard.PartialRelativeClass,
                Leaderboard.PartialRelativeCup,
                Leaderboard.RelativeOverall,
                Leaderboard.RelativeClass,
                Leaderboard.RelativeCup,
                Leaderboard.RelativeOnTrack,
                Leaderboard.RelativeOnTrackWoPit
            };
            this.CurrentLeaderboardName = this.Order[this._currentLeaderboardIdx].ToString();
        }

        internal void Rename(string newName) {
            var configFileName = $"{PluginSettings.leaderboardConfigsDataDirName}\\{this.Name}.json";
            if (File.Exists(configFileName)) {
                File.Move(configFileName, $"{PluginSettings.leaderboardConfigsDataDirName}\\{newName}.json");
            }

            for (int i = 5; i > -1; i--) {
                var currentBackupName = $"{PluginSettings.leaderboardConfigsDataBackupDirName}\\{this.Name}_b{i + 1}.json";
                if (File.Exists(currentBackupName)) {
                    File.Move(currentBackupName, $"{PluginSettings.leaderboardConfigsDataBackupDirName}\\{newName}_b{i + 1}.json");
                }
            }

            this.Name = newName;
        }

    }
}