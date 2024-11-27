using System;
using System.IO;

using KLPlugins.DynLeaderboards.Car;

namespace KLPlugins.DynLeaderboards.Settings;

internal class Infos {
    internal CarInfos CarInfos { get; private set; }

    private const string _CAR_INFOS_FILENAME = "CarInfos";

    private static string CarInfosPath() {
        return
            $"{PluginSettings.PLUGIN_DATA_DIR}\\{DynLeaderboardsPlugin.Game.Name}\\{Infos._CAR_INFOS_FILENAME}.json";
    }

    private static string CarInfosBasePath() {
        return
            $"{PluginSettings.PLUGIN_DATA_DIR}\\{DynLeaderboardsPlugin.Game.Name}\\{Infos._CAR_INFOS_FILENAME}.base.json";
    }

    private static CarInfos ReadCarInfos() {
        var basesPath = Infos.CarInfosBasePath();
        var path = Infos.CarInfosPath();
        return CarInfos.ReadFromJson(basePath: basesPath, path: path);
    }

    private void WriteCarInfos() {
        var path = Infos.CarInfosPath();
        var dirPath = Path.GetDirectoryName(path);
        if (dirPath == null) {
            throw new Exception($"invalid car infos path {path}");
        }

        if (!Directory.Exists(dirPath)) {
            Directory.CreateDirectory(dirPath);
        }

        this.CarInfos.WriteToJson(path: path, derivedPath: Infos.CarInfosBasePath());
    }

    internal ClassInfos ClassInfos { get; }
    private const string _CLASS_INFOS_FILENAME = "ClassInfos";

    private static string ClassInfosPath() {
        return
            $"{PluginSettings.PLUGIN_DATA_DIR}\\{DynLeaderboardsPlugin.Game.Name}\\{Infos._CLASS_INFOS_FILENAME}.json";
    }

    private static string ClassInfosBasePath() {
        return
            $"{PluginSettings.PLUGIN_DATA_DIR}\\{DynLeaderboardsPlugin.Game.Name}\\{Infos._CLASS_INFOS_FILENAME}.base.json";
    }

    private static ClassInfos ReadClassInfos() {
        var basesPath = Infos.ClassInfosBasePath();
        var path = Infos.ClassInfosPath();
        return ClassInfos.ReadFromJson(basePath: basesPath, path: path);
    }

    private void WriteClassInfos() {
        var path = Infos.ClassInfosPath();
        var dirPath = Path.GetDirectoryName(path);
        if (dirPath == null) {
            throw new Exception($"invalid class infos path {path}");
        }

        if (!Directory.Exists(dirPath)) {
            Directory.CreateDirectory(dirPath);
        }

        this.ClassInfos.WriteToJson(path: path, derivedPath: Infos.ClassInfosBasePath());
    }

    internal TextBoxColors<TeamCupCategory> TeamCupCategoryColors { get; }
    internal TextBoxColors<DriverCategory> DriverCategoryColors { get; }

    private static string TextBoxColorsPath(string fileName) {
        return $"{PluginSettings.PLUGIN_DATA_DIR}\\{DynLeaderboardsPlugin.Game.Name}\\{fileName}.json";
    }

    private static TextBoxColors<K> ReadTextBoxColors<K>(string fileName) {
        var basesPath = $"{PluginSettings.PLUGIN_DATA_DIR}\\{DynLeaderboardsPlugin.Game.Name}\\{fileName}.base.json";
        var path = Infos.TextBoxColorsPath(fileName);
        return TextBoxColors<K>.ReadFromJson(basePath: basesPath, path: path);
    }

    private static void WriteTextBoxColors<K>(TextBoxColors<K> colors, string fileName) {
        var path = Infos.TextBoxColorsPath(fileName);
        var dirPath = Path.GetDirectoryName(path);
        if (dirPath == null) {
            throw new Exception($"invalid text box colors path {path}");
        }

        if (!Directory.Exists(dirPath)) {
            Directory.CreateDirectory(dirPath);
        }

        colors.WriteToJson(path);
    }

    private const string _TEAM_CUP_CATEGORY_COLORS_JSON_NAME = "TeamCupCategoryColors";
    private const string _DRIVER_CATEGORY_COLORS_JSON_NAME = "DriverCategoryColors";

    internal void RereadCarInfos() {
        this.CarInfos = Infos.ReadCarInfos();
    }

    internal Infos() {
        this.CarInfos = Infos.ReadCarInfos();
        this.ClassInfos = Infos.ReadClassInfos();
        this.TeamCupCategoryColors =
            Infos.ReadTextBoxColors<TeamCupCategory>(Infos._TEAM_CUP_CATEGORY_COLORS_JSON_NAME);
        this.TeamCupCategoryColors.GetOrAdd(TeamCupCategory.Default);
        this.DriverCategoryColors = Infos.ReadTextBoxColors<DriverCategory>(Infos._DRIVER_CATEGORY_COLORS_JSON_NAME);
        this.DriverCategoryColors.GetOrAdd(DriverCategory.Default);
    }

    internal void Save() {
        this.WriteCarInfos();
        this.WriteClassInfos();
        Infos.WriteTextBoxColors(this.TeamCupCategoryColors, Infos._TEAM_CUP_CATEGORY_COLORS_JSON_NAME);
        Infos.WriteTextBoxColors(this.DriverCategoryColors, Infos._DRIVER_CATEGORY_COLORS_JSON_NAME);
    }
}