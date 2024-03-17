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
                }
            }

            this.SubscribeToSimHubEvents(pm);
        }

        private void AttachDynLeaderboard(DynLeaderboard l) {
            for (int i = 0; i < 20; i++) {
                this.AttachCarDelegates(l, i);
            }

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

        private void AttachCarDelegates(DynLeaderboard l, int i) {
            var prefix = $"{l.Config.Name}.{i + 1}.";
            this.AttachDelegate(l.Config.Name + "Leaderboard", () => l.Config.CurrentLeaderboardName);
            this.AttachDelegate(prefix + "Driver.0.Name", () => l.GetDynCar(i)?.DriverName);
            this.AttachDelegate(prefix + "Position.Overall", () => l.GetDynCar(i)?.PositionOverall);
            this.AttachDelegate(prefix + "Position.Class", () => l.GetDynCar(i)?.PositionInClass);
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
}