using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows.Media;

using GameReaderCommon;

using KLPlugins.DynLeaderboards.Car;
using KLPlugins.DynLeaderboards.Helpers;
using KLPlugins.DynLeaderboards.ksBroadcastingNetwork;
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
        internal static PluginManager PManager;

        private static FileStream? _logFile;
        private static StreamWriter? _logWriter;
        private static bool _isLogFlushed = false;
        private string? _logFileName;
        private Values _values;
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
            if (!Game.IsAcc) { return; } // Atm only ACC is supported
#if TIMINGS
            var timer = _timers?.AddAndRestart("DataUpdate");
#endif

            if (data.GameRunning && data.OldData != null && data.NewData != null) {
                _values.OnDataUpdate(pm, data);
            }
#if TIMINGS
            timer?.StopAndWriteMicros();
            if (data.GameRunning && data.OldData != null && data.NewData != null) {
                WriteFrameTimes(pm);
            }
#endif
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
            // Say something was acidentally copied there or file and leaderboard names were different which would render original file useless
            foreach (var fname in Directory.GetFiles(PluginSettings.leaderboardConfigsDataDirName)) {
                var leaderboardName = fname.Replace(".json", "").Split('\\').Last();
                if (!Settings.DynLeaderboardConfigs.Any(x => x.Name == leaderboardName)) {
                    File.Delete(fname);
                }
            }


            _values.Dispose();
            if (_logWriter != null) {
                _logWriter.Dispose();
                _logWriter = null;
            }
            if (_logFile != null) {
                _logFile.Dispose();
                _logFile = null;
            }
#if TIMINGS
            _timers?.Dispose();
            _timers = null;
            if (_timingWriter != null) {
                _timingWriter.Dispose();
                _timingWriter = null;
            }
            if (_timingFile != null) {
                _timingFile.Dispose();
                _timingFile = null;
            }
#endif
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
        /// <param name="pluginManager"></param>
        public void Init(PluginManager pluginManager) {
            PluginSettings.Migrate();
            Settings = this.ReadCommonSettings<PluginSettings>("GeneralSettings", () => new PluginSettings());
            Settings.ReadDynLeaderboardConfigs();
            _logFileName = $"{Settings.PluginDataLocation}\\Logs\\Log_{PluginStartTime}.txt";
            var gameName = (string)pluginManager.GetPropertyValue<SimHub.Plugins.DataPlugins.DataCore.DataCorePlugin>("CurrentGame");

#if TIMINGS
            InitTimings();
#endif
            Game = new Game(gameName);

            PManager = pluginManager;

            InitLogginig();
            PreJit(); // Performance is important while in game, prejit methods at startup, to avoid doing that mid races
            LogInfo("Starting plugin");

            GameDataPath = $@"{Settings.PluginDataLocation}\{gameName}";
            if (Settings.DynLeaderboardConfigs.Count == 0) {
                AddNewLeaderboard(new DynLeaderboardConfig("Dynamic"));
            }
            // _values is needed by SettingsControl on first time initialization, even if the game is not ACC
            _values = new Values();
            if (!Game.IsAcc)
                return;
            SubscribeToSimHubEvents();
            AttachGeneralDelegates();

            if (Settings.DynLeaderboardConfigs.Count > 0) {
                foreach (var l in _values.LeaderboardValues) {
                    if (!l.Settings.IsEnabled) {
                        continue;
                    }
                    AttachDynamicLeaderboard(l);
                }
            }

            this.AttachDelegate("IsBroadcastClientConnected", () => _values.BroadcastClient?.IsConnected);
        }

        internal void AddNewLeaderboard(DynLeaderboardConfig s) {
            if (_values != null) {
                Settings.DynLeaderboardConfigs.Add(s);
                _values.AddNewLeaderboard(s);
                Settings.SaveDynLeaderboardConfigs();
            }
        }

        internal void RemoveLeaderboardAt(int i) {
            Settings.RemoveLeaderboardAt(i);
            _values?.LeaderboardValues.RemoveAt(i);
        }

        private void SubscribeToSimHubEvents() {
            PManager.GameStateChanged += _values.OnGameStateChanged;
            PManager.GameStateChanged += (bool running, PluginManager _) => {
                LogInfo($"GameStateChanged to {running}");
                if (!running) {
                    if (_logWriter != null && !_isLogFlushed) {
                        _logWriter.Flush();
                        _isLogFlushed = true;
                    }
                }
            };
        }

        private void AttachGeneralDelegates() {
            // Add everything else
            if (Settings.OutGeneralProps.Includes(OutGeneralProp.SessionPhase))
                this.AttachDelegate("Session.Phase", () => _values.RealtimeData?.NewData?.Phase);
            if (Settings.OutGeneralProps.Includes(OutGeneralProp.MaxStintTime))
                this.AttachDelegate("Session.MaxStintTime", () => _values.MaxDriverStintTime);
            if (Settings.OutGeneralProps.Includes(OutGeneralProp.MaxDriveTime))
                this.AttachDelegate("Session.MaxDriveTime", () => _values.MaxDriverTotalDriveTime);

            if (Settings.OutGeneralProps.Includes(OutGeneralProp.CarClassColors)) {
                void addClassColor(CarClass cls) {
                    this.AttachDelegate($"Color.Class.{cls}", () => Settings.CarClassColors[cls]);
                }

                foreach (var c in Enum.GetValues(typeof(CarClass))) {
                    var cls = (CarClass)c;
                    if (cls == CarClass.Overall || cls == CarClass.Unknown)
                        continue;
                    addClassColor(cls);
                }
            }

            void addCupColor(TeamCupCategory cup) {
                if (Settings.OutGeneralProps.Includes(OutGeneralProp.TeamCupColors))
                    this.AttachDelegate($"Color.Cup.{cup}", () => Settings.TeamCupCategoryColors[cup]);
                if (Settings.OutGeneralProps.Includes(OutGeneralProp.TeamCupTextColors))
                    this.AttachDelegate($"Color.Cup.{cup}.Text", () => Settings.TeamCupCategoryTextColors[cup]);
            }

            foreach (var c in Enum.GetValues(typeof(TeamCupCategory))) {
                addCupColor((TeamCupCategory)c);
            }

            if (Settings.OutGeneralProps.Includes(OutGeneralProp.DriverCategoryColors)) {
                void addDriverCategoryColor(DriverCategory cat) {
                    this.AttachDelegate($"Color.DriverCategory.{cat}", () => Settings.DriverCategoryColors[cat]);
                }

                foreach (var c in Enum.GetValues(typeof(DriverCategory))) {
                    var cat = (DriverCategory)c;
                    if (cat == DriverCategory.Error)
                        continue;
                    addDriverCategoryColor(cat);
                }
            }
        }

        private void AttachDynamicLeaderboard(DynLeaderboardValues l) {
            void addCar(int i) {
                var startName = $"{l.Settings.Name}.{i + 1}";
                void AddProp<T>(OutCarProp prop, Func<T> valueProvider) {
                    if (l.Settings.OutCarProps.Includes(prop))
                        this.AttachDelegate($"{startName}.{prop.ToPropName()}", valueProvider);
                }

                void AddDriverProp<T>(OutDriverProp prop, string driverId, Func<T> valueProvider) {
                    if (l.Settings.OutDriverProps.Includes(prop))
                        this.AttachDelegate($"{startName}.{driverId}.{prop}", valueProvider);
                }

                void AddLapProp<T>(OutLapProp prop, Func<T> valueProvider) {
                    if (l.Settings.OutLapProps.Includes(prop))
                        this.AttachDelegate($"{startName}.{prop.ToPropName()}", valueProvider);
                }

                void AddStintProp<T>(OutStintProp prop, Func<T> valueProvider) {
                    if (l.Settings.OutStintProps.Includes(prop))
                        this.AttachDelegate($"{startName}.{prop.ToPropName()}", valueProvider);
                }

                void AddGapProp<T>(OutGapProp prop, Func<T> valueProvider) {
                    if (l.Settings.OutGapProps.Includes(prop))
                        this.AttachDelegate($"{startName}.{prop.ToPropName()}", valueProvider);
                }

                void AddPosProp<T>(OutPosProp prop, Func<T> valueProvider) {
                    if (l.Settings.OutPosProps.Includes(prop))
                        this.AttachDelegate($"{startName}.{prop.ToPropName()}", valueProvider);
                }

                void AddPitProp<T>(OutPitProp prop, Func<T> valueProvider) {
                    if (l.Settings.OutPitProps.Includes(prop))
                        this.AttachDelegate($"{startName}.{prop.ToPropName()}", valueProvider);
                }

                // Laps and sectors
                AddLapProp(OutLapProp.Laps, () => l.GetDynCar(i)?.NewData?.Laps);
                AddLapProp(OutLapProp.LastLapTime, () => l.GetDynCar(i)?.NewData?.LastLap.Laptime);
                if (l.Settings.OutLapProps.Includes(OutLapProp.LastLapSectors)) {
                    this.AttachDelegate($"{startName}.Laps.Last.S1", () => l.GetDynCar(i)?.NewData?.LastLap.Splits?[0]);
                    this.AttachDelegate($"{startName}.Laps.Last.S2", () => l.GetDynCar(i)?.NewData?.LastLap.Splits?[1]);
                    this.AttachDelegate($"{startName}.Laps.Last.S3", () => l.GetDynCar(i)?.NewData?.LastLap.Splits?[2]);
                }

                AddLapProp(OutLapProp.BestLapTime, () => l.GetDynCar(i)?.NewData?.BestSessionLap.Laptime);
                if (l.Settings.OutLapProps.Includes(OutLapProp.BestLapSectors)) {
                    this.AttachDelegate($"{startName}.Laps.Best.S1", () => l.GetDynCar(i)?.BestLapSectors?[0]);
                    this.AttachDelegate($"{startName}.Laps.Best.S2", () => l.GetDynCar(i)?.BestLapSectors?[1]);
                    this.AttachDelegate($"{startName}.Laps.Best.S3", () => l.GetDynCar(i)?.BestLapSectors?[2]);
                }
                if (l.Settings.OutLapProps.Includes(OutLapProp.BestSectors)) {
                    this.AttachDelegate($"{startName}.BestS1", () => l.GetDynCar(i)?.NewData?.BestSessionLap.Splits?[0]);
                    this.AttachDelegate($"{startName}.BestS2", () => l.GetDynCar(i)?.NewData?.BestSessionLap.Splits?[1]);
                    this.AttachDelegate($"{startName}.BestS3", () => l.GetDynCar(i)?.NewData?.BestSessionLap.Splits?[2]);
                }

                AddLapProp(OutLapProp.CurrentLapTime, () => l.GetDynCar(i)?.NewData?.CurrentLap.Laptime);

                AddLapProp(OutLapProp.CurrentLapIsValid, () => MaybeBoolToInt(l.GetDynCar(i)?.NewData?.CurrentLap.IsValidForBest));
                AddLapProp(OutLapProp.LastLapIsValid, () => {
                    // IsValidForBest is true even if there is no actual lap, make it consistent with other properties
                    var last_lap = l.GetDynCar(i)?.NewData?.LastLap;
                    if (last_lap?.Laptime != null) {
                        return MaybeBoolToInt(last_lap?.IsValidForBest);
                    } else {
                        return null;
                    }
                });

                AddLapProp(OutLapProp.CurrentLapIsOutLap, () => MaybeBoolToInt(l.GetDynCar(i)?.IsCurrentLapOutLap));
                AddLapProp(OutLapProp.LastLapIsOutLap, () => MaybeBoolToInt(l.GetDynCar(i)?.IsLastLapOutLap));
                AddLapProp(OutLapProp.CurrentLapIsInLap, () => MaybeBoolToInt(l.GetDynCar(i)?.IsCurrentLapInLap));
                AddLapProp(OutLapProp.LastLapIsInLap, () => MaybeBoolToInt(l.GetDynCar(i)?.IsLastLapInLap));

                void AddOneDriverFromList(int j) {
                    var driverId = $"Driver.{j + 1}";
                    AddDriverProp(OutDriverProp.FirstName, driverId, () => l.GetDynCar(i)?.GetDriver(j)?.FirstName);
                    AddDriverProp(OutDriverProp.LastName, driverId, () => l.GetDynCar(i)?.GetDriver(j)?.LastName);
                    AddDriverProp(OutDriverProp.ShortName, driverId, () => l.GetDynCar(i)?.GetDriver(j)?.ShortName);
                    AddDriverProp(OutDriverProp.FullName, driverId, () => l.GetDynCar(i)?.GetDriver(j)?.FullName);
                    AddDriverProp(OutDriverProp.InitialPlusLastName, driverId, () => l.GetDynCar(i)?.GetDriver(j)?.InitialPlusLastName);
                    AddDriverProp(OutDriverProp.Nationality, driverId, () => l.GetDynCar(i)?.GetDriver(j)?.Nationality);
                    AddDriverProp(OutDriverProp.Category, driverId, () => l.GetDynCar(i)?.GetDriver(j)?.Category);
                    AddDriverProp(OutDriverProp.TotalLaps, driverId, () => l.GetDynCar(i)?.GetDriver(j)?.TotalLaps);
                    AddDriverProp(OutDriverProp.BestLapTime, driverId, () => l.GetDynCar(i)?.GetDriver(j)?.BestSessionLap?.Laptime);
                    AddDriverProp(OutDriverProp.TotalDrivingTime, driverId, () => l.GetDynCar(i)?.GetDriverTotalDrivingTime(j));
                    AddDriverProp(OutDriverProp.CategoryColor, driverId, () => l.GetDynCar(i)?.GetDriver(j)?.CategoryColor);
                }

                if (l.Settings.NumDrivers > 0) {
                    for (int j = 0; j < l.Settings.NumDrivers; j++) {
                        AddOneDriverFromList(j);
                    }
                }

                // Car and team
                AddProp(OutCarProp.CarNumber, () => l.GetDynCar(i)?.RaceNumber);
                AddProp(OutCarProp.CarModel, () => l.GetDynCar(i)?.CarModelType.PrettyName());
                AddProp(OutCarProp.CarManufacturer, () => l.GetDynCar(i)?.CarModelType.Mark());
                AddProp(OutCarProp.CarClass, () => l.GetDynCar(i)?.CarClass.PrettyName());
                AddProp(OutCarProp.TeamName, () => l.GetDynCar(i)?.TeamName);
                AddProp(OutCarProp.TeamCupCategory, () => l.GetDynCar(i)?.TeamCupCategory.PrettyName());
                AddStintProp(OutStintProp.CurrentStintTime, () => l.GetDynCar(i)?.CurrentStintTime);
                AddStintProp(OutStintProp.LastStintTime, () => l.GetDynCar(i)?.LastStintTime);
                AddStintProp(OutStintProp.CurrentStintLaps, () => l.GetDynCar(i)?.CurrentStintLaps);
                AddStintProp(OutStintProp.LastStintLaps, () => l.GetDynCar(i)?.LastStintLaps);

                AddProp(OutCarProp.CarClassColor, () => l.GetDynCar(i)?.CarClassColor);
                AddProp(OutCarProp.TeamCupCategoryColor, () => l.GetDynCar(i)?.TeamCupCategoryColor);
                AddProp(OutCarProp.TeamCupCategoryTextColor, () => l.GetDynCar(i)?.TeamCupCategoryTextColor);

                // Gaps
                AddGapProp(OutGapProp.GapToLeader, () => l.GetDynCar(i)?.GapToLeader);
                AddGapProp(OutGapProp.GapToClassLeader, () => l.GetDynCar(i)?.GapToClassLeader);
                AddGapProp(OutGapProp.GapToFocusedOnTrack, () => l.GetDynCar(i)?.GapToFocusedOnTrack);
                AddGapProp(OutGapProp.GapToFocusedTotal, () => l.GetDynCar(i)?.GapToFocusedTotal);
                AddGapProp(OutGapProp.GapToAheadOverall, () => l.GetDynCar(i)?.GapToAhead);
                AddGapProp(OutGapProp.GapToAheadInClass, () => l.GetDynCar(i)?.GapToAheadInClass);
                AddGapProp(OutGapProp.GapToAheadOnTrack, () => l.GetDynCar(i)?.GapToAheadOnTrack);

                AddGapProp(OutGapProp.DynamicGapToFocused, () => l.GetDynGapToFocused(i));
                AddGapProp(OutGapProp.DynamicGapToAhead, () => l.GetDynGapToAhead(i));

                //// Positions
                AddPosProp(OutPosProp.ClassPosition, () => l.GetDynCar(i)?.InClassPos);
                AddPosProp(OutPosProp.OverallPosition, () => l.GetDynCar(i)?.OverallPos);
                AddPosProp(OutPosProp.ClassPositionStart, () => l.GetDynCar(i)?.StartPosInClass);
                AddPosProp(OutPosProp.OverallPositionStart, () => l.GetDynCar(i)?.StartPos);
                AddPosProp(OutPosProp.DynamicPosition, () => l.GetDynPosition(i));
                AddPosProp(OutPosProp.DynamicPositionStart, () => l.GetDynPositionStart(i));

                // Pit
                // OldData as to be in sync with RelativeOnTrackNoPit leaderboard
                // Don't use OldData.IsInPitLane as that also shows in pitlane if car if CarLocation == PitEntry and PitExit, 
                AddPitProp(OutPitProp.IsInPitLane, () => (l.GetDynCar(i)?.OldData?.CarLocation ?? CarLocationEnum.NONE) == CarLocationEnum.Pitlane ? 1 : 0);
                AddPitProp(OutPitProp.PitStopCount, () => l.GetDynCar(i)?.PitCount);
                AddPitProp(OutPitProp.PitTimeTotal, () => l.GetDynCar(i)?.TotalPitTime);
                AddPitProp(OutPitProp.PitTimeLast, () => l.GetDynCar(i)?.LastPitTime);
                AddPitProp(OutPitProp.PitTimeCurrent, () => l.GetDynCar(i)?.CurrentTimeInPits);

                // Lap deltas

                AddLapProp(OutLapProp.BestLapDeltaToOverallBest, () => l.GetDynCar(i)?.BestLapDeltaToOverallBest);
                AddLapProp(OutLapProp.BestLapDeltaToClassBest, () => l.GetDynCar(i)?.BestLapDeltaToClassBest);
                AddLapProp(OutLapProp.BestLapDeltaToLeaderBest, () => l.GetDynCar(i)?.BestLapDeltaToLeaderBest);
                AddLapProp(OutLapProp.BestLapDeltaToClassLeaderBest, () => l.GetDynCar(i)?.BestLapDeltaToClassLeaderBest);
                AddLapProp(OutLapProp.BestLapDeltaToFocusedBest, () => l.GetDynCar(i)?.BestLapDeltaToFocusedBest);
                AddLapProp(OutLapProp.BestLapDeltaToAheadBest, () => l.GetDynCar(i)?.BestLapDeltaToAheadBest);
                AddLapProp(OutLapProp.BestLapDeltaToAheadInClassBest, () => l.GetDynCar(i)?.BestLapDeltaToAheadInClassBest);

                AddLapProp(OutLapProp.LastLapDeltaToOverallBest, () => l.GetDynCar(i)?.LastLapDeltaToOverallBest);
                AddLapProp(OutLapProp.LastLapDeltaToClassBest, () => l.GetDynCar(i)?.LastLapDeltaToClassBest);
                AddLapProp(OutLapProp.LastLapDeltaToLeaderBest, () => l.GetDynCar(i)?.LastLapDeltaToLeaderBest);
                AddLapProp(OutLapProp.LastLapDeltaToClassLeaderBest, () => l.GetDynCar(i)?.LastLapDeltaToClassLeaderBest);
                AddLapProp(OutLapProp.LastLapDeltaToFocusedBest, () => l.GetDynCar(i)?.LastLapDeltaToFocusedBest);
                AddLapProp(OutLapProp.LastLapDeltaToAheadBest, () => l.GetDynCar(i)?.LastLapDeltaToAheadBest);
                AddLapProp(OutLapProp.LastLapDeltaToAheadInClassBest, () => l.GetDynCar(i)?.LastLapDeltaToAheadInClassBest);
                AddLapProp(OutLapProp.LastLapDeltaToOwnBest, () => l.GetDynCar(i)?.LastLapDeltaToOwnBest);

                AddLapProp(OutLapProp.LastLapDeltaToLeaderLast, () => l.GetDynCar(i)?.LastLapDeltaToLeaderLast);
                AddLapProp(OutLapProp.LastLapDeltaToClassLeaderLast, () => l.GetDynCar(i)?.LastLapDeltaToClassLeaderLast);
                AddLapProp(OutLapProp.LastLapDeltaToFocusedLast, () => l.GetDynCar(i)?.LastLapDeltaToFocusedLast);
                AddLapProp(OutLapProp.LastLapDeltaToAheadLast, () => l.GetDynCar(i)?.LastLapDeltaToAheadLast);
                AddLapProp(OutLapProp.LastLapDeltaToAheadInClassLast, () => l.GetDynCar(i)?.LastLapDeltaToAheadInClassLast);

                AddLapProp(OutLapProp.DynamicBestLapDeltaToFocusedBest, () => l.GetDynBestLapDeltaToFocusedBest(i));
                AddLapProp(OutLapProp.DynamicLastLapDeltaToFocusedBest, () => l.GetDynLastLapDeltaToFocusedBest(i));
                AddLapProp(OutLapProp.DynamicLastLapDeltaToFocusedLast, () => l.GetDynLastLapDeltaToFocusedLast(i));

                // Else
                AddProp(OutCarProp.IsFinished, () => (l.GetDynCar(i)?.IsFinished ?? false) ? 1 : 0);
                AddProp(OutCarProp.MaxSpeed, () => l.GetDynCar(i)?.MaxSpeed);
                AddProp(OutCarProp.IsFocused, () => (l.GetDynCar(i)?.IsFocused ?? false) ? 1 : 0);
                AddProp(OutCarProp.IsOverallBestLapCar, () => (l.GetDynCar(i)?.IsOverallBestLapCar ?? false) ? 1 : 0);
                AddProp(OutCarProp.IsClassBestLapCar, () => (l.GetDynCar(i)?.IsClassBestLapCar ?? false) ? 1 : 0);
                AddProp(OutCarProp.RelativeOnTrackLapDiff, () => l.GetDynCar(i)?.RelativeOnTrackLapDiff ?? 0);

                //this.AttachDelegate($"{startName}.DBG_TotalSplinePosition", () => (l.GetDynCar(i))?.TotalSplinePosition);
                //this.AttachDelegate($"{startName}.DBG_Position", () => (l.GetDynCar(i))?.NewData?.Position);
                //this.AttachDelegate($"{startName}.DBG_TrackPosition", () => (l.GetDynCar(i))?.NewData?.TrackPosition);
                //this.AttachDelegate($"{startName}.DBG_OffsetLapUpdate", () => (l.GetDynCar(i))?.OffsetLapUpdate);
            };

            var numPos = new int[] {
                l.Settings.NumOverallPos,
                l.Settings.NumClassPos,
                l.Settings.NumOverallRelativePos*2+1,
                l.Settings.NumClassRelativePos*2+1,
                l.Settings.NumOnTrackRelativePos*2+1,
                l.Settings.PartialRelativeClassNumClassPos + l.Settings.PartialRelativeClassNumRelativePos*2+1,
                l.Settings.PartialRelativeOverallNumOverallPos + l.Settings.PartialRelativeOverallNumRelativePos*2+1
            };

            for (int i = 0; i < numPos.Max(); i++) {
                addCar(i);
            }

            this.AttachDelegate($"{l.Settings.Name}.CurrentLeaderboard", () => l.Settings.CurrentLeaderboardName);
            this.AttachDelegate($"{l.Settings.Name}.FocusedPosInCurrentLeaderboard", () => l.GetFocusedCarIdxInDynLeaderboard());

            // Declare an action which can be called
            this.AddAction($"{l.Settings.Name}.NextLeaderboard", (a, b) => {
                if (l.Settings.CurrentLeaderboardIdx == l.Settings.Order.Count - 1) {
                    l.Settings.CurrentLeaderboardIdx = 0;
                } else {
                    l.Settings.CurrentLeaderboardIdx++;
                }
                _values.SetDynamicCarGetter();
            });

            // Declare an action which can be called
            this.AddAction($"{l.Settings.Name}.PreviousLeaderboard", (a, b) => {
                if (l.Settings.CurrentLeaderboardIdx == 0) {
                    l.Settings.CurrentLeaderboardIdx = l.Settings.Order.Count - 1;
                } else {
                    l.Settings.CurrentLeaderboardIdx--;
                }
                _values.SetDynamicCarGetter();
            });
        }
        int? MaybeBoolToInt(bool? v) {
            if (v == null) {
                return null;
            }

            return (bool)v ? 1 : 0;
        }

        internal void SetDynamicCarGetter(DynLeaderboardConfig l) {
            _values.LeaderboardValues.Find(x => x.Settings.Name == l.Name)?.SetDynGetters(_values);
        }

#if TIMINGS
        internal static Timers? _timers;
        private FileStream? _timingFile;
        private StreamWriter? _timingWriter;

        private void InitTimings() {
            var timingsRoot = $"{Settings.PluginDataLocation}\\Logs\\timings";
            _timers = new Timers(timingsRoot);
            var path = $"{timingsRoot}\\SH\\{PluginStartTime}.txt";
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            _timingFile = File.Create(path);
            _timingWriter = new StreamWriter(_timingFile);
        }


        private void WriteFrameTimes(PluginManager pm) {
            if (_timingWriter == null) return;

            var ftime = (double)pm.GetPropertyValue<SimHub.Plugins.DataPlugins.DataCore.DataCorePlugin>("Performance_FrameDuration");
            var cached = (double)pm.GetPropertyValue<SimHub.Plugins.DataPlugins.DataCore.DataCorePlugin>("Performance_CachedFormulasPerSecond");
            var jsFormulas = (double)pm.GetPropertyValue<SimHub.Plugins.DataPlugins.DataCore.DataCorePlugin>("Performance_JSFormulasPerSecond");
            var NALCFormulas = (double)pm.GetPropertyValue<SimHub.Plugins.DataPlugins.DataCore.DataCorePlugin>("Performance_NALCFormulasPerSecond");
            var NALCOptFormulas = (double)pm.GetPropertyValue<SimHub.Plugins.DataPlugins.DataCore.DataCorePlugin>("Performance_NALCOptimizedFormulasPerSecond");

            _timingWriter.WriteLine($"{ftime};{cached};{jsFormulas};{NALCFormulas};{NALCOptFormulas}");
        }
#endif

        #region Logging

        internal void InitLogginig() {
            if (Settings.Log) {
                Directory.CreateDirectory(Path.GetDirectoryName(_logFileName));
                _logFile = File.Create(_logFileName);
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
}