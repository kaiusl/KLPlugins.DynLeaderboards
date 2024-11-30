using System;
using System.IO;

using KLPlugins.DynLeaderboards.Log;

namespace KLPlugins.DynLeaderboards.Common;

internal static class PluginConstants {
    public static readonly string DataDir = Path.Combine("PluginsData", "KLPlugins", "DynLeaderboards");
    public const string PLUGIN_NAME = "Dynamic Leaderboards";
}

internal static class PluginPaths {
    public const string PLUGINS_DATA_DIR = "PluginsData";
    internal static readonly string _GeneralSettingsPath = Path.Combine(
        PluginPaths.PLUGINS_DATA_DIR,
        "Common",
        "DynLeaderboardsPlugin.GeneralSettings.json"
    );
    internal static readonly string _DataDir = Path.Combine(
        PluginPaths.PLUGINS_DATA_DIR,
        "KLPlugins",
        "DynLeaderboards"
    );
    internal static readonly string _LogsDir = Path.Combine(PluginPaths._DataDir, "Logs");
    internal static readonly string _LeaderboardConfigsDataDir = Path.Combine(
        PluginPaths._DataDir,
        "leaderboardConfigs"
    );
    internal static readonly string _LeaderboardConfigsDataBackupDir =
        Path.Combine(PluginPaths._LeaderboardConfigsDataDir, "b");

    private static string? _gameDataDir = null;
    private static string? _lapsDataDir = null;
    private static string? _splinePosOffserPath = null;

    private static string? _carInfosBasePath = null;
    private static string? _carInfosPath = null;

    private static string? _classInfosBasePath = null;
    private static string? _classInfosPath = null;
    private static string? _simhubClassColorsPath = null;

    private static string? _teamCupCategoryColorsBasePath = null;
    private static string? _teamCupCategoryColorsPath = null;

    private static string? _driverCategoryColorsBasePath = null;
    private static string? _driverCategoryColorsPath = null;


    internal static string _GameDataDir => PluginPaths._gameDataDir ?? throw new NullReferenceException();
    internal static string _LapsDataDir => PluginPaths._lapsDataDir ?? throw new NullReferenceException();
    internal static string _SplinePosOffsetsPath =>
        PluginPaths._splinePosOffserPath ?? throw new NullReferenceException();
    internal static string _CarInfosBasePath => PluginPaths._carInfosBasePath ?? throw new NullReferenceException();
    internal static string _CarInfosPath => PluginPaths._carInfosPath ?? throw new NullReferenceException();
    internal static string _ClassInfosBasePath => PluginPaths._classInfosBasePath ?? throw new NullReferenceException();
    internal static string _ClassInfosPath => PluginPaths._classInfosPath ?? throw new NullReferenceException();
    internal static string _SimhubClassColorsPath =>
        PluginPaths._simhubClassColorsPath ?? throw new NullReferenceException();
    internal static string _TeamCupCategoryColorsBasePath =>
        PluginPaths._teamCupCategoryColorsBasePath ?? throw new NullReferenceException();
    internal static string _TeamCupCategoryColorsPath =>
        PluginPaths._teamCupCategoryColorsPath ?? throw new NullReferenceException();
    internal static string _DriverCategoryColorsBasePath =>
        PluginPaths._driverCategoryColorsBasePath ?? throw new NullReferenceException();
    internal static string _DriverCategoryColorsPath =>
        PluginPaths._driverCategoryColorsPath ?? throw new NullReferenceException();

    private const string _CAR_INFOS_FILENAME = "CarInfos";
    private const string _CLASS_INFOS_FILENAME = "ClassInfos";
    private const string _TEAM_CUP_CATEGORY_COLORS_JSON_NAME = "TeamCupCategoryColors";
    private const string _DRIVER_CATEGORY_COLORS_JSON_NAME = "DriverCategoryColors";


    public static void CreateStaticDirs() {
        Directory.CreateDirectory(PluginPaths._DataDir);
        Directory.CreateDirectory(PluginPaths._LogsDir);
        Directory.CreateDirectory(PluginPaths._LeaderboardConfigsDataDir);
        Directory.CreateDirectory(PluginPaths._LeaderboardConfigsDataBackupDir);
    }

    public static void Init(string gameName) {
        Logging.LogInfo("Initializing plugin paths");

        PluginPaths._gameDataDir = Path.Combine(PluginPaths._DataDir, gameName);
        Directory.CreateDirectory(PluginPaths._GameDataDir);

        PluginPaths._lapsDataDir = Path.Combine(PluginPaths._GameDataDir, "laps_data");
        Directory.CreateDirectory(PluginPaths._LapsDataDir);

        PluginPaths._splinePosOffserPath = Path.Combine(PluginPaths._GameDataDir, "spline_pos_offsets.json");

        PluginPaths._carInfosBasePath = Path.Combine(
            PluginPaths._GameDataDir,
            $"{PluginPaths._CAR_INFOS_FILENAME}.base.json"
        );
        PluginPaths._carInfosPath = Path.Combine(PluginPaths._GameDataDir, $"{PluginPaths._CAR_INFOS_FILENAME}.json");

        PluginPaths._classInfosBasePath = Path.Combine(
            PluginPaths._GameDataDir,
            $"{PluginPaths._CLASS_INFOS_FILENAME}.base.json"
        );
        PluginPaths._classInfosPath = Path.Combine(
            PluginPaths._GameDataDir,
            $"{PluginPaths._CLASS_INFOS_FILENAME}.json"
        );
        PluginPaths._simhubClassColorsPath = Path.Combine(PluginPaths._GameDataDir, "ColorPalette.json");

        PluginPaths._teamCupCategoryColorsBasePath = Path.Combine(
            PluginPaths._GameDataDir,
            $"{PluginPaths._TEAM_CUP_CATEGORY_COLORS_JSON_NAME}.base.json"
        );
        PluginPaths._teamCupCategoryColorsPath = Path.Combine(
            PluginPaths._GameDataDir,
            $"{PluginPaths._TEAM_CUP_CATEGORY_COLORS_JSON_NAME}.json"
        );

        PluginPaths._driverCategoryColorsBasePath = Path.Combine(
            PluginPaths._GameDataDir,
            $"{PluginPaths._DRIVER_CATEGORY_COLORS_JSON_NAME}.base.json"
        );
        PluginPaths._driverCategoryColorsPath = Path.Combine(
            PluginPaths._GameDataDir,
            $"{PluginPaths._DRIVER_CATEGORY_COLORS_JSON_NAME}.json"
        );
    }

    public static string LapDataFilePath(string trackId, string cls) {
        return Path.Combine(PluginPaths._LapsDataDir, $"{trackId}_{cls}.txt");
    }

    public static string LogFilePath(string initTime) {
        return Path.Combine(PluginPaths._LogsDir, $"Log_{initTime}.log");
    }

    public static string DynLeaderboardConfigFilePath(string leaderboardName) {
        return Path.Combine(PluginPaths._LeaderboardConfigsDataDir, $"{leaderboardName}.json");
    }

    public static string DynLeaderboardConfigBackupFilePath(string leaderboardName, int num) {
        return Path.Combine(PluginPaths._LeaderboardConfigsDataDir, $"{leaderboardName}_b{num}.json");
    }
}