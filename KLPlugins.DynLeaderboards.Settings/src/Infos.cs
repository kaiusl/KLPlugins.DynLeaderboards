using KLPlugins.DynLeaderboards.Common;

namespace KLPlugins.DynLeaderboards.Settings;

public sealed class Infos {
    public CarInfos CarInfos { get; private set; }
    public ClassInfos ClassInfos { get; }
    public TextBoxColors<TeamCupCategory> TeamCupCategoryColors { get; }
    public TextBoxColors<DriverCategory> DriverCategoryColors { get; }

    internal void RereadCarInfos() {
        this.CarInfos = CarInfos.ReadFromJson(basePath: PluginPaths._CarInfosBasePath, path: PluginPaths._CarInfosPath);
    }

    internal Infos() {
        this.CarInfos = CarInfos.ReadFromJson(basePath: PluginPaths._CarInfosBasePath, path: PluginPaths._CarInfosPath);

        this.ClassInfos = ClassInfos.ReadFromJson(
            basePath: PluginPaths._ClassInfosBasePath,
            path: PluginPaths._ClassInfosPath
        );

        this.TeamCupCategoryColors = TextBoxColors<TeamCupCategory>.ReadFromJson(
            basePath: PluginPaths._TeamCupCategoryColorsBasePath,
            path: PluginPaths._TeamCupCategoryColorsPath
        );

        this.DriverCategoryColors = TextBoxColors<DriverCategory>.ReadFromJson(
            basePath: PluginPaths._DriverCategoryColorsBasePath,
            path: PluginPaths._DriverCategoryColorsPath
        );

        this.TeamCupCategoryColors.GetOrAdd(TeamCupCategory.Default);
        this.DriverCategoryColors.GetOrAdd(DriverCategory.Default);
    }

    internal void Save() {
        this.CarInfos.WriteToJson(PluginPaths._CarInfosPath);
        this.ClassInfos.WriteToJson(PluginPaths._ClassInfosPath);
        this.TeamCupCategoryColors.WriteToJson(PluginPaths._TeamCupCategoryColorsPath);
        this.DriverCategoryColors.WriteToJson(PluginPaths._DriverCategoryColorsPath);
    }
}