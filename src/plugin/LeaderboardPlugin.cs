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
            AttachDynamicLeaderboard("Dynamic", Settings.DynLeaderboardSettings);
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


        private void AttachDynamicLeaderboard(string name, DynLeaderboardSettings settings) {
            void addCar(int i) {
                var startName = $"{name}.{i + 1}";
                void AddProp<T>(OutCarProp prop, Func<T> valueProvider) {
                    if (settings.OutCarProps.Includes(prop)) this.AttachDelegate($"{startName}.{prop.ToPropName()}", valueProvider);
                }

                void AddDriverProp<T>(OutDriverProp prop, string driverId, Func<T> valueProvider) {
                    if (settings.OutDriverProps.Includes(prop)) this.AttachDelegate($"{startName}.{driverId}.{prop}", valueProvider);
                }

                void AddLapProp<T>(OutLapProp prop, Func<T> valueProvider) {
                    if (settings.OutLapProps.Includes(prop)) this.AttachDelegate($"{startName}.{prop.ToPropName()}", valueProvider);
                }

                void AddStintProp<T>(OutStintProp prop, Func<T> valueProvider) {
                    if (settings.OutStintProps.Includes(prop)) this.AttachDelegate($"{startName}.{prop.ToPropName()}", valueProvider);
                }

                void AddDistanceProp<T>(OutDistanceProp prop, Func<T> valueProvider) {
                    if (settings.OutDistanceProps.Includes(prop)) this.AttachDelegate($"{startName}.{prop.ToPropName()}", valueProvider);
                }

                void AddGapProp<T>(OutGapProp prop, Func<T> valueProvider) {
                    if (settings.OutGapProps.Includes(prop)) this.AttachDelegate($"{startName}.{prop.ToPropName()}", valueProvider);
                }


                void AddPosProp<T>(OutPosProp prop, Func<T> valueProvider) {
                    if (settings.OutPosProps.Includes(prop)) this.AttachDelegate($"{startName}.{prop.ToPropName()}", valueProvider);
                }

                void AddPitProp<T>(OutPitProp prop, Func<T> valueProvider) {
                    if (settings.OutPitProps.Includes(prop)) this.AttachDelegate($"{startName}.{prop.ToPropName()}", valueProvider);
                }

                // Laps and sectors
                AddLapProp(OutLapProp.Laps, () => _values.GetDynamicCar(i)?.NewData?.Laps);
                AddLapProp(OutLapProp.LastLapTime, () => _values.GetDynamicCar(i)?.NewData?.LastLap?.Laptime);
                if (settings.OutLapProps.Includes(OutLapProp.LastLapSectors)) {
                    this.AttachDelegate($"{startName}.Laps.Last.S1", () => _values.GetDynamicCar(i)?.NewData?.LastLap?.Splits?[0]);
                    this.AttachDelegate($"{startName}.Laps.Last.S2", () => _values.GetDynamicCar(i)?.NewData?.LastLap?.Splits?[1]);
                    this.AttachDelegate($"{startName}.Laps.Last.S3", () => _values.GetDynamicCar(i)?.NewData?.LastLap?.Splits?[2]);
                }

                AddLapProp(OutLapProp.BestLapTime, () => _values.GetDynamicCar(i)?.NewData?.BestSessionLap?.Laptime);
                if (settings.OutLapProps.Includes(OutLapProp.BestLapSectors)) {
                    this.AttachDelegate($"{startName}.Laps.Best.S1", () => _values.GetDynamicCar(i)?.BestLapSectors?[0]);
                    this.AttachDelegate($"{startName}.Laps.Best.S2", () => _values.GetDynamicCar(i)?.BestLapSectors?[1]);
                    this.AttachDelegate($"{startName}.Laps.Best.S3", () => _values.GetDynamicCar(i)?.BestLapSectors?[2]);
                }
                if (settings.OutLapProps.Includes(OutLapProp.BestSectors)) {
                    this.AttachDelegate($"{startName}.BestS1", () => _values.GetDynamicCar(i)?.NewData?.BestSessionLap?.Splits?[0]);
                    this.AttachDelegate($"{startName}.BestS2", () => _values.GetDynamicCar(i)?.NewData?.BestSessionLap?.Splits?[1]);
                    this.AttachDelegate($"{startName}.BestS3", () => _values.GetDynamicCar(i)?.NewData?.BestSessionLap?.Splits?[2]);
                }

                AddLapProp(OutLapProp.CurrentLapTime, () => _values.GetDynamicCar(i)?.NewData?.CurrentLap?.Laptime);


                void AddOneDriverFromList(int j) {
                    if (settings.NumDrivers > j) {
                        var driverId = $"Driver.{j + 1:00}";
                        AddDriverProp(OutDriverProp.FirstName, driverId, () => _values.GetDynamicCar(i)?.GetDriver(j)?.FirstName);
                        AddDriverProp(OutDriverProp.LastName, driverId, () => _values.GetDynamicCar(i)?.GetDriver(j)?.LastName);
                        AddDriverProp(OutDriverProp.ShortName, driverId, () => _values.GetDynamicCar(i)?.GetDriver(j)?.ShortName);
                        AddDriverProp(OutDriverProp.FullName, driverId, () => _values.GetDynamicCar(i)?.GetDriver(j)?.FullName());
                        AddDriverProp(OutDriverProp.InitialPlusLastName, driverId, () => _values.GetDynamicCar(i)?.GetDriver(j)?.InitialPlusLastName());
                        AddDriverProp(OutDriverProp.Nationality, driverId, () => _values.GetDynamicCar(i)?.GetDriver(j)?.Nationality);
                        AddDriverProp(OutDriverProp.Category, driverId, () => _values.GetDynamicCar(i)?.GetDriver(j)?.Category);
                        AddDriverProp(OutDriverProp.TotalLaps, driverId, () => _values.GetDynamicCar(i)?.GetDriver(j)?.TotalLaps);
                        AddDriverProp(OutDriverProp.BestLapTime, driverId, () => _values.GetDynamicCar(i)?.GetDriver(j)?.BestSessionLap?.Laptime);
                        AddDriverProp(OutDriverProp.TotalDrivingTime, driverId, () => _values.GetDynamicCar(i)?.GetDriverTotalDrivingTime(j));
                        AddDriverProp(OutDriverProp.CategoryColor, driverId, () => _values.GetDynamicCar(i)?.GetDriver(j)?.CategoryColor);
                    }
                }

                if (settings.NumDrivers > 0) {
                    for (int j = 0; j < settings.NumDrivers; j++) {
                        AddOneDriverFromList(j);
                    }
                }

                // Car and team
                AddProp(OutCarProp.CarNumber, () => _values.GetDynamicCar(i)?.RaceNumber);
                AddProp(OutCarProp.CarModel, () => _values.GetDynamicCar(i)?.CarModelType.ToPrettyString());
                AddProp(OutCarProp.CarManufacturer, () => _values.GetDynamicCar(i)?.CarModelType.GetMark());
                AddProp(OutCarProp.CarClass, () => _values.GetDynamicCar(i)?.CarClass.ToString());
                AddProp(OutCarProp.TeamName, () => _values.GetDynamicCar(i)?.TeamName);
                AddProp(OutCarProp.TeamCupCategory, () => _values.GetDynamicCar(i)?.TeamCupCategory.ToString());
                AddStintProp(OutStintProp.CurrentStintTime, () => _values.GetDynamicCar(i)?.CurrentStintTime);
                AddStintProp(OutStintProp.LastStintTime, () => _values.GetDynamicCar(i)?.LastStintTime);
                AddStintProp(OutStintProp.CurrentStintLaps, () => _values.GetDynamicCar(i)?.CurrentStintLaps);
                AddStintProp(OutStintProp.LastStintLaps, () => _values.GetDynamicCar(i)?.LastStintLaps);

                AddProp(OutCarProp.CarClassColor, () => _values.GetDynamicCar(i)?.CarClassColor);
                AddProp(OutCarProp.TeamCupCategoryColor, () => _values.GetDynamicCar(i)?.TeamCupCategoryColor);
                AddProp(OutCarProp.TeamCupCategoryTextColor, () => _values.GetDynamicCar(i)?.TeamCupCategoryTextColor);

                // Gaps and distances
                AddDistanceProp(OutDistanceProp.DistanceToLeader, () => _values.GetDynamicCar(i)?.DistanceToLeader);
                AddDistanceProp(OutDistanceProp.DistanceToClassLeader, () => _values.GetDynamicCar(i)?.DistanceToClassLeader);
                AddDistanceProp(OutDistanceProp.DistanceToFocusedTotal, () => _values.GetDynamicCar(i)?.TotalDistanceToFocused);
                AddDistanceProp(OutDistanceProp.DistanceToFocusedOnTrack, () => _values.GetDynamicCar(i)?.OnTrackDistanceToFocused);

                AddGapProp(OutGapProp.GapToLeader, () => _values.GetDynamicCar(i)?.GapToLeader);
                AddGapProp(OutGapProp.GapToClassLeader, () => _values.GetDynamicCar(i)?.GapToClassLeader);
                AddGapProp(OutGapProp.GapToFocusedOnTrack, () => _values.GetDynamicCar(i)?.GapToFocusedOnTrack);
                AddGapProp(OutGapProp.GapToFocusedTotal, () => _values.GetDynamicCar(i)?.GapToFocusedTotal);
                AddGapProp(OutGapProp.GapToAheadOverall, () => _values.GetDynamicCar(i)?.GapToAhead);
                AddGapProp(OutGapProp.GapToAheadInClass, () => _values.GetDynamicCar(i)?.GapToAheadInClass);

                AddGapProp(OutGapProp.DynamicGapToFocused, () => _values.GetDynamicCar(i)?.GetDynamicGapToFocused());
                AddGapProp(OutGapProp.DynamicGapToAhead, () => _values.GetDynamicCar(i)?.GetDynamicGapToAhead());

                //// Positions
                AddPosProp(OutPosProp.ClassPosition, () => _values.GetDynamicCar(i)?.InClassPos);
                AddPosProp(OutPosProp.OverallPosition, () => _values.GetDynamicCar(i)?.OverallPos);
                AddPosProp(OutPosProp.ClassPositionStart, () => _values.GetDynamicCar(i)?.StartPosInClass);
                AddPosProp(OutPosProp.OverallPositionStart, () => _values.GetDynamicCar(i)?.StartPos);

                // Pit
                AddPitProp(OutPitProp.IsInPitLane, () => (_values.GetDynamicCar(i)?.NewData?.CarLocation ?? CarLocationEnum.NONE) == CarLocationEnum.Pitlane ? 1 : 0);
                AddPitProp(OutPitProp.PitStopCount, () => _values.GetDynamicCar(i)?.PitCount);
                AddPitProp(OutPitProp.PitTimeTotal, () => _values.GetDynamicCar(i)?.TotalPitTime);
                AddPitProp(OutPitProp.PitTimeLast, () => _values.GetDynamicCar(i)?.LastPitTime);
                AddPitProp(OutPitProp.PitTimeCurrent, () => _values.GetDynamicCar(i)?.CurrentTimeInPits);

                // Lap deltas

                AddLapProp(OutLapProp.BestLapDeltaToOverallBest, () => _values.GetDynamicCar(i)?.BestLapDeltaToOverallBest);
                AddLapProp(OutLapProp.BestLapDeltaToClassBest, () => _values.GetDynamicCar(i)?.BestLapDeltaToClassBest);
                AddLapProp(OutLapProp.BestLapDeltaToLeaderBest, () => _values.GetDynamicCar(i)?.BestLapDeltaToLeaderBest);
                AddLapProp(OutLapProp.BestLapDeltaToClassLeaderBest, () => _values.GetDynamicCar(i)?.BestLapDeltaToClassLeaderBest);
                AddLapProp(OutLapProp.BestLapDeltaToFocusedBest, () => _values.GetDynamicCar(i)?.BestLapDeltaToFocusedBest);
                AddLapProp(OutLapProp.BestLapDeltaToAheadBest, () => _values.GetDynamicCar(i)?.BestLapDeltaToAheadBest);
                AddLapProp(OutLapProp.BestLapDeltaToAheadInClassBest, () => _values.GetDynamicCar(i)?.BestLapDeltaToAheadInClassBest);

                AddLapProp(OutLapProp.LastLapDeltaToOverallBest, () => _values.GetDynamicCar(i)?.LastLapDeltaToOverallBest);
                AddLapProp(OutLapProp.LastLapDeltaToClassBest, () => _values.GetDynamicCar(i)?.LastLapDeltaToClassBest);
                AddLapProp(OutLapProp.LastLapDeltaToLeaderBest, () => _values.GetDynamicCar(i)?.LastLapDeltaToLeaderBest);
                AddLapProp(OutLapProp.LastLapDeltaToClassLeaderBest, () => _values.GetDynamicCar(i)?.LastLapDeltaToClassLeaderBest);
                AddLapProp(OutLapProp.LastLapDeltaToFocusedBest, () => _values.GetDynamicCar(i)?.LastLapDeltaToFocusedBest);
                AddLapProp(OutLapProp.LastLapDeltaToAheadBest, () => _values.GetDynamicCar(i)?.LastLapDeltaToAheadBest);
                AddLapProp(OutLapProp.LastLapDeltaToAheadInClassBest, () => _values.GetDynamicCar(i)?.LastLapDeltaToAheadInClassBest);
                AddLapProp(OutLapProp.LastLapDeltaToOwnBest, () => _values.GetDynamicCar(i)?.LastLapDeltaToOwnBest);

                AddLapProp(OutLapProp.LastLapDeltaToLeaderLast, () => _values.GetDynamicCar(i)?.LastLapDeltaToLeaderLast);
                AddLapProp(OutLapProp.LastLapDeltaToClassLeaderLast, () => _values.GetDynamicCar(i)?.LastLapDeltaToClassLeaderLast);
                AddLapProp(OutLapProp.LastLapDeltaToFocusedLast, () => _values.GetDynamicCar(i)?.LastLapDeltaToFocusedLast);
                AddLapProp(OutLapProp.LastLapDeltaToAheadLast, () => _values.GetDynamicCar(i)?.LastLapDeltaToAheadLast);
                AddLapProp(OutLapProp.LastLapDeltaToAheadInClassLast, () => _values.GetDynamicCar(i)?.LastLapDeltaToAheadInClassLast);

                AddLapProp(OutLapProp.DynamicBestLapDeltaToFocusedBest, () => _values.GetDynamicCar(i).GetDynamicBestLapDeltaToFocusedBest());
                AddLapProp(OutLapProp.DynamicLastLapDeltaToFocusedBest, () => _values.GetDynamicCar(i).GetDynamicLastLapDeltaToFocusedBest());
                AddLapProp(OutLapProp.DynamicLastLapDeltaToFocusedLast, () => _values.GetDynamicCar(i).GetDynamicLastLapDeltaToFocusedLast());

                // Else
                AddProp(OutCarProp.IsFinished, () => (_values.GetDynamicCar(i)?.IsFinished ?? false) ? 1 : 0);
                AddProp(OutCarProp.MaxSpeed, () => _values.GetDynamicCar(i)?.MaxSpeed);
            };

            for (int i = 0; i < settings.NumOverallPos; i++) {
                addCar(i);
            }

            this.AttachDelegate("Dynamic.CurrentLeaderboard", () => settings.CurrentLeaderboard().ToString());
            this.AttachDelegate("Dynamic.FocusedPosInCurrentLeaderboard", () => _values.GetFocusedCarIdxInDynLeaderboard());

            // Declare an action which can be called
            this.AddAction($"{name}.NextLeaderboard", (a, b) => {
                if (settings.CurrentLeaderboardIdx == settings.Order.Count - 1) {
                    settings.CurrentLeaderboardIdx = 0;
                } else {
                    settings.CurrentLeaderboardIdx++;
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