using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows.Media;

using GameReaderCommon;

using KLPlugins.DynLeaderboards.Car;
using KLPlugins.DynLeaderboards.Settings;
using KLPlugins.DynLeaderboards.Settings.UI;
using KLPlugins.DynLeaderboards.Track;

using Newtonsoft.Json;

using SimHub.Plugins;

namespace KLPlugins.DynLeaderboards {

    [PluginDescription("")]
    [PluginAuthor("Kaius Loos")]
    [PluginName(PluginName)]
    public class DynLeaderboardsPlugin : IPlugin, IDataPlugin, IWPFSettingsV2 {
        // The properties that compiler yells at that can be null are set in Init method.
        // For the purposes of this plugin, they are never null
#pragma warning disable CS8618
        public PluginManager PluginManager { get; set; }
        public ImageSource PictureIcon => this.ToIcon(Properties.Resources.sdkmenuicon);
        public string LeftMenuTitle => PluginName;

        internal const string PluginName = "Dynamic Leaderboards";
        internal static PluginSettings Settings;
        internal static Game Game; // Const during the lifetime of this plugin, plugin is rebuilt at game change
        internal static string PluginStartTime = $"{DateTime.Now:dd-MM-yyyy_HH-mm-ss}";

        private static FileStream? _logFile;
        private static StreamWriter? _logWriter;
        private static bool _isLogFlushed = false;
        private string? _logFileName;
        private double _dataUpdateTime = 0;

        public Values Values { get; private set; }
        public List<DynLeaderboard> DynLeaderboards { get; set; } = new();
#pragma warning restore CS8618

        /// <summary>
        /// Called one time per game data update, contains all normalized game data,
        /// raw data are intentionally "hidden" under a generic object type (A plugin SHOULD NOT USE IT)
        ///
        /// This method is on the critical path, it must execute as fast as possible and avoid throwing any error
        ///
        /// </summary>
        /// <param name="pluginManager"></param>
        /// <param name="data"></param>
        public void DataUpdate(PluginManager pm, ref GameData data) {
            var swatch = Stopwatch.StartNew();
            if (data.GameRunning && data.OldData != null && data.NewData != null) {
                this.Values.OnDataUpdate(pm, data);
                foreach (var ldb in this.DynLeaderboards) {
                    ldb.OnDataUpdate(this.Values);
                }
            }
            swatch.Stop();
            TimeSpan ts = swatch.Elapsed;

            this._dataUpdateTime = ts.TotalMilliseconds;
        }

        /// <summary>
        /// Called at plugin manager stop, close/dispose anything needed here !
        /// Plugins are rebuilt at game change
        /// </summary>
        /// <param name="pluginManager"></param>
        public void End(PluginManager pluginManager) {
            this.SaveCommonSettings("GeneralSettings", Settings);
            Settings.SaveDynLeaderboardConfigs();
            // Delete unused files
            // Say something was accidentally copied there or file and leaderboard names were different which would render original file useless
            foreach (var fname in Directory.GetFiles(PluginSettings.LeaderboardConfigsDataDir)) {
                var leaderboardName = fname.Replace(".json", "").Split('\\').Last();
                if (!Settings.DynLeaderboardConfigs.Any(x => x.Name == leaderboardName)) {
                    File.Delete(fname);
                }
            }

            this.Values?.Dispose();
            if (_logWriter != null) {
                _logWriter.Dispose();
                _logWriter = null;
            }
            if (_logFile != null) {
                _logFile.Dispose();
                _logFile = null;
            }
        }

        /// <summary>
        /// Returns the settings control, return null if no settings control is required
        /// </summary>
        /// <param name="pluginManager"></param>
        /// <returns></returns>
        public System.Windows.Controls.Control GetWPFSettingsControl(PluginManager pluginManager) {
            return new SettingsControl(this);
        }

        /// <summary>
        /// Called once after plugins startup
        /// Plugins are rebuilt at game change
        /// </summary>
        /// <param name="pm"></param>
        public void Init(PluginManager pm) {
            PreJit(); // Performance is important while in game, prejit methods at startup, to avoid doing that mid races

            // Create new log file at game change
            PluginStartTime = $"{DateTime.Now:dd-MM-yyyy_HH-mm-ss}";

            PluginSettings.Migrate(); // migrate settings before reading them properly
            Settings = this.ReadCommonSettings("GeneralSettings", () => new PluginSettings());
            this.InitLogging(); // needs to know if logging is enabled, but we want to do it as soon as possible, eg right after reading settings

            LogInfo("Starting plugin.");

            Settings.ReadDynLeaderboardConfigs();

            var gameName = (string)pm.GetPropertyValue<SimHub.Plugins.DataPlugins.DataCore.DataCorePlugin>("CurrentGame");
            Game = new Game(gameName);
            TrackData.OnPluginInit(gameName);

            this.Values = new Values();
            if (Settings.DynLeaderboardConfigs.Count == 0) {
                this.AddNewLeaderboard(new DynLeaderboardConfig("Dynamic"));
            }

            foreach (var config in Settings.DynLeaderboardConfigs) {
                if (config.IsEnabled) {
                    var ldb = new DynLeaderboard(config, this.Values);
                    this.DynLeaderboards.Add(ldb);
                    this.AttachDynLeaderboard(ldb);
                    LogInfo($"Added enabled leaderboard: {ldb.Name}.");
                } else {
                    LogInfo($"Didn't add disabled leaderboard: {config.Name}.");
                }
            }
            this.AttachGeneralDelegates();
            this.SubscribeToSimHubEvents(pm);
        }

        private void AttachGeneralDelegates() {
            this.AttachDelegate<DynLeaderboardsPlugin, double>("Perf.DataUpdateMS", () => this._dataUpdateTime);

            var outGenProps = Settings.OutGeneralProps;
            // Add everything else
            if (outGenProps.Includes(OutGeneralProp.SessionPhase)) {
                this.AttachDelegate<DynLeaderboardsPlugin, string>(OutGeneralProp.SessionPhase.ToPropName(), () => this.Values.Session.SessionPhase.ToString());
            }

            if (outGenProps.Includes(OutGeneralProp.MaxStintTime)) {
                this.AttachDelegate<DynLeaderboardsPlugin, double>(OutGeneralProp.MaxStintTime.ToPropName(), () => this.Values.Session.MaxDriverStintTime?.TotalSeconds ?? -1);
            }

            if (outGenProps.Includes(OutGeneralProp.MaxDriveTime)) {
                this.AttachDelegate<DynLeaderboardsPlugin, double>(OutGeneralProp.MaxDriveTime.ToPropName(), () => this.Values.Session.MaxDriverTotalDriveTime?.TotalSeconds ?? -1);
            }

            if (outGenProps.Includes(OutGeneralProp.CarClassColors)) {
                foreach (var kv in this.Values.ClassInfos) {
                    var value = kv.Value;
                    this.AttachDelegate<DynLeaderboardsPlugin, string>(
                        OutGeneralProp.CarClassColors.ToPropName().Replace("<class>", kv.Key.AsString()),
                        () => value.Background() ?? OverridableTextBoxColor.DEF_BG
                    );
                }
            }
            if (outGenProps.Includes(OutGeneralProp.CarClassColors)) {
                foreach (var kv in this.Values.ClassInfos) {
                    var value = kv.Value;
                    this.AttachDelegate<DynLeaderboardsPlugin, string>(
                        OutGeneralProp.CarClassTextColors.ToPropName().Replace("<class>", kv.Key.AsString()),
                        () => value.Foreground() ?? OverridableTextBoxColor.DEF_FG
                    );
                }
            }

            if (outGenProps.Includes(OutGeneralProp.TeamCupColors)) {
                foreach (var kv in this.Values.TeamCupCategoryColors) {
                    var value = kv.Value;
                    this.AttachDelegate<DynLeaderboardsPlugin, string>(
                        OutGeneralProp.TeamCupColors.ToPropName().Replace("<cup>", kv.Key.AsString()),
                        () => value.Background() ?? OverridableTextBoxColor.DEF_BG
                    );
                }
            }
            if (outGenProps.Includes(OutGeneralProp.TeamCupTextColors)) {
                foreach (var kv in this.Values.TeamCupCategoryColors) {
                    var value = kv.Value;
                    this.AttachDelegate<DynLeaderboardsPlugin, string>(
                        OutGeneralProp.TeamCupTextColors.ToPropName().Replace("<cup>", kv.Key.AsString()),
                        () => value.Foreground() ?? OverridableTextBoxColor.DEF_FG
                    );
                }
            }

            if (outGenProps.Includes(OutGeneralProp.DriverCategoryColors)) {
                foreach (var kv in this.Values.DriverCategoryColors) {
                    var value = kv.Value;
                    this.AttachDelegate<DynLeaderboardsPlugin, string>(
                        OutGeneralProp.DriverCategoryColors.ToPropName().Replace("<category>", kv.Key.AsString()),
                        () => value.Background() ?? OverridableTextBoxColor.DEF_BG
                    );
                }
            }
            if (outGenProps.Includes(OutGeneralProp.DriverCategoryTextColors)) {
                foreach (var kv in this.Values.DriverCategoryColors) {
                    var value = kv.Value;
                    this.AttachDelegate<DynLeaderboardsPlugin, string>(
                        OutGeneralProp.DriverCategoryTextColors.ToPropName().Replace("<category>", kv.Key.AsString()),
                        () => value.Foreground() ?? OverridableTextBoxColor.DEF_FG
                    );
                }
            }

            if (outGenProps.Includes(OutGeneralProp.NumClassesInSession)) {
                this.AttachDelegate<DynLeaderboardsPlugin, int>(OutGeneralProp.NumClassesInSession.ToPropName(), () => this.Values.NumClassesInSession);
            }


            if (outGenProps.Includes(OutGeneralProp.NumCupsInSession)) {
                this.AttachDelegate<DynLeaderboardsPlugin, int>(OutGeneralProp.NumCupsInSession.ToPropName(), () => this.Values.NumCupsInSession);
            }
        }

        private void AttachDynLeaderboard(DynLeaderboard l) {
            void addCar(int i) {
                var startName = $"{l.Name}.{i + 1}";
                void AddProp<T>(OutCarProp prop, Func<T> valueProvider) {
                    if (l.Config.OutCarProps.Includes(prop)) {
                        this.AttachDelegate($"{startName}.{prop.ToPropName()}", valueProvider);
                    }
                }

                void AddDriverProp<T>(OutDriverProp prop, string driverId, Func<T> valueProvider) {
                    if (l.Config.OutDriverProps.Includes(prop)) {
                        this.AttachDelegate($"{startName}.{driverId}.{prop.ToPropName()}", valueProvider);
                    }
                }

                void AddLapProp<T>(OutLapProp prop, Func<T> valueProvider) {
                    if (l.Config.OutLapProps.Includes(prop)) {
                        this.AttachDelegate($"{startName}.{prop.ToPropName()}", valueProvider);
                    }
                }

                void AddStintProp<T>(OutStintProp prop, Func<T> valueProvider) {
                    if (l.Config.OutStintProps.Includes(prop)) {
                        this.AttachDelegate($"{startName}.{prop.ToPropName()}", valueProvider);
                    }
                }

                void AddGapProp<T>(OutGapProp prop, Func<T> valueProvider) {
                    if (l.Config.OutGapProps.Includes(prop)) {
                        this.AttachDelegate($"{startName}.{prop.ToPropName()}", valueProvider);
                    }
                }

                void AddPosProp<T>(OutPosProp prop, Func<T> valueProvider) {
                    if (l.Config.OutPosProps.Includes(prop)) {
                        this.AttachDelegate($"{startName}.{prop.ToPropName()}", valueProvider);
                    }
                }

                void AddPitProp<T>(OutPitProp prop, Func<T> valueProvider) {
                    if (l.Config.OutPitProps.Includes(prop)) {
                        this.AttachDelegate($"{startName}.{prop.ToPropName()}", valueProvider);
                    }
                }

                void AddSectors(OutLapProp prop, string name, Func<Sectors?> sectorsProvider) {
                    if (l.Config.OutLapProps.Includes(prop)) {
                        this.AttachDelegate<DynLeaderboardsPlugin, double?>($"{startName}.{name}1", () => sectorsProvider()?.S1Time?.TotalSeconds);
                        this.AttachDelegate<DynLeaderboardsPlugin, double?>($"{startName}.{name}2", () => sectorsProvider()?.S2Time?.TotalSeconds);
                        this.AttachDelegate<DynLeaderboardsPlugin, double?>($"{startName}.{name}3", () => sectorsProvider()?.S3Time?.TotalSeconds);
                    }
                }

                // Laps and sectors
                AddLapProp<int?>(OutLapProp.Laps, () => l.GetDynCar(i)?.Laps.New);

                AddLapProp<double?>(OutLapProp.LastLapTime, () => l.GetDynCar(i)?.LastLap?.Time?.TotalSeconds);
                AddSectors(OutLapProp.LastLapSectors, "Laps.Last.S", () => l.GetDynCar(i)?.LastLap);

                AddLapProp<double?>(OutLapProp.BestLapTime, () => l.GetDynCar(i)?.BestLap?.Time?.TotalSeconds);
                AddSectors(OutLapProp.BestLapSectors, "Laps.Best.S", () => l.GetDynCar(i)?.BestLap);

                AddSectors(OutLapProp.BestSectors, "BestS", () => l.GetDynCar(i)?.BestSectors);

                AddLapProp<double?>(OutLapProp.CurrentLapTime, () => l.GetDynCar(i)?.CurrentLapTime.TotalSeconds);

                AddLapProp<int?>(OutLapProp.CurrentLapIsValid, () => l.GetDynCar(i)?.IsCurrentLapValid.ToInt());
                AddLapProp<int?>(OutLapProp.LastLapIsValid, () => l.GetDynCar(i)?.LastLap?.IsValid.ToInt());

                AddLapProp<int?>(OutLapProp.CurrentLapIsOutLap, () => l.GetDynCar(i)?.IsCurrentLapOutLap.ToInt());
                AddLapProp<int?>(OutLapProp.LastLapIsOutLap, () => l.GetDynCar(i)?.LastLap?.IsOutLap.ToInt());
                AddLapProp<int?>(OutLapProp.CurrentLapIsInLap, () => l.GetDynCar(i)?.IsCurrentLapInLap.ToInt());
                AddLapProp<int?>(OutLapProp.LastLapIsInLap, () => l.GetDynCar(i)?.LastLap?.IsInLap.ToInt());

                void AddOneDriverFromList(int j) {
                    var driverId = $"Driver.{j + 1}";
                    AddDriverProp<string?>(OutDriverProp.FirstName, driverId, () => l.GetDynCar(i)?.Drivers.ElementAtOrDefault(j)?.FirstName);
                    AddDriverProp<string?>(OutDriverProp.LastName, driverId, () => l.GetDynCar(i)?.Drivers.ElementAtOrDefault(j)?.LastName);
                    AddDriverProp<string?>(OutDriverProp.ShortName, driverId, () => l.GetDynCar(i)?.Drivers.ElementAtOrDefault(j)?.ShortName);
                    AddDriverProp<string?>(OutDriverProp.FullName, driverId, () => l.GetDynCar(i)?.Drivers.ElementAtOrDefault(j)?.FullName);
                    AddDriverProp<string?>(OutDriverProp.InitialPlusLastName, driverId, () => l.GetDynCar(i)?.Drivers.ElementAtOrDefault(j)?.InitialPlusLastName);
                    AddDriverProp<string?>(OutDriverProp.Nationality, driverId, () => l.GetDynCar(i)?.Drivers.ElementAtOrDefault(j)?.Nationality);
                    AddDriverProp<string?>(OutDriverProp.Category, driverId, () => l.GetDynCar(i)?.Drivers.ElementAtOrDefault(j)?.Category.ToString());
                    AddDriverProp<int?>(OutDriverProp.TotalLaps, driverId, () => l.GetDynCar(i)?.Drivers.ElementAtOrDefault(j)?.TotalLaps);
                    AddDriverProp<double?>(OutDriverProp.BestLapTime, driverId, () => l.GetDynCar(i)?.Drivers.ElementAtOrDefault(j)?.BestLap?.Time?.TotalSeconds);
                    AddDriverProp<double?>(OutDriverProp.TotalDrivingTime, driverId, () => {
                        // We cannot pre calculate the total driving time because in some games (ACC) the current driver updates at first sector split.
                        var car = l.GetDynCar(i);
                        return car?.Drivers.ElementAtOrDefault(j)?.GetTotalDrivingTime(j == 0, car.CurrentStintTime).TotalSeconds;
                    });
                    AddDriverProp<string?>(OutDriverProp.CategoryColorDeprecated, driverId, () => l.GetDynCar(i)?.Drivers.ElementAtOrDefault(j)?.CategoryColor.Bg);
                    AddDriverProp<string?>(OutDriverProp.CategoryColor, driverId, () => l.GetDynCar(i)?.Drivers.ElementAtOrDefault(j)?.CategoryColor.Bg);
                    AddDriverProp<string?>(OutDriverProp.CategoryColorText, driverId, () => l.GetDynCar(i)?.Drivers.ElementAtOrDefault(j)?.CategoryColor.Fg);
                }

                if (l.Config.NumDrivers > 0) {
                    for (int j = 0; j < l.Config.NumDrivers; j++) {
                        AddOneDriverFromList(j);
                    }
                }

                // // Car and team
                AddProp<int?>(OutCarProp.CarNumber, () => l.GetDynCar(i)?.CarNumberAsInt);
                AddProp<string?>(OutCarProp.CarNumberText, () => l.GetDynCar(i)?.CarNumberAsString);
                AddProp<string?>(OutCarProp.CarModel, () => l.GetDynCar(i)?.CarModel);
                AddProp<string?>(OutCarProp.CarManufacturer, () => l.GetDynCar(i)?.CarManufacturer);
                AddProp<string?>(OutCarProp.CarClass, () => l.GetDynCar(i)?.CarClass.AsString());
                AddProp<string?>(OutCarProp.CarClassShortName, () => l.GetDynCar(i)?.CarClassShortName);
                AddProp<string?>(OutCarProp.TeamName, () => l.GetDynCar(i)?.TeamName);
                AddProp<string?>(OutCarProp.TeamCupCategory, () => l.GetDynCar(i)?.TeamCupCategory.ToString());

                AddStintProp<double?>(OutStintProp.CurrentStintTime, () => l.GetDynCar(i)?.CurrentStintTime?.TotalSeconds);
                AddStintProp<double?>(OutStintProp.LastStintTime, () => l.GetDynCar(i)?.LastStintTime?.TotalSeconds);
                AddStintProp<int?>(OutStintProp.CurrentStintLaps, () => l.GetDynCar(i)?.CurrentStintLaps);
                AddStintProp<int?>(OutStintProp.LastStintLaps, () => l.GetDynCar(i)?.LastStintLaps);

                AddProp<string?>(OutCarProp.CarClassColor, () => l.GetDynCar(i)?.CarClassColor.Bg);
                AddProp<string?>(OutCarProp.CarClassTextColor, () => l.GetDynCar(i)?.CarClassColor.Fg);
                AddProp<string?>(OutCarProp.TeamCupCategoryColor, () => l.GetDynCar(i)?.TeamCupCategoryColor.Bg);
                AddProp<string?>(OutCarProp.TeamCupCategoryTextColor, () => l.GetDynCar(i)?.TeamCupCategoryColor.Fg);

                // // Gaps
                AddGapProp<double?>(OutGapProp.GapToLeader, () => l.GetDynCar(i)?.GapToLeader?.TotalSeconds);
                AddGapProp<double?>(OutGapProp.GapToClassLeader, () => l.GetDynCar(i)?.GapToClassLeader?.TotalSeconds);
                AddGapProp<double?>(OutGapProp.GapToCupLeader, () => l.GetDynCar(i)?.GapToCupLeader?.TotalSeconds);
                AddGapProp<double?>(OutGapProp.GapToFocusedOnTrack, () => l.GetDynCar(i)?.GapToFocusedOnTrack?.TotalSeconds);
                AddGapProp<double?>(OutGapProp.GapToFocusedTotal, () => l.GetDynCar(i)?.GapToFocusedTotal?.TotalSeconds);
                AddGapProp<double?>(OutGapProp.GapToAheadOverall, () => l.GetDynCar(i)?.GapToAhead?.TotalSeconds);
                AddGapProp<double?>(OutGapProp.GapToAheadInClass, () => l.GetDynCar(i)?.GapToAheadInClass?.TotalSeconds);
                AddGapProp<double?>(OutGapProp.GapToAheadInCup, () => l.GetDynCar(i)?.GapToAheadInCup?.TotalSeconds);
                AddGapProp<double?>(OutGapProp.GapToAheadOnTrack, () => l.GetDynCar(i)?.GapToAheadOnTrack?.TotalSeconds);

                AddGapProp<double?>(OutGapProp.DynamicGapToFocused, () => l.GetDynGapToFocused(i)?.TotalSeconds);
                AddGapProp<double?>(OutGapProp.DynamicGapToAhead, () => l.GetDynGapToAhead(i)?.TotalSeconds);

                // //// Positions
                AddPosProp<int?>(OutPosProp.ClassPosition, () => l.GetDynCar(i)?.PositionInClass);
                AddPosProp<int?>(OutPosProp.CupPosition, () => l.GetDynCar(i)?.PositionInCup);
                AddPosProp<int?>(OutPosProp.OverallPosition, () => l.GetDynCar(i)?.PositionOverall);
                AddPosProp<int?>(OutPosProp.ClassPositionStart, () => l.GetDynCar(i)?.PositionInClassStart);
                AddPosProp<int?>(OutPosProp.CupPositionStart, () => l.GetDynCar(i)?.PositionInCupStart);
                AddPosProp<int?>(OutPosProp.OverallPositionStart, () => l.GetDynCar(i)?.PositionOverallStart);

                AddPosProp<int?>(OutPosProp.DynamicPosition, () => l.GetDynPosition(i));
                AddPosProp<int?>(OutPosProp.DynamicPositionStart, () => l.GetDynPositionStart(i));

                // // Pit
                AddPitProp<int?>(OutPitProp.IsInPitLane, () => l.GetDynCar(i)?.IsInPitLane.ToInt());
                AddPitProp<int?>(OutPitProp.PitStopCount, () => l.GetDynCar(i)?.PitCount);
                AddPitProp<double?>(OutPitProp.PitTimeTotal, () => l.GetDynCar(i)?.TotalPitTime.TotalSeconds);
                AddPitProp<double?>(OutPitProp.PitTimeLast, () => l.GetDynCar(i)?.PitTimeLast?.TotalSeconds);
                AddPitProp<double?>(OutPitProp.PitTimeCurrent, () => l.GetDynCar(i)?.PitTimeCurrent?.TotalSeconds);

                // // Lap deltas

                AddLapProp<double?>(OutLapProp.BestLapDeltaToOverallBest, () => l.GetDynCar(i)?.BestLap?.DeltaToOverallBest?.TotalSeconds);
                AddLapProp<double?>(OutLapProp.BestLapDeltaToClassBest, () => l.GetDynCar(i)?.BestLap?.DeltaToClassBest?.TotalSeconds);
                AddLapProp<double?>(OutLapProp.BestLapDeltaToCupBest, () => l.GetDynCar(i)?.BestLap?.DeltaToCupBest?.TotalSeconds);
                AddLapProp<double?>(OutLapProp.BestLapDeltaToLeaderBest, () => l.GetDynCar(i)?.BestLap?.DeltaToLeaderBest?.TotalSeconds);
                AddLapProp<double?>(OutLapProp.BestLapDeltaToClassLeaderBest, () => l.GetDynCar(i)?.BestLap?.DeltaToClassLeaderBest?.TotalSeconds);
                AddLapProp<double?>(OutLapProp.BestLapDeltaToCupLeaderBest, () => l.GetDynCar(i)?.BestLap?.DeltaToCupLeaderBest?.TotalSeconds);
                AddLapProp<double?>(OutLapProp.BestLapDeltaToFocusedBest, () => l.GetDynCar(i)?.BestLap?.DeltaToFocusedBest?.TotalSeconds);
                AddLapProp<double?>(OutLapProp.BestLapDeltaToAheadBest, () => l.GetDynCar(i)?.BestLap?.DeltaToAheadBest?.TotalSeconds);
                AddLapProp<double?>(OutLapProp.BestLapDeltaToAheadInClassBest, () => l.GetDynCar(i)?.BestLap?.DeltaToAheadInClassBest?.TotalSeconds);
                AddLapProp<double?>(OutLapProp.BestLapDeltaToAheadInCupBest, () => l.GetDynCar(i)?.BestLap?.DeltaToAheadInCupBest?.TotalSeconds);

                AddLapProp<double?>(OutLapProp.LastLapDeltaToOverallBest, () => l.GetDynCar(i)?.LastLap?.DeltaToOverallBest?.TotalSeconds);
                AddLapProp<double?>(OutLapProp.LastLapDeltaToClassBest, () => l.GetDynCar(i)?.LastLap?.DeltaToClassBest?.TotalSeconds);
                AddLapProp<double?>(OutLapProp.LastLapDeltaToCupBest, () => l.GetDynCar(i)?.LastLap?.DeltaToCupBest?.TotalSeconds);
                AddLapProp<double?>(OutLapProp.LastLapDeltaToLeaderBest, () => l.GetDynCar(i)?.LastLap?.DeltaToLeaderBest?.TotalSeconds);
                AddLapProp<double?>(OutLapProp.LastLapDeltaToClassLeaderBest, () => l.GetDynCar(i)?.LastLap?.DeltaToClassLeaderBest?.TotalSeconds);
                AddLapProp<double?>(OutLapProp.LastLapDeltaToCupLeaderBest, () => l.GetDynCar(i)?.LastLap?.DeltaToCupLeaderBest?.TotalSeconds);
                AddLapProp<double?>(OutLapProp.LastLapDeltaToFocusedBest, () => l.GetDynCar(i)?.LastLap?.DeltaToFocusedBest?.TotalSeconds);
                AddLapProp<double?>(OutLapProp.LastLapDeltaToAheadBest, () => l.GetDynCar(i)?.LastLap?.DeltaToAheadBest?.TotalSeconds);
                AddLapProp<double?>(OutLapProp.LastLapDeltaToAheadInClassBest, () => l.GetDynCar(i)?.LastLap?.DeltaToAheadInClassBest?.TotalSeconds);
                AddLapProp<double?>(OutLapProp.LastLapDeltaToAheadInCupBest, () => l.GetDynCar(i)?.LastLap?.DeltaToAheadInCupBest?.TotalSeconds);
                AddLapProp<double?>(OutLapProp.LastLapDeltaToOwnBest, () => l.GetDynCar(i)?.LastLap?.DeltaToOwnBest?.TotalSeconds);

                AddLapProp<double?>(OutLapProp.LastLapDeltaToLeaderLast, () => l.GetDynCar(i)?.LastLap?.DeltaToLeaderLast?.TotalSeconds);
                AddLapProp<double?>(OutLapProp.LastLapDeltaToClassLeaderLast, () => l.GetDynCar(i)?.LastLap?.DeltaToClassLeaderLast?.TotalSeconds);
                AddLapProp<double?>(OutLapProp.LastLapDeltaToCupLeaderLast, () => l.GetDynCar(i)?.LastLap?.DeltaToCupLeaderLast?.TotalSeconds);
                AddLapProp<double?>(OutLapProp.LastLapDeltaToFocusedLast, () => l.GetDynCar(i)?.LastLap?.DeltaToFocusedLast?.TotalSeconds);
                AddLapProp<double?>(OutLapProp.LastLapDeltaToAheadLast, () => l.GetDynCar(i)?.LastLap?.DeltaToAheadLast?.TotalSeconds);
                AddLapProp<double?>(OutLapProp.LastLapDeltaToAheadInClassLast, () => l.GetDynCar(i)?.LastLap?.DeltaToAheadInClassLast?.TotalSeconds);
                AddLapProp<double?>(OutLapProp.LastLapDeltaToAheadInCupLast, () => l.GetDynCar(i)?.LastLap?.DeltaToAheadInCupLast?.TotalSeconds);

                AddLapProp<double?>(OutLapProp.DynamicBestLapDeltaToFocusedBest, () => l.GetDynBestLapDeltaToFocusedBest(i)?.TotalSeconds);
                AddLapProp<double?>(OutLapProp.DynamicLastLapDeltaToFocusedBest, () => l.GetDynLastLapDeltaToFocusedBest(i)?.TotalSeconds);
                AddLapProp<double?>(OutLapProp.DynamicLastLapDeltaToFocusedLast, () => l.GetDynLastLapDeltaToFocusedLast(i)?.TotalSeconds);

                // // Else
                AddProp<int?>(OutCarProp.IsFinished, () => (l.GetDynCar(i)?.IsFinished ?? false).ToInt());
                AddProp<double?>(OutCarProp.MaxSpeed, () => l.GetDynCar(i)?.MaxSpeed);
                AddProp<int?>(OutCarProp.IsFocused, () => (l.GetDynCar(i)?.IsFocused ?? false).ToInt());
                AddProp<int?>(OutCarProp.IsOverallBestLapCar, () => (l.GetDynCar(i)?.IsBestLapCarOverall ?? false).ToInt());
                AddProp<int?>(OutCarProp.IsClassBestLapCar, () => (l.GetDynCar(i)?.IsBestLapCarInClass ?? false).ToInt());
                AddProp<int?>(OutCarProp.IsCupBestLapCar, () => (l.GetDynCar(i)?.IsBestLapCarInCup ?? false).ToInt());
                AddProp<int?>(OutCarProp.RelativeOnTrackLapDiff, () => (int?)l.GetDynCar(i)?.RelativeOnTrackLapDiff ?? 0);

                this.AttachDelegate($"{startName}.DBG_TotalSplinePosition", () => (l.GetDynCar(i))?.TotalSplinePosition);
                this.AttachDelegate($"{startName}.DBG_SplinePosition", () => (l.GetDynCar(i))?.SplinePosition);
                this.AttachDelegate($"{startName}.DBG_HasCrossedStartLine", () => (l.GetDynCar(i))?.HasCrossedStartLine);
                //this.AttachDelegate($"{startName}.DBG_Position", () => (l.GetDynCar(i))?.NewData?.Position);
                // //this.AttachDelegate($"{startName}.DBG_TrackPosition", () => (l.GetDynCar(i))?.NewData?.TrackPosition);
                this.AttachDelegate($"{startName}.DBG_OffsetLapUpdate", () => (l.GetDynCar(i))?.OffsetLapUpdate);
                this.AttachDelegate($"{startName}.DBG_Laps", () => $"{(l.GetDynCar(i))?.Laps.Old} : {l.GetDynCar(i)?.Laps.New}");
                //this.AttachDelegate($"{startName}.DBG_ID", () => (l.GetDynCar(i))?.Id);
            };

            for (int i = 0; i < l.MaxPositions; i++) {
                addCar(i);
            }

            // this.AttachDelegate($"{l.Name}.SessionPhase", () => this.Values.Session.SessionPhase);

            this.AttachDelegate<DynLeaderboardsPlugin, string>($"{l.Name}.CurrentLeaderboard", () => l.CurrentLeaderboardName);
            this.AttachDelegate<DynLeaderboardsPlugin, int?>($"{l.Name}.FocusedPosInCurrentLeaderboard", () => l.FocusedIndex);

            this.AddAction(l.NextLeaderboardActionNAme, (_, _) => l.NextLeaderboard(this.Values));
            this.AddAction(l.PreviousLeaderboardActionNAme, (_, _) => l.PreviousLeaderboard(this.Values));
        }

        internal void AddNewLeaderboard(DynLeaderboardConfig s) {
            Settings.DynLeaderboardConfigs.Add(s);
            this.DynLeaderboards.Add(new DynLeaderboard(s, this.Values));
            Settings.SaveDynLeaderboardConfigs();
        }

        internal void RemoveLeaderboardAt(int i) {
            Settings.RemoveLeaderboardAt(i);
            this.DynLeaderboards.RemoveAt(i);
        }

        private void SubscribeToSimHubEvents(PluginManager pm) {
            pm.GameStateChanged += this.Values.OnGameStateChanged;
            pm.GameStateChanged += (bool running, PluginManager _) => {
                LogInfo($"GameStateChanged to {running}");
                if (!running) {
                    if (_logWriter != null && !_isLogFlushed) {
                        _logWriter.Flush();
                        _isLogFlushed = true;
                    }
                }
            };
        }

        internal static void UpdateACCarInfos() {
            if (Settings.AcRootLocation == null) {
                return;
            }

            var carsFolder = Path.Combine(Settings.AcRootLocation, "content", "cars");
            if (!Directory.Exists(carsFolder)) {
                return;
            }

            Dictionary<string, CarInfo> carInfos = [];
            foreach (var carFolderPath in Directory.GetDirectories(carsFolder)) {
                var carId = Path.GetFileName(carFolderPath);
                var uiInfoFilePath = Path.Combine(carFolderPath, "ui", "ui_car.json");
                if (!File.Exists(uiInfoFilePath)) {
                    continue;
                }

                var uiInfo = JsonConvert.DeserializeObject<ACUiCarInfo>(File.ReadAllText(uiInfoFilePath));
                if (uiInfo == null) {
                    continue;
                }

                var cls = uiInfo.Class;
                if (cls == "race" || cls == "street") {
                    // Kunos cars have a proper class name in the tags as #... (for example #GT4 or #Vintage Touring)
                    var altCls = uiInfo.Tags?.Find(t => t.StartsWith("#"));
                    if (altCls != null) {
                        cls = altCls.Substring(1);
                    } else {
                        // Look for some more common patterns from the tags
                        string[] lookups = ["gt3", "gt2", "gt4", "gt1", "gte", "lmp1", "lmp2", "lmp3", "formula1", "formula", "dtm"];
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
                LogInfo($"Read AC car info from '{uiInfoFilePath}': {JsonConvert.SerializeObject(carInfos[carId])}");
            }

            if (carInfos.Count != 0) {
                var outPath = Path.Combine(PluginSettings.PluginDataDir, Game.AcName, "CarInfos.base.json");
                File.WriteAllText(outPath, JsonConvert.SerializeObject(carInfos, Formatting.Indented));
            }
        }

        private class ACUiCarInfo {
            public string Name { get; set; }
            public string Brand { get; set; }
            public string Class { get; set; }
            public List<string> Tags { get; set; }

            [JsonConstructor]
            public ACUiCarInfo(string name, string brand, string @class, List<string> tags) {
                this.Name = name;
                this.Brand = brand;
                this.Class = @class;
                this.Tags = tags;
            }
        }

        #region Logging

        internal void InitLogging() {
            this._logFileName = $"{PluginSettings.PluginDataDir}\\Logs\\Log_{PluginStartTime}.txt";
            if (Settings.Log) {
                Directory.CreateDirectory(Path.GetDirectoryName(this._logFileName));
                _logFile = File.Create(this._logFileName);
                _logWriter = new StreamWriter(_logFile);
            }
        }

        internal static void LogInfo(string msg, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int lineNumber = 0) {
            if (Settings.Log) {
                Log(msg, memberName, sourceFilePath, lineNumber, "INFO", SimHub.Logging.Current.Info);
            }
        }

        internal static void LogWarn(string msg, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int lineNumber = 0) {
            Log(msg, memberName, sourceFilePath, lineNumber, "WARN", SimHub.Logging.Current.Warn);
        }

        internal static void LogError(string msg, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int lineNumber = 0) {
            Log(msg, memberName, sourceFilePath, lineNumber, "ERROR", SimHub.Logging.Current.Error);
        }

        private static void Log(string msg, string memberName, string sourceFilePath, int lineNumber, string lvl, Action<string> simHubLog) {
            var pathParts = sourceFilePath.Split('\\');
            var m = CreateMessage(msg, pathParts[pathParts.Length - 1], memberName, lineNumber);
            simHubLog($"{PluginName} {m}");
            LogToFile($"{DateTime.Now:dd.MM.yyyy HH:mm.ss} {lvl.ToUpper()} {m}\n");
        }

        private static string CreateMessage(string msg, string source, string memberName, int lineNumber) {
            return $"({source}: {memberName},{lineNumber})\n\t{msg}";
        }

        internal static void LogFileSeparator() {
            LogToFile("\n----------------------------------------------------------\n");
        }

        private static void LogToFile(string msq) {
            if (_logWriter != null) {
                _logWriter.WriteLine(msq);
                _isLogFlushed = false;
            }
        }

        #endregion Logging

        private static void PreJit() {
            Stopwatch sw = new();
            sw.Start();

            var types = Assembly.GetExecutingAssembly().GetTypes();
            foreach (var type in types) {
                foreach (var method in type.GetMethods(BindingFlags.DeclaredOnly |
                                    BindingFlags.NonPublic |
                                    BindingFlags.Public | BindingFlags.Instance |
                                    BindingFlags.Static)) {
                    if ((method.Attributes & MethodAttributes.Abstract) == MethodAttributes.Abstract || method.ContainsGenericParameters) {
                        continue;
                    }
                    System.Runtime.CompilerServices.RuntimeHelpers.PrepareMethod(method.MethodHandle);
                }
            }

            _ = sw.Elapsed;
        }
    }

    internal static class Extensions {
        internal static int ToInt(this bool v) {
            return v ? 1 : 0;
        }
    }

}