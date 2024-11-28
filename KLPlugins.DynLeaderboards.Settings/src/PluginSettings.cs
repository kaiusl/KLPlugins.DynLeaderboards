using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

using KLPlugins.DynLeaderboards.Common;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
#if DEBUG
using System.Diagnostics;
#endif

namespace KLPlugins.DynLeaderboards.Settings;

public class PluginSettings {
    [JsonIgnore]
    public static readonly string LeaderboardConfigsDataDir = Path.Combine(
        PluginConstants.DataDir,
        "leaderboardConfigs"
    );

    [JsonIgnore]
    internal static readonly string LeaderboardConfigsDataBackupDir =
        Path.Combine(PluginSettings.LeaderboardConfigsDataDir, "b");

    [JsonIgnore]
    private static readonly string _defAccDataLocation = Path.Combine(
        "C:",
        "Users",
        Environment.UserName,
        "Documents",
        "Assetto Corsa Competizione"
    );

    [JsonIgnore] public const double LAP_DATA_TIME_DELAY_SEC = 0.5;
    [JsonIgnore] private const int _CURRENT_SETTINGS_VERSION = 3;
    [JsonProperty] public int Version { get; set; } = PluginSettings._CURRENT_SETTINGS_VERSION;
    [JsonProperty] public string? AccDataLocation { get; set; }
    [JsonProperty] public string? AcRootLocation { get; set; }
    [JsonProperty] public bool Log { get; set; }
    [JsonProperty] public int BroadcastDataUpdateRateMs { get; set; }

    [JsonProperty("OutGeneralProps")]
    internal OutGeneralProps OutGeneralPropsInternal { get; set; } = new(OutGeneralProp.NONE);

    [JsonIgnore]
    public ReadonlyOutProp<OutPropsBase<OutGeneralProp>, OutGeneralProp> OutGeneralProps =>
        this.OutGeneralPropsInternal.AsReadonly();

    [JsonIgnore] public ReadOnlyCollection<DynLeaderboardConfig> DynLeaderboardConfigs { get; set; }
    [JsonIgnore] private readonly List<DynLeaderboardConfig> _dynLeaderboardConfigs = [];
    [JsonIgnore] public Infos Infos = null!; // this is immediately set after reading the settings by SimHub from Json

    private delegate JObject Migration(JObject o);

    public PluginSettings() {
        this.AccDataLocation = PluginSettings._defAccDataLocation;
        this.Log = false;
        this.BroadcastDataUpdateRateMs = 500;
        this.DynLeaderboardConfigs = this._dynLeaderboardConfigs.AsReadOnly();
    }

    public void FinalizeInit(string gameName) {
        this.ReadDynLeaderboardConfigs();
        this.Infos = new Infos(gameName);
    }

    private void ReadDynLeaderboardConfigs() {
        Directory.CreateDirectory(PluginSettings.LeaderboardConfigsDataDir);

        foreach (var fileName in Directory.GetFiles(PluginSettings.LeaderboardConfigsDataDir)) {
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

                var newLeaderboard = new LeaderboardConfig(l);
                cfg.Order.Add(newLeaderboard);
            }

            this._dynLeaderboardConfigs.Add(cfg);
        }
    }

    public void Dispose() {
        this.SaveDynLeaderboardConfigs();
        this.Infos.Save();
    }

    private void SaveDynLeaderboardConfigs() {
        // Keep 5 latest backups of each config.
        // New config is only saved and backups are made if the config has changed.

        Directory.CreateDirectory(PluginSettings.LeaderboardConfigsDataBackupDir);

        foreach (var cfg in this.DynLeaderboardConfigs) {
            var cfgFileName = $"{PluginSettings.LeaderboardConfigsDataDir}\\{cfg.Name}.json";
            var serializedCfg = JsonConvert.SerializeObject(cfg, Formatting.Indented);
            var isSame = File.Exists(cfgFileName) && serializedCfg == File.ReadAllText(cfgFileName);

            if (!isSame) {
                RenameOrDeleteOldBackups(cfg);
                if (File.Exists(cfgFileName)) {
                    File.Move(
                        cfgFileName,
                        $"{PluginSettings.LeaderboardConfigsDataBackupDir}\\{cfg.Name}_b{1}.json"
                    );
                }

                File.WriteAllText(cfgFileName, serializedCfg);
            }
        }

        return;

        static void RenameOrDeleteOldBackups(DynLeaderboardConfig cfg) {
            for (var i = 5; i > -1; i--) {
                var currentBackupName =
                    $"{PluginSettings.LeaderboardConfigsDataBackupDir}\\{cfg.Name}_b{i + 1}.json";
                if (File.Exists(currentBackupName)) {
                    if (i != 4) {
                        File.Move(
                            currentBackupName,
                            $"{PluginSettings.LeaderboardConfigsDataBackupDir}\\{cfg.Name}_b{i + 2}.json"
                        );
                    } else {
                        File.Delete(currentBackupName);
                    }
                }
            }
        }
    }


    internal void AddLeaderboard(DynLeaderboardConfig cfg) {
        this._dynLeaderboardConfigs.Add(cfg);
    }

    internal void RemoveLeaderboard(DynLeaderboardConfig cfg) {
        var fname = $"{PluginSettings.LeaderboardConfigsDataDir}\\{cfg.Name}.json";
        if (File.Exists(fname)) {
            File.Delete(fname);
        }

        this._dynLeaderboardConfigs.Remove(cfg);
    }

    internal bool IsAccDataLocationValid() {
        return Directory.Exists($"{this.AccDataLocation}\\Config");
    }

    internal bool IsAcRootLocationValid() {
        return Directory.Exists($"{this.AcRootLocation}\\content\\cars");
    }

    [method: JsonConstructor]
    private class AcUiCarInfo(string name, string brand, string @class, List<string> tags) {
        public string Name { get; } = name;
        public string Brand { get; } = brand;
        public string Class { get; } = @class;
        public List<string> Tags { get; } = tags;
    }

    internal void UpdateAcCarInfos() {
        if (this.AcRootLocation == null) {
            Logging.LogWarn("AC root location is not set. Please check your settings.");
            return;
        }

        var carsFolder = Path.Combine(this.AcRootLocation, "content", "cars");
        if (!Directory.Exists(carsFolder)) {
            Logging.LogWarn("AC cars folder is not found. Please check your settings.");
            return;
        }

        Dictionary<string, CarInfo> carInfos = [];
        foreach (var carFolderPath in Directory.GetDirectories(carsFolder)) {
            var carId = Path.GetFileName(carFolderPath);
            var uiInfoFilePath = Path.Combine(carFolderPath, "ui", "ui_car.json");
            if (!File.Exists(uiInfoFilePath)) {
                continue;
            }

            var uiInfo = JsonConvert.DeserializeObject<AcUiCarInfo>(File.ReadAllText(uiInfoFilePath));
            if (uiInfo == null) {
                continue;
            }

            var cls = uiInfo.Class;
            if (cls is "race" or "street") {
                // Kunos cars have a proper class name in the tags as #... (for example #GT4 or #Vintage Touring)
                var altCls = uiInfo.Tags.Find(t => t.StartsWith("#"));
                if (altCls != null) {
                    cls = altCls.Substring(1);
                } else {
                    // Look for some more common patterns from the tags
                    string[] lookups = [
                        "gt3", "gt2", "gt4", "gt1", "gte", "lmp1", "lmp2", "lmp3", "formula1", "formula", "dtm",
                    ];
                    foreach (var lookup in lookups) {
                        altCls = uiInfo.Tags?.Find(t => t.ToLower() == lookup);
                        if (altCls != null) {
                            cls = altCls;
                            break;
                        }
                    }
                }
            }

            carInfos[carId] = new CarInfo(uiInfo.Name, uiInfo.Brand, new CarClass(cls));
            Logging.LogInfo(
                $"Read AC car info from '{uiInfoFilePath}': {JsonConvert.SerializeObject(carInfos[carId])}"
            );
        }

        if (carInfos.Count != 0) {
            var outPath = Path.Combine(PluginConstants.DataDir, Game.AC_NAME, "CarInfos.base.json");
            File.WriteAllText(outPath, JsonConvert.SerializeObject(carInfos, Formatting.Indented));
        }
    }

    public static bool GetLogValueFromDisk() {
        const string settingsFname = "PluginsData\\Common\\DynLeaderboardsPlugin.GeneralSettings.json";
        if (!File.Exists(settingsFname)) {
            return false;
        }

        var savedSettings = JObject.Parse(File.ReadAllText(settingsFname));

        var log = false; // If settings doesn't contain version key, it's 0
        if (savedSettings.TryGetValue(nameof(PluginSettings.Log), out var savedSetting)) {
            log = (bool)savedSetting!;
        }

        return log;
    }

    /// <summary>
    ///     Checks if settings version is changed since last save and migrates to current version if needed.
    ///     Old settings file is rewritten by the new one.
    ///     Should be called before reading the settings from file.
    /// </summary>
    public static void Migrate() {
        var migrations = PluginSettings.CreateMigrationsDict();

        const string settingsFname = "PluginsData\\Common\\DynLeaderboardsPlugin.GeneralSettings.json";
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
                // create backup of old settings before migrating
                using var backupFile = File.CreateText($"{settingsFname}.v{version}.bak");
                var serializer1 = new JsonSerializer { Formatting = Formatting.Indented };
                serializer1.Serialize(backupFile, savedSettings);

                // migrate
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

        Directory.CreateDirectory(PluginSettings.LeaderboardConfigsDataDir);
        Directory.CreateDirectory(PluginSettings.LeaderboardConfigsDataBackupDir);
        const string key = "DynLeaderboardConfigs";
        if (o.TryGetValue(key, out var configs)) {
            foreach (var cfg in configs) {
                var fname = $"{PluginSettings.LeaderboardConfigsDataDir}\\{cfg["Name"]}.json";
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