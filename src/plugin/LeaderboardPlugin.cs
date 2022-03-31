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
        public void DataUpdate(PluginManager pluginManager, ref GameData data) {
            if (!Game.IsAcc) { return; } // Atm only ACC is supported

            //if (data.GameRunning && data.OldData != null && data.NewData != null) {
            //    _values.OnDataUpdate(pluginManager, data);

            //    var laps = (int)pluginManager.GetPropertyValue<SimHub.Plugins.DataPlugins.DataCore.DataCorePlugin>("GameRawData.Graphics.CompletedLaps");
            //    var pos = (float)pluginManager.GetPropertyValue<SimHub.Plugins.DataPlugins.DataCore.DataCorePlugin>("GameRawData.Graphics.NormalizedCarPosition");
            //    var track = (string)PluginManager.GetPropertyValue<SimHub.Plugins.DataPlugins.DataCore.DataCorePlugin>("GameData.TrackId");

            //    if (pos > 0.9 || pos < 0.1) {
            //        File.AppendAllText($"{Settings.PluginDataLocation}\\{track}_pos.txt", $"\n{pos};{laps};");
            //    }

            //    prevPos = pos;
            //    prevLaps = laps;
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

            AttachDebugDelegates();
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
                var startName = $"DBG.Overall.{i + 1:00}";
                this.AttachDelegate($"{startName}.Info", () => {
                    var car = _values.GetCar(i);
                    if (car == null) return null;
                    return $"#{car.Info.RaceNumber,-4}, isFinished:{car.IsFinished}, FinishTime:{car.FinishTime?.TotalSeconds}, L{car.RealtimeCarUpdate?.Laps}, startPos:{car.StartPos:00}/{car.StartPosInClass:00}, TSP:{car.TotalSplinePosition}";
                });
                //this.AttachDelegate($"DBG.{startName}.SplinePosition", () => _values.GetCar(i)?.RealtimeCarUpdate?.SplinePosition);
                //this.AttachDelegate($"DBG.{startName}.Laps", () => _values.GetCar(i)?.RealtimeCarUpdate?.Laps);
                //this.AttachDelegate($"DBG.{startName}.LapsBySplinePosition", () => _values.GetCar(i)?.LapsBySplinePosition);
                //this.AttachDelegate($"DBG.{startName}.TotalSplinePosition", () => _values.GetCar(i)?.TotalSplinePosition);
            };

            for (int i = 0; i < Settings.NumOverallPos; i++) {
                addCar(i);
            }


        }

        private void AttachDelegates() {
            // Idea with properties is to add cars 

            void addCar(int i) {
                var startName = $"Overall.{i + 1:00}";
                this.AttachDelegate($"{startName}.Numlaps", () => _values.GetCar(i)?.RealtimeCarUpdate?.Laps);
                this.AttachDelegate($"{startName}.LastLap", () => _values.GetCar(i)?.RealtimeCarUpdate?.LastLap.LaptimeMS / 1000.0);
                this.AttachDelegate($"{startName}.BestLap", () => _values.GetCar(i)?.RealtimeCarUpdate?.BestSessionLap.LaptimeMS / 1000.0);
                //this.AttachDelegate($"{startName}.CurrentDriverFirstName", () => _values.GetCar(i)?.GetCurrentDriver().FirstName);
                //this.AttachDelegate($"{startName}.CurrentDriverLastName", () => _values.GetCar(i)?.GetCurrentDriver().LastName);
                //this.AttachDelegate($"{startName}.CurrentDriverShortName", () => _values.GetCar(i)?.GetCurrentDriver().ShortName);
                //this.AttachDelegate($"{startName}.CurrentDrivetFullName", () => _values.GetCar(i)?.GetCurrentDriver().FullName());
                this.AttachDelegate($"{startName}.CurrentDriverInitialPlusLastName", () => _values.GetCar(i)?.GetCurrentDriver().InitialPlusLastName());
                //this.AttachDelegate($"{startName}.CurrentDrivetInitials", () => _values.GetCar(i)?.GetCurrentDriver().Initials());
                this.AttachDelegate($"{startName}.CarNumber", () => _values.GetCar(i)?.Info.RaceNumber);
                this.AttachDelegate($"{startName}.CarModel", () => _values.GetCar(i)?.Info.CarModelType.ToPrettyString());
                //this.AttachDelegate($"{startName}.CarMark", () => _values.GetCar(i)?.Info.CarModelType.GetMark());
                this.AttachDelegate($"{startName}.CarClass", () => _values.GetCar(i)?.Info.CarClass.ToString());
                //this.AttachDelegate($"{startName}.TeamName", () => _values.GetCar(i)?.Info.TeamName);
                //this.AttachDelegate($"{startName}.CurrentDeltaToBest", () => _values.GetCar(i)?.RealtimeCarUpdate?.Delta);
                this.AttachDelegate($"{startName}.CupCategory", () => _values.GetCar(i)?.Info.CupCategory.ToString());
                this.AttachDelegate($"{startName}.DistToLeader", () => _values.GetCar(i)?.DistanceToLeader);
                this.AttachDelegate($"{startName}.DistToClassLeader", () => _values.GetCar(i)?.DistanceToClassLeader);
                this.AttachDelegate($"{startName}.DistToFocusedTotal", () => _values.GetCar(i)?.TotalDistanceToFocused);
                this.AttachDelegate($"{startName}.DistToFocusedOnTrack", () => _values.GetCar(i)?.OnTrackDistanceToFocused);
                this.AttachDelegate($"{startName}.IsInPitlane", () => _values.GetCar(i)?.RealtimeCarUpdate?.CarLocation == CarLocationEnum.Pitlane ? 1 : 0);
                this.AttachDelegate($"{startName}.GapToLeader", () => _values.GetCar(i)?.GapToLeader);
                this.AttachDelegate($"{startName}.GapToClassLeader", () => _values.GetCar(i)?.GapToClassLeader);
                this.AttachDelegate($"{startName}.GapToFocusedOnTrack", () => _values.GetCar(i)?.GapToFocusedOnTrack);
                this.AttachDelegate($"{startName}.GapToFocusedTotal", () => _values.GetCar(i)?.GapToFocusedTotal);
                this.AttachDelegate($"{startName}.ClassPosition", () => _values.GetCar(i)?.InClassPos);
                this.AttachDelegate($"{startName}.OverallPosition", () => i + 1);
                this.AttachDelegate($"{startName}.ClassPositionStart", () => _values.GetCar(i)?.StartPosInClass);
                this.AttachDelegate($"{startName}.OverallPositionStart", () => _values.GetCar(i)?.StartPos);
                this.AttachDelegate($"{startName}.IsFinished", () => _values.GetCar(i)?.IsFinished ?? false ? 1:0);
            };

            void addOverall(int i) {
                this.AttachDelegate($"InClass.{i + 1:00}.OverallPosition", () => _values.PosInClassCarsIdxs[i] + 1);
            }

            for (int i = 0; i < Settings.NumOverallPos; i++) {
                addCar(i);
                addOverall(i);
            }

            void addRelative(int i) {
                this.AttachDelegate($"Relative.{i + 1:00}.OverallPosition", () => _values.RelativePosOnTrackCarsIdxs[i] + 1);
            }

            for (int i = 0; i < Settings.NumRelativePos * 2 + 1; i++) {
                addRelative(i);
            }

            this.AttachDelegate("Focused.OverallPosition", () => _values.FocusedCarIdx + 1);
            this.AttachDelegate("Session.Phase", () => _values.RealtimeUpdate?.Phase);
            //this.AttachDelegate("Overall.BestLap", () => _values.RealtimeUpdate?.BestSessionLap?.LaptimeMS / 1000.0 ?? null);
            this.AttachDelegate("Overall.BestLapCarOverallPosition", () => _values.GetBestLapCarIdx(CarClass.Overall) + 1);

            //this.AttachDelegate("InClass.BestLap", () => _values.GetFocusedClassBestLapCar().RealtimeCarUpdate?.BestSessionLap?.LaptimeMS / 1000.0 ?? null);
            this.AttachDelegate("InClass.BestLapCarOverallPosition", () => _values.GetFocusedClassBestLapCarIdx() + 1);

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