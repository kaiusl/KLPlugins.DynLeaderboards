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
using KLPlugins.DynLeaderboards.Track;

using Newtonsoft.Json;

using SimHub.Plugins;

namespace KLPlugins.DynLeaderboards {

    [PluginDescription("")]
    [PluginAuthor("Kaius Loos")]
    [PluginName("DynLeaderboardsPlugin")]
    public class DynLeaderboardsPlugin : IPlugin, IDataPlugin, IWPFSettingsV2 {
        // The properties that compiler yells at that can be null are set in Init method.
        // For the purposes of this plugin, they are never null
#pragma warning disable CS8618
        public PluginManager PluginManager { get; set; }
        public ImageSource PictureIcon => this.ToIcon(Properties.Resources.sdkmenuicon);
        public string LeftMenuTitle => PluginName;

        internal const string PluginName = "DynLeaderboards";
        internal static PluginSettings Settings;
        internal static Game Game; // Const during the lifetime of this plugin, plugin is rebuilt at game change
        internal static string PluginStartTime = $"{DateTime.Now:dd-MM-yyyy_HH-mm-ss}";

        private static FileStream? _logFile;
        private static StreamWriter? _logWriter;
        private static bool _isLogFlushed = false;
        private string? _logFileName;
        private double _dataUpdateTime = 0;

        internal Values Values { get; private set; }
        internal List<DynLeaderboard> DynLeaderboards { get; set; } = new();
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

            this.Values.Dispose();
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

            PluginSettings.Migrate(); // migrate settings before reading them properly
            Settings = this.ReadCommonSettings("GeneralSettings", () => new PluginSettings());
            this.InitLogging(); // needs to know if logging is enabled, but we want to do it as soon as possible, eg right after reading settings

            LogInfo("Starting plugin.");

            Settings.ReadDynLeaderboardConfigs();

            var gameName = (string)pm.GetPropertyValue<SimHub.Plugins.DataPlugins.DataCore.DataCorePlugin>("CurrentGame");
            Game = new Game(gameName);
            TrackData.OnPluginInit(gameName);

            this.Values = new Values();

            foreach (var config in Settings.DynLeaderboardConfigs) {
                if (config.IsEnabled) {
                    var ldb = new DynLeaderboard(config, this.Values);
                    this.DynLeaderboards.Add(ldb);
                    this.AttachDynLeaderboard(ldb);
                    LogInfo($"Added enabled leaderboard: {ldb.Config.Name}.");
                } else {
                    LogInfo($"Didn't add disabled leaderboard: {config.Name}.");
                }
            }
            this.AttachGeneralDelegates();
            this.SubscribeToSimHubEvents(pm);
        }

        private void AttachGeneralDelegates() {
            this.AttachDelegate("Perf.DataUpdateMS", () => this._dataUpdateTime);

            // Add everything else
            if (Settings.OutGeneralProps.Includes(OutGeneralProp.SessionPhase)) {
                this.AttachDelegate("Session.Phase", () => this.Values.Session.SessionPhase.ToString());
            }

            if (Settings.OutGeneralProps.Includes(OutGeneralProp.MaxStintTime)) {
                this.AttachDelegate("Session.MaxStintTime", () => this.Values.Session.MaxDriverStintTime?.TotalSeconds);
            }

            if (Settings.OutGeneralProps.Includes(OutGeneralProp.MaxDriveTime)) {
                this.AttachDelegate("Session.MaxDriveTime", () => this.Values.Session.MaxDriverTotalDriveTime?.TotalSeconds);
            }

            if (Settings.OutGeneralProps.Includes(OutGeneralProp.CarClassColors)) {
                foreach (var kv in this.Values.CarClassColors) {
                    this.AttachDelegate($"Color.Class.{kv.Key}", () => kv.Value.Bg);
                }
            }

            if (Settings.OutGeneralProps.Includes(OutGeneralProp.CarClassColors)) {
                foreach (var kv in this.Values.CarClassColors) {
                    this.AttachDelegate($"Color.Class.{kv.Key}.Text", () => kv.Value.Fg);
                }
            }

            if (Settings.OutGeneralProps.Includes(OutGeneralProp.TeamCupColors)) {
                foreach (var kv in this.Values.TeamCupCategoryColors) {
                    this.AttachDelegate($"Color.Cup.{kv.Key}", () => kv.Value.Bg);
                }
            }

            if (Settings.OutGeneralProps.Includes(OutGeneralProp.TeamCupTextColors)) {
                foreach (var kv in this.Values.TeamCupCategoryColors) {
                    this.AttachDelegate($"Color.Cup.{kv.Key}.Text", () => kv.Value.Fg);
                }
            }

            // if (Settings.OutGeneralProps.Includes(OutGeneralProp.DriverCategoryColors)) {
            //     void addDriverCategoryColor(DriverCategory cat) => this.AttachDelegate($"Color.DriverCategory.{cat}", () => Settings.DriverCategoryColors[cat]);

            //     foreach (var c in Enum.GetValues(typeof(DriverCategory))) {
            //         var cat = (DriverCategory)c;
            //         if (cat == DriverCategory.Error) {
            //             continue;
            //         }

            //         addDriverCategoryColor(cat);
            //     }
            // }
        }

        private void AttachDynLeaderboard(DynLeaderboard l) {
            void addCar(int i) {
                var startName = $"{l.Config.Name}.{i + 1}";
                void AddProp<T>(OutCarProp prop, Func<T> valueProvider) {
                    if (l.Config.OutCarProps.Includes(prop)) {
                        this.AttachDelegate($"{startName}.{prop.ToPropName()}", valueProvider);
                    }
                }

                void AddDriverProp<T>(OutDriverProp prop, string driverId, Func<T> valueProvider) {
                    if (l.Config.OutDriverProps.Includes(prop)) {
                        this.AttachDelegate($"{startName}.{driverId}.{prop}", valueProvider);
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

                // Laps and sectors
                AddLapProp(OutLapProp.Laps, () => l.GetDynCar(i)?.Laps.New);
                AddLapProp(OutLapProp.LastLapTime, () => l.GetDynCar(i)?.LastLap?.Time?.TotalSeconds);
                if (l.Config.OutLapProps.Includes(OutLapProp.LastLapSectors)) {
                    this.AttachDelegate($"{startName}.Laps.Last.S1", () => l.GetDynCar(i)?.LastLap?.S1Time?.TotalSeconds);
                    this.AttachDelegate($"{startName}.Laps.Last.S2", () => l.GetDynCar(i)?.LastLap?.S2Time?.TotalSeconds);
                    this.AttachDelegate($"{startName}.Laps.Last.S3", () => l.GetDynCar(i)?.LastLap?.S3Time?.TotalSeconds);
                }

                AddLapProp(OutLapProp.BestLapTime, () => l.GetDynCar(i)?.BestLap?.Time?.TotalSeconds);
                if (l.Config.OutLapProps.Includes(OutLapProp.BestLapSectors)) {
                    this.AttachDelegate($"{startName}.Laps.Best.S1", () => l.GetDynCar(i)?.BestLap?.S1Time?.TotalSeconds);
                    this.AttachDelegate($"{startName}.Laps.Best.S2", () => l.GetDynCar(i)?.BestLap?.S2Time?.TotalSeconds);
                    this.AttachDelegate($"{startName}.Laps.Best.S3", () => l.GetDynCar(i)?.BestLap?.S2Time?.TotalSeconds);
                }
                if (l.Config.OutLapProps.Includes(OutLapProp.BestSectors)) {
                    this.AttachDelegate($"{startName}.BestS1", () => l.GetDynCar(i)?.BestSectors.GetSectorSplit(0)?.TotalSeconds);
                    this.AttachDelegate($"{startName}.BestS2", () => l.GetDynCar(i)?.BestSectors.GetSectorSplit(1)?.TotalSeconds);
                    this.AttachDelegate($"{startName}.BestS3", () => l.GetDynCar(i)?.BestSectors.GetSectorSplit(2)?.TotalSeconds);
                }

                AddLapProp(OutLapProp.CurrentLapTime, () => l.GetDynCar(i)?.CurrentLapTime.TotalSeconds);

                AddLapProp(OutLapProp.CurrentLapIsValid, () => l.GetDynCar(i)?.IsCurrentLapValid.ToInt());
                AddLapProp(OutLapProp.LastLapIsValid, () => l.GetDynCar(i)?.LastLap?.IsValid.ToInt());

                AddLapProp(OutLapProp.CurrentLapIsOutLap, () => l.GetDynCar(i)?.IsCurrentLapOutLap.ToInt());
                AddLapProp(OutLapProp.LastLapIsOutLap, () => l.GetDynCar(i)?.LastLap?.IsOutLap.ToInt());
                AddLapProp(OutLapProp.CurrentLapIsInLap, () => l.GetDynCar(i)?.IsCurrentLapInLap.ToInt());
                AddLapProp(OutLapProp.LastLapIsInLap, () => l.GetDynCar(i)?.LastLap?.IsInLap.ToInt());

                void AddOneDriverFromList(int j) {
                    var driverId = $"Driver.{j + 1}";
                    AddDriverProp(OutDriverProp.FirstName, driverId, () => l.GetDynCar(i)?.Drivers.ElementAtOrDefault(j)?.FirstName);
                    AddDriverProp(OutDriverProp.LastName, driverId, () => l.GetDynCar(i)?.Drivers.ElementAtOrDefault(j)?.LastName);
                    AddDriverProp(OutDriverProp.ShortName, driverId, () => l.GetDynCar(i)?.Drivers.ElementAtOrDefault(j)?.ShortName);
                    AddDriverProp(OutDriverProp.FullName, driverId, () => l.GetDynCar(i)?.Drivers.ElementAtOrDefault(j)?.FullName);
                    AddDriverProp(OutDriverProp.InitialPlusLastName, driverId, () => l.GetDynCar(i)?.Drivers.ElementAtOrDefault(j)?.InitialPlusLastName);
                    AddDriverProp(OutDriverProp.Nationality, driverId, () => l.GetDynCar(i)?.Drivers.ElementAtOrDefault(j)?.Nationality);
                    AddDriverProp(OutDriverProp.Category, driverId, () => l.GetDynCar(i)?.Drivers.ElementAtOrDefault(j)?.Category);
                    AddDriverProp(OutDriverProp.TotalLaps, driverId, () => l.GetDynCar(i)?.Drivers.ElementAtOrDefault(j)?.TotalLaps);
                    AddDriverProp(OutDriverProp.BestLapTime, driverId, () => l.GetDynCar(i)?.Drivers.ElementAtOrDefault(j)?.BestLap?.Time?.TotalSeconds);
                    AddDriverProp(OutDriverProp.TotalDrivingTime, driverId, () => {
                        // We cannot pre calculate the total driving time because in some games (ACC) the current driver updates at first sector split.
                        var car = l.GetDynCar(i);
                        return car?.Drivers.ElementAtOrDefault(j).GetTotalDrivingTime(j == 0, car.CurrentStintTime).TotalSeconds;
                    });
                    AddDriverProp(OutDriverProp.CategoryColor, driverId, () => l.GetDynCar(i)?.Drivers.ElementAtOrDefault(j)?.CategoryColor);
                }

                if (l.Config.NumDrivers > 0) {
                    for (int j = 0; j < l.Config.NumDrivers; j++) {
                        AddOneDriverFromList(j);
                    }
                }

                // // Car and team
                AddProp(OutCarProp.CarNumber, () => l.GetDynCar(i)?.CarNumber);
                AddProp(OutCarProp.CarModel, () => l.GetDynCar(i)?.CarModel);
                AddProp(OutCarProp.CarManufacturer, () => l.GetDynCar(i)?.CarManufacturer);
                AddProp(OutCarProp.CarClass, () => l.GetDynCar(i)?.CarClass.AsString());
                AddProp(OutCarProp.TeamName, () => l.GetDynCar(i)?.TeamName);
                AddProp(OutCarProp.TeamCupCategory, () => l.GetDynCar(i)?.TeamCupCategory);

                AddStintProp(OutStintProp.CurrentStintTime, () => l.GetDynCar(i)?.CurrentStintTime?.TotalSeconds);
                AddStintProp(OutStintProp.LastStintTime, () => l.GetDynCar(i)?.LastStintTime?.TotalSeconds);
                AddStintProp(OutStintProp.CurrentStintLaps, () => l.GetDynCar(i)?.CurrentStintLaps);
                AddStintProp(OutStintProp.LastStintLaps, () => l.GetDynCar(i)?.LastStintLaps);

                AddProp(OutCarProp.CarClassColor, () => l.GetDynCar(i)?.CarClassColor.Bg);
                AddProp(OutCarProp.CarClassTextColor, () => l.GetDynCar(i)?.CarClassColor.Fg);
                AddProp(OutCarProp.TeamCupCategoryColor, () => l.GetDynCar(i)?.TeamCupCategoryColor.Bg);
                AddProp(OutCarProp.TeamCupCategoryTextColor, () => l.GetDynCar(i)?.TeamCupCategoryColor.Fg);

                // // Gaps
                AddGapProp(OutGapProp.GapToLeader, () => l.GetDynCar(i)?.GapToLeader?.TotalSeconds);
                AddGapProp(OutGapProp.GapToClassLeader, () => l.GetDynCar(i)?.GapToClassLeader?.TotalSeconds);
                AddGapProp(OutGapProp.GapToCupLeader, () => l.GetDynCar(i)?.GapToCupLeader?.TotalSeconds);
                AddGapProp(OutGapProp.GapToFocusedOnTrack, () => l.GetDynCar(i)?.GapToFocusedOnTrack?.TotalSeconds);
                AddGapProp(OutGapProp.GapToFocusedTotal, () => l.GetDynCar(i)?.GapToFocusedTotal?.TotalSeconds);
                AddGapProp(OutGapProp.GapToAheadOverall, () => l.GetDynCar(i)?.GapToAhead?.TotalSeconds);
                AddGapProp(OutGapProp.GapToAheadInClass, () => l.GetDynCar(i)?.GapToAheadInClass?.TotalSeconds);
                AddGapProp(OutGapProp.GapToAheadInCup, () => l.GetDynCar(i)?.GapToAheadInCup?.TotalSeconds);
                AddGapProp(OutGapProp.GapToAheadOnTrack, () => l.GetDynCar(i)?.GapToAheadOnTrack?.TotalSeconds);

                AddGapProp(OutGapProp.DynamicGapToFocused, () => l.GetDynGapToFocused(i)?.TotalSeconds);
                AddGapProp(OutGapProp.DynamicGapToAhead, () => l.GetDynGapToAhead(i)?.TotalSeconds);

                // //// Positions
                AddPosProp(OutPosProp.ClassPosition, () => l.GetDynCar(i)?.PositionInClass);
                // AddPosProp(OutPosProp.CupPosition, () => l.GetDynCar(i)?.InCupPos);
                AddPosProp(OutPosProp.OverallPosition, () => l.GetDynCar(i)?.PositionOverall);
                AddPosProp(OutPosProp.ClassPositionStart, () => l.GetDynCar(i)?.PositionInClassStart);
                // AddPosProp(OutPosProp.CupPositionStart, () => l.GetDynCar(i)?.StartPosInCup);
                AddPosProp(OutPosProp.OverallPositionStart, () => l.GetDynCar(i)?.PositionOverallStart);

                AddPosProp(OutPosProp.DynamicPosition, () => l.GetDynPosition(i));
                AddPosProp(OutPosProp.DynamicPositionStart, () => l.GetDynPositionStart(i));

                // // Pit
                AddPitProp(OutPitProp.IsInPitLane, () => l.GetDynCar(i)?.IsInPitLane);
                AddPitProp(OutPitProp.PitStopCount, () => l.GetDynCar(i)?.PitCount);
                AddPitProp(OutPitProp.PitTimeTotal, () => l.GetDynCar(i)?.TotalPitTime.TotalSeconds);
                AddPitProp(OutPitProp.PitTimeLast, () => l.GetDynCar(i)?.PitTimeLast?.TotalSeconds);
                AddPitProp(OutPitProp.PitTimeCurrent, () => l.GetDynCar(i)?.PitTimeCurrent?.TotalSeconds);

                // // Lap deltas

                AddLapProp(OutLapProp.BestLapDeltaToOverallBest, () => l.GetDynCar(i)?.BestLap?.DeltaToOverallBest?.TotalSeconds);
                AddLapProp(OutLapProp.BestLapDeltaToClassBest, () => l.GetDynCar(i)?.BestLap?.DeltaToClassBest?.TotalSeconds);
                AddLapProp(OutLapProp.BestLapDeltaToCupBest, () => l.GetDynCar(i)?.BestLap?.DeltaToCupBest?.TotalSeconds);
                AddLapProp(OutLapProp.BestLapDeltaToLeaderBest, () => l.GetDynCar(i)?.BestLap?.DeltaToLeaderBest?.TotalSeconds);
                AddLapProp(OutLapProp.BestLapDeltaToClassLeaderBest, () => l.GetDynCar(i)?.BestLap?.DeltaToClassLeaderBest?.TotalSeconds);
                AddLapProp(OutLapProp.BestLapDeltaToCupLeaderBest, () => l.GetDynCar(i)?.BestLap?.DeltaToCupLeaderBest?.TotalSeconds);
                AddLapProp(OutLapProp.BestLapDeltaToFocusedBest, () => l.GetDynCar(i)?.BestLap?.DeltaToFocusedBest?.TotalSeconds);
                AddLapProp(OutLapProp.BestLapDeltaToAheadBest, () => l.GetDynCar(i)?.BestLap?.DeltaToAheadBest?.TotalSeconds);
                AddLapProp(OutLapProp.BestLapDeltaToAheadInClassBest, () => l.GetDynCar(i)?.BestLap?.DeltaToAheadInClassBest?.TotalSeconds);
                AddLapProp(OutLapProp.BestLapDeltaToAheadInCupBest, () => l.GetDynCar(i)?.BestLap?.DeltaToAheadInCupBest?.TotalSeconds);

                AddLapProp(OutLapProp.LastLapDeltaToOverallBest, () => l.GetDynCar(i)?.LastLap?.DeltaToOverallBest?.TotalSeconds);
                AddLapProp(OutLapProp.LastLapDeltaToClassBest, () => l.GetDynCar(i)?.LastLap?.DeltaToClassBest?.TotalSeconds);
                AddLapProp(OutLapProp.LastLapDeltaToCupBest, () => l.GetDynCar(i)?.LastLap?.DeltaToCupBest?.TotalSeconds);
                AddLapProp(OutLapProp.LastLapDeltaToLeaderBest, () => l.GetDynCar(i)?.LastLap?.DeltaToLeaderBest?.TotalSeconds);
                AddLapProp(OutLapProp.LastLapDeltaToClassLeaderBest, () => l.GetDynCar(i)?.LastLap?.DeltaToClassLeaderBest?.TotalSeconds);
                AddLapProp(OutLapProp.LastLapDeltaToCupLeaderBest, () => l.GetDynCar(i)?.LastLap?.DeltaToCupLeaderBest?.TotalSeconds);
                AddLapProp(OutLapProp.LastLapDeltaToFocusedBest, () => l.GetDynCar(i)?.LastLap?.DeltaToFocusedBest?.TotalSeconds);
                AddLapProp(OutLapProp.LastLapDeltaToAheadBest, () => l.GetDynCar(i)?.LastLap?.DeltaToAheadBest?.TotalSeconds);
                AddLapProp(OutLapProp.LastLapDeltaToAheadInClassBest, () => l.GetDynCar(i)?.LastLap?.DeltaToAheadInClassBest?.TotalSeconds);
                AddLapProp(OutLapProp.LastLapDeltaToAheadInCupBest, () => l.GetDynCar(i)?.LastLap?.DeltaToAheadInCupBest?.TotalSeconds);
                AddLapProp(OutLapProp.LastLapDeltaToOwnBest, () => l.GetDynCar(i)?.LastLap?.DeltaToOwnBest);

                AddLapProp(OutLapProp.LastLapDeltaToLeaderLast, () => l.GetDynCar(i)?.LastLap?.DeltaToLeaderLast?.TotalSeconds);
                AddLapProp(OutLapProp.LastLapDeltaToClassLeaderLast, () => l.GetDynCar(i)?.LastLap?.DeltaToClassLeaderLast?.TotalSeconds);
                AddLapProp(OutLapProp.LastLapDeltaToCupLeaderLast, () => l.GetDynCar(i)?.LastLap?.DeltaToCupLeaderLast?.TotalSeconds);
                AddLapProp(OutLapProp.LastLapDeltaToFocusedLast, () => l.GetDynCar(i)?.LastLap?.DeltaToFocusedLast?.TotalSeconds);
                AddLapProp(OutLapProp.LastLapDeltaToAheadLast, () => l.GetDynCar(i)?.LastLap?.DeltaToAheadLast?.TotalSeconds);
                AddLapProp(OutLapProp.LastLapDeltaToAheadInClassLast, () => l.GetDynCar(i)?.LastLap?.DeltaToAheadInClassLast?.TotalSeconds);
                AddLapProp(OutLapProp.LastLapDeltaToAheadInCupLast, () => l.GetDynCar(i)?.LastLap?.DeltaToAheadInCupLast?.TotalSeconds);

                AddLapProp(OutLapProp.DynamicBestLapDeltaToFocusedBest, () => l.GetDynBestLapDeltaToFocusedBest(i)?.TotalSeconds);
                AddLapProp(OutLapProp.DynamicLastLapDeltaToFocusedBest, () => l.GetDynLastLapDeltaToFocusedBest(i)?.TotalSeconds);
                AddLapProp(OutLapProp.DynamicLastLapDeltaToFocusedLast, () => l.GetDynLastLapDeltaToFocusedLast(i)?.TotalSeconds);

                // // Else
                AddProp(OutCarProp.IsFinished, () => (l.GetDynCar(i)?.IsFinished ?? false).ToInt());
                AddProp(OutCarProp.MaxSpeed, () => l.GetDynCar(i)?.MaxSpeed);
                AddProp(OutCarProp.IsFocused, () => (l.GetDynCar(i)?.IsFocused ?? false).ToInt());
                AddProp(OutCarProp.IsOverallBestLapCar, () => (l.GetDynCar(i)?.IsBestLapCarOverall ?? false).ToInt());
                AddProp(OutCarProp.IsClassBestLapCar, () => (l.GetDynCar(i)?.IsBestLapCarInClass ?? false).ToInt());
                // AddProp(OutCarProp.IsCupBestLapCar, () => (l.GetDynCar(i)?.IsCupBestLapCar ?? false) ? 1 : 0);
                AddProp(OutCarProp.RelativeOnTrackLapDiff, () => (int?)l.GetDynCar(i)?.RelativeOnTrackLapDiff ?? 0);

                this.AttachDelegate($"{startName}.DBG_TotalSplinePosition", () => (l.GetDynCar(i))?.TotalSplinePosition);
                this.AttachDelegate($"{startName}.DBG_SplinePosition", () => (l.GetDynCar(i))?.SplinePosition);
                //this.AttachDelegate($"{startName}.DBG_Position", () => (l.GetDynCar(i))?.NewData?.Position);
                // //this.AttachDelegate($"{startName}.DBG_TrackPosition", () => (l.GetDynCar(i))?.NewData?.TrackPosition);
                this.AttachDelegate($"{startName}.DBG_OffsetLapUpdate", () => (l.GetDynCar(i))?.OffsetLapUpdate);
                this.AttachDelegate($"{startName}.DBG_Laps", () => $"{(l.GetDynCar(i))?.Laps.Old} : {l.GetDynCar(i)?.Laps.New}");
                //this.AttachDelegate($"{startName}.DBG_ID", () => (l.GetDynCar(i))?.Id);
                this.AttachDelegate($"{startName}.DBG_SessionType", () => this.Values.Session.SessionType.ToString());
                this.AttachDelegate($"{startName}.DBG_Session.IsLapLimited", () => this.Values.Session.IsLapLimited);
                this.AttachDelegate($"{startName}.DBG_Session.IsTimeLimited", () => this.Values.Session.IsTimeLimited);
                this.AttachDelegate($"{startName}.DBG_IsFirstFinished", () => this.Values.IsFirstFinished);
            };

            var numPos = new int[] {
                l.Config.NumOverallPos,
                l.Config.NumClassPos,
                l.Config.NumOverallRelativePos*2+1,
                l.Config.NumClassRelativePos*2+1,
                l.Config.NumOnTrackRelativePos*2+1,
                l.Config.PartialRelativeClassNumClassPos + l.Config.PartialRelativeClassNumRelativePos*2+1,
                l.Config.PartialRelativeOverallNumOverallPos + l.Config.PartialRelativeOverallNumRelativePos*2+1
            };

            for (int i = 0; i < numPos.Max(); i++) {
                addCar(i);
            }

            this.AttachDelegate($"{l.Config.Name}.CurrentLeaderboard", () => l.Config.CurrentLeaderboardName);
            this.AttachDelegate($"{l.Config.Name}.FocusedPosInCurrentLeaderboard", () => l.FocusedIndex);

            // Declare an action which can be called
            this.AddAction($"{l.Config.Name}.NextLeaderboard", (_, _) => {
                if (l.Config.CurrentLeaderboardIdx == l.Config.Order.Count - 1) {
                    l.Config.CurrentLeaderboardIdx = 0;
                } else {
                    l.Config.CurrentLeaderboardIdx++;
                }
                l.OnLeaderboardChange(this.Values);
            });

            // Declare an action which can be called
            this.AddAction($"{l.Config.Name}.PreviousLeaderboard", (_, _) => {
                if (l.Config.CurrentLeaderboardIdx == 0) {
                    l.Config.CurrentLeaderboardIdx = l.Config.Order.Count - 1;
                } else {
                    l.Config.CurrentLeaderboardIdx--;
                }
                l.OnLeaderboardChange(this.Values);
            });
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
                var outPath = Path.Combine(PluginSettings.PluginDataDirBase, Game.AcName, "CarInfos.json");
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