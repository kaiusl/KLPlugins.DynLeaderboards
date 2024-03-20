using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows.Media;

using GameReaderCommon;

using KLPlugins.DynLeaderboards.Settings;

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
        internal static string GameDataPath; // Same as above
        internal static string PluginStartTime = $"{DateTime.Now:dd-MM-yyyy_HH-mm-ss}";

        private static FileStream? _logFile;
        private static StreamWriter? _logWriter;
        private static bool _isLogFlushed = false;
        private string? _logFileName;
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
            if (data.GameRunning && data.OldData != null && data.NewData != null) {
                this.Values.OnDataUpdate(pm, data);
                foreach (var ldb in this.DynLeaderboards) {
                    ldb.OnDataUpdate(this.Values);
                }
            }
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
            foreach (var fname in Directory.GetFiles(PluginSettings.leaderboardConfigsDataDirName)) {
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
            GameDataPath = $@"{Settings.PluginDataLocation}\{gameName}";

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
            // // Add everything else
            // if (Settings.OutGeneralProps.Includes(OutGeneralProp.SessionPhase)) {
            //     this.AttachDelegate("Session.Phase", () => this._values.RealtimeData?.NewData?.Phase);
            // }

            // if (Settings.OutGeneralProps.Includes(OutGeneralProp.MaxStintTime)) {
            //     this.AttachDelegate("Session.MaxStintTime", () => this._values.MaxDriverStintTime);
            // }

            // if (Settings.OutGeneralProps.Includes(OutGeneralProp.MaxDriveTime)) {
            //     this.AttachDelegate("Session.MaxDriveTime", () => this._values.MaxDriverTotalDriveTime);
            // }

            // if (Settings.OutGeneralProps.Includes(OutGeneralProp.CarClassColors)) {
            //     void addClassColor(CarClass cls) => this.AttachDelegate($"Color.Class.{cls}", () => Settings.CarClassColors[cls]);

            //     foreach (var c in Enum.GetValues(typeof(CarClass))) {
            //         var cls = (CarClass)c;
            //         if (cls == CarClass.Overall || cls == CarClass.Unknown) {
            //             continue;
            //         }

            //         addClassColor(cls);
            //     }
            // }

            // void addCupColor(TeamCupCategory cup) {
            //     if (Settings.OutGeneralProps.Includes(OutGeneralProp.TeamCupColors)) {
            //         this.AttachDelegate($"Color.Cup.{cup}", () => Settings.TeamCupCategoryColors[cup]);
            //     }

            //     if (Settings.OutGeneralProps.Includes(OutGeneralProp.TeamCupTextColors)) {
            //         this.AttachDelegate($"Color.Cup.{cup}.Text", () => Settings.TeamCupCategoryTextColors[cup]);
            //     }
            // }

            // foreach (var c in Enum.GetValues(typeof(TeamCupCategory))) {
            //     addCupColor((TeamCupCategory)c);
            // }

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
                AddLapProp(OutLapProp.LastLapTime, () => l.GetDynCar(i)?.LastLap?.Time);
                if (l.Config.OutLapProps.Includes(OutLapProp.LastLapSectors)) {
                    this.AttachDelegate($"{startName}.Laps.Last.S1", () => l.GetDynCar(i)?.LastLap?.S1Time);
                    this.AttachDelegate($"{startName}.Laps.Last.S2", () => l.GetDynCar(i)?.LastLap?.S2Time);
                    this.AttachDelegate($"{startName}.Laps.Last.S3", () => l.GetDynCar(i)?.LastLap?.S3Time);
                }

                AddLapProp(OutLapProp.BestLapTime, () => l.GetDynCar(i)?.BestLap?.Time);
                if (l.Config.OutLapProps.Includes(OutLapProp.BestLapSectors)) {
                    this.AttachDelegate($"{startName}.Laps.Best.S1", () => l.GetDynCar(i)?.BestLap?.S1Time);
                    this.AttachDelegate($"{startName}.Laps.Best.S2", () => l.GetDynCar(i)?.BestLap?.S2Time);
                    this.AttachDelegate($"{startName}.Laps.Best.S3", () => l.GetDynCar(i)?.BestLap?.S2Time);
                }
                if (l.Config.OutLapProps.Includes(OutLapProp.BestSectors)) {
                    this.AttachDelegate($"{startName}.BestS1", () => l.GetDynCar(i)?.BestSectors.GetSectorSplit(0)?.TotalSeconds);
                    this.AttachDelegate($"{startName}.BestS2", () => l.GetDynCar(i)?.BestSectors.GetSectorSplit(1)?.TotalSeconds);
                    this.AttachDelegate($"{startName}.BestS3", () => l.GetDynCar(i)?.BestSectors.GetSectorSplit(2)?.TotalSeconds);
                }

                AddLapProp(OutLapProp.CurrentLapTime, () => l.GetDynCar(i)?.CurrentLapTime);

                AddLapProp(OutLapProp.CurrentLapIsValid, () => this.MaybeBoolToInt(l.GetDynCar(i)?.IsCurrentLapValid));
                AddLapProp(OutLapProp.LastLapIsValid, () => this.MaybeBoolToInt(l.GetDynCar(i)?.LastLap?.IsValid));

                AddLapProp(OutLapProp.CurrentLapIsOutLap, () => this.MaybeBoolToInt(l.GetDynCar(i)?.IsCurrentLapOutLap));
                AddLapProp(OutLapProp.LastLapIsOutLap, () => this.MaybeBoolToInt(l.GetDynCar(i)?.LastLap?.IsOutLap));
                AddLapProp(OutLapProp.CurrentLapIsInLap, () => this.MaybeBoolToInt(l.GetDynCar(i)?.IsCurrentLapInLap));
                AddLapProp(OutLapProp.LastLapIsInLap, () => this.MaybeBoolToInt(l.GetDynCar(i)?.LastLap?.IsInLap));

                void AddOneDriverFromList(int j) {
                    var driverId = $"Driver.{j + 1}";
                    // AddDriverProp(OutDriverProp.FirstName, driverId, () => l.GetDynCar(i)?.GetDriver(j)?.FirstName);
                    // AddDriverProp(OutDriverProp.LastName, driverId, () => l.GetDynCar(i)?.GetDriver(j)?.LastName);
                    AddDriverProp(OutDriverProp.ShortName, driverId, () => l.GetDynCar(i)?.Drivers.ElementAtOrDefault(j)?.ShortName);
                    AddDriverProp(OutDriverProp.FullName, driverId, () => l.GetDynCar(i)?.Drivers.ElementAtOrDefault(j)?.FullName);
                    AddDriverProp(OutDriverProp.InitialPlusLastName, driverId, () => l.GetDynCar(i)?.Drivers.ElementAtOrDefault(j)?.InitialPlusLastName);
                    // AddDriverProp(OutDriverProp.Nationality, driverId, () => l.GetDynCar(i)?.GetDriver(j)?.Nationality);
                    // AddDriverProp(OutDriverProp.Category, driverId, () => l.GetDynCar(i)?.GetDriver(j)?.Category);
                    // AddDriverProp(OutDriverProp.TotalLaps, driverId, () => l.GetDynCar(i)?.GetDriver(j)?.TotalLaps);
                    // AddDriverProp(OutDriverProp.BestLapTime, driverId, () => l.GetDynCar(i)?.GetDriver(j)?.BestSessionLap?.Laptime);
                    // AddDriverProp(OutDriverProp.TotalDrivingTime, driverId, () => l.GetDynCar(i)?.GetDriverTotalDrivingTime(j));
                    // AddDriverProp(OutDriverProp.CategoryColor, driverId, () => l.GetDynCar(i)?.GetDriver(j)?.CategoryColor);
                }

                if (l.Config.NumDrivers > 0) {
                    for (int j = 0; j < l.Config.NumDrivers; j++) {
                        AddOneDriverFromList(j);
                    }
                }

                // // Car and team
                AddProp(OutCarProp.CarNumber, () => l.GetDynCar(i)?.CarNumber);
                AddProp(OutCarProp.CarModel, () => l.GetDynCar(i)?.CarModel);
                // AddProp(OutCarProp.CarManufacturer, () => l.GetDynCar(i)?.CarModelType.Mark());
                AddProp(OutCarProp.CarClass, () => l.GetDynCar(i)?.CarClass);
                AddProp(OutCarProp.TeamName, () => l.GetDynCar(i)?.TeamName);
                // AddProp(OutCarProp.TeamCupCategory, () => l.GetDynCar(i)?.TeamCupCategory.PrettyName());

                // AddStintProp(OutStintProp.CurrentStintTime, () => l.GetDynCar(i)?.CurrentStintTime);
                // AddStintProp(OutStintProp.LastStintTime, () => l.GetDynCar(i)?.LastStintTime);
                // AddStintProp(OutStintProp.CurrentStintLaps, () => l.GetDynCar(i)?.CurrentStintLaps);
                // AddStintProp(OutStintProp.LastStintLaps, () => l.GetDynCar(i)?.LastStintLaps);

                AddProp(OutCarProp.CarClassColor, () => l.GetDynCar(i)?.CarClassColor);
                AddProp(OutCarProp.CarClassTextColor, () => l.GetDynCar(i)?.CarClassTextColor);
                // AddProp(OutCarProp.TeamCupCategoryColor, () => l.GetDynCar(i)?.TeamCupCategoryColor);
                // AddProp(OutCarProp.TeamCupCategoryTextColor, () => l.GetDynCar(i)?.TeamCupCategoryTextColor);

                // // Gaps
                AddGapProp(OutGapProp.GapToLeader, () => l.GetDynCar(i)?.GapToLeader);
                AddGapProp(OutGapProp.GapToClassLeader, () => l.GetDynCar(i)?.GapToClassLeader);
                // AddGapProp(OutGapProp.GapToCupLeader, () => l.GetDynCar(i)?.GapToCupLeader);
                // AddGapProp(OutGapProp.GapToFocusedOnTrack, () => l.GetDynCar(i)?.GapToFocusedOnTrack);
                AddGapProp(OutGapProp.GapToFocusedTotal, () => l.GetDynCar(i)?.GapToFocusedTotal);
                // AddGapProp(OutGapProp.GapToAheadOverall, () => l.GetDynCar(i)?.GapToAhead);
                // AddGapProp(OutGapProp.GapToAheadInClass, () => l.GetDynCar(i)?.GapToAheadInClass);
                // AddGapProp(OutGapProp.GapToAheadInCup, () => l.GetDynCar(i)?.GapToAheadInCup);
                // AddGapProp(OutGapProp.GapToAheadOnTrack, () => l.GetDynCar(i)?.GapToAheadOnTrack);

                AddGapProp(OutGapProp.DynamicGapToFocused, () => l.GetDynGapToFocused(i));
                AddGapProp(OutGapProp.DynamicGapToAhead, () => l.GetDynGapToAhead(i));

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
                // AddPitProp(OutPitProp.PitTimeTotal, () => l.GetDynCar(i)?.TotalPitTime);
                AddPitProp(OutPitProp.PitTimeLast, () => l.GetDynCar(i)?.PitTimeLast);
                // AddPitProp(OutPitProp.PitTimeCurrent, () => l.GetDynCar(i)?.CurrentTimeInPits);

                // // Lap deltas

                AddLapProp(OutLapProp.BestLapDeltaToOverallBest, () => l.GetDynCar(i)?.BestLap?.DeltaToOverallBest);
                AddLapProp(OutLapProp.BestLapDeltaToClassBest, () => l.GetDynCar(i)?.BestLap?.DeltaToClassBest);
                AddLapProp(OutLapProp.BestLapDeltaToCupBest, () => l.GetDynCar(i)?.BestLap?.DeltaToCupBest);
                AddLapProp(OutLapProp.BestLapDeltaToLeaderBest, () => l.GetDynCar(i)?.BestLap?.DeltaToLeaderBest);
                AddLapProp(OutLapProp.BestLapDeltaToClassLeaderBest, () => l.GetDynCar(i)?.BestLap?.DeltaToClassLeaderBest);
                AddLapProp(OutLapProp.BestLapDeltaToCupLeaderBest, () => l.GetDynCar(i)?.BestLap?.DeltaToCupLeaderBest);
                AddLapProp(OutLapProp.BestLapDeltaToFocusedBest, () => l.GetDynCar(i)?.BestLap?.DeltaToFocusedBest);
                AddLapProp(OutLapProp.BestLapDeltaToAheadBest, () => l.GetDynCar(i)?.BestLap?.DeltaToAheadBest);
                AddLapProp(OutLapProp.BestLapDeltaToAheadInClassBest, () => l.GetDynCar(i)?.BestLap?.DeltaToAheadInClassBest);
                AddLapProp(OutLapProp.BestLapDeltaToAheadInCupBest, () => l.GetDynCar(i)?.BestLap?.DeltaToAheadInCupBest);

                AddLapProp(OutLapProp.LastLapDeltaToOverallBest, () => l.GetDynCar(i)?.LastLap?.DeltaToOverallBest);
                AddLapProp(OutLapProp.LastLapDeltaToClassBest, () => l.GetDynCar(i)?.LastLap?.DeltaToClassBest);
                AddLapProp(OutLapProp.LastLapDeltaToCupBest, () => l.GetDynCar(i)?.LastLap?.DeltaToCupBest);
                AddLapProp(OutLapProp.LastLapDeltaToLeaderBest, () => l.GetDynCar(i)?.LastLap?.DeltaToLeaderBest);
                AddLapProp(OutLapProp.LastLapDeltaToClassLeaderBest, () => l.GetDynCar(i)?.LastLap?.DeltaToClassLeaderBest);
                AddLapProp(OutLapProp.LastLapDeltaToCupLeaderBest, () => l.GetDynCar(i)?.LastLap?.DeltaToCupLeaderBest);
                AddLapProp(OutLapProp.LastLapDeltaToFocusedBest, () => l.GetDynCar(i)?.LastLap?.DeltaToFocusedBest);
                AddLapProp(OutLapProp.LastLapDeltaToAheadBest, () => l.GetDynCar(i)?.LastLap?.DeltaToAheadBest);
                AddLapProp(OutLapProp.LastLapDeltaToAheadInClassBest, () => l.GetDynCar(i)?.LastLap?.DeltaToAheadInClassBest);
                AddLapProp(OutLapProp.LastLapDeltaToAheadInCupBest, () => l.GetDynCar(i)?.LastLap?.DeltaToAheadInCupBest);
                AddLapProp(OutLapProp.LastLapDeltaToOwnBest, () => l.GetDynCar(i)?.LastLap?.DeltaToOwnBest);

                AddLapProp(OutLapProp.LastLapDeltaToLeaderLast, () => l.GetDynCar(i)?.LastLap?.DeltaToLeaderLast);
                AddLapProp(OutLapProp.LastLapDeltaToClassLeaderLast, () => l.GetDynCar(i)?.LastLap?.DeltaToClassLeaderLast);
                AddLapProp(OutLapProp.LastLapDeltaToCupLeaderLast, () => l.GetDynCar(i)?.LastLap?.DeltaToCupLeaderLast);
                AddLapProp(OutLapProp.LastLapDeltaToFocusedLast, () => l.GetDynCar(i)?.LastLap?.DeltaToFocusedLast);
                AddLapProp(OutLapProp.LastLapDeltaToAheadLast, () => l.GetDynCar(i)?.LastLap?.DeltaToAheadLast);
                AddLapProp(OutLapProp.LastLapDeltaToAheadInClassLast, () => l.GetDynCar(i)?.LastLap?.DeltaToAheadInClassLast);
                AddLapProp(OutLapProp.LastLapDeltaToAheadInCupLast, () => l.GetDynCar(i)?.LastLap?.DeltaToAheadInCupLast);

                AddLapProp(OutLapProp.DynamicBestLapDeltaToFocusedBest, () => l.GetDynBestLapDeltaToFocusedBest(i));
                AddLapProp(OutLapProp.DynamicLastLapDeltaToFocusedBest, () => l.GetDynLastLapDeltaToFocusedBest(i));
                AddLapProp(OutLapProp.DynamicLastLapDeltaToFocusedLast, () => l.GetDynLastLapDeltaToFocusedLast(i));

                // // Else
                AddProp(OutCarProp.IsFinished, () => (l.GetDynCar(i)?.IsFinished ?? false) ? 1 : 0);
                // AddProp(OutCarProp.MaxSpeed, () => l.GetDynCar(i)?.MaxSpeed);
                AddProp(OutCarProp.IsFocused, () => (l.GetDynCar(i)?.IsFocused ?? false) ? 1 : 0);
                AddProp(OutCarProp.IsOverallBestLapCar, () => (l.GetDynCar(i)?.IsBestLapCarOverall ?? false).ToInt());
                AddProp(OutCarProp.IsClassBestLapCar, () => (l.GetDynCar(i)?.IsBestLapCarInClass ?? false).ToInt());
                // AddProp(OutCarProp.IsCupBestLapCar, () => (l.GetDynCar(i)?.IsCupBestLapCar ?? false) ? 1 : 0);
                // AddProp(OutCarProp.RelativeOnTrackLapDiff, () => l.GetDynCar(i)?.RelativeOnTrackLapDiff ?? 0);

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

        int? MaybeBoolToInt(bool? v) {
            if (v == null) {
                return null;
            }

            return (bool)v ? 1 : 0;
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

        #region Logging

        internal void InitLogging() {
            this._logFileName = $"{Settings.PluginDataLocation}\\Logs\\Log_{PluginStartTime}.txt";
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