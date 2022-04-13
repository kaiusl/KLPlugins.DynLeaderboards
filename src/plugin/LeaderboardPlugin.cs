using GameReaderCommon;
using SimHub.Plugins;
using System;
using System.Windows.Media;
using Newtonsoft.Json;
using System.IO;
using System.Threading;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using System.Reflection;
using GameReaderCommon.Enums;
using KLPlugins.Leaderboard.ksBroadcastingNetwork;
using KLPlugins.Leaderboard.Enums;
using System.Collections.Generic;
using System.Linq;
using KLPlugins.Leaderboard.ksBroadcastingNetwork.Structs;
using System.Linq.Expressions;

namespace KLPlugins.Leaderboard {
    public enum Leaderboard {
        None,
        Overall,
        Class,
        RelativeOverall,
        RelativeClass,
        PartialRelativeOverall,
        PartialRelativeClass,
        RelativeOnTrack
    }

    [PluginDescription("")]
    [PluginAuthor("Kaius Loos")]
    [PluginName("LeaderboardPlugin")]
    public class LeaderboardPlugin : IPlugin, IDataPlugin, IWPFSettingsV2 {
        public static PluginSettings Settings;
        public PluginManager PluginManager { get; set; }
        public ImageSource PictureIcon => this.ToIcon(Properties.Resources.sdkmenuicon);
        public string LeftMenuTitle => PluginName;

        internal const string PluginName = "Leaderboard";
        internal static Game Game; // Const during the lifetime of this plugin, plugin is rebuilt at game change
        internal static string GameDataPath; // Same as above
        internal static string PluginStartTime = $"{DateTime.Now.ToString("dd-MM-yyyy_HH-mm-ss")}";
        internal static PluginManager PManager;

        private static FileStream _logFile;
        private static StreamWriter _logWriter;
        private static bool _isLogFlushed = false;
        private const string SettingsPath = @"PluginsData\KLPlugins\\Leaderboard\Settings.json";
        private string LogFileName;
        private Values _values;

        /// <summary>
        /// Called one time per game data update, contains all normalized game data, 
        /// raw data are intentionnally "hidden" under a generic object type (A plugin SHOULD NOT USE IT)
        /// 
        /// This method is on the critical path, it must execute as fast as possible and avoid throwing any error
        /// 
        /// </summary>
        /// <param name="pluginManager"></param>
        /// <param name="data"></param>
        public void DataUpdate(PluginManager pm, ref GameData data) {
            if (!Game.IsAcc) { return; } // Atm only ACC is supported

            if (data.GameRunning && data.OldData != null && data.NewData != null) {
                //WriteFrameTimes(pm);
                _values.OnDataUpdate(pm, data);
            }
        }

        private void WriteFrameTimes(PluginManager pm) {
            var ftime = (double)pm.GetPropertyValue<SimHub.Plugins.DataPlugins.DataCore.DataCorePlugin>("Performance_FrameDuration");
            var cached = (double)pm.GetPropertyValue<SimHub.Plugins.DataPlugins.DataCore.DataCorePlugin>("Performance_CachedFormulasPerSecond");
            var jsFormulas = (double)pm.GetPropertyValue<SimHub.Plugins.DataPlugins.DataCore.DataCorePlugin>("Performance_JSFormulasPerSecond");
            var NALCFormulas = (double)pm.GetPropertyValue<SimHub.Plugins.DataPlugins.DataCore.DataCorePlugin>("Performance_NALCFormulasPerSecond");
            var NALCOptFormulas = (double)pm.GetPropertyValue<SimHub.Plugins.DataPlugins.DataCore.DataCorePlugin>("Performance_NALCOptimizedFormulasPerSecond");

            if (_timingWriter != null) {
                _timingWriter.WriteLine($"{ftime};{cached};{jsFormulas};{NALCFormulas};{NALCOptFormulas}");
            }
        }

        /// <summary>
        /// Called at plugin manager stop, close/dispose anything needed here !
        /// Plugins are rebuilt at game change
        /// </summary>
        /// <param name="pluginManager"></param>
        public void End(PluginManager pluginManager) {
            this.SaveCommonSettings("GeneralSettings", Settings);
            if (_values != null) {
                _values.Dispose();
            }
            if (_logWriter != null) {
                _logWriter.Dispose();
                _logWriter = null;
            }
            if (_logFile != null) {
                _logFile.Dispose();
                _logFile = null;
            }

            if (_timingWriter != null) { 
                _timingWriter.Dispose();
                _timingWriter = null;
            }

            if (_timingFile != null) { 
                _timingFile.Dispose();
                _timingFile = null;
            }
        }

        /// <summary>
        /// Returns the settings control, return null if no settings control is required
        /// </summary>
        /// <param name="pluginManager"></param>
        /// <returns></returns>
        public System.Windows.Controls.Control GetWPFSettingsControl(PluginManager pluginManager) {
            return new SettingsControlDemo(this);
        }


        private FileStream _timingFile;
        private StreamWriter _timingWriter;

        /// <summary>
        /// Called once after plugins startup
        /// Plugins are rebuilt at game change
        /// </summary>
        /// <param name="pluginManager"></param>
        public void Init(PluginManager pluginManager) {
            Settings = this.ReadCommonSettings<PluginSettings>("GeneralSettings", () => new PluginSettings());
            LogFileName = $"{Settings.PluginDataLocation}\\Logs\\Log_{PluginStartTime}.txt";
            var gameName = (string)pluginManager.GetPropertyValue<SimHub.Plugins.DataPlugins.DataCore.DataCorePlugin>("CurrentGame");
            Game = new Game(gameName);
            if (!Game.IsAcc) return;
            var timingFName = $"{Settings.PluginDataLocation}\\Logs\\timings\\frametime\\{PluginStartTime}.txt";
            Directory.CreateDirectory(Path.GetDirectoryName(timingFName));
            _timingFile = File.Create(timingFName);
            _timingWriter = new StreamWriter(_timingFile);

            PManager = pluginManager;

            InitLogginig();
            PreJit(); // Performance is important while in game, pre jit methods at startup, to avoid doing that mid races

            LogInfo("Starting plugin");
            

            GameDataPath = $@"{Settings.PluginDataLocation}\{gameName}";
            _values = new Values();

            SubscribeToSimHubEvents();

            AttachGeneralDelegates();
            if (Settings.DynLeaderboardSettings.Count > 0) {
                LogInfo($"Settings.DynLeaderboardSettings.Count = {Settings.DynLeaderboardSettings.Count}, _values.LeaderboardValues.Count = {_values.LeaderboardValues.Count}");

                for (int i = 0; i < Settings.DynLeaderboardSettings.Count; i++) {
                    AttachDynamicLeaderboard($"Dynamic{i}", _values.LeaderboardValues[i]);
                }
            }
            
        }

        public void AddNewLeaderboard(DynLeaderboardSettings s) {
            _values.AddNewLeaderboard(s);
        }

        public void RemoveLeaderboardAt(int i) {
            _values.LeaderboardValues.RemoveAt(i);
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
            if (Settings.OutGeneralProps.Includes(OutGeneralProp.SessionPhase)) this.AttachDelegate("Session.Phase", () => _values.RealtimeData?.Phase);
            if (Settings.OutGeneralProps.Includes(OutGeneralProp.MaxStintTime)) this.AttachDelegate("Session.MaxStintTime", () => _values.MaxDriverStintTime);
            if (Settings.OutGeneralProps.Includes(OutGeneralProp.MaxDriveTime)) this.AttachDelegate("Session.MaxDriveTime", () => _values.MaxDriverTotalDriveTime);


            void addClassColor(CarClass cls) {
                this.AttachDelegate($"Color.Class.{cls}", () => Settings.CarClassColors[cls]);
            }

            foreach (var c in Enum.GetValues(typeof(CarClass))) {
                var cls = (CarClass)c;
                if (cls == CarClass.Overall || cls == CarClass.Unknown) continue;
                addClassColor(cls);
            }

            void addCupColor(TeamCupCategory cup) {
                this.AttachDelegate($"Color.Cup.{cup}", () => Settings.TeamCupCategoryColors[cup]);
                this.AttachDelegate($"Color.Cup.{cup}.Text", () => Settings.TeamCupCategoryTextColors[cup]);
            }

            foreach (var c in Enum.GetValues(typeof(TeamCupCategory))) {
                addCupColor((TeamCupCategory)c);
            }

            void addDriverCategoryColor(DriverCategory cat) {
                this.AttachDelegate($"Color.DriverCategory.{cat}", () => Settings.DriverCategoryColors[cat]);
            }

            foreach (var c in Enum.GetValues(typeof(DriverCategory))) {
                var cat = (DriverCategory)c;
                if (cat == DriverCategory.Error) continue;
                addDriverCategoryColor(cat);
            }
        }



        private void AttachDynamicLeaderboard(string name, Values.DynLeaderboardValues l) {
            void addCar(int i) {
                var startName = $"{name}.{i + 1}";
                void AddProp<T>(OutCarProp prop, Func<T> valueProvider) {
                    if (l.Settings.OutCarProps.Includes(prop)) this.AttachDelegate($"{startName}.{prop.ToPropName()}", valueProvider);
                }

                void AddDriverProp<T>(OutDriverProp prop, string driverId, Func<T> valueProvider) {
                    if (l.Settings.OutDriverProps.Includes(prop)) this.AttachDelegate($"{startName}.{driverId}.{prop}", valueProvider);
                }

                void AddLapProp<T>(OutLapProp prop, Func<T> valueProvider) {
                    if (l.Settings.OutLapProps.Includes(prop)) this.AttachDelegate($"{startName}.{prop.ToPropName()}", valueProvider);
                }

                void AddStintProp<T>(OutStintProp prop, Func<T> valueProvider) {
                    if (l.Settings.OutStintProps.Includes(prop)) this.AttachDelegate($"{startName}.{prop.ToPropName()}", valueProvider);
                }

                void AddDistanceProp<T>(OutDistanceProp prop, Func<T> valueProvider) {
                    if (l.Settings.OutDistanceProps.Includes(prop)) this.AttachDelegate($"{startName}.{prop.ToPropName()}", valueProvider);
                }

                void AddGapProp<T>(OutGapProp prop, Func<T> valueProvider) {
                    if (l.Settings.OutGapProps.Includes(prop)) this.AttachDelegate($"{startName}.{prop.ToPropName()}", valueProvider);
                }


                void AddPosProp<T>(OutPosProp prop, Func<T> valueProvider) {
                    if (l.Settings.OutPosProps.Includes(prop)) this.AttachDelegate($"{startName}.{prop.ToPropName()}", valueProvider);
                }

                void AddPitProp<T>(OutPitProp prop, Func<T> valueProvider) {
                    if (l.Settings.OutPitProps.Includes(prop)) this.AttachDelegate($"{startName}.{prop.ToPropName()}", valueProvider);
                }

                // Laps and sectors
                AddLapProp(OutLapProp.Laps, () => l.GetDynamicCar(i)?.NewData?.Laps);
                AddLapProp(OutLapProp.LastLapTime, () => l.GetDynamicCar(i)?.NewData?.LastLap?.Laptime);
                if (l.Settings.OutLapProps.Includes(OutLapProp.LastLapSectors)) {
                    this.AttachDelegate($"{startName}.Laps.Last.S1", () => l.GetDynamicCar(i)?.NewData?.LastLap?.Splits?[0]);
                    this.AttachDelegate($"{startName}.Laps.Last.S2", () => l.GetDynamicCar(i)?.NewData?.LastLap?.Splits?[1]);
                    this.AttachDelegate($"{startName}.Laps.Last.S3", () => l.GetDynamicCar(i)?.NewData?.LastLap?.Splits?[2]);
                }

                AddLapProp(OutLapProp.BestLapTime, () => l.GetDynamicCar(i)?.NewData?.BestSessionLap?.Laptime);
                if (l.Settings.OutLapProps.Includes(OutLapProp.BestLapSectors)) {
                    this.AttachDelegate($"{startName}.Laps.Best.S1", () => l.GetDynamicCar(i)?.BestLapSectors?[0]);
                    this.AttachDelegate($"{startName}.Laps.Best.S2", () => l.GetDynamicCar(i)?.BestLapSectors?[1]);
                    this.AttachDelegate($"{startName}.Laps.Best.S3", () => l.GetDynamicCar(i)?.BestLapSectors?[2]);
                }
                if (l.Settings.OutLapProps.Includes(OutLapProp.BestSectors)) {
                    this.AttachDelegate($"{startName}.BestS1", () => l.GetDynamicCar(i)?.NewData?.BestSessionLap?.Splits?[0]);
                    this.AttachDelegate($"{startName}.BestS2", () => l.GetDynamicCar(i)?.NewData?.BestSessionLap?.Splits?[1]);
                    this.AttachDelegate($"{startName}.BestS3", () => l.GetDynamicCar(i)?.NewData?.BestSessionLap?.Splits?[2]);
                }

                AddLapProp(OutLapProp.CurrentLapTime, () => l.GetDynamicCar(i)?.NewData?.CurrentLap?.Laptime);


                void AddOneDriverFromList(int j) {
                    var driverId = $"Driver.{j + 1:00}";
                    AddDriverProp(OutDriverProp.FirstName, driverId, () => l.GetDynamicCar(i)?.GetDriver(j)?.FirstName);
                    AddDriverProp(OutDriverProp.LastName, driverId, () => l.GetDynamicCar(i)?.GetDriver(j)?.LastName);
                    AddDriverProp(OutDriverProp.ShortName, driverId, () => l.GetDynamicCar(i)?.GetDriver(j)?.ShortName);
                    AddDriverProp(OutDriverProp.FullName, driverId, () => l.GetDynamicCar(i)?.GetDriver(j)?.FullName());
                    AddDriverProp(OutDriverProp.InitialPlusLastName, driverId, () => l.GetDynamicCar(i)?.GetDriver(j)?.InitialPlusLastName());
                    AddDriverProp(OutDriverProp.Nationality, driverId, () => l.GetDynamicCar(i)?.GetDriver(j)?.Nationality);
                    AddDriverProp(OutDriverProp.Category, driverId, () => l.GetDynamicCar(i)?.GetDriver(j)?.Category);
                    AddDriverProp(OutDriverProp.TotalLaps, driverId, () => l.GetDynamicCar(i)?.GetDriver(j)?.TotalLaps);
                    AddDriverProp(OutDriverProp.BestLapTime, driverId, () => l.GetDynamicCar(i)?.GetDriver(j)?.BestSessionLap?.Laptime);
                    AddDriverProp(OutDriverProp.TotalDrivingTime, driverId, () => l.GetDynamicCar(i)?.GetDriverTotalDrivingTime(j));
                    AddDriverProp(OutDriverProp.CategoryColor, driverId, () => l.GetDynamicCar(i)?.GetDriver(j)?.CategoryColor);
                }

                if (l.Settings.NumDrivers > 0) {
                    for (int j = 0; j < l.Settings.NumDrivers; j++) {
                        AddOneDriverFromList(j);
                    }
                }

                // Car and team
                AddProp(OutCarProp.CarNumber, () => l.GetDynamicCar(i)?.RaceNumber);
                AddProp(OutCarProp.CarModel, () => l.GetDynamicCar(i)?.CarModelType.ToPrettyString());
                AddProp(OutCarProp.CarManufacturer, () => l.GetDynamicCar(i)?.CarModelType.GetMark());
                AddProp(OutCarProp.CarClass, () => l.GetDynamicCar(i)?.CarClass.ToString());
                AddProp(OutCarProp.TeamName, () => l.GetDynamicCar(i)?.TeamName);
                AddProp(OutCarProp.TeamCupCategory, () => l.GetDynamicCar(i)?.TeamCupCategory.ToString());
                AddStintProp(OutStintProp.CurrentStintTime, () => l.GetDynamicCar(i)?.CurrentStintTime);
                AddStintProp(OutStintProp.LastStintTime, () => l.GetDynamicCar(i)?.LastStintTime);
                AddStintProp(OutStintProp.CurrentStintLaps, () => l.GetDynamicCar(i)?.CurrentStintLaps);
                AddStintProp(OutStintProp.LastStintLaps, () => l.GetDynamicCar(i)?.LastStintLaps);

                AddProp(OutCarProp.CarClassColor, () => l.GetDynamicCar(i)?.CarClassColor);
                AddProp(OutCarProp.TeamCupCategoryColor, () => l.GetDynamicCar(i)?.TeamCupCategoryColor);
                AddProp(OutCarProp.TeamCupCategoryTextColor, () => l.GetDynamicCar(i)?.TeamCupCategoryTextColor);

                // Gaps and distances
                AddDistanceProp(OutDistanceProp.DistanceToLeader, () => l.GetDynamicCar(i)?.DistanceToLeader);
                AddDistanceProp(OutDistanceProp.DistanceToClassLeader, () => l.GetDynamicCar(i)?.DistanceToClassLeader);
                AddDistanceProp(OutDistanceProp.DistanceToFocusedTotal, () => l.GetDynamicCar(i)?.TotalDistanceToFocused);
                AddDistanceProp(OutDistanceProp.DistanceToFocusedOnTrack, () => l.GetDynamicCar(i)?.OnTrackDistanceToFocused);

                AddGapProp(OutGapProp.GapToLeader, () => l.GetDynamicCar(i)?.GapToLeader);
                AddGapProp(OutGapProp.GapToClassLeader, () => l.GetDynamicCar(i)?.GapToClassLeader);
                AddGapProp(OutGapProp.GapToFocusedOnTrack, () => l.GetDynamicCar(i)?.GapToFocusedOnTrack);
                AddGapProp(OutGapProp.GapToFocusedTotal, () => l.GetDynamicCar(i)?.GapToFocusedTotal);
                AddGapProp(OutGapProp.GapToAheadOverall, () => l.GetDynamicCar(i)?.GapToAhead);
                AddGapProp(OutGapProp.GapToAheadInClass, () => l.GetDynamicCar(i)?.GapToAheadInClass);

                AddGapProp(OutGapProp.DynamicGapToFocused, () => l.GetDynamicGapToFocused(i));
                AddGapProp(OutGapProp.DynamicGapToAhead, () => l.GetDynamicGapToAhead(i));

                //// Positions
                AddPosProp(OutPosProp.ClassPosition, () => l.GetDynamicCar(i)?.InClassPos);
                AddPosProp(OutPosProp.OverallPosition, () => l.GetDynamicCar(i)?.OverallPos);
                AddPosProp(OutPosProp.ClassPositionStart, () => l.GetDynamicCar(i)?.StartPosInClass);
                AddPosProp(OutPosProp.OverallPositionStart, () => l.GetDynamicCar(i)?.StartPos);

                // Pit
                AddPitProp(OutPitProp.IsInPitLane, () => (l.GetDynamicCar(i)?.NewData?.CarLocation ?? CarLocationEnum.NONE) == CarLocationEnum.Pitlane ? 1 : 0);
                AddPitProp(OutPitProp.PitStopCount, () => l.GetDynamicCar(i)?.PitCount);
                AddPitProp(OutPitProp.PitTimeTotal, () => l.GetDynamicCar(i)?.TotalPitTime);
                AddPitProp(OutPitProp.PitTimeLast, () => l.GetDynamicCar(i)?.LastPitTime);
                AddPitProp(OutPitProp.PitTimeCurrent, () => l.GetDynamicCar(i)?.CurrentTimeInPits);

                // Lap deltas

                AddLapProp(OutLapProp.BestLapDeltaToOverallBest, () => l.GetDynamicCar(i)?.BestLapDeltaToOverallBest);
                AddLapProp(OutLapProp.BestLapDeltaToClassBest, () => l.GetDynamicCar(i)?.BestLapDeltaToClassBest);
                AddLapProp(OutLapProp.BestLapDeltaToLeaderBest, () => l.GetDynamicCar(i)?.BestLapDeltaToLeaderBest);
                AddLapProp(OutLapProp.BestLapDeltaToClassLeaderBest, () => l.GetDynamicCar(i)?.BestLapDeltaToClassLeaderBest);
                AddLapProp(OutLapProp.BestLapDeltaToFocusedBest, () => l.GetDynamicCar(i)?.BestLapDeltaToFocusedBest);
                AddLapProp(OutLapProp.BestLapDeltaToAheadBest, () => l.GetDynamicCar(i)?.BestLapDeltaToAheadBest);
                AddLapProp(OutLapProp.BestLapDeltaToAheadInClassBest, () => l.GetDynamicCar(i)?.BestLapDeltaToAheadInClassBest);

                AddLapProp(OutLapProp.LastLapDeltaToOverallBest, () => l.GetDynamicCar(i)?.LastLapDeltaToOverallBest);
                AddLapProp(OutLapProp.LastLapDeltaToClassBest, () => l.GetDynamicCar(i)?.LastLapDeltaToClassBest);
                AddLapProp(OutLapProp.LastLapDeltaToLeaderBest, () => l.GetDynamicCar(i)?.LastLapDeltaToLeaderBest);
                AddLapProp(OutLapProp.LastLapDeltaToClassLeaderBest, () => l.GetDynamicCar(i)?.LastLapDeltaToClassLeaderBest);
                AddLapProp(OutLapProp.LastLapDeltaToFocusedBest, () => l.GetDynamicCar(i)?.LastLapDeltaToFocusedBest);
                AddLapProp(OutLapProp.LastLapDeltaToAheadBest, () => l.GetDynamicCar(i)?.LastLapDeltaToAheadBest);
                AddLapProp(OutLapProp.LastLapDeltaToAheadInClassBest, () => l.GetDynamicCar(i)?.LastLapDeltaToAheadInClassBest);
                AddLapProp(OutLapProp.LastLapDeltaToOwnBest, () => l.GetDynamicCar(i)?.LastLapDeltaToOwnBest);

                AddLapProp(OutLapProp.LastLapDeltaToLeaderLast, () => l.GetDynamicCar(i)?.LastLapDeltaToLeaderLast);
                AddLapProp(OutLapProp.LastLapDeltaToClassLeaderLast, () => l.GetDynamicCar(i)?.LastLapDeltaToClassLeaderLast);
                AddLapProp(OutLapProp.LastLapDeltaToFocusedLast, () => l.GetDynamicCar(i)?.LastLapDeltaToFocusedLast);
                AddLapProp(OutLapProp.LastLapDeltaToAheadLast, () => l.GetDynamicCar(i)?.LastLapDeltaToAheadLast);
                AddLapProp(OutLapProp.LastLapDeltaToAheadInClassLast, () => l.GetDynamicCar(i)?.LastLapDeltaToAheadInClassLast);

                AddLapProp(OutLapProp.DynamicBestLapDeltaToFocusedBest, () => l.GetDynamicBestLapDeltaToFocusedBest(i));
                AddLapProp(OutLapProp.DynamicLastLapDeltaToFocusedBest, () => l.GetDynamicLastLapDeltaToFocusedBest(i));
                AddLapProp(OutLapProp.DynamicLastLapDeltaToFocusedLast, () => l.GetDynamicLastLapDeltaToFocusedLast(i));

                // Else
                AddProp(OutCarProp.IsFinished, () => (l.GetDynamicCar(i)?.IsFinished ?? false) ? 1 : 0);
                AddProp(OutCarProp.MaxSpeed, () => l.GetDynamicCar(i)?.MaxSpeed);
            };

            for (int i = 0; i < l.Settings.NumOverallPos; i++) {
                addCar(i);
            }

            this.AttachDelegate($"{name}.CurrentLeaderboard", () => l.Settings.CurrentLeaderboard().ToString());
            this.AttachDelegate($"{name}.FocusedPosInCurrentLeaderboard", () => l.GetFocusedCarIdxInDynLeaderboard());

            // Declare an action which can be called
            this.AddAction($"{name}.NextLeaderboard", (a, b) => {
                if (l.Settings.CurrentLeaderboardIdx == l.Settings.Order.Count - 1) {
                    l.Settings.CurrentLeaderboardIdx = 0;
                } else {
                    l.Settings.CurrentLeaderboardIdx++;
                }
                _values.SetDynamicCarGetter();
                SimHub.Logging.Current.Info("Speed warning changed");
            });
        }


        #region Logging

        public void InitLogginig() {
            if (Settings.Log) {
                Directory.CreateDirectory(Path.GetDirectoryName(LogFileName));
                _logFile = File.Create(LogFileName);
                _logWriter = new StreamWriter(_logFile);
            }
        }

        public static void LogInfo(string msg, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int lineNumber = 0) {
            if (Settings.Log) {
                Log(msg, memberName, sourceFilePath, lineNumber, "INFO", SimHub.Logging.Current.Info);
            }
        }

        public static void LogWarn(string msg, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int lineNumber = 0) {
            Log(msg, memberName, sourceFilePath, lineNumber, "WARN", SimHub.Logging.Current.Warn);
        }

        public static void LogError(string msg, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int lineNumber = 0) {
            Log(msg, memberName, sourceFilePath, lineNumber, "ERROR", SimHub.Logging.Current.Error);
        }


        private static void Log(string msg, string memberName, string sourceFilePath, int lineNumber, string lvl, Action<string> simHubLog) {
            var pathParts = sourceFilePath.Split('\\');
            var m = CreateMessage(msg, pathParts[pathParts.Length - 1], memberName, lineNumber);
            simHubLog($"{PluginName} {m}");
            LogToFile($"{DateTime.Now.ToString("dd.MM.yyyy HH:mm.ss")} {lvl.ToUpper()} {m}\n");
        }

        private static string CreateMessage(string msg, string source, string memberName, int lineNumber) {
            return $"({source}: {memberName},{lineNumber})\n\t{msg}";
        }

        public static void LogFileSeparator() {
            LogToFile("\n----------------------------------------------------------\n");
        }


        private static void LogToFile(string msq) {
            if (_logWriter != null) {
                _logWriter.WriteLine(msq);
                _isLogFlushed = false;
            }
        }

        #endregion

        private static void PreJit() {
            Stopwatch sw = new Stopwatch();
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

            var t = sw.Elapsed;
            LogInfo($"Prejit finished in {t.TotalMilliseconds}ms");

        }
    }
}