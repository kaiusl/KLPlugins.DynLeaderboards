using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows.Media;

using GameReaderCommon;

using KLPlugins.DynLeaderboards.Car;
using KLPlugins.DynLeaderboards.Common;
using KLPlugins.DynLeaderboards.Log;
using KLPlugins.DynLeaderboards.Settings;
using KLPlugins.DynLeaderboards.Settings.UI;
using KLPlugins.DynLeaderboards.Track;

using SimHub.Plugins;

namespace KLPlugins.DynLeaderboards;

[PluginDescription("")]
[PluginAuthor("Kaius Loos")]
[PluginName(PluginConstants.PLUGIN_NAME)]
public sealed class DynLeaderboardsPlugin : IDataPlugin, IWPFSettingsV2 {
    // The properties that compiler yells at that can be null are set in Init method.
    // For the purposes of this plugin, they are never null
    #pragma warning disable CS8618
    public PluginManager PluginManager { get; set; }
    public ImageSource PictureIcon => this.ToIcon(Properties.Resources.sdkmenuicon);
    public string LeftMenuTitle => PluginConstants.PLUGIN_NAME;

    internal static PluginSettings _Settings;
    internal static Game _Game; // Const during the lifetime of this plugin, plugin is rebuilt at game change

    private double _dataUpdateTime = 0;

    public Values Values { get; private set; }
    public List<DynLeaderboard> DynLeaderboards { get; set; } = [];
    #pragma warning restore CS8618

    /// <summary>
    ///     Called one time per game data update, contains all normalized game data,
    ///     raw data are intentionally "hidden" under a generic object type (A plugin SHOULD NOT USE IT)
    ///     This method is on the critical path, it must execute as fast as possible and avoid throwing any error
    /// </summary>
    public void DataUpdate(PluginManager pm, ref GameData data) {
        var swatch = Stopwatch.StartNew();
        if (data.GameRunning && data.OldData != null && data.NewData != null) {
            this.Values.OnDataUpdate(pm, data);
            foreach (var ldb in this.DynLeaderboards) {
                ldb.OnDataUpdate(this.Values);
            }
        }

        swatch.Stop();
        var ts = swatch.Elapsed;

        this._dataUpdateTime = ts.TotalMilliseconds;
    }

    /// <summary>
    ///     Called at plugin manager stop, close/dispose anything needed here !
    ///     Plugins are rebuilt at game change
    /// </summary>
    /// <param name="pluginManager"></param>
    public void End(PluginManager pluginManager) {
        this.SaveCommonSettings("GeneralSettings", DynLeaderboardsPlugin._Settings);
        DynLeaderboardsPlugin._Settings.Dispose();
        this.Values.Dispose();
        Logging.Dispose();
    }

    /// <summary>
    ///     Returns the settings control, return null if no settings control is required
    /// </summary>
    /// <param name="pluginManager"></param>
    /// <returns></returns>
    public System.Windows.Controls.Control GetWPFSettingsControl(PluginManager pluginManager) {
        Logging.DebugLogInfo("GetWPFSettingsControl");
        return new SettingsControl(DynLeaderboardsPlugin._Settings, DynLeaderboardsPlugin._Game);
    }

    /// <summary>
    ///     Called once after plugins startup
    ///     Plugins are rebuilt at game change
    /// </summary>
    /// <param name="pm"></param>
    public void Init(PluginManager pm) {
        // Performance is important while in game, pre-jit methods at startup, to avoid doing that mid-races
        DynLeaderboardsPlugin.PreJit();

        var log = PluginSettings.GetLogValueFromDisk();
        Logging.Init(log);

        Logging.LogInfo("Starting plugin.");

        // game doesn't depend on anything else, do it first
        var gameName = pm.GameName;
        DynLeaderboardsPlugin._Game = new Game(gameName);
        PluginPaths.Init(gameName);

        PluginSettings.Migrate(); // migrate settings before reading them properly
        DynLeaderboardsPlugin._Settings = this.ReadCommonSettings("GeneralSettings", () => new PluginSettings());
        DynLeaderboardsPlugin._Settings.FinalizeInit();

        TrackData.OnPluginInit(gameName);

        this.Values = new Values();

        foreach (var config in DynLeaderboardsPlugin._Settings.DynLeaderboardConfigs) {
            if (config.IsEnabled) {
                var ldb = new DynLeaderboard(config, this.Values);
                this.DynLeaderboards.Add(ldb);
                this.AttachDynLeaderboard(ldb);
                Logging.LogInfo($"Added enabled leaderboard: {ldb.Name}.");
            } else {
                Logging.LogInfo($"Didn't add disabled leaderboard: {config.Name}.");
            }
        }

        this.AttachGeneralDelegates();
        this.SubscribeToSimHubEvents(pm);
    }

    private void AttachDelegate<T>(
        string name,
        Func<T> valueProvider,
        SupportStatus supportStatus = SupportStatus.Supported
    ) {
        this.PluginManager.AttachDelegate<T>(
            name,
            typeof(DynLeaderboardsPlugin),
            valueProvider,
            supportStatus: supportStatus
        );
    }

    private void AttachGeneralDelegates() {
        this.AttachDelegate<double>("Perf.DataUpdateMS", () => this._dataUpdateTime);

        var outGenProps = DynLeaderboardsPlugin._Settings.OutGeneralProps;

        // Add everything else
        if (outGenProps.Includes(OutGeneralProp.SESSION_PHASE)) {
            this.AttachDelegate<string>(
                OutGeneralProp.SESSION_PHASE.ToPropName(),
                () => this.Values.Session.SessionPhase.ToString()
            );
        }

        if (outGenProps.Includes(OutGeneralProp.MAX_STINT_TIME)) {
            this.AttachDelegate<double>(
                OutGeneralProp.MAX_STINT_TIME.ToPropName(),
                () => this.Values.Session.MaxDriverStintTime?.TotalSeconds ?? -1
            );
        }

        if (outGenProps.Includes(OutGeneralProp.MAX_DRIVE_TIME)) {
            this.AttachDelegate<double>(
                OutGeneralProp.MAX_DRIVE_TIME.ToPropName(),
                () => this.Values.Session.MaxDriverTotalDriveTime?.TotalSeconds ?? -1
            );
        }

        if (outGenProps.Includes(OutGeneralProp.CAR_CLASS_COLORS)) {
            foreach (var kv in DynLeaderboardsPlugin._Settings.Infos.ClassInfos) {
                var value = kv.Value;
                this.AttachDelegate<string>(
                    OutGeneralProp.CAR_CLASS_COLORS.ToPropName().Replace("<class>", kv.Key.AsString()),
                    () => value.Background() ?? TextBoxColor.DEF_BG
                );
            }
        }

        if (outGenProps.Includes(OutGeneralProp.CAR_CLASS_COLORS)) {
            foreach (var kv in DynLeaderboardsPlugin._Settings.Infos.ClassInfos) {
                var value = kv.Value;
                this.AttachDelegate<string>(
                    OutGeneralProp.CAR_CLASS_TEXT_COLORS.ToPropName().Replace("<class>", kv.Key.AsString()),
                    () => value.Foreground() ?? TextBoxColor.DEF_FG
                );
            }
        }

        if (outGenProps.Includes(OutGeneralProp.TEAM_CUP_COLORS)) {
            foreach (var kv in DynLeaderboardsPlugin._Settings.Infos.TeamCupCategoryColors) {
                var value = kv.Value;
                this.AttachDelegate<string>(
                    OutGeneralProp.TEAM_CUP_COLORS.ToPropName().Replace("<cup>", kv.Key.AsString()),
                    () => value.Background() ?? TextBoxColor.DEF_BG
                );
            }
        }

        if (outGenProps.Includes(OutGeneralProp.TEAM_CUP_TEXT_COLORS)) {
            foreach (var kv in DynLeaderboardsPlugin._Settings.Infos.TeamCupCategoryColors) {
                var value = kv.Value;
                this.AttachDelegate<string>(
                    OutGeneralProp.TEAM_CUP_TEXT_COLORS.ToPropName().Replace("<cup>", kv.Key.AsString()),
                    () => value.Foreground() ?? TextBoxColor.DEF_FG
                );
            }
        }

        if (outGenProps.Includes(OutGeneralProp.DRIVER_CATEGORY_COLORS)) {
            foreach (var kv in DynLeaderboardsPlugin._Settings.Infos.DriverCategoryColors) {
                var value = kv.Value;
                this.AttachDelegate<string>(
                    OutGeneralProp.DRIVER_CATEGORY_COLORS.ToPropName().Replace("<category>", kv.Key.AsString()),
                    () => value.Background() ?? TextBoxColor.DEF_BG
                );
            }
        }

        if (outGenProps.Includes(OutGeneralProp.DRIVER_CATEGORY_TEXT_COLORS)) {
            foreach (var kv in DynLeaderboardsPlugin._Settings.Infos.DriverCategoryColors) {
                var value = kv.Value;
                this.AttachDelegate<string>(
                    OutGeneralProp.DRIVER_CATEGORY_TEXT_COLORS.ToPropName().Replace("<category>", kv.Key.AsString()),
                    () => value.Foreground() ?? TextBoxColor.DEF_FG
                );
            }
        }

        if (outGenProps.Includes(OutGeneralProp.NUM_CLASSES_IN_SESSION)) {
            this.AttachDelegate<int>(
                OutGeneralProp.NUM_CLASSES_IN_SESSION.ToPropName(),
                () => this.Values.NumClassesInSession
            );
        }

        if (outGenProps.Includes(OutGeneralProp.NUM_CUPS_IN_SESSION)) {
            this.AttachDelegate<int>(
                OutGeneralProp.NUM_CUPS_IN_SESSION.ToPropName(),
                () => this.Values.NumCupsInSession
            );
        }
    }

    private void AttachDynLeaderboard(DynLeaderboard l) {
        void AddCar(int i) {
            var startName = $"{l.Name}.{i + 1}";

            this.AttachDelegate<int>(
                $"{startName}.Exists",
                () => (l.GetDynCar(i) != null).ToInt()
            );

            void AddProp<T>(OutCarProp prop, Func<T> valueProvider) {
                if (l._Config.OutCarProps.Includes(prop)) {
                    this.AttachDelegate<T>($"{startName}.{prop.ToPropName()}", valueProvider);
                }
            }

            void AddDriverProp<T>(OutDriverProp prop, string driverId, Func<T> valueProvider) {
                if (l._Config.OutDriverProps.Includes(prop)) {
                    this.AttachDelegate<T>(
                        $"{startName}.{driverId}.{prop.ToPropName()}",
                        valueProvider
                    );
                }
            }

            void AddLapProp<T>(OutLapProp prop, Func<T> valueProvider) {
                if (l._Config.OutLapProps.Includes(prop)) {
                    this.AttachDelegate<T>($"{startName}.{prop.ToPropName()}", valueProvider);
                }
            }

            void AddStintProp<T>(OutStintProp prop, Func<T> valueProvider) {
                if (l._Config.OutStintProps.Includes(prop)) {
                    this.AttachDelegate<T>($"{startName}.{prop.ToPropName()}", valueProvider);
                }
            }

            void AddGapProp<T>(OutGapProp prop, Func<T> valueProvider) {
                if (l._Config.OutGapProps.Includes(prop)) {
                    this.AttachDelegate<T>($"{startName}.{prop.ToPropName()}", valueProvider);
                }
            }

            void AddPosProp<T>(OutPosProp prop, Func<T> valueProvider) {
                if (l._Config.OutPosProps.Includes(prop)) {
                    this.AttachDelegate<T>($"{startName}.{prop.ToPropName()}", valueProvider);
                }
            }

            void AddPitProp<T>(OutPitProp prop, Func<T> valueProvider) {
                if (l._Config.OutPitProps.Includes(prop)) {
                    this.AttachDelegate<T>($"{startName}.{prop.ToPropName()}", valueProvider);
                }
            }

            void AddSectors(OutLapProp prop, string name, Func<Sectors?> sectorsProvider) {
                if (l._Config.OutLapProps.Includes(prop)) {
                    this.AttachDelegate<double?>(
                        $"{startName}.{name}1",
                        () => sectorsProvider()?.S1Time?.TotalSeconds
                    );
                    this.AttachDelegate<double?>(
                        $"{startName}.{name}2",
                        () => sectorsProvider()?.S2Time?.TotalSeconds
                    );
                    this.AttachDelegate<double?>(
                        $"{startName}.{name}3",
                        () => sectorsProvider()?.S3Time?.TotalSeconds
                    );
                }
            }

            // Laps and sectors
            AddLapProp<int?>(OutLapProp.LAPS, () => l.GetDynCar(i)?.Laps.New);

            AddLapProp<double?>(OutLapProp.LAST_LAP_TIME, () => l.GetDynCar(i)?.LastLap?.Time?.TotalSeconds);
            AddSectors(OutLapProp.LAST_LAP_SECTORS, "Laps.Last.S", () => l.GetDynCar(i)?.LastLap);

            AddLapProp<double?>(OutLapProp.BEST_LAP_TIME, () => l.GetDynCar(i)?.BestLap?.Time?.TotalSeconds);
            AddSectors(OutLapProp.BEST_LAP_SECTORS, "Laps.Best.S", () => l.GetDynCar(i)?.BestLap);

            AddSectors(OutLapProp.BEST_SECTORS, "BestS", () => l.GetDynCar(i)?.BestSectors);

            AddLapProp<double?>(OutLapProp.CURRENT_LAP_TIME, () => l.GetDynCar(i)?.CurrentLapTime.TotalSeconds);

            AddLapProp<int?>(OutLapProp.CURRENT_LAP_IS_VALID, () => l.GetDynCar(i)?.IsCurrentLapValid.ToInt());
            AddLapProp<int?>(OutLapProp.LAST_LAP_IS_VALID, () => l.GetDynCar(i)?.LastLap?.IsValid.ToInt());

            AddLapProp<int?>(OutLapProp.CURRENT_LAP_IS_OUT_LAP, () => l.GetDynCar(i)?.IsCurrentLapOutLap.ToInt());
            AddLapProp<int?>(OutLapProp.LAST_LAP_IS_OUT_LAP, () => l.GetDynCar(i)?.LastLap?.IsOutLap.ToInt());
            AddLapProp<int?>(OutLapProp.CURRENT_LAP_IS_IN_LAP, () => l.GetDynCar(i)?.IsCurrentLapInLap.ToInt());
            AddLapProp<int?>(OutLapProp.LAST_LAP_IS_IN_LAP, () => l.GetDynCar(i)?.LastLap?.IsInLap.ToInt());

            void AddOneDriverFromList(int j) {
                var driverId = $"Driver.{j + 1}";
                AddDriverProp<string?>(
                    OutDriverProp.FIRST_NAME,
                    driverId,
                    () => l.GetDynCar(i)?.Drivers.ElementAtOrDefault<Driver>(j)?.FirstName
                );
                AddDriverProp<string?>(
                    OutDriverProp.LAST_NAME,
                    driverId,
                    () => l.GetDynCar(i)?.Drivers.ElementAtOrDefault<Driver>(j)?.LastName
                );
                AddDriverProp<string?>(
                    OutDriverProp.SHORT_NAME,
                    driverId,
                    () => l.GetDynCar(i)?.Drivers.ElementAtOrDefault<Driver>(j)?.ShortName
                );
                AddDriverProp<string?>(
                    OutDriverProp.FULL_NAME,
                    driverId,
                    () => l.GetDynCar(i)?.Drivers.ElementAtOrDefault<Driver>(j)?.FullName
                );
                AddDriverProp<string?>(
                    OutDriverProp.INITIAL_PLUS_LAST_NAME,
                    driverId,
                    () => l.GetDynCar(i)?.Drivers.ElementAtOrDefault<Driver>(j)?.InitialPlusLastName
                );
                AddDriverProp<string?>(
                    OutDriverProp.NATIONALITY,
                    driverId,
                    () => l.GetDynCar(i)?.Drivers.ElementAtOrDefault<Driver>(j)?.Nationality
                );
                AddDriverProp<string?>(
                    OutDriverProp.CATEGORY,
                    driverId,
                    () => l.GetDynCar(i)?.Drivers.ElementAtOrDefault<Driver>(j)?.Category.ToString()
                );
                AddDriverProp<int?>(
                    OutDriverProp.TOTAL_LAPS,
                    driverId,
                    () => l.GetDynCar(i)?.Drivers.ElementAtOrDefault<Driver>(j)?.TotalLaps
                );
                AddDriverProp<double?>(
                    OutDriverProp.BEST_LAP_TIME,
                    driverId,
                    () => l.GetDynCar(i)?.Drivers.ElementAtOrDefault<Driver>(j)?.BestLap?.Time?.TotalSeconds
                );
                AddDriverProp<double?>(
                    OutDriverProp.TOTAL_DRIVING_TIME,
                    driverId,
                    () => {
                        // We cannot pre-calculate the total driving time because in some games (ACC) the current driver updates at first sector split.
                        var car = l.GetDynCar(i);
                        return car?.Drivers.ElementAtOrDefault<Driver>(j)
                            ?.GetTotalDrivingTime(j == 0, car.CurrentStintTime)
                            .TotalSeconds;
                    }
                );
                AddDriverProp<string?>(
                    OutDriverProp.CATEGORY_COLOR_DEPRECATED,
                    driverId,
                    () => l.GetDynCar(i)?.Drivers.ElementAtOrDefault<Driver>(j)?.CategoryColor.Bg
                );
                AddDriverProp<string?>(
                    OutDriverProp.CATEGORY_COLOR,
                    driverId,
                    () => l.GetDynCar(i)?.Drivers.ElementAtOrDefault<Driver>(j)?.CategoryColor.Bg
                );
                AddDriverProp<string?>(
                    OutDriverProp.CATEGORY_COLOR_TEXT,
                    driverId,
                    () => l.GetDynCar(i)?.Drivers.ElementAtOrDefault<Driver>(j)?.CategoryColor.Fg
                );
            }

            if (l._Config.NumDrivers > 0) {
                for (var j = 0; j < l._Config.NumDrivers; j++) {
                    AddOneDriverFromList(j);
                }
            }

            // // Car and team
            AddProp<int?>(OutCarProp.CAR_NUMBER, () => l.GetDynCar(i)?.CarNumberAsInt);
            AddProp<string?>(OutCarProp.CAR_NUMBER_TEXT, () => l.GetDynCar(i)?.CarNumberAsString);
            AddProp<string?>(OutCarProp.CAR_MODEL, () => l.GetDynCar(i)?.CarModel);
            AddProp<string?>(OutCarProp.CAR_MANUFACTURER, () => l.GetDynCar(i)?.CarManufacturer);
            AddProp<string?>(OutCarProp.CAR_CLASS, () => l.GetDynCar(i)?.CarClass.AsString());
            AddProp<string?>(OutCarProp.CAR_CLASS_SHORT_NAME, () => l.GetDynCar(i)?.CarClassShortName);
            AddProp<string?>(OutCarProp.TEAM_NAME, () => l.GetDynCar(i)?.TeamName);
            AddProp<string?>(OutCarProp.TEAM_CUP_CATEGORY, () => l.GetDynCar(i)?.TeamCupCategory.ToString());

            AddStintProp<double?>(
                OutStintProp.CURRENT_STINT_TIME,
                () => l.GetDynCar(i)?.CurrentStintTime?.TotalSeconds
            );
            AddStintProp<double?>(OutStintProp.LAST_STINT_TIME, () => l.GetDynCar(i)?.LastStintTime?.TotalSeconds);
            AddStintProp<int?>(OutStintProp.CURRENT_STINT_LAPS, () => l.GetDynCar(i)?.CurrentStintLaps);
            AddStintProp<int?>(OutStintProp.LAST_STINT_LAPS, () => l.GetDynCar(i)?.LastStintLaps);

            AddProp<string?>(OutCarProp.CAR_CLASS_COLOR, () => l.GetDynCar(i)?.CarClassColor.Bg);
            AddProp<string?>(OutCarProp.CAR_CLASS_TEXT_COLOR, () => l.GetDynCar(i)?.CarClassColor.Fg);
            AddProp<string?>(OutCarProp.TEAM_CUP_CATEGORY_COLOR, () => l.GetDynCar(i)?.TeamCupCategoryColor.Bg);
            AddProp<string?>(OutCarProp.TEAM_CUP_CATEGORY_TEXT_COLOR, () => l.GetDynCar(i)?.TeamCupCategoryColor.Fg);

            // // Gaps
            AddGapProp<double?>(OutGapProp.GAP_TO_LEADER, () => l.GetDynCar(i)?.GapToLeader?.TotalSeconds);
            AddGapProp<double?>(OutGapProp.GAP_TO_CLASS_LEADER, () => l.GetDynCar(i)?.GapToClassLeader?.TotalSeconds);
            AddGapProp<double?>(OutGapProp.GAP_TO_CUP_LEADER, () => l.GetDynCar(i)?.GapToCupLeader?.TotalSeconds);
            AddGapProp<double?>(
                OutGapProp.GAP_TO_FOCUSED_ON_TRACK,
                () => l.GetDynCar(i)?.GapToFocusedOnTrack?.TotalSeconds
            );
            AddGapProp<double?>(OutGapProp.GAP_TO_FOCUSED_TOTAL, () => l.GetDynCar(i)?.GapToFocusedTotal?.TotalSeconds);
            AddGapProp<double?>(OutGapProp.GAP_TO_AHEAD_OVERALL, () => l.GetDynCar(i)?.GapToAhead?.TotalSeconds);
            AddGapProp<double?>(
                OutGapProp.GAP_TO_AHEAD_IN_CLASS,
                () => l.GetDynCar(i)?.GapToAheadInClass?.TotalSeconds
            );
            AddGapProp<double?>(OutGapProp.GAP_TO_AHEAD_IN_CUP, () => l.GetDynCar(i)?.GapToAheadInCup?.TotalSeconds);
            AddGapProp<double?>(
                OutGapProp.GAP_TO_AHEAD_ON_TRACK,
                () => l.GetDynCar(i)?.GapToAheadOnTrack?.TotalSeconds
            );

            AddGapProp<double?>(OutGapProp.DYNAMIC_GAP_TO_FOCUSED, () => l.GetDynGapToFocused(i)?.TotalSeconds);
            AddGapProp<double?>(OutGapProp.DYNAMIC_GAP_TO_AHEAD, () => l.GetDynGapToAhead(i)?.TotalSeconds);

            // //// Positions
            AddPosProp<int?>(OutPosProp.CLASS_POSITION, () => l.GetDynCar(i)?.PositionInClass);
            AddPosProp<int?>(OutPosProp.CUP_POSITION, () => l.GetDynCar(i)?.PositionInCup);
            AddPosProp<int?>(OutPosProp.OVERALL_POSITION, () => l.GetDynCar(i)?.PositionOverall);
            AddPosProp<int?>(OutPosProp.CLASS_POSITION_START, () => l.GetDynCar(i)?.PositionInClassStart);
            AddPosProp<int?>(OutPosProp.CUP_POSITION_START, () => l.GetDynCar(i)?.PositionInCupStart);
            AddPosProp<int?>(OutPosProp.OVERALL_POSITION_START, () => l.GetDynCar(i)?.PositionOverallStart);

            AddPosProp<int?>(OutPosProp.DYNAMIC_POSITION, () => l.GetDynPosition(i));
            AddPosProp<int?>(OutPosProp.DYNAMIC_POSITION_START, () => l.GetDynPositionStart(i));

            // // Pit
            AddPitProp<int?>(OutPitProp.IS_IN_PIT_LANE, () => l.GetDynCar(i)?.IsInPitLane.ToInt());
            AddPitProp<int?>(OutPitProp.PIT_STOP_COUNT, () => l.GetDynCar(i)?.PitCount);
            AddPitProp<double?>(OutPitProp.PIT_TIME_TOTAL, () => l.GetDynCar(i)?.TotalPitTime.TotalSeconds);
            AddPitProp<double?>(OutPitProp.PIT_TIME_LAST, () => l.GetDynCar(i)?.PitTimeLast?.TotalSeconds);
            AddPitProp<double?>(OutPitProp.PIT_TIME_CURRENT, () => l.GetDynCar(i)?.PitTimeCurrent?.TotalSeconds);

            // // Lap deltas

            AddLapProp<double?>(
                OutLapProp.BEST_LAP_DELTA_TO_OVERALL_BEST,
                () => l.GetDynCar(i)?.BestLap?.DeltaToOverallBest?.TotalSeconds
            );
            AddLapProp<double?>(
                OutLapProp.BEST_LAP_DELTA_TO_CLASS_BEST,
                () => l.GetDynCar(i)?.BestLap?.DeltaToClassBest?.TotalSeconds
            );
            AddLapProp<double?>(
                OutLapProp.BEST_LAP_DELTA_TO_CUP_BEST,
                () => l.GetDynCar(i)?.BestLap?.DeltaToCupBest?.TotalSeconds
            );
            AddLapProp<double?>(
                OutLapProp.BEST_LAP_DELTA_TO_LEADER_BEST,
                () => l.GetDynCar(i)?.BestLap?.DeltaToLeaderBest?.TotalSeconds
            );
            AddLapProp<double?>(
                OutLapProp.BEST_LAP_DELTA_TO_CLASS_LEADER_BEST,
                () => l.GetDynCar(i)?.BestLap?.DeltaToClassLeaderBest?.TotalSeconds
            );
            AddLapProp<double?>(
                OutLapProp.BEST_LAP_DELTA_TO_CUP_LEADER_BEST,
                () => l.GetDynCar(i)?.BestLap?.DeltaToCupLeaderBest?.TotalSeconds
            );
            AddLapProp<double?>(
                OutLapProp.BEST_LAP_DELTA_TO_FOCUSED_BEST,
                () => l.GetDynCar(i)?.BestLap?.DeltaToFocusedBest?.TotalSeconds
            );
            AddLapProp<double?>(
                OutLapProp.BEST_LAP_DELTA_TO_AHEAD_BEST,
                () => l.GetDynCar(i)?.BestLap?.DeltaToAheadBest?.TotalSeconds
            );
            AddLapProp<double?>(
                OutLapProp.BEST_LAP_DELTA_TO_AHEAD_IN_CLASS_BEST,
                () => l.GetDynCar(i)?.BestLap?.DeltaToAheadInClassBest?.TotalSeconds
            );
            AddLapProp<double?>(
                OutLapProp.BEST_LAP_DELTA_TO_AHEAD_IN_CUP_BEST,
                () => l.GetDynCar(i)?.BestLap?.DeltaToAheadInCupBest?.TotalSeconds
            );

            AddLapProp<double?>(
                OutLapProp.LAST_LAP_DELTA_TO_OVERALL_BEST,
                () => l.GetDynCar(i)?.LastLap?.DeltaToOverallBest?.TotalSeconds
            );
            AddLapProp<double?>(
                OutLapProp.LAST_LAP_DELTA_TO_CLASS_BEST,
                () => l.GetDynCar(i)?.LastLap?.DeltaToClassBest?.TotalSeconds
            );
            AddLapProp<double?>(
                OutLapProp.LAST_LAP_DELTA_TO_CUP_BEST,
                () => l.GetDynCar(i)?.LastLap?.DeltaToCupBest?.TotalSeconds
            );
            AddLapProp<double?>(
                OutLapProp.LAST_LAP_DELTA_TO_LEADER_BEST,
                () => l.GetDynCar(i)?.LastLap?.DeltaToLeaderBest?.TotalSeconds
            );
            AddLapProp<double?>(
                OutLapProp.LAST_LAP_DELTA_TO_CLASS_LEADER_BEST,
                () => l.GetDynCar(i)?.LastLap?.DeltaToClassLeaderBest?.TotalSeconds
            );
            AddLapProp<double?>(
                OutLapProp.LAST_LAP_DELTA_TO_CUP_LEADER_BEST,
                () => l.GetDynCar(i)?.LastLap?.DeltaToCupLeaderBest?.TotalSeconds
            );
            AddLapProp<double?>(
                OutLapProp.LAST_LAP_DELTA_TO_FOCUSED_BEST,
                () => l.GetDynCar(i)?.LastLap?.DeltaToFocusedBest?.TotalSeconds
            );
            AddLapProp<double?>(
                OutLapProp.LAST_LAP_DELTA_TO_AHEAD_BEST,
                () => l.GetDynCar(i)?.LastLap?.DeltaToAheadBest?.TotalSeconds
            );
            AddLapProp<double?>(
                OutLapProp.LAST_LAP_DELTA_TO_AHEAD_IN_CLASS_BEST,
                () => l.GetDynCar(i)?.LastLap?.DeltaToAheadInClassBest?.TotalSeconds
            );
            AddLapProp<double?>(
                OutLapProp.LAST_LAP_DELTA_TO_AHEAD_IN_CUP_BEST,
                () => l.GetDynCar(i)?.LastLap?.DeltaToAheadInCupBest?.TotalSeconds
            );
            AddLapProp<double?>(
                OutLapProp.LAST_LAP_DELTA_TO_OWN_BEST,
                () => l.GetDynCar(i)?.LastLap?.DeltaToOwnBest?.TotalSeconds
            );

            AddLapProp<double?>(
                OutLapProp.LAST_LAP_DELTA_TO_LEADER_LAST,
                () => l.GetDynCar(i)?.LastLap?.DeltaToLeaderLast?.TotalSeconds
            );
            AddLapProp<double?>(
                OutLapProp.LAST_LAP_DELTA_TO_CLASS_LEADER_LAST,
                () => l.GetDynCar(i)?.LastLap?.DeltaToClassLeaderLast?.TotalSeconds
            );
            AddLapProp<double?>(
                OutLapProp.LAST_LAP_DELTA_TO_CUP_LEADER_LAST,
                () => l.GetDynCar(i)?.LastLap?.DeltaToCupLeaderLast?.TotalSeconds
            );
            AddLapProp<double?>(
                OutLapProp.LAST_LAP_DELTA_TO_FOCUSED_LAST,
                () => l.GetDynCar(i)?.LastLap?.DeltaToFocusedLast?.TotalSeconds
            );
            AddLapProp<double?>(
                OutLapProp.LAST_LAP_DELTA_TO_AHEAD_LAST,
                () => l.GetDynCar(i)?.LastLap?.DeltaToAheadLast?.TotalSeconds
            );
            AddLapProp<double?>(
                OutLapProp.LAST_LAP_DELTA_TO_AHEAD_IN_CLASS_LAST,
                () => l.GetDynCar(i)?.LastLap?.DeltaToAheadInClassLast?.TotalSeconds
            );
            AddLapProp<double?>(
                OutLapProp.LAST_LAP_DELTA_TO_AHEAD_IN_CUP_LAST,
                () => l.GetDynCar(i)?.LastLap?.DeltaToAheadInCupLast?.TotalSeconds
            );

            AddLapProp<double?>(
                OutLapProp.DYNAMIC_BEST_LAP_DELTA_TO_FOCUSED_BEST,
                () => l.GetDynBestLapDeltaToFocusedBest(i)?.TotalSeconds
            );
            AddLapProp<double?>(
                OutLapProp.DYNAMIC_LAST_LAP_DELTA_TO_FOCUSED_BEST,
                () => l.GetDynLastLapDeltaToFocusedBest(i)?.TotalSeconds
            );
            AddLapProp<double?>(
                OutLapProp.DYNAMIC_LAST_LAP_DELTA_TO_FOCUSED_LAST,
                () => l.GetDynLastLapDeltaToFocusedLast(i)?.TotalSeconds
            );

            // // Else
            AddProp<int?>(OutCarProp.IS_FINISHED, () => (l.GetDynCar(i)?.IsFinished ?? false).ToInt());
            AddProp<double?>(OutCarProp.MAX_SPEED, () => l.GetDynCar(i)?.MaxSpeed);
            AddProp<int?>(OutCarProp.IS_FOCUSED, () => (l.GetDynCar(i)?.IsFocused ?? false).ToInt());
            AddProp<int?>(
                OutCarProp.IS_OVERALL_BEST_LAP_CAR,
                () => (l.GetDynCar(i)?.IsBestLapCarOverall ?? false).ToInt()
            );
            AddProp<int?>(
                OutCarProp.IS_CLASS_BEST_LAP_CAR,
                () => (l.GetDynCar(i)?.IsBestLapCarInClass ?? false).ToInt()
            );
            AddProp<int?>(OutCarProp.IS_CUP_BEST_LAP_CAR, () => (l.GetDynCar(i)?.IsBestLapCarInCup ?? false).ToInt());
            AddProp<int?>(
                OutCarProp.RELATIVE_ON_TRACK_LAP_DIFF,
                () => (int?)l.GetDynCar(i)?.RelativeOnTrackLapDiff ?? 0
            );

            #if DEBUG
            this.AttachDelegate<double?>(
                $"{startName}.DBG_TotalSplinePosition",
                () => l.GetDynCar(i)?.TotalSplinePosition
            );
            this.AttachDelegate<double?>(
                $"{startName}.DBG_SplinePosition",
                () => l.GetDynCar(i)?.SplinePosition
            );
            this.AttachDelegate<bool?>(
                $"{startName}.DBG_HasCrossedStartLine",
                () => l.GetDynCar(i)?._HasCrossedStartLine
            );
            //this.AttachDelegate($"{startName}.DBG_Position", () => (l.GetDynCar(i))?.NewData?.Position);
            // //this.AttachDelegate($"{startName}.DBG_TrackPosition", () => (l.GetDynCar(i))?.NewData?.TrackPosition);
            this.AttachDelegate<CarData.OffsetLapUpdateType?>(
                $"{startName}.DBG_OffsetLapUpdate",
                () => l.GetDynCar(i)?._OffsetLapUpdate
            );
            this.AttachDelegate<string>(
                $"{startName}.DBG_Laps",
                () => $"{l.GetDynCar(i)?.Laps.Old} : {l.GetDynCar(i)?.Laps.New}"
            );
            //this.AttachDelegate($"{startName}.DBG_ID", () => (l.GetDynCar(i))?.Id);
            #endif
        }

        for (var i = 0; i < l.MaxPositions; i++) {
            AddCar(i);
        }

        this.AttachDelegate<string>(
            $"{l.Name}.CurrentLeaderboard",
            () => l.CurrentLeaderboardCompactName
        );
        this.AttachDelegate<string>(
            $"{l.Name}.CurrentLeaderboard.DisplayName",
            () => l.CurrentLeaderboardDisplayName
        );
        this.AttachDelegate<int?>(
            $"{l.Name}.FocusedPosInCurrentLeaderboard",
            () => l.FocusedIndex
        );

        this.AddAction(l.NextLeaderboardActionNAme, (_, _) => l.NextLeaderboard(this.Values));
        this.AddAction(l.PreviousLeaderboardActionNAme, (_, _) => l.PreviousLeaderboard(this.Values));
    }

    private void SubscribeToSimHubEvents(PluginManager pm) {
        pm.GameStateChanged += this.Values.OnGameStateChanged;
        pm.GameStateChanged += (running, _) => {
            Logging.LogInfo($"GameStateChanged to running={running}");
            if (running) {
                return;
            }

            Logging.TryFlush();
        };
    }

    private static void PreJit() {
        var types = Assembly.GetExecutingAssembly().GetTypes();
        foreach (var type in types)
        foreach (var method in type.GetMethods(
                BindingFlags.DeclaredOnly
                | BindingFlags.NonPublic
                | BindingFlags.Public
                | BindingFlags.Instance
                | BindingFlags.Static
            )) {
            if ((method.Attributes & MethodAttributes.Abstract) == MethodAttributes.Abstract
                || method.ContainsGenericParameters) {
                continue;
            }

            RuntimeHelpers.PrepareMethod(method.MethodHandle);
        }
    }
}