using System;
using System.IO;

using KLPlugins.DynLeaderboards.Common;

namespace KLPlugins.DynLeaderboards.Settings;

public class Infos {
    private const string _CAR_INFOS_FILENAME = "CarInfos";
    private const string _CLASS_INFOS_FILENAME = "ClassInfos";
    private const string _TEAM_CUP_CATEGORY_COLORS_JSON_NAME = "TeamCupCategoryColors";
    private const string _DRIVER_CATEGORY_COLORS_JSON_NAME = "DriverCategoryColors";

    public CarInfos CarInfos { get; private set; }
    public ClassInfos ClassInfos { get; }
    public TextBoxColors<TeamCupCategory> TeamCupCategoryColors { get; }
    public TextBoxColors<DriverCategory> DriverCategoryColors { get; }

    private readonly string _gameName;

    internal void RereadCarInfos() {
        this.CarInfos = Infos.ReadCarInfos(this._gameName);
    }

    internal Infos(string gameName) {
        this._gameName = gameName;
        this.CarInfos = Infos.ReadCarInfos(gameName);
        this.ClassInfos = Infos.ReadClassInfos(gameName);
        this.TeamCupCategoryColors =
            Infos.ReadTextBoxColors<TeamCupCategory>(Infos._TEAM_CUP_CATEGORY_COLORS_JSON_NAME, gameName);
        this.TeamCupCategoryColors.GetOrAdd(TeamCupCategory.Default);
        this.DriverCategoryColors =
            Infos.ReadTextBoxColors<DriverCategory>(Infos._DRIVER_CATEGORY_COLORS_JSON_NAME, gameName);
        this.DriverCategoryColors.GetOrAdd(DriverCategory.Default);
    }

    internal void Save() {
        this.WriteCarInfos();
        this.WriteClassInfos();
        Infos.WriteTextBoxColors(this.TeamCupCategoryColors, Infos._TEAM_CUP_CATEGORY_COLORS_JSON_NAME, this._gameName);
        Infos.WriteTextBoxColors(this.DriverCategoryColors, Infos._DRIVER_CATEGORY_COLORS_JSON_NAME, this._gameName);
    }

    private void WriteCarInfos() {
        var path = Infos.CarInfosPath(this._gameName);
        var dirPath = Path.GetDirectoryName(path);
        if (dirPath == null) {
            throw new Exception($"invalid car infos path {path}");
        }

        if (!Directory.Exists(dirPath)) {
            Directory.CreateDirectory(dirPath);
        }

        this.CarInfos.WriteToJson(path: path, derivedPath: Infos.CarInfosBasePath(this._gameName));
    }

    private void WriteClassInfos() {
        var path = Infos.ClassInfosPath(this._gameName);
        var dirPath = Path.GetDirectoryName(path);
        if (dirPath == null) {
            throw new Exception($"invalid class infos path {path}");
        }

        if (!Directory.Exists(dirPath)) {
            Directory.CreateDirectory(dirPath);
        }

        this.ClassInfos.WriteToJson(path: path, derivedPath: Infos.ClassInfosBasePath(this._gameName));
    }


    private static string CarInfosPath(string gameName) {
        return
            $"{PluginConstants.DataDir}\\{gameName}\\{Infos._CAR_INFOS_FILENAME}.json";
    }

    private static string CarInfosBasePath(string gameName) {
        return
            $"{PluginConstants.DataDir}\\{gameName}\\{Infos._CAR_INFOS_FILENAME}.base.json";
    }

    private static string ClassInfosPath(string gameName) {
        return
            $"{PluginConstants.DataDir}\\{gameName}\\{Infos._CLASS_INFOS_FILENAME}.json";
    }

    private static string ClassInfosBasePath(string gameName) {
        return
            $"{PluginConstants.DataDir}\\{gameName}\\{Infos._CLASS_INFOS_FILENAME}.base.json";
    }

    private static CarInfos ReadCarInfos(string gameName) {
        var basesPath = Infos.CarInfosBasePath(gameName);
        var path = Infos.CarInfosPath(gameName);
        return CarInfos.ReadFromJson(basePath: basesPath, path: path);
    }

    private static ClassInfos ReadClassInfos(string gameName) {
        var basesPath = Infos.ClassInfosBasePath(gameName);
        var path = Infos.ClassInfosPath(gameName);
        return ClassInfos.ReadFromJson(basePath: basesPath, path: path, gameName: gameName);
    }

    private static string TextBoxColorsPath(string fileName, string gameName) {
        return $"{PluginConstants.DataDir}\\{gameName}\\{fileName}.json";
    }

    private static TextBoxColors<K> ReadTextBoxColors<K>(string fileName, string gameName) {
        var basesPath = $"{PluginConstants.DataDir}\\{gameName}\\{fileName}.base.json";
        var path = Infos.TextBoxColorsPath(fileName, gameName);
        return TextBoxColors<K>.ReadFromJson(basePath: basesPath, path: path);
    }

    private static void WriteTextBoxColors<K>(TextBoxColors<K> colors, string fileName, string gameName) {
        var path = Infos.TextBoxColorsPath(fileName, gameName);
        var dirPath = Path.GetDirectoryName(path);
        if (dirPath == null) {
            throw new Exception($"invalid text box colors path {path}");
        }

        if (!Directory.Exists(dirPath)) {
            Directory.CreateDirectory(dirPath);
        }

        colors.WriteToJson(path);
    }
}