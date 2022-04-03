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

namespace KLPlugins.Leaderboard {
    [PluginDescription("")]
    [PluginAuthor("Kaius Loos")]
    [PluginName("LeaderboardPlugin")]
    public class LeaderboardPlugin : IPlugin, IDataPlugin, IWPFSettingsV2 {
        //public PluginSettings ShSettings;
        public PluginManager PluginManager { get; set; }
        public ImageSource PictureIcon => this.ToIcon(Properties.Resources.sdkmenuicon);
        public string LeftMenuTitle => PluginName;

        internal const string PluginName = "Leaderboard";
        internal static Settings Settings = new Settings();
        internal static Game Game; // Const during the lifetime of this plugin, plugin is rebuilt at game change
        internal static string GameDataPath; // Same as above
        internal static string PluginStartTime = $"{DateTime.Now.ToString("dd-MM-yyyy_HH-mm-ss")}";
        internal static PluginManager PManager;

        private static FileStream _logFile;
        private static StreamWriter _logWriter;
        private static bool _isLogFlushed = false;
        private const string SettingsPath = @"PluginsData\KLPlugins\\Leaderboard\Settings.json";
        private readonly string LogFileName = $"{Settings.PluginDataLocation}\\Logs\\Log_{PluginStartTime}.txt";
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

        float prevPos = 0.0f;
        int prevLaps = 0;
        public void DataUpdate(PluginManager pm, ref GameData data) {
            if (!Game.IsAcc) { return; } // Atm only ACC is supported

            //if (data.GameRunning && data.OldData != null && data.NewData != null) {
            //    var track = (string)pm.GetPropertyValue<SimHub.Plugins.DataPlugins.DataCore.DataCorePlugin>("GameRawData.StaticInfo.Track");
            //    var npos = (float)pm.GetPropertyValue<SimHub.Plugins.DataPlugins.DataCore.DataCorePlugin>("GameRawData.Graphics.NormalizedCarPosition");
            //    var ctime = (int)pm.GetPropertyValue<SimHub.Plugins.DataPlugins.DataCore.DataCorePlugin>("GameRawData.Graphics.iCurrentTime");
            //    var speed = (float)pm.GetPropertyValue<SimHub.Plugins.DataPlugins.DataCore.DataCorePlugin>("GameRawData.Physics.SpeedKmh");
            //    var laps = (int)pm.GetPropertyValue<SimHub.Plugins.DataPlugins.DataCore.DataCorePlugin>("GameData.CompletedLaps");

 
            //    File.AppendAllText($"{LeaderboardPlugin.Settings.PluginDataLocation}\\NewLaps\\{track}_{laps + 1}.txt", $"{npos};{ctime};{speed}\n");


            //}
        }

        /// <summary>
        /// Called at plugin manager stop, close/dispose anything needed here !
        /// Plugins are rebuilt at game change
        /// </summary>
        /// <param name="pluginManager"></param>
        public void End(PluginManager pluginManager) {
            //this.SaveCommonSettings("GeneralSettings", ShSettings);
            _values.Dispose();
            _logWriter.Dispose();
            _logFile.Dispose();
            _logWriter = null;
            _logFile = null;
        }

        /// <summary>
        /// Returns the settings control, return null if no settings control is required
        /// </summary>
        /// <param name="pluginManager"></param>
        /// <returns></returns>
        public System.Windows.Controls.Control GetWPFSettingsControl(PluginManager pluginManager) {
            return null;//new SettingsControlDemo(this);
        }

        /// <summary>
        /// Called once after plugins startup
        /// Plugins are rebuilt at game change
        /// </summary>
        /// <param name="pluginManager"></param>
        public void Init(PluginManager pluginManager) {
            var gameName = (string)pluginManager.GetPropertyValue<SimHub.Plugins.DataPlugins.DataCore.DataCorePlugin>("CurrentGame");
            if (gameName != Game.AccName) return;

            PManager = pluginManager;

            ReadSettings();
            InitLogginig();
            PreJit(); // Performance is important while in game, pre jit methods at startup, to avoid doing that mid races

            LogInfo("Starting plugin");
            //ShSettings = this.ReadCommonSettings<PluginSettings>("GeneralSettings", () => new PluginSettings());

            Game = new Game(gameName);
            GameDataPath = $@"{Settings.PluginDataLocation}\{gameName}";
            _values = new Values();

            SubscribeToSimHubEvents();

            //AttachDebugDelegates();
            AttachDelegates();
        }

        private void ReadSettings() {
            try {
                Settings = JsonConvert.DeserializeObject<Settings>(File.ReadAllText(SettingsPath).Replace("\"", "'"));
                Settings.Validate();
            } catch (Exception e) {
                Settings = new Settings();
                string txt = JsonConvert.SerializeObject(Settings, Formatting.Indented);
                Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath));
                File.WriteAllText(SettingsPath, txt);
            }
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

        private void AttachDebugDelegates() {
            //this.AttachDelegate("DBG.LenCars", () => _values.Cars.Count);
            //this.AttachDelegate("DBG.FocusedCar", () => _values.GetFocusedCar()?.ToString());

            //void overallDBG(int i) => this.AttachDelegate($"DBG.Overall.{i + 1:00}.Numlaps", () => _values.GetCar(i)?.ToString());
            //void InClassDBG(int i) => this.AttachDelegate($"DBG.InClass.{i + 1:00}.Numlaps", () => _values.DbgGetInClassPos(i)?.ToString());
            //for (int i = 0; i < Settings.NumOverallPos; i++) {
            //    overallDBG(i);
            //    InClassDBG(i);
            //}

            //void overallOnTrackDBG(int i) => this.AttachDelegate($"DBG.Relative.{i + 1:00}.Numlaps", () => _values.DbgGetOverallPosOnTrack(i)?.ToString());
            //for (int i = 0; i < Settings.NumRelativePos * 2 + 1; i++) {
            //    overallOnTrackDBG(i);
            //}

            //this.AttachDelegate("DBG.Realtime.SessionTime", () => _values.RealtimeUpdate?.SessionTime);
            //this.AttachDelegate("DBG.Realtime.RemainingTime", () => _values.RealtimeUpdate?.RemainingTime);
            //this.AttachDelegate("DBG.Realtime.TimeOfDay", () => _values.RealtimeUpdate?.TimeOfDay);
            //this.AttachDelegate("DBG.Realtime.SessionRemainingTime", () => _values.RealtimeUpdate?.SessionRemainingTime);
            //this.AttachDelegate("DBG.Realtime.SessionEndTime", () => _values.RealtimeUpdate?.SessionEndTime);

            void addCar(int i) {
                var startName = $"Overall.{i + 1:00}";
                this.AttachDelegate($"DBG_info.{startName}", () => {
                    var car = _values.GetCar(i);
                    if (car == null) return null;
                    return $"#{car.RaceNumber,-4}, isFinished:{car.IsFinished}, FinishTime:{car.FinishTime?.TotalSeconds}, L{car.NewData?.Laps}, startPos:{car.StartPos:00}/{car.StartPosInClass:00}, TSP:{car.TotalSplinePosition}";
                });

                this.AttachDelegate($"DBG_PitInfo.{startName}", () => {
                    var car = _values.GetCar(i);
                    if (car == null) return null;
                    return $"#{car.RaceNumber,-4}, InPitLane:{car.NewData?.CarLocation == CarLocationEnum.Pitlane,-5}, Count:{car.PitCount:00}, PitTimes(C/L/T):{_values.GetCar(i)?.CurrentTimeInPits ?? 0:000.0}/{_values.GetCar(i)?.LastPitTime:000.0}/{_values.GetCar(i)?.TotalPitTime:000.0}";
                });

                this.AttachDelegate($"DBG_DriverInfo.{startName}", () => {
                    var car = _values.GetCar(i);
                    if (car == null) return null;
                    var driver = car.CurrentDriver;
                    
                    return $"#{car.RaceNumber,-4}, DCount:{car.NewData.DriverCount}, DId:{car.NewData.DriverIndex}, {driver.InitialPlusLastName()}, Total:{driver.TotalLaps:00}laps/{car.CurrentDriverTotalDrivingTime:000.0}s, Stint(C/L):{car.CurrentStintTime:000.0}s/{car.LastStintTime:000.0}s, BestLap:{driver.BestSessionLap?.Laptime ?? -1:000.000}";
                });

                //this.AttachDelegate($"DBG.{startName}.SplinePosition", () => _values.GetCar(i)?.RealtimeCarUpdate?.SplinePosition);
                //this.AttachDelegate($"DBG.{startName}.Laps", () => _values.GetCar(i)?.RealtimeCarUpdate?.Laps);
                //this.AttachDelegate($"DBG.{startName}.LapsBySplinePosition", () => _values.GetCar(i)?.LapsBySplinePosition);
                //this.AttachDelegate($"DBG.{startName}.TotalSplinePosition", () => _values.GetCar(i)?.TotalSplinePosition);
            };

            for (int i = 0; i < Settings.NumOverallPos; i++) {
                addCar(i);
            }

            //this.AttachDelegate("DBG_car.01", () => _values.GetCar(1));


        }

        private void AttachDelegates() {
            // Idea with properties is to add cars in overall order and then for different orderings provide indexes into overall order.

            void addCar(int i) {
                var startName = $"Overall.{i + 1:00}";
                void AddProp<T>(ExposedProperties prop, Func<T> valueProvider) {
                    if (Settings.ExposedProperties.Includes(prop)) this.AttachDelegate($"{startName}.{prop}", valueProvider);
                }

                // Laps and sectors

                AddProp(ExposedProperties.NumberOfLaps, () => _values.GetCar(i)?.NewData?.Laps);
                AddProp(ExposedProperties.LastLap, () => _values.GetCar(i)?.NewData?.LastLap?.Laptime);
                if (Settings.ExposedProperties.Includes(ExposedProperties.LastLapSectors)) {
                    this.AttachDelegate($"{startName}.LastLapS1", () => _values.GetCar(i)?.NewData?.LastLap?.Splits?[0]);
                    this.AttachDelegate($"{startName}.LastLapS2", () => _values.GetCar(i)?.NewData?.LastLap?.Splits?[1]);
                    this.AttachDelegate($"{startName}.LastLapS3", () => _values.GetCar(i)?.NewData?.LastLap?.Splits?[2]);
                }

                AddProp(ExposedProperties.BestLap, () => _values.GetCar(i)?.NewData?.BestSessionLap.Laptime);
                if (Settings.ExposedProperties.Includes(ExposedProperties.BestLapSectors)) {
                    this.AttachDelegate($"{startName}.BestLapS1", () => _values.GetCar(i)?.BestLapSectors?[0]);
                    this.AttachDelegate($"{startName}.BestLapS2", () => _values.GetCar(i)?.BestLapSectors?[1]);
                    this.AttachDelegate($"{startName}.BestLapS3", () => _values.GetCar(i)?.BestLapSectors?[2]);
                }
                if (Settings.ExposedProperties.Includes(ExposedProperties.BestSectors)) {
                    this.AttachDelegate($"{startName}.BestS1", () => _values.GetCar(i)?.NewData?.BestSessionLap?.Splits?[0]);
                    this.AttachDelegate($"{startName}.BestS2", () => _values.GetCar(i)?.NewData?.BestSessionLap?.Splits?[1]);
                    this.AttachDelegate($"{startName}.BestS3", () => _values.GetCar(i)?.NewData?.BestSessionLap?.Splits?[2]);
                }

                AddProp(ExposedProperties.CurrentLap, () => _values.GetCar(i)?.NewData?.CurrentLap?.Laptime);

                void AddDriverProp<T>(ExposedDriverProperties prop, string driverId, Func<T> valueProvider) {
                    if (Settings.ExposedDriverProperties.Includes(prop)) this.AttachDelegate($"{startName}.{driverId}{prop}", valueProvider);
                }

                if (Settings.ExposedProperties.Includes(ExposedProperties.CurrentDriver)) {
                    var driverId = "CurrentDriver";
                    AddDriverProp(ExposedDriverProperties.FirstName, driverId, () => _values.GetCar(i)?.CurrentDriver?.FirstName);
                    AddDriverProp(ExposedDriverProperties.LastName, driverId, () => _values.GetCar(i)?.CurrentDriver?.LastName);
                    AddDriverProp(ExposedDriverProperties.ShortName, driverId, () => _values.GetCar(i)?.CurrentDriver?.ShortName);
                    AddDriverProp(ExposedDriverProperties.FullName, driverId, () => _values.GetCar(i)?.CurrentDriver?.FullName());
                    AddDriverProp(ExposedDriverProperties.InitialPlusLastName, driverId, () => _values.GetCar(i)?.CurrentDriver?.InitialPlusLastName());
                    AddDriverProp(ExposedDriverProperties.Nationality, driverId, () => _values.GetCar(i)?.CurrentDriver?.Nationality);
                    AddDriverProp(ExposedDriverProperties.Category, driverId, () => _values.GetCar(i)?.CurrentDriver?.Category);
                    AddDriverProp(ExposedDriverProperties.TotalLaps, driverId, () => _values.GetCar(i)?.CurrentDriver?.TotalLaps);
                    AddDriverProp(ExposedDriverProperties.BestLap, driverId, () => _values.GetCar(i)?.CurrentDriver?.BestSessionLap?.Laptime);
                    AddDriverProp(ExposedDriverProperties.TotalDrivingTime, driverId, () => _values.GetCar(i)?.CurrentDriverTotalDrivingTime);
                }


                if (Settings.ExposedProperties.Includes(ExposedProperties.AllDrivers)) {
                    void AddOneDriverFromList(int j) {
                        if (Settings.NumDrivers > j) {
                            var driverId = $"Driver{j + 1:00}";
                            AddDriverProp(ExposedDriverProperties.FirstName, driverId, () => _values.GetCar(i)?.GetDriver(j)?.FirstName);
                            AddDriverProp(ExposedDriverProperties.LastName, driverId, () => _values.GetCar(i)?.GetDriver(j)?.LastName);
                            AddDriverProp(ExposedDriverProperties.ShortName, driverId, () => _values.GetCar(i)?.GetDriver(j)?.ShortName);
                            AddDriverProp(ExposedDriverProperties.FullName, driverId, () => _values.GetCar(i)?.GetDriver(j)?.FullName());
                            AddDriverProp(ExposedDriverProperties.InitialPlusLastName, driverId, () => _values.GetCar(i)?.GetDriver(j)?.InitialPlusLastName());
                            AddDriverProp(ExposedDriverProperties.Nationality, driverId, () => _values.GetCar(i)?.GetDriver(j)?.Nationality);
                            AddDriverProp(ExposedDriverProperties.Category, driverId, () => _values.GetCar(i)?.GetDriver(j)?.Category);
                            AddDriverProp(ExposedDriverProperties.TotalLaps, driverId, () => _values.GetCar(i)?.GetDriver(j)?.TotalLaps);
                            AddDriverProp(ExposedDriverProperties.BestLap, driverId, () => _values.GetCar(i)?.GetDriver(j)?.BestSessionLap?.Laptime);
                            AddDriverProp(ExposedDriverProperties.TotalDrivingTime, driverId, () => _values.GetCar(i)?.GetDriverTotalDrivingTime(j));
                        }
                    }

                    for (int j = 0; j < Settings.NumDrivers; j++) {
                        AddOneDriverFromList(j);
                    }
                }

                // Car and team
                AddProp(ExposedProperties.CarNumber, () => _values.GetCar(i)?.RaceNumber);
                AddProp(ExposedProperties.CarModel, () => _values.GetCar(i)?.CarModelType.ToPrettyString());
                AddProp(ExposedProperties.CarManufacturer, () => _values.GetCar(i)?.CarModelType.GetMark());
                AddProp(ExposedProperties.CarClass, () => _values.GetCar(i)?.CarClass.ToString());
                AddProp(ExposedProperties.TeamName, () => _values.GetCar(i)?.TeamName);
                AddProp(ExposedProperties.CurrentDeltaToBest, () => _values.GetCar(i)?.NewData?.Delta);
                AddProp(ExposedProperties.CupCategory, () => _values.GetCar(i)?.CupCategory.ToString());
                AddProp(ExposedProperties.CurrentStintTime, () => _values.GetCar(i)?.CurrentStintTime);
                AddProp(ExposedProperties.LastStintTime, () => _values.GetCar(i)?.LastStintTime);
                AddProp(ExposedProperties.CurrentStintLaps, () => _values.GetCar(i)?.CurrentStintLaps);
                AddProp(ExposedProperties.LastStintLaps, () => _values.GetCar(i)?.LastStintLaps);

                // Gaps and distances
                AddProp(ExposedProperties.DistanceToLeader, () => _values.GetCar(i)?.DistanceToLeader);
                AddProp(ExposedProperties.DistanceToClassLeader, () => _values.GetCar(i)?.DistanceToClassLeader);
                AddProp(ExposedProperties.DistanceToFocusedTotal, () => _values.GetCar(i)?.TotalDistanceToFocused);
                AddProp(ExposedProperties.DistanceToFocusedOnTrack, () => _values.GetCar(i)?.OnTrackDistanceToFocused);
                AddProp(ExposedProperties.GapToLeader, () => _values.GetCar(i)?.GapToLeader);
                AddProp(ExposedProperties.GapToClassLeader, () => _values.GetCar(i)?.GapToClassLeader);
                AddProp(ExposedProperties.GapToFocusedOnTrack, () => _values.GetCar(i)?.GapToFocusedOnTrack);
                AddProp(ExposedProperties.GapToFocusedTotal, () => _values.GetCar(i)?.GapToFocusedTotal);
                AddProp(ExposedProperties.GapToAhead, () => _values.GetCar(i)?.GapToAhead);
                AddProp(ExposedProperties.GapToAheadInClass, () => _values.GetCar(i)?.GapToAheadInClass);


                // Positions
                AddProp(ExposedProperties.ClassPosition, () => _values.GetCar(i)?.InClassPos);
                AddProp(ExposedProperties.OverallPosition, () => i + 1);
                AddProp(ExposedProperties.ClassPositionStart, () => _values.GetCar(i)?.StartPosInClass);
                AddProp(ExposedProperties.OverallPositionStart, () => _values.GetCar(i)?.StartPos);

                // Pit
                AddProp(ExposedProperties.IsInPitLane, () => _values.GetCar(i)?.NewData?.CarLocation == CarLocationEnum.Pitlane ? 1 : 0);
                AddProp(ExposedProperties.PitStopCount, () => _values.GetCar(i)?.PitCount);
                AddProp(ExposedProperties.PitTimeTotal, () => _values.GetCar(i)?.TotalPitTime);
                AddProp(ExposedProperties.PitTimeLast, () => _values.GetCar(i)?.LastPitTime);
                AddProp(ExposedProperties.PitTimeCurrent, () => _values.GetCar(i)?.CurrentTimeInPits);

                // Else
                AddProp(ExposedProperties.IsFinished, () => (_values.GetCar(i)?.IsFinished ?? false) ? 1 : 0);
            };

            for (int i = 0; i < Settings.NumOverallPos; i++) {
                addCar(i);
            }

            if (Settings.ExposedProperties.Includes(ExposedProperties.InClassPositions)) {
                // Add indexes into overall order
                void addInClassIdxs(int i) {
                    this.AttachDelegate($"InClass.{i + 1:00}.OverallPosition", () => _values.PosInClassCarsIdxs[i] + 1);
                }

                for (int i = 0; i < Settings.NumOverallPos; i++) {
                    addInClassIdxs(i);
                }
            }

            if (Settings.ExposedProperties.Includes(ExposedProperties.RelativePositions)) {
                void addRelativeIdxs(int i) {
                    this.AttachDelegate($"Relative.{i + 1:00}.OverallPosition", () => _values.RelativePosOnTrackCarsIdxs[i] + 1);
                }

                for (int i = 0; i < Settings.NumRelativePos * 2 + 1; i++) {
                    addRelativeIdxs(i);
                }
            }

            if (Settings.ExposedProperties.Includes(ExposedProperties.FocusedCarPosition)) this.AttachDelegate("Focused.OverallPosition", () => _values.FocusedCarIdx + 1);
            if (Settings.ExposedProperties.Includes(ExposedProperties.OverallBestLapPosition)) this.AttachDelegate("Overall.BestLapCarOverallPosition", () => _values.GetBestLapCarIdx(CarClass.Overall) + 1);
            if (Settings.ExposedProperties.Includes(ExposedProperties.InClassBestLapPosition)) this.AttachDelegate("InClass.BestLapCarOverallPosition", () => _values.GetFocusedClassBestLapCarIdx() + 1);

            // Add everything else 
            if (Settings.ExposedProperties.Includes(ExposedProperties.SessionPhase)) this.AttachDelegate("Session.Phase", () => _values.RealtimeData?.Phase);
            if (Settings.ExposedProperties.Includes(ExposedProperties.MaxStintTime)) this.AttachDelegate("Session.MaxStintTime", () => _values.MaxDriverStintTime);
            if (Settings.ExposedProperties.Includes(ExposedProperties.MaxDriveTime)) this.AttachDelegate("Session.MaxDriveTime", () => _values.MaxDriverTotalDriveTime);
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