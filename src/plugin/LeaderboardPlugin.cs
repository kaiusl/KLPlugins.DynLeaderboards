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
        public const string PluginName = "Leaderboard";

        public PluginSettings ShSettings;
        public PluginManager PluginManager { get; set; }
        public ImageSource PictureIcon => this.ToIcon(Properties.Resources.sdkmenuicon);
        public string LeftMenuTitle => PluginName;

        public static Settings Settings = new Settings();
        public static Game Game; // Const during the lifetime of this plugin, plugin is rebuilt at game change
        public static string GameDataPath; // Same as above
        public static string PluginStartTime = $"{DateTime.Now.ToString("dd-MM-yyyy_HH-mm-ss")}";
        private const string SETTINGS_PATH = @"PluginsData\KLPluginsLeaderboard\Settings.json";

        private static FileStream _logFile;
        private static StreamWriter _logWriter;
        private static bool _isLogFlushed = false;

        private Values _values;
        public static PluginManager pluginManager;

        /// <summary>
        /// Called one time per game data update, contains all normalized game data, 
        /// raw data are intentionnally "hidden" under a generic object type (A plugin SHOULD NOT USE IT)
        /// 
        /// This method is on the critical path, it must execute as fast as possible and avoid throwing any error
        /// 
        /// </summary>
        /// <param name="pluginManager"></param>
        /// <param name="data"></param>
        public void DataUpdate(PluginManager pluginManager, ref GameData data) {
            if (!Game.IsAcc) { return; } // ATM only support ACC, some parts could probably work with other games but not tested yet, so let's be safe for now

            if (data.GameRunning && data.OldData != null && data.NewData != null) {
                //var swatch = Stopwatch.StartNew();

                _values.OnDataUpdate(pluginManager, data);

                //swatch.Stop();
                //TimeSpan ts = swatch.Elapsed;
                //File.AppendAllText($"{SETTINGS.DataLocation}\\Logs\\timings\\RETiming_DataUpdate_{pluginStartTime}.txt", $"{ts.TotalMilliseconds}, {BoolToInt(values.booleans.NewData.IsInMenu)}, {BoolToInt(values.booleans.NewData.IsOnTrack)}, {BoolToInt(values.booleans.NewData.IsInPitLane)}, {BoolToInt(values.booleans.NewData.IsInPitBox)}, {BoolToInt(values.booleans.NewData.HasFinishedLap)}\n");
            }
        }

        private int BoolToInt(bool b) {
            return b ? 1 : 0;
        }

        /// <summary>
        /// Called at plugin manager stop, close/dispose anything needed here !
        /// Plugins are rebuilt at game change
        /// </summary>
        /// <param name="pluginManager"></param>
        public void End(PluginManager pluginManager) {
            this.SaveCommonSettings("GeneralSettings", ShSettings);
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
            LeaderboardPlugin.pluginManager = pluginManager;

            var gameName = (string)pluginManager.GetPropertyValue<SimHub.Plugins.DataPlugins.DataCore.DataCorePlugin>("CurrentGame");
            if (gameName != Game.AccName) return;

            try { 
               Settings = JsonConvert.DeserializeObject<Settings>(File.ReadAllText(SETTINGS_PATH).Replace("\"", "'"));
            } catch (Exception e) {
                Settings = new Settings();
                string txt = JsonConvert.SerializeObject(Settings, Formatting.Indented);
                Directory.CreateDirectory(Path.GetDirectoryName(SETTINGS_PATH));
                File.WriteAllText(SETTINGS_PATH, txt);
            }

            if (Settings.Log) {
                var fpath = $"{Settings.DataLocation}\\Logs\\RELog_{PluginStartTime}.txt";
                Directory.CreateDirectory(Path.GetDirectoryName(fpath));
                _logFile = File.Create(fpath);
                _logWriter = new StreamWriter(_logFile);
            }
            PreJit();

            LogInfo("Starting plugin");
            ShSettings = this.ReadCommonSettings<PluginSettings>("GeneralSettings", () => new PluginSettings());

            // DataCorePlugin should be built before, thus this property should be available.

            Game = new Game(gameName);
            GameDataPath = $@"{Settings.DataLocation}\{gameName}";
            _values = new Values();

            pluginManager.GameStateChanged += _values.OnGameStateChanged;
            pluginManager.GameStateChanged += (bool running, PluginManager _) => {
                LogInfo($"GameStateChanged to {running}");
                if (!running) {
                    if (_logWriter != null && !_isLogFlushed) {
                        _logWriter.Flush();
                        _isLogFlushed = true;
                    }
                }
            };

            #region ADD DELEGATES

            this.AttachDelegate("LenCars", () => _values.Cars.Count);
            this.AttachDelegate("FocusedCar", () => _values.GetFocusedCar()?.ToString());


            // DBG
            Action<int> overallDBG = (i) => this.AttachDelegate($"DBG.Overall.{i + 1:00}.Numlaps", () => _values.DbgGetOverallPos(i)?.ToString());
            Action<int> InClassDBG = (i) => this.AttachDelegate($"DBG.InClass.{i + 1:00}.Numlaps", () => _values.DbgGetInClassPos(i)?.ToString());
            for (int i = 0; i < Settings.NumOverallPos; i++) {
                overallDBG(i);
                InClassDBG(i);
            }

            Action<int> overallOnTrackDBG = (i) => this.AttachDelegate($"DBG.Relative.{i + 1:00}.Numlaps", () => _values.DbgGetOverallPosOnTrack(i)?.ToString());
            for (int i = 0; i < Settings.NumRelativePos*2 + 1; i++) {
                overallOnTrackDBG(i);
            }
            // DBG


            void addCar(int i) {
                var startName = $"CarData.{i + 1:00}";
                this.AttachDelegate($"DBG_{startName}.Info", () => _values.GetCar(i)?.ToString());
                this.AttachDelegate($"{startName}.Numlaps", () => _values.GetCar(i)?.RealtimeUpdate?.Laps);
                this.AttachDelegate($"{startName}.LastLap", () => _values.GetCar(i)?.RealtimeUpdate?.LastLap.LaptimeMS / 1000.0);
                this.AttachDelegate($"{startName}.BestLap", () => _values.GetCar(i)?.RealtimeUpdate?.BestSessionLap.LaptimeMS / 1000.0);
                this.AttachDelegate($"{startName}.CurrentDriverFirstName", () => _values.GetCar(i)?.GetCurrentDriver().FirstName);
                this.AttachDelegate($"{startName}.CurrentDriverLastName", () => _values.GetCar(i)?.GetCurrentDriver().LastName);
                this.AttachDelegate($"{startName}.CurrentDriverShortName", () => _values.GetCar(i)?.GetCurrentDriver().ShortName);
                this.AttachDelegate($"{startName}.CarNumber", () => _values.GetCar(i)?.Info.RaceNumber);
                this.AttachDelegate($"{startName}.CarModel", () => _values.GetCar(i)?.Info.CarModelType.ToPrettyString());
                this.AttachDelegate($"{startName}.CarMark", () => _values.GetCar(i)?.Info.CarModelType.GetMark());
                this.AttachDelegate($"{startName}.Class", () => _values.GetCar(i)?.Info.CarClass);
                this.AttachDelegate($"{startName}.TeamName", () => _values.GetCar(i)?.Info.TeamName);
                this.AttachDelegate($"{startName}.DeltaToBest", () => _values.GetCar(i)?.RealtimeUpdate?.Delta);
                this.AttachDelegate($"{startName}.CupCategory", () => _values.GetCar(i)?.Info.CupCategory);
                this.AttachDelegate($"{startName}.DistToLeader", () => _values.GetCar(i)?.DistanceToLeader);
                this.AttachDelegate($"{startName}.DistToClassLeader", () => _values.GetCar(i)?.DistanceToClassLeader);
                this.AttachDelegate($"{startName}.DistToFocused", () => _values.GetCar(i)?.DistanceToFocused);
                this.AttachDelegate($"{startName}.IsInPitlane", () => _values.GetCar(i)?.RealtimeUpdate?.CarLocation == CarLocationEnum.Pitlane ? 1 : 0);
                this.AttachDelegate($"{startName}.GapToLeader", () => _values.GetCar(i)?.DistanceToLeader ?? 0.0 /55.0);
                this.AttachDelegate($"{startName}.GapToClassLeader", () => _values.GetCar(i)?.DistanceToClassLeader ?? 0.0 /55.0);
                this.AttachDelegate($"{startName}.GapToFocused", () => _values.GetCar(i)?.DistanceToFocused ?? 0.0 / 55.0);
            };

            void addOverall(int i) {
                this.AttachDelegate($"Overall.{i + 1:00}.Idx", () => _values.OverallPosCarsIdxs[i] + 1);
                this.AttachDelegate($"InClass.{i + 1:00}.Idx", () => _values.PosInClassCarsIdxs[i] + 1);
            }
                     
            for (int i = 0; i < Settings.NumOverallPos; i++) {
                addCar(i);
                addOverall(i);
            }

            void addRelative(int i) {
                this.AttachDelegate($"Relative.{i + 1:00}.Idx", () => _values.OverallPosOnTrackCarsIdxs[i] + 1);
            }

            for (int i = 0; i < Settings.NumRelativePos * 2 + 1; i++) {
                addRelative(i);
            }

            #endregion

        }




        public static void LogToFile(string msq) {
            if (_logWriter != null) { 
                _logWriter.WriteLine(msq);
                _isLogFlushed = false;
            }
        }

        public static void LogInfo(string msq, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int lineNumber = 0) {
            if (Settings.Log) {
                var pathParts = sourceFilePath.Split('\\');
                SimHub.Logging.Current.Info($"{PluginName} ({pathParts[pathParts.Length - 1]}: {memberName},{lineNumber})\n\t{msq}");
                LogToFile($"{DateTime.Now.ToString("dd.MM.yyyy HH:mm.ss")} INFO ({pathParts[pathParts.Length - 1]}: {memberName},{lineNumber})\n\t{msq}\n");
            }
        }

        public static void LogWarn(string msq, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int lineNumber = 0) {
            var pathParts = sourceFilePath.Split('\\');
            SimHub.Logging.Current.Warn($"{PluginName} ({pathParts[pathParts.Length - 1]}: {memberName},{lineNumber})\n\t{msq}");
            LogToFile($"{DateTime.Now.ToString("dd.MM.yyyy HH:mm.ss")} WARN ({pathParts[pathParts.Length - 1]}: {memberName},{lineNumber})\n\t{msq}\n");
        }

        public static void LogError(string msq, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int lineNumber = 0) {
            var pathParts = sourceFilePath.Split('\\');
            SimHub.Logging.Current.Error($"{PluginName} ({pathParts[pathParts.Length - 1]}: {memberName},{lineNumber})\n\t{msq}");
            LogToFile($"{DateTime.Now.ToString("dd.MM.yyyy HH:mm.ss")} ERROR ({pathParts[pathParts.Length - 1]}: {memberName},{lineNumber})\n\t{msq}\n");
        }

        public static void LogFileSeparator() {
            if (Settings.Log) {
                LogToFile("\n----------------------------------------------------------\n");
            }
        }

        static void PreJit() {
            Stopwatch sw = new Stopwatch();
            sw.Start();

            var types = Assembly.GetExecutingAssembly().GetTypes();//new Type[] { typeof(Database.Database) };
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